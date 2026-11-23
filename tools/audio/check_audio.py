"""Sanity-check the generated audio in staging_audio/ (or a given root).

Usage:  python tools/audio/check_audio.py [audio_root]
Checks: format, duration, peak/RMS, clipping, silence, NaN-free (PCM ints),
every name from docs/audio-list.md + level JSON "sfx" beats has a file,
BGM loop seam is no bigger than ordinary sample-to-sample motion.
Exit code 1 if anything fails.
"""
import glob
import json
import math
import os
import re
import sys
import wave
from array import array

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def expected_names():
    sfx, bgm = set(), set()
    with open(os.path.join(ROOT, "docs", "audio-list.md"), encoding="utf-8") as f:
        section = ""
        for line in f:
            if line.startswith("## "):
                section = line
            m = re.match(r"^\|\s*([a-z_]+)\s*\|", line)
            if m:
                (bgm if "音乐" in section else sfx).add(m.group(1))
    beats = set()
    for p in glob.glob(os.path.join(ROOT, "Assets", "Resources", "Levels", "*.json")):
        with open(p, encoding="utf-8") as f:
            data = json.load(f)

        def walk(o):
            if isinstance(o, dict):
                if any(o.get(k) == "sfx" for k in ("do", "type", "op", "kind")):
                    if o.get("arg"):
                        beats.add(o["arg"])
                for v in o.values():
                    walk(v)
            elif isinstance(o, list):
                for v in o:
                    walk(v)
        walk(data)
    return sfx, bgm, beats


def read(path):
    with wave.open(path, "rb") as w:
        ch, sw, sr, n = w.getnchannels(), w.getsampwidth(), w.getframerate(), w.getnframes()
        raw = w.readframes(n)
    a = array("h")
    a.frombytes(raw)
    if sys.byteorder == "big":
        a.byteswap()
    chans = [a[c::ch] for c in range(ch)]
    return ch, sw, sr, n, chans


def stats(x):
    pk = max(max(x), -min(x)) / 32768.0
    rms = math.sqrt(sum(v * v for v in x) / len(x)) / 32768.0
    clip = sum(1 for v in x if v >= 32767 or v <= -32768)
    return pk, rms, clip


def dbs(v):
    return 20 * math.log10(max(v, 1e-9))


# Bar length of each loop (must match gen_bgm.py). Every piece is in 3/4.
BAR_SEC = {"level": 3 * 60 / 96.0, "title": 3 * 60 / 72.0, "credits": 3 * 60 / 72.0}


def _rms(x):
    return math.sqrt(sum(v * v for v in x) / max(1, len(x))) or 1.0


def seam(x, sr, bar_sec):
    """Loop-seam check.
    1) sample jump end->start vs the 99th percentile of adjacent-sample differences;
    2) 50 ms head/tail level step at the seam vs the same step at every interior
       bar line (the seam is a bar line too, so its downbeat onset is expected)."""
    d = sorted(abs(x[i + 1] - x[i]) for i in range(0, len(x) - 1, 7))
    p50 = d[len(d) // 2]
    p99 = d[int(len(d) * 0.99)]
    jump = abs(x[0] - x[-1])
    w = int(0.05 * sr)
    seam_db = dbs(_rms(x[:w]) / _rms(x[-w:]))
    steps = []
    nb = int(round(len(x) / (bar_sec * sr)))
    for b in range(1, nb):
        k = int(round(b * bar_sec * sr))
        steps.append(dbs(_rms(x[k:k + w]) / _rms(x[k - w:k])))
    return jump, p50, p99, seam_db, min(steps), max(steps)


def main():
    base = sys.argv[1] if len(sys.argv) > 1 else os.path.join(ROOT, "staging_audio")
    sfx_names, bgm_names, beat_names = expected_names()
    fails = []
    print("%-26s %6s %6s %3s %8s %8s %5s  %s" % ("file", "sec", "Hz", "ch", "peak dB", "rms dB", "clip", "status"))
    total_bgm = 0
    for kind, names in (("Sfx", sorted(sfx_names | beat_names)), ("Bgm", sorted(bgm_names))):
        for name in names:
            path = os.path.join(base, kind, name + ".wav")
            if not os.path.exists(path):
                fails.append("missing %s/%s" % (kind, name))
                print("%-26s MISSING" % (kind + "/" + name))
                continue
            ch, sw, sr, n, chans = read(path)
            sec = n / sr
            pk = rms = 0.0
            clip = 0
            for c in chans:
                p, r, k = stats(c)
                pk, rms, clip = max(pk, p), max(rms, r), clip + k
            st = []
            if sw != 2 or sr != 44100:
                st.append("format")
            if kind == "Sfx":
                if ch != 1:
                    st.append("not mono")
                if not (0.1 - 1e-3 <= sec <= 2.5):
                    st.append("duration")
                if pk > 0.75:
                    st.append("peak>-2.5dB")
            else:
                total_bgm += os.path.getsize(path) if name != "credits" else 0
                if not (60 <= sec <= 120):
                    st.append("duration")
            if clip:
                st.append("clipping")
            if rms < 10 ** (-60 / 20):
                st.append("silent")
            extra = ""
            if kind == "Bgm":
                for ci, c in enumerate(chans):
                    jump, p50, p99, lvl, lo, hi = seam(c, sr, BAR_SEC[name])
                    extra += ("\n%28s seam ch%d: jump=%d (adjacent-diff p50=%d p99=%d); "
                              "50ms head/tail step %+.1f dB (interior bar lines %+.1f..%+.1f dB)"
                              % ("", ci, jump, p50, p99, lvl, lo, hi))
                    if jump > p99 or lvl > hi + 1.0 or lvl < lo - 1.0:
                        st.append("seam ch%d" % ci)
            status = "OK" if not st else "FAIL: " + ", ".join(st)
            if st:
                fails.append("%s/%s: %s" % (kind, name, ", ".join(st)))
            print("%-26s %6.3f %6d %3d %8.1f %8.1f %5d  %s%s" % (kind + "/" + name, sec, sr, ch, dbs(pk), dbs(rms), clip, status, extra))
    print()
    print("names in audio-list.md: %d sfx, %d bgm; level JSON sfx beats: %d (%s)" % (
        len(sfx_names), len(bgm_names), len(beat_names), ", ".join(sorted(beat_names))))
    print("beat names not in audio-list.md:", sorted(beat_names - sfx_names) or "none")
    stray = []
    for kind, names in (("Sfx", sfx_names | beat_names), ("Bgm", bgm_names)):
        for p in glob.glob(os.path.join(base, kind, "*.wav")):
            if os.path.splitext(os.path.basename(p))[0] not in names:
                stray.append(kind + "/" + os.path.basename(p))
    print("files not referenced by the list:", stray or "none")
    print("level+title BGM size: %.1f MB" % (total_bgm / 1e6))
    print("(16-bit PCM cannot hold NaN/Inf; generator clamps before quantising)")
    if fails:
        print("\nFAILURES:\n  " + "\n  ".join(fails))
        sys.exit(1)
    print("\nALL CHECKS PASSED")


if __name__ == "__main__":
    main()
