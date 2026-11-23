"""Tiny pure-Python synthesis toolkit for Clocking Out's procedural audio.

Standard library only (no numpy). Signals are plain lists of floats at SR Hz.
Everything is deterministic given the caller's seeded random.Random.
"""
import math
import random
import struct
import wave
import zlib
from array import array

SR = 44100
TAU = 2.0 * math.pi


# ---------------------------------------------------------------- basics
def n_of(sec):
    return max(1, int(round(sec * SR)))


def silence(sec):
    return [0.0] * n_of(sec)


def rng_for(name):
    return random.Random(zlib.crc32(name.encode("utf-8")))


def db(x):
    return 20.0 * math.log10(max(x, 1e-12))


def undb(d):
    return 10.0 ** (d / 20.0)


def midi_hz(m):
    return 440.0 * 2.0 ** ((m - 69) / 12.0)


def mix(dst, src, at_sec=0.0, gain=1.0):
    """Add src into dst starting at at_sec, growing dst if needed."""
    off = int(round(at_sec * SR))
    end = off + len(src)
    if end > len(dst):
        dst.extend([0.0] * (end - len(dst)))
    for i, v in enumerate(src):
        dst[off + i] += v * gain
    return dst


def mul(a, b):
    """Elementwise product; b may be a list (shorter list wins) or a number."""
    if isinstance(b, (int, float)):
        return [v * b for v in a]
    n = min(len(a), len(b))
    return [a[i] * b[i] for i in range(n)]


def as_fn(v):
    if callable(v):
        return v
    if isinstance(v, list):
        last = len(v) - 1
        return lambda t: v[min(int(t * SR), last)]
    return lambda t: v


# ---------------------------------------------------------------- envelopes
def env_lin(sec, points):
    """Piecewise-linear envelope. points: [(t_sec, value), ...] sorted."""
    n = n_of(sec)
    out = [0.0] * n
    pts = list(points)
    j = 0
    for i in range(n):
        t = i / SR
        while j < len(pts) - 2 and t > pts[j + 1][0]:
            j += 1
        t0, v0 = pts[j]
        t1, v1 = pts[min(j + 1, len(pts) - 1)]
        if t <= t0:
            out[i] = v0
        elif t >= t1:
            out[i] = v1
        else:
            out[i] = v0 + (v1 - v0) * (t - t0) / (t1 - t0)
    return out


def env_exp(sec, tau, attack=0.002):
    n = n_of(sec)
    na = max(1, int(attack * SR))
    k = math.exp(-1.0 / (tau * SR))
    out = [0.0] * n
    g = 1.0
    for i in range(n):
        a = i / na if i < na else 1.0
        out[i] = a * g
        g *= k
    return out


