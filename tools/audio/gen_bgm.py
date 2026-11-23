"""Procedurally compose + synthesise the placeholder BGM for Clocking Out.

Usage:  python tools/audio/gen_bgm.py [level|title ...]
Writes staging_audio/Bgm/<name>.wav (16-bit PCM, 44.1 kHz, stereo) and copies
title.wav to credits.wav.

Seamless looping: the piece is a whole number of bars; every note tail that runs
past the end is wrapped (added modulo loop length) back onto the start, and the
reverb is run over the loop twice so its tail at the end feeds the start.
"""
import math
import os
import random
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from synthlib import (SR, clar_note, highpass1, hit, ks_pluck, lowpass1, mbox_note,  # noqa: E402
                      midi_hz, peak_of, reverb_loop, rms_of, undb, write_wav)

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "staging_audio", "Bgm")

# ------------------------------------------------------------------ harmony
#            bass root (MIDI), pizz voicing, chord tones (pitch classes)
CHORDS = {
    "Am":  (45, [57, 60, 64]),
    "Dm":  (50, [57, 62, 65]),
    "E7":  (40, [56, 59, 62]),
    "F":   (41, [57, 60, 65]),
    "C":   (48, [55, 60, 64]),
    "G7":  (43, [55, 59, 65]),
    "A7":  (45, [55, 61, 64]),
    "Dm6": (50, [59, 62, 65]),
}

CH_A = ["Am", "Dm", "E7", "Am", "F", "Dm", "E7", "Am"]
CH_A2 = ["Am", "Dm", "E7", "Am", "F", "Dm", "E7", "E7"]
CH_B = ["C", "G7", "C", "A7", "Dm", "G7", "C", "E7"]
CH_B2 = ["F", "C", "Dm", "Am", "Dm6", "E7", "Am", "E7"]
CH_CODA = ["Am", "F", "Dm", "E7"]

# ------------------------------------------------------------------ melody (hand-written)
# One string per 3/4 bar: "Note:beats ..." ; r = rest.
MEL_A = [
    "E5:1 A5:.5 B5:.5 C6:1",
    "A5:1.5 F5:.5 D5:1",
    "E5:1 G#5:1 B5:1",
    "A5:2 r:.5 E5:.5",
    "C6:1 A5:.5 F5:.5 A5:1",
    "F5:1.5 E5:.5 D5:1",
    "B4:1 D5:1 G#5:1",
    "A5:3",
]
MEL_A2 = [
    "E5:1 A5:.5 B5:.5 C6:.5 D6:.5",
    "D6:1.5 A5:.5 F5:1",
    "E5:.5 F5:.5 G#5:1 B5:1",
    "A5:1 C6:.5 B5:.5 A5:1",
    "C6:1 A5:.5 F5:.5 C6:1",
    "D6:1 C6:.5 B5:.5 A5:1",
    "G#5:1 B5:1 D6:1",
    "B5:1 G#5:1 E5:1",
]
MEL_B = [
    "G5:1 E5:1 G5:1",
    "F5:1.5 D5:.5 B4:1",
    "C5:1 E5:1 G5:1",
    "C#6:2 A5:1",
    "D6:1 A5:1 F5:1",
    "G5:1 F5:.5 E5:.5 D5:1",
    "E5:2 C5:1",
    "B4:1 E5:1 G#5:1",
]
MEL_B2 = [
    "A5:1 C6:1 A5:1",
    "G5:1.5 E5:.5 C5:1",
    "F5:1 A5:1 D6:1",
    "C6:2 B5:1",
    "A5:1 F5:1 D5:1",
    "E5:1 G#5:1 B5:1",
    "C6:1 B5:1 A5:1",
    "G#5:1 A5:.5 G#5:.5 B5:1",
]
MEL_A3 = MEL_A[:7] + ["B5:1 G#5:1 E5:1"]
MEL_CODA = ["C6:3", "A5:3", "F5:2 E5:1", "B5:1.5 G#5:1.5"]

PC = {"C": 0, "D": 2, "E": 4, "F": 5, "G": 7, "A": 9, "B": 11}
A_HARM_MINOR = [9, 11, 0, 2, 4, 5, 8]  # A B C D E F G#
C_MAJOR = [0, 2, 4, 5, 7, 9, 11]


