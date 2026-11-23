using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Small procedural textures for the UI (nothing extra ships with the build, total well under 1 MB of
    /// GPU memory): paper grain, edge shading, ink speckle for rubber stamps, a ragged stamp frame, a soft
    /// vertical fade, and the Grim Reaper silhouette (body and the stamping arm as separate sprites).
    /// Every texture is generated once on first use.
    /// </summary>
    public static class UITextures
    {
        private static Sprite grain, edge, speckle, frame, fade, reaperBody, reaperArm, diamond, vignette;

        private static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263 + seed * 982451653;
                h = (h ^ (h >> 13)) * 1274126177;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }

        /// <summary>Tileable value noise in 0..1 (period = n cells).</summary>
        private static float Noise(float x, float y, int n, int seed)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            int xa = ((x0 % n) + n) % n, xb = (xa + 1) % n, ya = ((y0 % n) + n) % n, yb = (ya + 1) % n;
            float a = Hash(xa, ya, seed), b = Hash(xb, ya, seed), c = Hash(xa, yb, seed), d = Hash(xb, yb, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static Texture2D Tex(int w, int h, bool repeat)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            return t;
        }

        private static Sprite Make(Texture2D t, Color32[] px, Vector4 border, Vector2 pivot)
        {
            t.SetPixels32(px);
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivot, 100f, 0, SpriteMeshType.FullRect, border);
        }

        /// <summary>Paper grain: soft blotches plus fine fibres, white-ish (multiply by the paper colour). 128px, tiles.</summary>
        public static Sprite PaperGrain()
        {
            if (grain != null) return grain;
            const int n = 128;
            Texture2D t = Tex(n, n, true);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float blot = Noise(x / 16f, y / 16f, 8, 1) * 0.6f + Noise(x / 8f, y / 8f, 16, 2) * 0.4f;
                    float fibre = Noise(x / 1.5f, y / 9f, 85, 3);
                    float fine = Hash(x, y, 4);
                    float v = 1f - 0.05f * blot - 0.035f * Mathf.Pow(fibre, 3f) - 0.025f * fine;
                    byte b = (byte)Mathf.RoundToInt(Mathf.Clamp01(v) * 255f);
                    px[y * n + x] = new Color32(b, b, (byte)Mathf.RoundToInt(Mathf.Clamp01(v - 0.006f) * 255f), 255);
                }
            }
            grain = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            grain.name = "PaperGrain";
            return grain;
        }

        /// <summary>Edge shading for 9-slicing: transparent centre, darkening toward the borders (24px).</summary>
        public static Sprite EdgeShade()
        {
            if (edge != null) return edge;
            const int n = 64;
            const float b = 24f;
            Texture2D t = Tex(n, n, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Min(x + 0.5f, n - x - 0.5f), dy = Mathf.Min(y + 0.5f, n - y - 0.5f);
                    float d = Mathf.Min(dx, dy) / b;
                    float a = Mathf.Pow(1f - Mathf.Clamp01(d), 2.2f);
                    // corners a little darker still
                    float c = Mathf.Pow(1f - Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / (b * 1.4f)), 2f) * 0.5f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a + c) * 255f));
                }
            }
            edge = Make(t, px, new Vector4(b, b, b, b), new Vector2(0.5f, 0.5f));
            edge.name = "EdgeShade";
            return edge;
        }

        /// <summary>Screen vignette: clear in the middle, alpha rising toward the edges and corners (stretch it).</summary>
        public static Sprite Vignette()
        {
            if (vignette != null) return vignette;
            const int n = 64;
            Texture2D t = Tex(n, n, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.414f;
                    float a = Mathf.Clamp01((d - 0.45f) / 0.55f);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            vignette = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            vignette.name = "Vignette";
            return vignette;
        }

        /// <summary>Ink gaps for rubber stamps: white blotches with alpha (tint with the paper colour, put on top).</summary>
        public static Sprite Speckle()
        {
            if (speckle != null) return speckle;
            const int n = 128;
            Texture2D t = Tex(n, n, true);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float v = Noise(x / 5f, y / 5f, 26, 7) * 0.65f + Noise(x / 2f, y / 2f, 64, 8) * 0.35f;
                    float a = Mathf.Clamp01((v - 0.62f) * 5f);
                    if (Hash(x, y, 9) > 0.985f) a = 1f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            speckle = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            speckle.name = "Speckle";
            return speckle;
        }

        private static Sprite inkMask;

        /// <summary>Mask for stamp ink: opaque with small irregular holes (where the paper shows through).</summary>
        public static Sprite InkMask()
        {
            if (inkMask != null) return inkMask;
            const int n = 128;
            Texture2D t = Tex(n, n, true);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float v = Noise(x / 5f, y / 5f, 26, 7) * 0.65f + Noise(x / 2f, y / 2f, 64, 8) * 0.35f;
                    bool hole = v > 0.66f || Hash(x, y, 9) > 0.985f;
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(hole ? 0 : 255));
                }
            }
            inkMask = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            inkMask.name = "InkMask";
            return inkMask;
        }

        /// <summary>A rubber-stamp frame: double rounded rectangle with ragged, uneven ink (9-sliced, 40px borders).</summary>
        public static Sprite StampFrame()
        {
            if (frame != null) return frame;
            const int w = 256, h = 128;
            const float r = 18f;
            Texture2D t = Tex(w, h, false);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float d = RoundRect(x + 0.5f, y + 0.5f, w, h, 6f, r);   // distance inside the outer edge (negative outside)
                    float wobble = (Noise(x / 6f, y / 6f, 43, 11) - 0.5f) * 2.4f;
                    float outer = Band(d + wobble, 0f, 7f);
                    float inner = Band(d + wobble * 0.7f, 11f, 13.5f);
                    float a = Mathf.Max(outer, inner);
                    float gaps = Noise(x / 3f, y / 3f, 86, 12);
                    a *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01((0.75f - gaps) * 4f));
                    px[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(a) * 255f));
                }
            }
            frame = Make(t, px, new Vector4(40, 40, 40, 40), new Vector2(0.5f, 0.5f));
            frame.name = "StampFrame";
            return frame;
        }

        private static float RoundRect(float x, float y, float w, float h, float inset, float r)
        {
            float hx = w * 0.5f - inset, hy = h * 0.5f - inset;
            float qx = Mathf.Abs(x - w * 0.5f) - (hx - r), qy = Mathf.Abs(y - h * 0.5f) - (hy - r);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
            return -outside;
        }

        private static float Band(float d, float from, float to)
        {
            return Mathf.Clamp01(d - from + 0.5f) * Mathf.Clamp01(to - d + 0.5f);
        }

        /// <summary>Vertical fade: opaque at the bottom, transparent at the top (rotate / flip for other edges).</summary>
        public static Sprite Fade()
        {
            if (fade != null) return fade;
            const int n = 64;
            Texture2D t = Tex(4, n, false);
            var px = new Color32[4 * n];
            for (int y = 0; y < n; y++)
            {
                float a = 1f - y / (n - 1f);
                a = a * a * (3f - 2f * a);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            fade = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            fade.name = "Fade";
            return fade;
        }

        private static Sprite fadeDown;

        /// <summary>Vertical fade: opaque at the top, transparent at the bottom.</summary>
        public static Sprite FadeDown()
        {
            if (fadeDown != null) return fadeDown;
            const int n = 64;
            Texture2D t = Tex(4, n, false);
            var px = new Color32[4 * n];
            for (int y = 0; y < n; y++)
            {
                float a = y / (n - 1f);
                a = a * a * (3f - 2f * a);
                for (int x = 0; x < 4; x++) px[y * 4 + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            fadeDown = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            fadeDown.name = "FadeDown";
            return fadeDown;
        }

        private static Sprite fadeRight;

        /// <summary>Horizontal fade: opaque on the left, transparent on the right.</summary>
        public static Sprite FadeRight()
        {
            if (fadeRight != null) return fadeRight;
            const int n = 64;
            Texture2D t = Tex(n, 4, false);
            var px = new Color32[n * 4];
            for (int x = 0; x < n; x++)
            {
                float a = 1f - x / (n - 1f);
                a = a * a * (3f - 2f * a);
                for (int y = 0; y < 4; y++) px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
            fadeRight = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            fadeRight.name = "FadeRight";
            return fadeRight;
        }

        /// <summary>A small diamond ornament (for rules under headings).</summary>
        public static Sprite Diamond()
        {
            if (diamond != null) return diamond;
            const int n = 32;
            Texture2D t = Tex(n, n, false);
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Abs(x + 0.5f - n * 0.5f) + Mathf.Abs(y + 0.5f - n * 0.5f);
                    float a = Mathf.Clamp01(n * 0.5f - 1f - d + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            diamond = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.5f));
            return diamond;
        }

        // ---------------------------------------------------------------- the Reaper

        private static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float Cover(float dist) { return Mathf.Clamp01(0.5f - dist); }

        /// <summary>
        /// The Reaper, facing left: hooded head, robe flaring to the floor, a scythe behind him, two green eyes.
        /// 256 x 512, pivot at the feet. Colours: robe near-black violet, a cool rim on the left edge.
        /// </summary>
        public static Sprite ReaperBody()
        {
            if (reaperBody != null) return reaperBody;
            const int w = 256, h = 512;
            Texture2D t = Tex(w, h, false);
            var px = new Color32[w * h];
            Color robe = new Color(0.12f, 0.10f, 0.15f);
            Color rimC = new Color(0.42f, 0.38f, 0.58f);
            Color face = new Color(0.02f, 0.015f, 0.03f);
            Color eye = new Color(0.62f, 1f, 0.78f);
            Color staff = new Color(0.30f, 0.22f, 0.18f);
            Color blade = new Color(0.78f, 0.80f, 0.86f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Color c = new Color(0, 0, 0, 0);

                    // Scythe (behind): staff from bottom-right up to top-left, curved blade at the top
                    float ds = Seg(p, new Vector2(200f, 6f), new Vector2(168f, 492f)) - 4.5f;
                    float bladeR = (p - new Vector2(110f, 470f)).magnitude;
                    float bladeArc = Mathf.Abs(bladeR - 62f) - Mathf.Lerp(2f, 9f, Mathf.Clamp01((p.x - 60f) / 110f));
                    bool inBladeSector = p.y > 470f - 4f && p.x > 52f && p.x < 172f;
                    float aBlade = inBladeSector ? Cover(bladeArc) : 0f;
                    c = Over(c, staff, Cover(ds));
                    c = Over(c, blade, aBlade);

                    // Robe: trapezoid from shoulders (y 330) to the floor, flared, rounded shoulders
                    float yy = p.y;
                    float halfW = Mathf.Lerp(108f, 62f, Mathf.Clamp01(yy / 330f));
                    float cx = 118f + Mathf.Sin(yy * 0.02f) * 3f;
                    float robeD = Mathf.Abs(p.x - cx) - halfW;
                    float robeA = yy < 336f ? Cover(robeD) : 0f;
                    // hem: wavy bottom
                    float hem = 6f + Mathf.Sin(p.x * 0.09f) * 4f;
                    robeA *= Mathf.Clamp01(yy - hem + 0.5f);
                    // shoulders + hood (ellipse)
                    float hoodD = new Vector2((p.x - 112f) / 70f, (p.y - 372f) / 78f).magnitude - 1f;
                    float hoodA = Cover(hoodD * 70f);
                    // hood point at the back (right)
                    float tipD = Seg(p, new Vector2(150f, 420f), new Vector2(178f, 360f)) - 14f;
                    float body = Mathf.Max(robeA, Mathf.Max(hoodA, Cover(tipD)));
                    if (body > 0f)
                    {
                        // cool rim light on the left edge
                        float edgeDist = Mathf.Min(-robeD, -hoodD * 70f);
                        float rim = Mathf.Clamp01(1f - Mathf.Max(0f, Mathf.Max(edgeDist, 0f)) / 5f) * (p.x < 118f ? 1f : 0.2f);
                        // fold lines
                        float fold = Mathf.Abs(Mathf.Sin((p.x - cx) * 0.11f + yy * 0.004f));
                        Color rc = Color.Lerp(robe, robe * 0.7f, (1f - fold) * 0.6f * Mathf.Clamp01((330f - yy) / 200f));
                        rc = Color.Lerp(rc, rimC, rim * 0.8f);
                        rc.a = 1f;
                        c = Over(c, rc, body);
                    }
                    // Face hollow (facing left) and eyes
                    float faceD = new Vector2((p.x - 96f) / 40f, (p.y - 366f) / 48f).magnitude - 1f;
                    c = Over(c, face, Cover(faceD * 40f));
                    float e1 = (p - new Vector2(82f, 372f)).magnitude - 5.5f;
                    float e2 = (p - new Vector2(106f, 372f)).magnitude - 5.5f;
                    float eyeA = Mathf.Max(Cover(e1), Cover(e2));
                    float glowA = Mathf.Clamp01(1f - Mathf.Min(e1, e2) / 12f) * 0.35f * Cover(faceD * 40f);
                    c = Over(c, eye, Mathf.Max(eyeA, glowA));
                    px[y * w + x] = c;
                }
            }
            reaperBody = Make(t, px, Vector4.zero, new Vector2(0.5f, 0f));
            reaperBody.name = "ReaperBody";
            return reaperBody;
        }

        /// <summary>The stamping arm: sleeve, bony hand, a rubber stamp. 128 x 256, pivot at the shoulder (top).</summary>
        public static Sprite ReaperArm()
        {
            if (reaperArm != null) return reaperArm;
            const int w = 128, h = 256;
            Texture2D t = Tex(w, h, false);
            var px = new Color32[w * h];
            Color sleeve = new Color(0.14f, 0.12f, 0.18f);
            Color bone = new Color(0.9f, 0.88f, 0.8f);
            Color wood = new Color(0.55f, 0.36f, 0.22f);
            Color rubber = new Color(0.62f, 0.14f, 0.12f);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    Color c = new Color(0, 0, 0, 0);
                    // sleeve: tapered capsule from the shoulder (64, 240) down to the wrist (64, 120)
                    float wid = Mathf.Lerp(18f, 30f, Mathf.Clamp01((240f - p.y) / 120f));
                    float sd = Seg(p, new Vector2(64f, 236f), new Vector2(64f, 124f)) - wid;
                    c = Over(c, sleeve, Cover(sd));
                    // hand: knuckles gripping the handle
                    float hd = Seg(p, new Vector2(56f, 112f), new Vector2(74f, 112f)) - 10f;
                    // stamp: handle knob, neck, block, red rubber
                    float knob = (p - new Vector2(64f, 104f)).magnitude - 12f;
                    float neck = Seg(p, new Vector2(64f, 100f), new Vector2(64f, 60f)) - 7f;
                    float block = BoxD(p, new Vector2(64f, 46f), new Vector2(36f, 12f));
                    float rub = BoxD(p, new Vector2(64f, 28f), new Vector2(34f, 6f));
                    c = Over(c, wood, Cover(knob));
                    c = Over(c, wood, Cover(neck));
                    c = Over(c, wood * 0.85f + new Color(0, 0, 0, 0.15f), Cover(block));
                    c = Over(c, rubber, Cover(rub));
                    c = Over(c, bone, Cover(hd));
                    px[y * w + x] = c;
                }
            }
            reaperArm = Make(t, px, Vector4.zero, new Vector2(0.5f, 0.94f));
            reaperArm.name = "ReaperArm";
            return reaperArm;
        }

        private static float BoxD(Vector2 p, Vector2 c, Vector2 half)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - c.x) - half.x, Mathf.Abs(p.y - c.y) - half.y);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f);
        }

        private static Color Over(Color dst, Color src, float a)
        {
            a = Mathf.Clamp01(a) * (src.a > 0f ? src.a : 1f);
            if (a <= 0f) return dst;
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) return new Color(0, 0, 0, 0);
            Color o = (src * a + dst * dst.a * (1f - a)) / outA;
            o.a = outA;
            return o;
        }
    }
}
