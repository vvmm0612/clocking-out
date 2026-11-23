"""Procedurally synthesise every sound effect for Clocking Out.

Usage:  python tools/audio/gen_sfx.py [name ...]
Writes staging_audio/Sfx/<name>.wav (16-bit PCM, 44.1 kHz, mono).
Deterministic: each sound has its own RNG seeded from its name.
Names must match docs/audio-list.md exactly.
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from synthlib import (SR, bandpass, clar_note, dsine, env_exp, env_lin, finalize_sfx,  # noqa: E402
                      glide, highpass, hit, lowpass, mbox_note, midi_hz, mix, mul, n_of,
                      noise, osc, rng_for, silence, smooth_rand, softclip, write_wav)

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "staging_audio", "Sfx")

SFX = {}


def sfx(name, peak_db=-3.0, rms_cap=-15.0):
    def deco(fn):
        SFX[name] = (fn, peak_db, rms_cap)
        return fn
    return deco


# ------------------------------------------------------------------ shared bits
def wood_tick(rng, scale=1.0, amp=1.0, sec=0.06):
    """Short woody tick (pen on desk / clock escapement)."""
    m = [(1500 * scale, 1.0, 0.010), (2900 * scale, 0.6, 0.006),
         (4300 * scale, 0.3, 0.004), (760 * scale, 0.25, 0.015)]
    return mul(hit(sec, m, rng, noise_amt=0.35, noise_tau=0.0015), amp)


def thud(sec, f0, f1, tau, amp=1.0, glide_t=0.06):
    return mul(mul(osc(sec, glide(f0, f1, glide_t)), env_exp(sec, tau, 0.001)), amp)


def noise_burst(sec, rng, tau, kind="lp", f=400.0, q=0.707, amp=1.0, attack=0.001):
    x = mul(noise(sec, rng), env_exp(sec, tau, attack))
    if kind == "lp":
        x = lowpass(x, f, q)
    elif kind == "hp":
        x = highpass(x, f, q)
    else:
        x = bandpass(x, f, q)
    return mul(x, amp)


def poisson_times(rng, t0, t1, rate):
    t = t0
    out = []
    while True:
        t += rng.expovariate(rate)
        if t >= t1:
            return out
        out.append(t)


def stamp_core(rng):
    sec = 0.3
    x = thud(sec, 150, 50, 0.05, 1.0)
    mix(x, hit(sec, [(210, 0.5, 0.06), (470, 0.35, 0.035), (980, 0.2, 0.02), (1650, 0.1, 0.012)]))
    mix(x, noise_burst(sec, rng, 0.012, "hp", 1500, amp=0.35))        # paper slap
    mix(x, noise_burst(sec, rng, 0.03, "lp", 420, amp=0.5))           # rubber squish
    return x


def formant_voice(sec, f0_list, formants, rng, breath=0.04):
    """Glottal saw through 3 parallel band-pass formants.
    formants: list of (f_list, q, gain) with per-sample frequency lists."""
    src = osc(sec, f0_list, "saw")
    src = lowpass(src, 3500)
    br = noise(sec, rng)
    src = [src[i] + br[i] * breath for i in range(len(src))]
    out = [0.0] * len(src)
    for fl, q, g in formants:
        y = bandpass(src, fl, q)
        for i in range(len(out)):
            out[i] += y[i] * g
    return out


VOWELS = {  # F1, F2, F3 (Hz), adult male-ish
    "a": (730, 1090, 2440), "e": (530, 1840, 2480), "i": (300, 2200, 2950),
    "o": (570, 840, 2410), "u": (320, 870, 2240), "uh": (640, 1190, 2390),
}


# ------------------------------------------------------------------ UI
@sfx("hover", peak_db=-12.0)
def s_hover(rng):
    x = hit(0.1, [(2200, 1.0, 0.012), (3500, 0.5, 0.007), (5200, 0.2, 0.004)], rng, 0.25, 0.001)
    mix(x, mul(osc(0.1, 1100), env_exp(0.1, 0.02)), 0, 0.15)
    return x


@sfx("click", peak_db=-4.0)
def s_click(rng):
    x = silence(0.1)
    mix(x, wood_tick(rng, 1.0, 1.0, 0.08))
    mix(x, wood_tick(rng, 1.12, 0.55, 0.07), 0.022)   # pen click-clack
    return x


@sfx("open", peak_db=-5.0)
def s_open(rng):
    sec = 0.12
    x = mul(noise(sec, rng), env_lin(sec, [(0, 0), (0.005, 1), (0.03, 0.6), (0.09, 0.0)]))
    x = bandpass(x, glide(1800, 5000, 0.08), 1.2)
    mix(x, wood_tick(rng, 1.3, 0.5, 0.04))
    return x


@sfx("page", peak_db=-4.0)
def s_page(rng):
    sec = 0.36
    flutter = smooth_rand(sec, 45, rng, 0.35, 1.0)
    e = env_lin(sec, [(0, 0), (0.12, 1.0), (0.2, 0.8), (0.3, 0.0)])
    x = mul(mul(noise(sec, rng), flutter), e)
    f = lambda t: 800 + 2700 * min(t / 0.15, 1) - 1500 * max(0.0, (t - 0.15) / 0.15)
    x = highpass(bandpass(x, f, 0.9), 400)
    for t in poisson_times(rng, 0.04, 0.26, 70):                    # paper crinkles
        mix(x, noise_burst(0.01, rng, 0.0015, "hp", 3000, amp=rng.uniform(0.1, 0.3)), t)
    mix(x, noise_burst(0.06, rng, 0.02, "lp", 350, amp=0.6, attack=0.004), 0.26)  # flap
    return x


@sfx("submit", peak_db=-4.0)
def s_submit(rng):
    sec = 0.42
    e = env_lin(sec, [(0, 0), (0.03, 1.0), (0.2, 0.7), (0.27, 0.0)])
    x = mul(mul(noise(sec, rng), smooth_rand(sec, 20, rng, 0.6, 1.0)), e)
    x = bandpass(x, glide(2600, 1300, 0.27), 1.0)
    mix(x, hit(0.12, [(420, 0.6, 0.03), (930, 0.35, 0.018), (1900, 0.15, 0.008)], rng, 0.2), 0.27)
    return x


@sfx("stamp")
def s_stamp(rng):
    return stamp_core(rng)


@sfx("approve", peak_db=-4.0)
def s_approve(rng):
    # Played on the same frame as "stamp", so no stamp here: just the bright answer.
    x = silence(1.3)
    notes = [(0.0, 86, 0.35, 0.8), (0.14, 90, 0.35, 0.85), (0.28, 93, 0.7, 1.0)]  # D6 F#6 A6
    for t, m, tau, a in notes:
        mix(x, mbox_note(midi_hz(m), tau, a, 1.2), t)
    mix(x, mbox_note(midi_hz(98), 0.4, 0.3, 1.0), 0.30)     # D7 sparkle
    mix(x, mbox_note(midi_hz(74), 0.6, 0.35, 1.0), 0.28)    # soft D5 underneath
    return x


@sfx("reject", peak_db=-4.0)
def s_reject(rng):
    # "womp womp" on a toy bassoon: two notes, second one sags with a closing wah.
    sec = 0.95

    def bassoon(dur, f, cutoff, vib_depth):
        fn = f if callable(f) else (lambda t, f=f: f)
        fr = lambda t: fn(t) * (1 + vib_depth * math.sin(2 * math.pi * 6 * t))
        src = osc(dur, fr, "saw")
        e = env_lin(dur, [(0, 0), (0.03, 1.0), (dur * 0.7, 0.8), (dur, 0.0)])
        y = mul(src, e)
        y = bandpass(y, 480, 1.4)
        y2 = lowpass(mul(src, e), cutoff)
        return [y[i] * 0.8 + y2[i] * 0.5 for i in range(len(y))]

    x = silence(sec)
    mix(x, bassoon(0.22, 98.0, 900, 0.0), 0.0)                                   # G2
    mix(x, bassoon(0.62, glide(92.5, 69.3, 0.5), glide(1200, 260, 0.55), 0.02), 0.26)  # F#2 -> C#2
    return softclip(x, 1.3)


@sfx("shift_in", peak_db=-5.0)
def s_shift_in(rng):
    sec = 0.5
    e = env_lin(sec, [(0, 0), (0.38, 1.0), (0.42, 1.0), (0.5, 0.0)])
    e = [v * v for v in e]
    x = [0.0] * n_of(sec)
    for f, a in ((659.3, 1.0), (987.8, 0.6), (1318.5, 0.35)):     # E5 B5 E6 glass chord
        fr = glide(f, f * 1.03, 0.45)
        y = osc(sec, lambda t, fr=fr: fr(t) * (1 + 0.004 * math.sin(2 * math.pi * 6.5 * t)))
        mix(x, y, 0, a * 0.5)
    trem = [1 - 0.2 * (0.5 + 0.5 * math.sin(2 * math.pi * 7 * i / SR)) for i in range(len(x))]
    x = mul(mul(x, trem), e)
    wh = bandpass(noise(sec, rng), glide(400, 3200, 0.45), 1.5)
    mix(x, mul(wh, e), 0, 0.6)
    return x


@sfx("shift_out", peak_db=-6.0)
def s_shift_out(rng):
    sec = 0.32
    e = env_lin(sec, [(0, 0), (0.01, 1.0), (0.32, 0.0)])
    e = [v ** 1.5 for v in e]
    x = [0.0] * n_of(sec)
    for f, a in ((1318.5, 0.35), (987.8, 0.6), (659.3, 1.0)):
        mix(x, osc(sec, glide(f, f * 0.75, 0.3)), 0, a * 0.5)
    x = mul(x, e)
    wh = bandpass(noise(sec, rng), glide(3000, 400, 0.3), 1.5)
    mix(x, mul(wh, e), 0, 0.5)
    return x


@sfx("tick_ui", peak_db=-12.0)
def s_tick_ui(rng):
    return hit(0.1, [(4200, 1.0, 0.004), (6100, 0.4, 0.0025), (2100, 0.3, 0.006)], rng, 0.2, 0.0008)


# ------------------------------------------------------------------ deaths
def fire_pops(x, rng, t0, t1, rate, big_frac=0.12, amp=1.0):
    for t in poisson_times(rng, t0, t1, rate):
        if rng.random() < big_frac:
            f = rng.uniform(600, 1500)
            p = hit(0.05, [(f, 1.0, 0.008), (f * 2.3, 0.4, 0.004)], rng, 0.8, 0.003, 800)
            a = rng.uniform(0.7, 1.0)
        else:
            f = rng.uniform(1500, 5000)
            p = hit(0.02, [(f, 1.0, rng.uniform(0.001, 0.004))], rng, 0.5, 0.001, 2000)
            a = rng.uniform(0.15, 0.5)
        mix(x, p, t, a * amp)


@sfx("die_burn")
def s_die_burn(rng):
    sec = 1.3
    e = env_lin(sec, [(0, 0), (0.18, 1.0), (0.45, 0.8), (1.0, 0.35), (1.3, 0.0)])
    e = [v ** 1.4 for v in e]
    fw = lowpass(noise(sec, rng), lambda t: 200 + 3800 * min(t / 0.3, 1) - 2000 * max(0, (t - 0.3)), 0.9)
    x = mul(fw, e)
    mix(x, thud(0.4, 90, 45, 0.15, 0.6, 0.2), 0.05)                      # "fwoomp"
    bed = mul(bandpass(noise(sec, rng), 900, 0.8), smooth_rand(sec, 15, rng, 0.2, 1.0))
    mix(x, mul(bed, env_lin(sec, [(0, 0), (0.3, 0.5), (1.0, 0.4), (1.3, 0)])), 0, 0.5)
    fire_pops(x, rng, 0.25, 1.2, 28, amp=0.7)
    return x


@sfx("die_fall", peak_db=-4.0)
def s_die_fall(rng):
    sec = 0.9
    base = glide(1800, 420, 0.9)
    fr = lambda t: base(t) * (1 + 0.015 * math.sin(2 * math.pi * 7 * t))
    e = env_lin(sec, [(0, 0), (0.03, 1.0), (0.75, 0.85), (0.9, 0.0)])
    tone = osc(sec, fr)
    h2 = osc(sec, lambda t: 2 * fr(t))
    x = [tone[i] + 0.1 * h2[i] for i in range(len(tone))]
    x = mul(x, e)
    breath = bandpass(noise(sec, rng), base, 8.0)
    mix(x, mul(breath, e), 0, 0.5)
    wind = lowpass(noise(sec, rng), 600)
    mix(x, mul(wind, env_lin(sec, [(0, 0), (0.6, 1.0), (0.9, 0.0)])), 0, 0.35)
    return x


@sfx("impact")
def s_impact(rng):
    sec = 0.35
    x = thud(sec, 110, 40, 0.07, 1.0, 0.08)
    mix(x, noise_burst(sec, rng, 0.04, "lp", 300, amp=0.7))
    mix(x, hit(sec, [(180, 0.4, 0.05), (310, 0.3, 0.03), (640, 0.12, 0.015)]))
    mix(x, noise_burst(sec, rng, 0.015, "hp", 2500, amp=0.15))
    return softclip(x, 1.5)


@sfx("die_drown")
def s_die_drown(rng):
    sec = 1.45
    x = silence(sec)
    mix(x, mul(osc(0.15, glide(900, 180, 0.05)), env_exp(0.15, 0.05)), 0, 0.8)   # plop
    mix(x, noise_burst(0.35, rng, 0.12, "bp", 2500, 0.7, amp=1.0, attack=0.002))   # splash
    for t in poisson_times(rng, 0.04, 0.3, 25):                                     # droplets
        mix(x, noise_burst(0.04, rng, 0.008, "bp", rng.uniform(3000, 6000), 2.0,
                           amp=rng.uniform(0.1, 0.3)), t)
    rum = lowpass(noise(sec, rng), 250)
    mix(x, mul(rum, env_lin(sec, [(0, 0), (0.2, 1.0), (1.0, 0.6), (1.45, 0)])), 0, 0.5)
    t = 0.22
    k = 0
    while t < 1.3:                                                                  # bubbles
        prog = (t - 0.22) / 1.1
        f0 = rng.uniform(380, 950) * (1 - 0.4 * prog)
        dur = rng.uniform(0.03, 0.07)
        b = mul(osc(dur + 0.03, glide(f0, f0 * 1.7, dur)), env_exp(dur + 0.03, dur * 0.45, 0.001))
        mix(x, b, t, rng.uniform(0.35, 0.6) * (1 - 0.4 * prog))
        t += rng.uniform(0.03, 0.09) * (1 + 1.5 * prog)
        k += 1
    return x


# ------------------------------------------------------------------ level beats
@sfx("roar")
def s_roar(rng):
    sec = 1.3
    x = hit(0.1, [(1800, 0.5, 0.01), (3200, 0.3, 0.006)], rng, 0.3)                  # igniter click
    e = env_lin(sec, [(0, 0), (0.08, 0.7), (0.15, 1.0), (0.5, 0.6), (1.0, 0.55), (1.3, 0.0)])
    turb = smooth_rand(sec, 20, rng, 0.6, 1.0)
    r = bandpass(noise(sec, rng), glide(250, 700, 0.15), 0.6)
    r = lowpass(r, 1500)
    mix(x, mul(mul(r, turb), e), 0, 1.6)
    hum = [math.sin(2 * math.pi * 55 * i / SR) + 0.6 * math.sin(2 * math.pi * 110 * i / SR)
           + 0.3 * math.sin(2 * math.pi * 165 * i / SR) for i in range(n_of(sec))]
    mix(x, mul(hum, env_lin(sec, [(0, 0), (0.2, 0.2), (1.0, 0.25), (1.3, 0)])), 0, 0.5)
    mix(x, thud(0.5, 70, 40, 0.2, 0.7, 0.3), 0.02)
    return softclip(x, 1.2)


@sfx("creak", peak_db=-4.0)
def s_creak(rng):
    """Stick-slip friction: an irregular impulse train through wooden resonances."""
    sec = 1.6
    n = n_of(sec)
    imp = [0.0] * n
    segs = [(0.0, 0.9, 35, 110, 1.0), (1.0, 1.5, 90, 50, 0.7)]
    for t0, t1, r0, r1, a in segs:
        t = t0
        while t < t1:
            u = (t - t0) / (t1 - t0)
            rate = r0 + (r1 - r0) * u
            env = math.sin(math.pi * u) ** 0.5
            i = int(t * SR)
            if i < n:
                imp[i] += a * env * rng.uniform(0.7, 1.3)
            t += (1.0 / rate) * rng.uniform(0.9, 1.1)
    x = [0.0] * n
    for f, q, g in ((380, 12, 1.0), (900, 14, 0.7), (1650, 16, 0.5), (2600, 18, 0.25)):
        y = bandpass(imp, f, q)
        for i in range(n):
            x[i] += y[i] * g
    mix(x, hit(0.4, [(120, 0.15, 0.08), (260, 0.1, 0.05)]), 0)    # wooden groan of the axle
    return x


@sfx("thump")
def s_thump(rng):
    sec = 0.3
    x = thud(sec, 90, 45, 0.06, 1.0, 0.08)
    mix(x, noise_burst(sec, rng, 0.05, "lp", 220, amp=1.2))
    puff = mul(bandpass(noise(sec, rng), 1800, 0.5), env_lin(sec, [(0, 0), (0.02, 1), (0.25, 0)]))
    mix(x, puff, 0.01, 0.25)                                          # flour puff
    return softclip(x, 1.3)


@sfx("crackle")
def s_crackle(rng):
    sec = 1.8
    e = env_lin(sec, [(0, 0), (0.1, 1.0), (1.4, 1.0), (1.8, 0.0)])
    bed = mul(bandpass(noise(sec, rng), 700, 0.7), smooth_rand(sec, 8, rng, 0.3, 1.0))
    x = mul(bed, 0.5)
    mix(x, highpass(noise(sec, rng), 3000), 0, 0.04)
    fire_pops(x, rng, 0.0, 1.7, 35, 0.15)
    return mul(x, e)


@sfx("oof", peak_db=-4.0)
def s_oof(rng):
    sec = 0.42
    n = n_of(sec)
    f0 = [220 * (130 / 220) ** min(i / SR / 0.12, 1.0) * (1 - 0.1 * i / n) for i in range(n)]
    uh, u = VOWELS["uh"], VOWELS["u"]
    forms = []
    for k, (q, g) in enumerate(((5.0, 1.0), (8.0, 0.55), (12.0, 0.25))):
        fl = [uh[k] + (u[k] - uh[k]) * min(1.0, i / SR / 0.2) for i in range(n)]
        forms.append((fl, q, g))
    v = formant_voice(sec, f0, forms, rng)
    v = mul(v, env_lin(sec, [(0, 0), (0.012, 1.0), (0.08, 0.9), (0.26, 0.0)]))
    f = mul(highpass(noise(sec, rng), 1500), env_lin(sec, [(0, 0), (0.2, 0), (0.24, 0.5), (0.36, 0)]))
    mix(v, f, 0, 0.08)                                               # the "f"
    return v


@sfx("swear", peak_db=-4.0)
def s_swear(rng):
    """Muffled gibberish grumble: random syllables with jumpy, cross pitch."""
    sec = 1.05
    n = n_of(sec)
    f0 = [120.0] * n
    amp = [0.0] * n
    fl = [[500.0] * n for _ in range(3)]
    t = 0.0
    names = list(VOWELS)
    prev = VOWELS["uh"]
    syl = []
    while t < 0.95:
        d = rng.uniform(0.09, 0.19)
        syl.append((t, min(d, 0.98 - t)))
        t += d + rng.uniform(0.0, 0.04)
    for si, (t0, d) in enumerate(syl):
        vw = VOWELS[rng.choice(names)]
        p = rng.uniform(105, 175) * (1.2 if si == len(syl) - 2 else 1.0)
        i0, i1 = int(t0 * SR), min(n, int((t0 + d) * SR))
        for i in range(i0, i1):
            u = (i - i0) / max(1, i1 - i0)
            f0[i] = p * (1.12 - 0.25 * u)
            a = min(1.0, u / 0.12, (1 - u) / 0.25)
            amp[i] = max(0.0, a)
            m = min(1.0, (i - i0) / (0.03 * SR))
            for k in range(3):
                fl[k][i] = prev[k] + (vw[k] - prev[k]) * m
        prev = vw
    for i in range(1, n):                       # hold pitch / formants through gaps
        if amp[i] == 0.0:
            f0[i] = f0[i - 1]
            for k in range(3):
                fl[k][i] = fl[k][i - 1]
    v = formant_voice(sec, f0, [(fl[0], 5.0, 1.0), (fl[1], 8.0, 0.5), (fl[2], 12.0, 0.2)], rng)
    v = mul(v, lowpass1_ctrl(amp, 120.0))
    for t0, d in syl:                                               # consonant bursts
        if rng.random() < 0.6:
            mix(v, noise_burst(0.03, rng, 0.006, "hp", 2000, amp=0.15), max(0.0, t0 - 0.005))
    v = lowpass(v, 2600)                                            # muffled
    return softclip(v, 1.5)


def lowpass1_ctrl(x, fc):
    a = math.exp(-2 * math.pi * fc / SR)
    y = [0.0] * len(x)
    s = 0.0
    for i, v in enumerate(x):
        s = v + a * (s - v)
        y[i] = s
    return y


@sfx("whistle", peak_db=-5.0)
def s_whistle(rng):
    """Station attendant's pea whistle: short-long blasts with the pea's warble."""
    sec = 1.2
    n = n_of(sec)
    trill_rate = smooth_rand(sec, 6, rng, 24, 34)
    ph = 0.0
    lfo = [0.0] * n
    for i in range(n):
        ph += trill_rate[i] / SR
        lfo[i] = math.sin(2 * math.pi * ph)
    fr = [2650 * (1 + 0.035 * lfo[i]) for i in range(n)]
    tone = osc(sec, fr)
    h2 = osc(sec, [2 * f for f in fr])
    am = [0.6 + 0.4 * (0.5 + 0.5 * lfo[i]) for i in range(n)]
    x = [(tone[i] + 0.08 * h2[i]) * am[i] for i in range(n)]
    breath = bandpass(noise(sec, rng), fr, 6.0)
    for i in range(n):
        x[i] += breath[i] * 0.6 * am[i]
    mix(x, highpass(noise(sec, rng), 4000), 0, 0.03)
    e = env_lin(sec, [(0, 0), (0.015, 1.1), (0.05, 1.0), (0.26, 1.0), (0.29, 0.0),
                      (0.38, 0.0), (0.395, 1.15), (0.45, 1.0), (1.12, 0.95), (1.18, 0.0)])
    return mul(x, e)


@sfx("clunk")
def s_clunk(rng):
    sec = 0.32
    modes = [(240, 0.6, 0.08), (560, 0.5, 0.05), (1130, 0.35, 0.03), (1870, 0.25, 0.02), (2950, 0.12, 0.012)]
    x = hit(sec, modes, rng, 0.3, 0.003, 1000)
    mix(x, thud(sec, 90, 55, 0.05, 0.8))
    mix(x, hit(0.25, [(f * 1.12, a, t) for f, a, t in modes], rng, 0.3), 0.07, 0.55)
    return softclip(x, 1.2)


@sfx("brakes", peak_db=-4.0, rms_cap=-16.0)
def s_brakes(rng):
    sec = 1.7
    n = n_of(sec)
    wob = smooth_rand(sec, 6, rng, -1, 1)
    rough = smooth_rand(sec, 40, rng, 0.6, 1.0)
    fr = [2100 * (1 - 0.12 * i / n) * (1 + 0.01 * wob[i]) for i in range(n)]
    sq = osc(sec, fr)
    sq2 = osc(sec, [2 * f for f in fr])
    e = env_lin(sec, [(0, 0), (0.1, 1.0), (1.2, 0.9), (1.45, 0.0)])
    x = [(sq[i] + 0.2 * sq2[i]) * rough[i] * e[i] * 0.5 for i in range(n)]
    sq3 = osc(sec, [f * 1.5 for f in fr])
    mix(x, mul(sq3, env_lin(sec, [(0, 0), (0.2, 0), (0.35, 0.2), (1.1, 0.15), (1.4, 0)])), 0, 1.0)
    hiss = mul(bandpass(noise(sec, rng), 4500, 0.8), e)
    mix(x, hiss, 0, 0.2)
    rum = mul(lowpass(noise(sec, rng), 150), env_lin(sec, [(0, 0.8), (1.4, 0.2), (1.7, 0)]))
    mix(x, rum, 0, 0.5)
    t = 0.05
    while t < 1.35:                                     # rail joints, slowing down
        rate = 8 - 5 * t / 1.35
        mix(x, hit(0.08, [(95, 1.0, 0.025), (220, 0.4, 0.015)]), t, 0.25)
        t += 1.0 / rate
    pssh = mul(highpass(noise(0.4, rng), 2500), env_lin(0.4, [(0, 0), (0.02, 1), (0.35, 0)]))
    mix(x, pssh, 1.3, 0.3)                              # air release
    return x


@sfx("crash")
def s_crash(rng):
    sec = 1.45
    x = thud(sec, 80, 35, 0.25, 1.0, 0.3)
    mix(x, noise_burst(sec, rng, 0.2, "lp", 400, amp=0.9))
    modes = [(rng.uniform(180, 3500), rng.uniform(0.2, 0.6), rng.uniform(0.05, 0.4)) for _ in range(12)]
    mix(x, hit(sec, modes, rng, 0.6, 0.05, 1000), 0, 0.6)
    for k in range(18):                                 # debris clatter
        t = 0.1 + 1.1 * (k / 18.0) ** 1.3 + rng.uniform(0, 0.03)
        m = [(rng.uniform(800, 5000), 1.0, rng.uniform(0.01, 0.06)), (rng.uniform(800, 5000), 0.5, 0.02)]
        mix(x, hit(0.12, m, rng, 0.3), t, 0.4 * (1 - k / 20.0))
    mix(x, hit(1.0, [(620, 0.4, 0.6), (1480, 0.3, 0.4), (2210, 0.2, 0.3)]), 0.35, 0.5)  # hubcap
    return softclip(x, 1.6)


@sfx("grab", peak_db=-4.0)
def s_grab(rng):
    sec = 0.3
    rust = smooth_rand(sec, 150, rng, 0.2, 1.0)
    c = bandpass(highpass(noise(sec, rng), 1200), 3000, 0.5)
    x = mul(mul(c, rust), env_exp(sec, 0.06, 0.005))
    mix(x, mul(mul(c, rust), env_exp(sec, 0.04, 0.005)), 0.08, 0.5)
    mix(x, noise_burst(sec, rng, 0.02, "lp", 250, amp=0.5))
    mix(x, mul(osc(sec, 140), env_exp(sec, 0.03)), 0, 0.3)
    return x


def clock_tick(rng, high=True):
    if high:
        m = [(2600, 1.0, 0.012), (4100, 0.5, 0.008), (1300, 0.3, 0.02)]
    else:
        m = [(1100, 1.0, 0.025), (1750, 0.5, 0.015), (600, 0.4, 0.04)]
    m += [(320, 0.3, 0.06), (780, 0.2, 0.04)]          # clock case
    return hit(0.3, m, rng, 0.3, 0.0015)


@sfx("tick")
def s_tick(rng):
    x = silence(0.85)
    mix(x, clock_tick(rng, True), 0.0)
    mix(x, clock_tick(rng, False), 0.28)
    mix(x, clock_tick(rng, True), 0.56)
    return x


@sfx("grind")
def s_grind(rng):
    sec = 1.45
    x = silence(sec)
    t = 0.0
    while t < 1.05:                                     # gear teeth, decelerating
        rate = 70 * (12 / 70) ** (t / 1.05)
        m = [(f * rng.uniform(0.95, 1.05), a, 0.006) for f, a in ((900, 1.0), (1600, 0.6), (2700, 0.4), (4100, 0.2))]
        mix(x, hit(0.03, m), t, rng.uniform(0.5, 1.0) * 0.5)
        t += 1.0 / rate
    fr = bandpass(noise(sec, rng), 1200, 1.0)
    mix(x, mul(fr, env_lin(sec, [(0, 0.3), (1.0, 0.25), (1.08, 0)])), 0, 0.6)
    motor = lowpass(osc(sec, glide(55, 35, 1.05), "saw"), 300)
    mix(x, mul(motor, env_lin(sec, [(0, 0.3), (1.0, 0.25), (1.1, 0)])), 0, 0.8)
    jam = hit(0.35, [(150, 1.0, 0.08), (340, 0.6, 0.05), (810, 0.4, 0.03), (1450, 0.25, 0.02)], rng, 0.4)
    mix(x, jam, 1.07)
    mix(x, thud(0.3, 100, 50, 0.06, 0.8), 1.07)
    mix(x, hit(0.3, [(3300, 0.3, 0.12), (5100, 0.15, 0.08)]), 1.22, 0.6)     # loose spring "ting"
    return softclip(x, 1.3)


@sfx("punch_clock")
def s_punch_clock(rng):
    sec = 0.75
    x = mul(bandpass(noise(0.07, rng), 2500, 1.0), env_lin(0.07, [(0, 0), (0.01, 1), (0.07, 0)]))
    x = mul(x, 0.3)
    x += [0.0] * (n_of(sec) - len(x))
    mix(x, hit(0.1, [(1200, 0.5, 0.01), (2400, 0.3, 0.006)], rng, 0.2), 0.07, 0.4)   # ka-
    chunk = hit(0.3, [(330, 0.5, 0.05), (760, 0.4, 0.03), (1500, 0.3, 0.02), (2600, 0.15, 0.01)], rng, 0.4)
    mix(x, chunk, 0.11)
    mix(x, thud(0.3, 120, 60, 0.05, 0.8), 0.11)                                       # -CHUNK
    bell = hit(0.55, [(1318.5, 1.0, 0.4), (2637, 0.35, 0.2), (3639, 0.25, 0.12), (7120, 0.08, 0.05)])
    mix(x, bell, 0.22, 0.45)                                                          # ding!
    return x


# ------------------------------------------------------------------ main
def main(names):
    os.makedirs(OUT, exist_ok=True)
    todo = names or list(SFX)
    for name in todo:
        fn, peak_db, rms_cap = SFX[name]
        x = finalize_sfx(fn(rng_for(name)), peak_db, rms_cap)
        path = os.path.join(OUT, name + ".wav")
        write_wav(path, x)
        print("%-12s %.3f s" % (name, len(x) / SR))


if __name__ == "__main__":
    main(sys.argv[1:])