def note_midi(s):
    letter = s[0]
    acc = 0
    rest = s[1:]
    while rest and rest[0] in "#b":
        acc += 1 if rest[0] == "#" else -1
        rest = rest[1:]
    return 12 * (int(rest) + 1) + PC[letter] + acc


def parse_bar(s):
    out = []
    t = 0.0
    for tok in s.split():
        n, d = tok.split(":")
        d = float(d)
        if n != "r":
            out.append((t, note_midi(n), d))
        t += d
    return out


def third_below(m, scale):
    """Diatonic third below m in the given scale (chromatic notes get a major third)."""
    if m % 12 not in scale:
        return m - 4
    below = [x for x in range(m - 5, m) if x % 12 in scale]
    return below[-2]


# ------------------------------------------------------------------ score
class Score:
    def __init__(self, bpm, bars):
        self.beat = 60.0 / bpm
        self.bars = bars
        self.length = int(round(bars * 3 * self.beat * SR))
        self.events = []  # (beat_pos, inst, midi, dur_beats, vel, pan, jitter_ok)

    def add(self, beat_pos, inst, midi, dur, vel, pan, jitter=True):
        self.events.append((beat_pos, inst, midi, dur, vel, pan, jitter))


def put_melody(sc, bar0, bars, inst, vel, pan, transpose=0, harmony=None, hvel=0.0, hpan=0.0):
    for b, s in enumerate(bars):
        for t, m, d in parse_bar(s):
            acc = 1.08 if t == 0 else 1.0
            sc.add((bar0 + b) * 3 + t, inst, m + transpose, d, vel * acc, pan)
            if harmony is not None:
                sc.add((bar0 + b) * 3 + t, inst, third_below(m, harmony) + transpose, d, hvel, hpan)


def put_waltz_accomp(sc, bar0, chords, bass_style, pizz_vel, bass_vel=0.7, pizz_inst="pizz",
                     pizz_transpose=0):
    for b, ch in enumerate(chords):
        root, voicing = CHORDS[ch]
        pos = (bar0 + b) * 3
        if bass_style == "long":
            sc.add(pos, "bass", root, 2.6, bass_vel, 0.0)
        elif bass_style == "root5":
            sc.add(pos, "bass", root, 1.2, bass_vel, 0.0)
            sc.add(pos + 2, "bass", root + 7, 0.8, bass_vel * 0.75, 0.0)
        elif bass_style == "walk":
            third = root + (3 if ch[1:2] == "m" else 4)
            sc.add(pos, "bass", root, 0.9, bass_vel, 0.0)
            sc.add(pos + 1, "bass", root + 7, 0.9, bass_vel * 0.7, 0.0)
            sc.add(pos + 2, "bass", third, 0.9, bass_vel * 0.7, 0.0)
        elif bass_style == "short":
            sc.add(pos, "bass", root, 0.9, bass_vel, 0.0)
        elif bass_style == "boxlow":
            sc.add(pos, "mbox_low", root + 12, 3.0, bass_vel, 0.0)
        if pizz_vel > 0:
            for k, beat in enumerate((1, 2)):
                for j, m in enumerate(voicing):
                    sc.add(pos + beat, pizz_inst, m + pizz_transpose, 0.9,
                           pizz_vel * (1.0 if k == 0 else 0.85) * (0.85 if j else 1.0),
                           -0.3 + 0.15 * j)


def put_clock(sc, vel_tick, vel_tock, pan=-0.15):
    for beat in range(sc.bars * 3):
        if beat % 3 == 0:
            sc.add(beat, "tock", 0, 0.2, vel_tock, pan, jitter=False)
        else:
            sc.add(beat, "tick", 0, 0.1, vel_tick, pan, jitter=False)


