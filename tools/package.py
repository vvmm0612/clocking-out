"""Zip the WebGL build for itch.io: index.html at the zip root, forward-slash paths.

    python tools/package.py [--src Builds/WebGL] [--out Builds/clocking-out-webgl.zip]
"""
import argparse
import os
import sys
import zipfile

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--src", default=os.path.join(ROOT, "Builds", "WebGL"))
    ap.add_argument("--out", default=os.path.join(ROOT, "Builds", "clocking-out-webgl.zip"))
    a = ap.parse_args()

    src = os.path.abspath(a.src)
    if not os.path.isfile(os.path.join(src, "index.html")):
        sys.exit("no index.html in " + src + " (build first)")
    tmp = a.out + ".tmp"
    total = 0
    with zipfile.ZipFile(tmp, "w", zipfile.ZIP_DEFLATED) as z:
        for d, _, files in os.walk(src):
            for f in sorted(files):
                p = os.path.join(d, f)
                rel = os.path.relpath(p, src).replace(os.sep, "/")
                # .unityweb files are already compressed: store them as-is
                z.write(p, rel, zipfile.ZIP_STORED if f.endswith(".unityweb") else zipfile.ZIP_DEFLATED)
                total += os.path.getsize(p)
                print("  " + rel)
    os.replace(tmp, a.out)
    print("build %.2f MB -> %s (%.2f MB)" % (total / 1048576.0, a.out, os.path.getsize(a.out) / 1048576.0))


if __name__ == "__main__":
    main()