def smooth_rand(sec, rate, rng, lo=0.0, hi=1.0):
    """Random control signal: new random target every 1/rate s, cosine-interpolated."""
    n = n_of(sec)
    step = max(1, int(SR / rate))
    pts = [rng.uniform(lo, hi) for _ in range(n // step + 3)]
    out = [0.0] * n
    for i in range(n):
        k, f = divmod(i, step)
        u = (1.0 - math.cos(math.pi * f / step)) * 0.5
        out[i] = pts[k] + (pts[k + 1] - pts[k]) * u
    return out


# ---------------------------------------------------------------- sources
def noise(sec, rng):
    r = rng.random
    return [r() * 2.0 - 1.0 for _ in range(n_of(sec))]


def osc(sec, freq, shape="sine", phase=0.0):
    """Oscillator. freq: Hz number, list, or fn(t)->Hz."""
    n = n_of(sec)
    out = [0.0] * n
    inv = 1.0 / SR
    const = isinstance(freq, (int, float))
    f = None if const else as_fn(freq)
    ph = phase
    sin = math.sin
    for i in range(n):
        fr = freq if const else f(i * inv)
        if shape == "sine":
            out[i] = sin(TAU * ph)
        elif shape == "saw":
            out[i] = 2.0 * ph - 1.0
        elif shape == "square":
            out[i] = 1.0 if ph < 0.5 else -1.0
        else:  # triangle
            out[i] = 4.0 * abs(ph - 0.5) - 1.0
        ph += fr * inv
        if ph >= 1.0:
            ph -= int(ph)
    return out


def glide(f0, f1, T, curve="exp"):
    """fn(t) frequency glide from f0 to f1 over T seconds, then holds."""
    if curve == "exp":
        return lambda t: f0 * (f1 / f0) ** min(t / T, 1.0)
    return lambda t: f0 + (f1 - f0) * min(t / T, 1.0)


def dsine(n, freq, tau, amp=1.0, out=None, off=0):
    """Exponentially decaying sine via a 2-pole recurrence (cheap)."""
    if out is None:
        out = [0.0] * n
        off = 0
    w = TAU * freq / SR
    r = math.exp(-1.0 / (tau * SR))
    c1 = 2.0 * r * math.cos(w)
    c2 = r * r
    y2 = 0.0
    y1 = amp * r * math.sin(w)
    lim = min(n, len(out) - off)
    if lim <= 0:
        return out
    out[off] += 0.0
    for i in range(1, lim):
        out[off + i] += y1
        y = c1 * y1 - c2 * y2
        y2 = y1
        y1 = y
    return out


def hit(sec, modes, rng=None, noise_amt=0.0, noise_tau=0.003, noise_hp=1500.0):
    """Modal impact: modes = [(freq, amp, tau_sec), ...] + optional noise burst."""
    n = n_of(sec)
    out = [0.0] * n
    for f, a, tau in modes:
        if f < SR * 0.45:
            dsine(n, f, tau, a, out)
    if noise_amt and rng is not None:
        nb = highpass(mul(noise(sec, rng), env_exp(sec, noise_tau, 0.0003)), noise_hp)
        for i in range(n):
            out[i] += nb[i] * noise_amt
    # 0.4 ms attack ramp to tame the step
    na = int(0.0004 * SR)
    for i in range(min(na, n)):
        out[i] *= i / na
    return out


# ---------------------------------------------------------------- filters
def lowpass1(x, fc):
    a = math.exp(-TAU * fc / SR)
    y = [0.0] * len(x)
    s = 0.0
    for i, v in enumerate(x):
        s = v + a * (s - v)
        y[i] = s
    return y


def highpass1(x, fc):
    lp = lowpass1(x, fc)
    return [x[i] - lp[i] for i in range(len(x))]


def _coefs(kind, fr, q):
    fr = min(max(fr, 10.0), SR * 0.45)
    w = TAU * fr / SR
    c = math.cos(w)
    a = math.sin(w) / (2.0 * q)
    if kind == "lp":
        b0 = (1 - c) / 2; b1 = 1 - c; b2 = b0
    elif kind == "hp":
        b0 = (1 + c) / 2; b1 = -(1 + c); b2 = b0
    else:  # band-pass, 0 dB peak
        b0 = a; b1 = 0.0; b2 = -a
    a0 = 1 + a
    return b0 / a0, b1 / a0, b2 / a0, (-2 * c) / a0, (1 - a) / a0


def biquad(x, kind, f, q=0.707, step=16):
    """RBJ biquad. kind: 'lp' | 'hp' | 'bp'. f: Hz, list or fn(t) (updated every `step`)."""
    n = len(x)
    y = [0.0] * n
    varying = not isinstance(f, (int, float))
    getf = as_fn(f) if varying else None
    b0, b1, b2, a1, a2 = _coefs(kind, f if not varying else getf(0.0), q)
    x1 = x2 = y1 = y2 = 0.0
    for i in range(n):
        if varying and i % step == 0:
            b0, b1, b2, a1, a2 = _coefs(kind, getf(i / SR), q)
        xi = x[i]
        yi = b0 * xi + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2 = x1; x1 = xi; y2 = y1; y1 = yi
        y[i] = yi
    return y


def lowpass(x, f, q=0.707):
    return biquad(x, "lp", f, q)


def highpass(x, f, q=0.707):
    return biquad(x, "hp", f, q)


def bandpass(x, f, q=1.0):
    return biquad(x, "bp", f, q)


def softclip(x, drive=1.0):
    t = math.tanh
    k = t(drive)
    return [t(v * drive) / k for v in x]


# ---------------------------------------------------------------- instruments
def mbox_note(freq, tau=1.0, amp=1.0, max_sec=6.0):
    """Music-box tine: near-harmonic partials with fast-decaying upper modes."""
    parts = ((1.0, 1.0, 1.0), (2.0, 0.16, 0.45), (5.43, 0.09, 0.16),
             (8.93, 0.035, 0.08), (3.01, 0.04, 0.3))
    sec = min(max_sec, tau * 7.0)
    n = n_of(sec)
    out = [0.0] * n
    for r, a, ts in parts:
        f = freq * r
        if f > 14000:
            continue
        dsine(n, f, max(0.01, tau * ts), a * amp, out)
    # tiny metallic pluck transient
    dsine(n, min(freq * 12.1, 16000), 0.002, 0.05 * amp, out)
    return out


def ks_pluck(freq, sec, rng, t60=0.6, bright=0.5, amp=1.0):
    """Karplus-Strong with fractional (all-pass) tuning: pizzicato / plucked string."""
    n = n_of(sec)
    P = SR / freq - 0.5
    N = max(2, int(P))
    d = P - N
    c = (1.0 - d) / (1.0 + d)
    g = 10.0 ** (-3.0 / (t60 * freq))
    buf = [rng.uniform(-1, 1) for _ in range(N)]
    # soften the excitation (pizz is plucked by a fingertip, not a pick)
    s = 0.0
    for k in range(3):
        for i in range(N):
            s = s + bright * (buf[i] - s)
            buf[i] = s
    m = sum(buf) / N
    buf = [v - m for v in buf]
    pk = max(abs(v) for v in buf) or 1.0
    buf = [v / pk for v in buf]
    out = [0.0] * n
    idx = 0
    last = 0.0
    ax = ay = 0.0
    for i in range(n):
        v = buf[idx]
        out[i] = v * amp
        new = g * 0.5 * (v + last)
        last = v
        ap = c * new + ax - c * ay
        ax = new
        ay = ap
        buf[idx] = ap
        idx += 1
        if idx == N:
            idx = 0
    return out


_WT_CACHE = {}


def wavetable(harmonics, size=2048):
    key = (tuple(harmonics), size)
    if key not in _WT_CACHE:
        t = [0.0] * (size + 1)
        for i in range(size):
            ph = TAU * i / size
            t[i] = sum(a * math.sin(h * ph) for h, a in harmonics)
        pk = max(abs(v) for v in t) or 1.0
        t = [v / pk for v in t]
        t[size] = t[0]
        _WT_CACHE[key] = t
    return _WT_CACHE[key]


CLARINET = ((1, 1.0), (2, 0.04), (3, 0.5), (4, 0.03), (5, 0.26), (7, 0.13), (9, 0.06), (11, 0.03))


def clar_note(freq, dur, amp=1.0, attack=0.05, release=0.14, vib=0.004):
    """Clarinet-ish: odd-harmonic wavetable, soft attack, delayed vibrato."""
    table = wavetable(CLARINET)
    size = len(table) - 1
    sec = dur + release
    n = n_of(sec)
    out = [0.0] * n
    na = max(1, int(attack * SR))
    nd = int(dur * SR)
    nr = max(1, int(release * SR))
    ph = 0.0
    inc0 = freq * size / SR
    sin = math.sin
    for i in range(n):
        t = i / SR
        v_amt = vib * min(1.0, max(0.0, (t - 0.18) / 0.25))
        inc = inc0 * (1.0 + v_amt * sin(TAU * 5.2 * t))
        k = int(ph)
        fr = ph - k
        s = table[k] + (table[k + 1] - table[k]) * fr
        if i < na:
            e = (1.0 - math.cos(math.pi * i / na)) * 0.5
        elif i < nd:
            e = 1.0 - 0.15 * (i - na) / max(1, nd - na)
        else:
            e = 0.85 * (1.0 - (i - nd) / nr)
            e = max(0.0, e) ** 1.5
        out[i] = s * e * amp
        ph += inc
        if ph >= size:
            ph -= size
    return out


# ---------------------------------------------------------------- reverb
def reverb_loop(x, room=0.84, damp=0.3, spread=0, passes=2):
    """Freeverb-style mono reverb (6 combs + 2 all-pass), run over x `passes` times
    so the tail of the end wraps into the start: returns the last pass (wet only).
    """
    comb_len = [1116, 1188, 1277, 1356, 1422, 1491]
    ap_len = [556, 441]
    cb = [[0.0] * (L + spread) for L in comb_len]
    ab = [[0.0] * (L + spread) for L in ap_len]
    n = len(x)
    y = [0.0] * n
    ci = [0] * 6
    cs = [0.0] * 6
    ai = [0, 0]
    d1 = damp
    d2 = 1.0 - damp
    fb = room
    b0, b1, b2, b3, b4, b5 = cb
    l0, l1, l2, l3, l4, l5 = [len(b) for b in cb]
    a0b, a1b = ab
    la0, la1 = len(a0b), len(a1b)
    for p in range(passes):
        i0 = i1 = i2 = i3 = i4 = i5 = 0
        s0 = s1 = s2 = s3 = s4 = s5 = 0.0
        j0 = j1 = 0
        if p > 0:
            i0, i1, i2, i3, i4, i5 = ci
            s0, s1, s2, s3, s4, s5 = cs
            j0, j1 = ai
        for k in range(n):
            inp = x[k] * 0.02
            o = b0[i0]; s0 = o * d2 + s0 * d1; b0[i0] = inp + s0 * fb; acc = o
            i0 += 1
            if i0 == l0: i0 = 0
            o = b1[i1]; s1 = o * d2 + s1 * d1; b1[i1] = inp + s1 * fb; acc += o
            i1 += 1
            if i1 == l1: i1 = 0
            o = b2[i2]; s2 = o * d2 + s2 * d1; b2[i2] = inp + s2 * fb; acc += o
            i2 += 1
            if i2 == l2: i2 = 0
            o = b3[i3]; s3 = o * d2 + s3 * d1; b3[i3] = inp + s3 * fb; acc += o
            i3 += 1
            if i3 == l3: i3 = 0
            o = b4[i4]; s4 = o * d2 + s4 * d1; b4[i4] = inp + s4 * fb; acc += o
            i4 += 1
            if i4 == l4: i4 = 0
            o = b5[i5]; s5 = o * d2 + s5 * d1; b5[i5] = inp + s5 * fb; acc += o
            i5 += 1
            if i5 == l5: i5 = 0
            bo = a0b[j0]; out = -acc + bo; a0b[j0] = acc + bo * 0.5; acc = out
            j0 += 1
            if j0 == la0: j0 = 0
            bo = a1b[j1]; out = -acc + bo; a1b[j1] = acc + bo * 0.5
            j1 += 1
            if j1 == la1: j1 = 0
            y[k] = out
        ci = [i0, i1, i2, i3, i4, i5]
        cs = [s0, s1, s2, s3, s4, s5]
        ai = [j0, j1]
    return y


# ---------------------------------------------------------------- output
def peak_of(x):
    return max((abs(v) for v in x), default=0.0)


def rms_of(x):
    if not x:
        return 0.0
    return math.sqrt(sum(v * v for v in x) / len(x))


def finalize_sfx(x, peak_db=-3.0, rms_cap_db=-14.0, fade_in=0.002, fade_out=0.012,
                 min_sec=0.1):
    """DC-block, trim the inaudible tail, fade edges, normalise peak (with an RMS cap)."""
    x = highpass1(x, 25.0)
    pk = peak_of(x) or 1.0
    thr = pk * undb(-60)
    end = len(x)
    while end > 1 and abs(x[end - 1]) < thr:
        end -= 1
    end = max(end, n_of(min_sec))
    x = (x + [0.0] * max(0, end - len(x)))[:end]
    ni = max(1, int(fade_in * SR))
    no = max(1, int(fade_out * SR))
    for i in range(min(ni, len(x))):
        x[i] *= i / ni
    for i in range(min(no, len(x))):
        x[len(x) - 1 - i] *= i / no
    g = undb(peak_db) / (peak_of(x) or 1.0)
    r = rms_of(x) * g
    if rms_cap_db is not None and r > undb(rms_cap_db):
        g *= undb(rms_cap_db) / r
    return [v * g for v in x]


def _to_int16(x):
    a = array("h")
    for v in x:
        s = int(round(v * 32767.0))
        if s > 32767:
            s = 32767
        elif s < -32768:
            s = -32768
        a.append(s)
    return a


def write_wav(path, left, right=None):
    """Write 16-bit PCM WAV, mono (right=None) or stereo."""
    if right is None:
        data = _to_int16(left)
        ch = 1
    else:
        L = _to_int16(left)
        R = _to_int16(right)
        data = array("h", [0]) * (2 * len(L))
        data[0::2] = L
        data[1::2] = R
        ch = 2
    if struct.pack("=h", 1) != struct.pack("<h", 1):
        data.byteswap()
    with wave.open(path, "wb") as w:
        w.setnchannels(ch)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())