def score_level():
    sc = Score(96, 40)
    # A  (bars 0-7): music box alone over bass + light pizz
    put_melody(sc, 0, MEL_A, "mbox", 0.9, 0.2)
    put_waltz_accomp(sc, 0, CH_A, "long", 0.22)
    # A' (8-15): ornamented melody, bass moves root-fifth
    put_melody(sc, 8, MEL_A2, "mbox", 0.9, 0.2)
    put_waltz_accomp(sc, 8, CH_A2, "root5", 0.3)
    # B  (16-23): relative-major turn, walking bass
    put_melody(sc, 16, MEL_B, "mbox", 0.85, 0.2)
    put_waltz_accomp(sc, 16, CH_B, "walk", 0.3, 0.6)
    # B' (24-31): melody + diatonic thirds below
    put_melody(sc, 24, MEL_B2, "mbox", 0.85, 0.25, harmony=A_HARM_MINOR, hvel=0.4, hpan=-0.2)
    put_waltz_accomp(sc, 24, CH_B2, "root5", 0.32)
    # A'' (32-39): sly clarinet takes the tune an octave down, music box twinkles above
    put_melody(sc, 32, MEL_A3, "clar", 0.55, 0.1, transpose=-12)
    put_waltz_accomp(sc, 32, CH_A2, "short", 0.0, 0.55)
    for b, ch in enumerate(CH_A2):
        _, v = CHORDS[ch]
        if b < 7:
            sc.add((32 + b) * 3 + 1, "mbox", v[2] + 24, 0.8, 0.28, 0.35)
            sc.add((32 + b) * 3 + 2, "mbox", v[1] + 24, 0.8, 0.24, 0.35)
    put_clock(sc, 0.22, 0.3)
    return sc, dict(wet=0.3, room=0.82, damp=0.35)


def score_title():
    sc = Score(72, 28)
    put_melody(sc, 0, MEL_A, "mbox_slow", 0.85, 0.15)
    put_waltz_accomp(sc, 0, CH_A, "boxlow", 0.26, 0.5, pizz_inst="mbox_soft", pizz_transpose=12)
    put_melody(sc, 8, MEL_B, "mbox_slow", 0.8, 0.15, harmony=C_MAJOR, hvel=0.3, hpan=-0.25)
    put_waltz_accomp(sc, 8, CH_B, "boxlow", 0.24, 0.5, pizz_inst="mbox_soft", pizz_transpose=12)
    put_melody(sc, 16, MEL_A2, "mbox_slow", 0.85, 0.15, harmony=A_HARM_MINOR, hvel=0.3, hpan=-0.25)
    put_waltz_accomp(sc, 16, CH_A2, "boxlow", 0.22, 0.5, pizz_inst="mbox_soft", pizz_transpose=12)
    put_melody(sc, 24, MEL_CODA, "mbox_slow", 0.75, 0.15)
    put_waltz_accomp(sc, 24, CH_CODA, "boxlow", 0.2, 0.45, pizz_inst="mbox_soft", pizz_transpose=12)
    put_clock(sc, 0.12, 0.16)
    return sc, dict(wet=0.45, room=0.86, damp=0.3)


# ------------------------------------------------------------------ rendering
def make_clock_sample(high):
    rng = random.Random(7 if high else 8)
    if high:
        m = [(2600, 1.0, 0.010), (4100, 0.5, 0.007), (1300, 0.3, 0.015), (500, 0.15, 0.03)]
    else:
        m = [(1100, 1.0, 0.02), (1750, 0.5, 0.012), (600, 0.4, 0.03), (300, 0.2, 0.05)]
    x = hit(0.25, m, rng, 0.25, 0.0012)
    pk = peak_of(x)
    return [v / pk for v in x]


class Renderer:
    def __init__(self, beat):
        self.beat = beat
        self.cache = {}
        self.clock = {"tick": make_clock_sample(True), "tock": make_clock_sample(False)}

    def note(self, inst, m, dur_beats):
        key = (inst, m, dur_beats)
        if key in self.cache:
            return self.cache[key]
        f = midi_hz(m)
        dur = dur_beats * self.beat
        if inst in ("tick", "tock"):
            x = self.clock[inst]
        elif inst == "mbox":
            tau = max(0.25, min(1.4, 0.5 + 0.35 * dur)) * (880.0 / f) ** 0.3
            x = mbox_note(f, tau, 1.0, 4.0)
        elif inst == "mbox_slow":
            tau = max(0.4, min(2.2, 0.8 + 0.4 * dur)) * (880.0 / f) ** 0.3
            x = mbox_note(f, tau, 1.0, 6.0)
        elif inst == "mbox_soft":
            x = lowpass1(mbox_note(f, 0.45, 1.0, 2.5), 3000)
        elif inst == "mbox_low":
            x = lowpass1(mbox_note(f, 1.6, 1.0, 6.0), 1500)
        elif inst == "pizz":
            x = ks_pluck(f, 0.9, random.Random(m * 31 + 5), t60=0.45, bright=0.35)
            x = lowpass1(x, 2500)
        elif inst == "bass":
            x = clar_note(f, dur * 0.95, 1.0, attack=0.04, release=0.12, vib=0.0)
            x = lowpass1(x, 1400)
        elif inst == "clar":
            x = clar_note(f, dur * 0.92, 1.0, attack=0.05, release=0.15, vib=0.005)
            x = lowpass1(x, 3200)
        else:
            raise ValueError(inst)
        self.cache[key] = x
        return x


INST_GAIN = {"mbox": 0.55, "mbox_slow": 0.55, "mbox_soft": 0.18, "mbox_low": 0.45,
             "pizz": 0.35, "bass": 0.42, "clar": 0.5, "tick": 0.22, "tock": 0.28}


def render(sc, fx, seed):
    rng = random.Random(seed)
    L = sc.length
    left = [0.0] * L
    right = [0.0] * L
    r = Renderer(sc.beat)
    for pos, inst, m, dur, vel, pan, jit in sc.events:
        x = r.note(inst, m, dur)
        start = int(round(pos * sc.beat * SR))
        if jit:
            start += int(rng.uniform(-0.004, 0.004) * SR)
            vel *= rng.uniform(0.92, 1.05)
        g = vel * INST_GAIN[inst]
        a = (pan + 1.0) * math.pi / 4.0
        gl = g * math.cos(a)
        gr = g * math.sin(a)
        for i, v in enumerate(x):
            k = (start + i) % L          # wrap tails onto the loop start
            left[k] += v * gl
            right[k] += v * gr
    print("  notes rendered: %d events, %d cached voices" % (len(sc.events), len(r.cache)))
    # reverb (two passes so the end's tail wraps into the start)
    mono = [(left[i] + right[i]) * 0.5 for i in range(L)]
    mono = highpass1(mono, 150.0)                   # keep the bass out of the reverb
    wl = reverb_loop(mono, fx["room"], fx["damp"], 0)
    wr = reverb_loop(mono, fx["room"], fx["damp"], 23)
    dry_rms = rms_of(mono) or 1.0
    wet_g = fx["wet"] * dry_rms / (rms_of(wl) or 1.0)
    for i in range(L):
        left[i] += wl[i] * wet_g
        right[i] += wr[i] * wet_g
    # DC removal that is loop-safe: subtract the mean only
    for ch in (left, right):
        m = sum(ch) / L
        for i in range(L):
            ch[i] -= m
    # loudness: aim for ~-20 dBFS RMS (music sits under SFX), peak capped at -1 dBFS
    pk = max(peak_of(left), peak_of(right)) or 1.0
    rm = math.sqrt((rms_of(left) ** 2 + rms_of(right) ** 2) / 2) or 1.0
    g = min(undb(-20.0) / rm, undb(-1.0) / pk)
    left = [v * g for v in left]
    right = [v * g for v in right]
    return left, right


PIECES = {"level": (score_level, 1001), "title": (score_title, 2002)}


def main(names):
    os.makedirs(OUT, exist_ok=True)
    for name in names or ["level", "title"]:
        fn, seed = PIECES[name]
        sc, fx = fn()
        print("%s: %d bars, %.2f s" % (name, sc.bars, sc.length / SR))
        l, r = render(sc, fx, seed)
        path = os.path.join(OUT, name + ".wav")
        write_wav(path, l, r)
        print("  wrote", path)
        if name == "title":
            shutil.copyfile(path, os.path.join(OUT, "credits.wav"))
            print("  copied -> credits.wav")


if __name__ == "__main__":
    main(sys.argv[1:])
