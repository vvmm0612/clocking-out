using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// The six greybox dioramas, one method per level. Every anchor listed in the level JSON is created
    /// here as a named node (its position = where a figure stands / where effects happen) with its
    /// visuals as children. Coordinates are world units; +Z points away from the camera.
    /// </summary>
    public static class StageLayouts
    {
        private static Color C(float r, float g, float b) { return new Color(r, g, b, 1f); }
        private static Color CA(float r, float g, float b, float a) { return new Color(r, g, b, a); }

        public static StagePalette PaletteFor(string levelId)
        {
            var p = new StagePalette();
            switch (levelId)
            {
                case "level_01":
                    p.sky = C(0.96f, 0.80f, 0.68f); p.ambient = C(0.62f, 0.55f, 0.55f); p.sun = C(1f, 0.88f, 0.74f);
                    p.ground = C(0.56f, 0.57f, 0.56f); p.a = C(0.94f, 0.92f, 0.88f); p.b = C(0.56f, 0.41f, 0.31f);
                    p.c = C(0.62f, 0.70f, 0.52f); p.accent = C(0.82f, 0.38f, 0.33f); p.dark = C(0.32f, 0.33f, 0.36f);
                    p.water = CA(0.28f, 0.52f, 0.68f, 0.72f); p.glow = C(1f, 0.86f, 0.45f);
                    break;
                case "level_02":
                    p.sky = C(0.74f, 0.86f, 0.86f); p.ambient = C(0.58f, 0.62f, 0.62f); p.sun = C(1f, 0.96f, 0.86f);
                    p.ground = C(0.60f, 0.72f, 0.50f); p.a = C(0.78f, 0.74f, 0.66f); p.b = C(0.58f, 0.43f, 0.30f);
                    p.c = C(0.45f, 0.52f, 0.62f); p.accent = C(0.75f, 0.45f, 0.30f); p.dark = C(0.30f, 0.30f, 0.32f);
                    p.water = CA(0.33f, 0.58f, 0.74f, 0.7f);
                    break;
                case "level_03":
                    p.sky = C(0.30f, 0.32f, 0.48f); p.ambient = C(0.46f, 0.45f, 0.55f); p.sun = C(0.80f, 0.82f, 0.98f);
                    p.ground = C(0.55f, 0.52f, 0.50f); p.a = C(0.92f, 0.86f, 0.76f); p.b = C(0.60f, 0.42f, 0.30f);
                    p.c = C(0.72f, 0.42f, 0.36f); p.accent = C(0.95f, 0.70f, 0.40f); p.dark = C(0.28f, 0.24f, 0.24f);
                    p.water = CA(0.75f, 0.78f, 0.80f, 0.8f); p.player = C(0.86f, 0.82f, 0.92f);
                    break;
                case "level_04":
                    p.sky = C(0.20f, 0.24f, 0.36f); p.ambient = C(0.40f, 0.43f, 0.55f); p.sun = C(0.72f, 0.78f, 1f);
                    p.ground = C(0.45f, 0.44f, 0.42f); p.a = C(0.70f, 0.66f, 0.60f); p.b = C(0.52f, 0.38f, 0.30f);
                    p.c = C(0.38f, 0.55f, 0.45f); p.accent = C(0.80f, 0.30f, 0.28f); p.dark = C(0.28f, 0.28f, 0.31f);
                    p.water = CA(0.30f, 0.50f, 0.75f, 0.8f); p.player = C(0.92f, 0.86f, 0.62f);
                    break;
                case "level_05":
                    p.sky = C(0.18f, 0.20f, 0.32f); p.ambient = C(0.44f, 0.43f, 0.52f); p.sun = C(0.85f, 0.85f, 1f);
                    p.ground = C(0.50f, 0.48f, 0.46f); p.a = C(0.70f, 0.66f, 0.60f); p.b = C(0.50f, 0.36f, 0.26f);
                    p.c = C(0.85f, 0.68f, 0.36f); p.accent = C(0.40f, 0.45f, 0.60f); p.dark = C(0.25f, 0.24f, 0.26f);
                    p.water = CA(0.25f, 0.45f, 0.60f, 0.75f); p.player = C(0.90f, 0.84f, 0.74f);
                    break;
                case "level_06":
                    p.sky = C(0.12f, 0.11f, 0.15f); p.ambient = C(0.36f, 0.34f, 0.42f); p.sun = C(0.85f, 0.80f, 0.95f);
                    p.ground = C(0.32f, 0.30f, 0.36f); p.a = C(0.45f, 0.42f, 0.50f); p.b = C(0.40f, 0.28f, 0.24f);
                    p.c = C(0.85f, 0.82f, 0.74f); p.accent = C(0.45f, 0.65f, 0.50f); p.dark = C(0.16f, 0.15f, 0.18f);
                    p.player = C(0.16f, 0.15f, 0.19f);
                    p.peekSky = C(0.10f, 0.08f, 0.16f);
                    break;
            }
            return p;
        }

        /// <summary>Builds the diorama for a level id. Returns false for unknown ids (caller adds a fallback).</summary>
        public static bool Build(string levelId, StageKit k)
        {
            switch (levelId)
            {
                case "level_01": Lighthouse(k); return true;
                case "level_02": Mill(k); return true;
                case "level_03": Bakery(k); return true;
                case "level_04": Railway(k); return true;
                case "level_05": ClockTower(k); return true;
                case "level_06": Office(k); return true;
            }
            return false;
        }

        // =============================================================== helpers

        private static void Frame(Stage s, Vector3 focus, float distance, float pitch, float yaw)
        {
            s.camFocus = focus;
            s.camDistance = distance;
            s.camPitch = pitch;
            s.camYaw = yaw;
        }

        private static void HitBox(AnchorTag t, Vector3 worldCenter, Vector3 size)
        {
            BoxCollider bc = t.gameObject.AddComponent<BoxCollider>();
            bc.center = t.transform.InverseTransformPoint(worldCenter);
            bc.size = size;
        }

        private static void Tree(StageKit k, Vector3 foot, float h)
        {
            k.Cyl(null, foot + Vector3.up * (h * 0.3f), 0.18f, h * 0.6f, k.pal.b);
            k.Ball(null, foot + Vector3.up * (h * 0.75f), h * 0.55f, Color.Lerp(k.pal.c, k.pal.ground, 0.4f));
        }

        /// <summary>A flat gear (disc + teeth) on a pivot facing the camera.</summary>
        private static Transform Gear(StageKit k, Transform parent, Vector3 center, float d, Color c, int teeth)
        {
            Transform pivot = k.Pivot(parent, "gear", center);
            k.CylR(pivot, center, d, 0.14f, new Vector3(90f, 0f, 0f), c);
            k.CylR(pivot, center + new Vector3(0f, 0f, -0.08f), d * 0.25f, 0.1f, new Vector3(90f, 0f, 0f), Color.Lerp(c, Color.black, 0.35f));
            for (int i = 0; i < teeth; i++)
            {
                float ang = i * 360f / teeth;
                Vector3 dir = Quaternion.Euler(0f, 0f, ang) * Vector3.up;
                k.BoxR(pivot, center + dir * (d * 0.5f + 0.04f), new Vector3(0.1f, 0.14f, 0.12f), new Vector3(0f, 0f, ang), c);
            }
            return pivot;
        }

        // =============================================================== level 1

        private static void Lighthouse(StageKit k)
        {
            StagePalette p = k.pal;
            Color sand = C(0.93f, 0.84f, 0.64f);
            Frame(k.stage, new Vector3(0.2f, 2.9f, 0.6f), 17f, 18f, -18f);

            // Island and sea
            k.Plinth(new Vector3(0f, -0.5f, 0.5f), new Vector3(8f, 1f, 6f), p.c, p.ground);
            k.Box(null, new Vector3(0f, -1.3f, 0.5f), new Vector3(17f, 0.3f, 13f), C(0.20f, 0.30f, 0.40f));
            k.Water(null, new Vector3(0f, -0.4f, 0.5f), new Vector3(17f, 0.6f, 13f), p.water);
            k.Ball(null, new Vector3(3.5f, -0.1f, -1.9f), 0.9f, p.dark);
            k.Ball(null, new Vector3(-3.3f, -0.15f, -2.1f), 0.7f, p.dark);
            k.Ball(null, new Vector3(3.7f, -0.1f, 2.9f), 1.1f, p.dark);
            k.Ball(null, new Vector3(-2.6f, -0.2f, 3.2f), 0.8f, p.dark);

            // Keeper's cottage
            k.Box(null, new Vector3(-1.6f, 0.6f, 1.9f), new Vector3(2f, 1.2f, 1.6f), p.a);
            k.BoxR(null, new Vector3(-1.6f, 1.45f, 2.25f), new Vector3(2.2f, 0.12f, 1.25f), new Vector3(-38f, 0f, 0f), p.accent);
            k.Box(null, new Vector3(-1.6f, 0.4f, 1.09f), new Vector3(0.35f, 0.8f, 0.04f), p.b);
            k.GlowBox(null, new Vector3(-2.2f, 0.75f, 1.09f), new Vector3(0.3f, 0.3f, 0.03f), p.glow);

            // Beach
            AnchorTag beach = k.Anchor("beach", new Vector3(-4.9f, -0.12f, 1.0f));
            k.BoxR(beach.transform, new Vector3(-4.6f, -0.15f, 1.2f), new Vector3(2.4f, 0.2f, 3.0f), new Vector3(0f, 0f, 9f), sand);
            k.Ball(beach.transform, new Vector3(-4.2f, 0.0f, 2.2f), 0.3f, p.dark);
            beach.drownHop = new Vector3(-0.5f, -0.15f, 0f);

            // Dock
            AnchorTag dock = k.Anchor("dock", new Vector3(-1.4f, 0.11f, -4.5f));
            k.Box(dock.transform, new Vector3(-1.4f, 0.05f, -3.6f), new Vector3(1.0f, 0.12f, 2.6f), p.b);
            for (int i = 0; i < 4; i++)
            {
                float x = (i % 2 == 0) ? -1.85f : -0.95f;
                float z = (i < 2) ? -4.8f : -3.4f;
                k.Cyl(dock.transform, new Vector3(x, -0.4f, z), 0.14f, 1.0f, p.b);
            }
            k.Cyl(dock.transform, new Vector3(-1.0f, 0.3f, -4.7f), 0.2f, 0.35f, p.dark);
            dock.drownHop = new Vector3(0f, -0.3f, -0.8f);

            // Tower
            Vector3 t = new Vector3(2f, 0f, 1.5f);
            k.Cyl(null, t + new Vector3(0f, 3f, 0f), 1.6f, 6f, p.a);
            k.Cyl(null, t + new Vector3(0f, 1.5f, 0f), 1.63f, 0.6f, p.accent);
            k.Cyl(null, t + new Vector3(0f, 4.3f, 0f), 1.63f, 0.6f, p.accent);
            k.Box(null, t + new Vector3(0f, 0.45f, -0.79f), new Vector3(0.4f, 0.9f, 0.05f), p.b);
            k.Box(null, t + new Vector3(0f, 2.8f, -0.79f), new Vector3(0.2f, 0.35f, 0.04f), p.dark);
            k.GlassCyl(null, t + new Vector3(0f, 6.72f, 0f), 1.3f, 1.2f, CA(0.85f, 0.95f, 1f, 0.22f));
            k.Cyl(null, t + new Vector3(0f, 7.4f, 0f), 1.5f, 0.2f, p.accent);
            k.Ball(null, t + new Vector3(0f, 7.55f, 0f), 0.4f, p.accent);

            AnchorTag gallery = k.Anchor("gallery", new Vector3(2f, 6.12f, 0.35f));
            k.Cyl(gallery.transform, t + new Vector3(0f, 6.05f, 0f), 2.5f, 0.12f, p.dark);
            for (int i = 0; i < 10; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, i * 36f, 0f) * Vector3.forward;
                k.Box(gallery.transform, t + dir * 1.18f + new Vector3(0f, 6.3f, 0f), new Vector3(0.05f, 0.4f, 0.05f), p.dark);
            }
            gallery.hasFallLanding = true;
            gallery.fallLanding = new Vector3(2.3f, 0f, -0.9f);

            AnchorTag lamp = k.Anchor("lamp", new Vector3(2f, 6.12f, 1.2f));
            k.Cyl(lamp.transform, t + new Vector3(0f, 6.3f, 0.15f), 0.35f, 0.35f, p.dark);
            GameObject lens = k.Ball(lamp.transform, t + new Vector3(0f, 6.72f, 0.15f), 0.55f, C(0.72f, 0.70f, 0.6f));
            Transform beamPivot = k.Pivot(lamp.transform, "beam", t + new Vector3(0f, 6.72f, 0.15f));
            GameObject beam = k.Water(beamPivot, t + new Vector3(-2.4f, 6.72f, 0.15f), new Vector3(4.4f, 0.35f, 0.35f), CA(1f, 0.92f, 0.6f, 0.28f));
            beam.SetActive(false);
            Light lampLight = k.Point(lamp.transform, t + new Vector3(0f, 6.9f, -0.3f), p.glow, 14f, 4f);
            lampLight.gameObject.SetActive(false);
            lamp.AddSpin(beamPivot, Vector3.up, 1f);
            lamp.onLight = delegate
            {
                if (lens != null) lens.GetComponent<Renderer>().sharedMaterial = Mats.Glow(p.glow);
                if (lampLight != null) lampLight.gameObject.SetActive(true);
                if (beam != null) beam.SetActive(true);
                lamp.SetSpin(0.8f);
            };
            lamp.fxOffset = new Vector3(0f, 0.6f, 0.4f);

            AnchorTag lantern = k.Anchor("lantern", new Vector3(1.52f, 6.12f, 1.0f));
            k.Box(lantern.transform, new Vector3(1.52f, 6.17f, 1.0f), new Vector3(0.26f, 0.06f, 0.26f), p.dark);
            k.GlowBox(lantern.transform, new Vector3(1.52f, 6.34f, 1.0f), new Vector3(0.2f, 0.3f, 0.2f), C(1f, 0.62f, 0.25f));
            k.Box(lantern.transform, new Vector3(1.52f, 6.53f, 1.0f), new Vector3(0.24f, 0.06f, 0.24f), p.dark);
            k.Point(lantern.transform, new Vector3(1.52f, 6.5f, 0.8f), C(1f, 0.6f, 0.3f), 2.5f, 1.5f);
            lantern.fxOffset = new Vector3(0f, 0.3f, 0f);
        }

        // =============================================================== level 2

        private static void Mill(StageKit k)
        {
            StagePalette p = k.pal;
            Frame(k.stage, new Vector3(0.3f, 1.4f, 1.3f), 17.5f, 30f, -12f);
            k.Plinth(new Vector3(0f, -0.25f, 0.8f), new Vector3(12.5f, 0.5f, 6.6f), p.ground, Color.Lerp(p.ground, p.dark, 0.4f));

            // Pond basin (x -5.5..-1.5, z 1.2..3.8)
            k.Box(null, new Vector3(-3.5f, 0.8f, 3.8f), new Vector3(4.2f, 1.6f, 0.2f), p.a);
            k.Box(null, new Vector3(-5.5f, 0.8f, 2.5f), new Vector3(0.2f, 1.6f, 2.8f), p.a);
            k.Box(null, new Vector3(-3.6f, 0.45f, 1.2f), new Vector3(4.0f, 0.9f, 0.2f), p.a);
            k.Box(null, new Vector3(-1.5f, 0.8f, 1.325f), new Vector3(0.2f, 1.6f, 0.25f), p.a);
            k.Box(null, new Vector3(-1.5f, 0.8f, 2.875f), new Vector3(0.2f, 1.6f, 1.85f), p.a);
            k.Box(null, new Vector3(-3.5f, 0.02f, 2.5f), new Vector3(3.8f, 0.04f, 2.4f), Color.Lerp(p.dark, p.water, 0.3f));
            GameObject pondWater = k.Water(null, new Vector3(-3.5f, 0.2f, 2.5f), new Vector3(3.8f, 0.4f, 2.4f), p.water);
            AnchorTag pond = k.Anchor("pond", new Vector3(-2.1f, 0f, 2.0f));

            AnchorTag gauge = k.Anchor("gauge", new Vector3(-2.3f, 0f, 1.45f));
            k.Box(gauge.transform, new Vector3(-2.3f, 1.0f, 1.45f), new Vector3(0.1f, 2.0f, 0.06f), C(0.95f, 0.94f, 0.9f));
            for (int i = 0; i < 4; i++)
            {
                k.Box(gauge.transform, new Vector3(-2.3f, 0.3f + i * 0.45f, 1.41f), new Vector3(0.16f, 0.04f, 0.02f), p.accent);
            }
            GameObject marker = k.GlowBox(gauge.transform, new Vector3(-2.3f, 0.4f, 1.38f), new Vector3(0.22f, 0.08f, 0.04f), C(0.3f, 0.75f, 1f));

            k.stage.RegisterState("water_level", "low", delegate (string v)
            {
                float top = (v == "high") ? 1.4f : (v == "mid" || v == "medium") ? 0.9f : 0.4f;
                if (pondWater != null)
                {
                    pondWater.transform.position = new Vector3(-3.5f, top * 0.5f, 2.5f);
                    pondWater.transform.localScale = new Vector3(3.8f, top, 2.4f);
                }
                if (marker != null) marker.transform.position = new Vector3(-2.3f, top, 1.38f);
                if (pond != null) pond.transform.position = new Vector3(-2.1f, Mathf.Max(0f, top - 0.45f), 2.0f);
            });

            // Sluice gate, catwalk and lever
            AnchorTag gate = k.Anchor("gate", new Vector3(-1.5f, 0.6f, 1.7f));
            k.Box(gate.transform, new Vector3(-1.5f, 0.8f, 1.7f), new Vector3(0.12f, 1.5f, 0.5f), Color.Lerp(p.b, p.dark, 0.3f));
            gate.waterDir = new Vector3(1f, -0.35f, 0f);
            gate.fxOffset = new Vector3(0.2f, 0f, 0f);

            AnchorTag sluice = k.Anchor("sluice", new Vector3(-1.2f, 2.26f, 1.55f));
            k.Box(sluice.transform, new Vector3(-1.5f, 2.2f, 1.7f), new Vector3(1.6f, 0.1f, 0.7f), p.b);
            float[] px = { -2.2f, -0.8f };
            float[] pz = { 1.4f, 2.0f };
            for (int i = 0; i < 2; i++)
                for (int j = 0; j < 2; j++)
                    k.Box(sluice.transform, new Vector3(px[i], 1.1f, pz[j]), new Vector3(0.08f, 2.2f, 0.08f), p.b);
            k.Box(sluice.transform, new Vector3(-1.5f, 2.6f, 2.02f), new Vector3(1.6f, 0.05f, 0.05f), p.b);

            AnchorTag lever = k.Anchor("lever", new Vector3(-1.15f, 1.15f, 1.38f));
            k.BoxR(lever.transform, new Vector3(-1.3f, 1.2f, 1.4f), new Vector3(0.08f, 0.7f, 0.08f), new Vector3(0f, 0f, -35f), p.accent);
            k.Ball(lever.transform, new Vector3(-1.1f, 1.48f, 1.4f), 0.14f, p.dark);
            k.Box(lever.transform, new Vector3(-1.42f, 0.95f, 1.4f), new Vector3(0.14f, 0.14f, 0.14f), p.dark);

            // Flume to the wheel
            k.Box(null, new Vector3(-0.7f, 0.55f, 1.7f), new Vector3(1.5f, 0.08f, 0.5f), p.b);
            k.Box(null, new Vector3(-0.7f, 0.68f, 1.47f), new Vector3(1.5f, 0.25f, 0.05f), p.b);
            k.Box(null, new Vector3(-0.7f, 0.68f, 1.93f), new Vector3(1.5f, 0.25f, 0.05f), p.b);

            // Wheel and wheel pit
            AnchorTag pit = k.Anchor("pit", new Vector3(0.35f, 0.04f, 1.05f));
            k.Box(pit.transform, new Vector3(0.9f, 0.02f, 1.7f), new Vector3(2.6f, 0.04f, 1.5f), Color.Lerp(p.a, p.dark, 0.45f));
            k.Box(pit.transform, new Vector3(0.9f, 0.18f, 0.9f), new Vector3(2.6f, 0.36f, 0.15f), p.a);
            k.Box(pit.transform, new Vector3(0.9f, 0.45f, 2.5f), new Vector3(2.6f, 0.9f, 0.15f), p.a);

            AnchorTag wheel = k.Anchor("wheel", new Vector3(0.9f, 0.1f, 1.2f));
            Vector3 hub = new Vector3(0.9f, 1.4f, 1.7f);
            Transform wp = k.Pivot(wheel.transform, "wheelPivot", hub);
            k.CylR(wp, hub, 2.2f, 0.3f, new Vector3(90f, 0f, 0f), p.b);
            k.CylR(wp, hub + new Vector3(0f, 0f, -0.17f), 0.4f, 0.1f, new Vector3(90f, 0f, 0f), p.dark);
            for (int i = 0; i < 10; i++)
            {
                float ang = i * 36f;
                Vector3 dir = Quaternion.Euler(0f, 0f, ang) * Vector3.up;
                k.BoxR(wp, hub + dir * 1.15f, new Vector3(0.12f, 0.35f, 0.5f), new Vector3(0f, 0f, ang), Color.Lerp(p.b, p.dark, 0.25f));
            }
            k.CylR(null, new Vector3(1.55f, 1.4f, 1.7f), 0.15f, 1.3f, new Vector3(0f, 0f, 90f), p.dark);
            wheel.AddSpin(wp, Vector3.forward, -1f);
            wheel.fxOffset = new Vector3(0f, 1.3f, 0.5f);

            // Mill house (cutaway: no front wall)
            Vector3 m = new Vector3(3.7f, 0f, 1.7f);
            k.Box(null, m + new Vector3(0f, 1.8f, 1.5f), new Vector3(3f, 3.6f, 0.2f), p.a);
            k.Box(null, m + new Vector3(-1.5f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 3f), p.a);
            k.Box(null, m + new Vector3(1.5f, 1.8f, 0f), new Vector3(0.2f, 3.6f, 3f), p.a);
            k.BoxR(null, m + new Vector3(0f, 4.15f, 0.85f), new Vector3(3.4f, 0.15f, 1.9f), new Vector3(-32f, 0f, 0f), p.c);
            k.Box(null, m + new Vector3(0f, 2.3f, 0f), new Vector3(2.8f, 0.12f, 2.8f), p.b);
            for (int i = 0; i < 3; i++)
            {
                GameObject sack = k.Ball(null, new Vector3(2.7f + i * 0.35f, 0.22f, 1.0f + (i % 2) * 0.3f), 0.5f, C(0.92f, 0.88f, 0.78f));
                sack.transform.localScale = new Vector3(0.6f, 0.4f, 0.5f);
            }
            AnchorTag loft = k.Anchor("loft", new Vector3(4.2f, 2.36f, 0.9f));
            k.Box(loft.transform, new Vector3(4.6f, 2.6f, 2.6f), new Vector3(0.6f, 0.5f, 0.5f), p.b);
            GameObject sack2 = k.Ball(loft.transform, new Vector3(3.9f, 2.6f, 2.5f), 0.5f, C(0.92f, 0.88f, 0.78f));
            sack2.transform.localScale = new Vector3(0.5f, 0.45f, 0.45f);

            AnchorTag trapdoor = k.Anchor("trapdoor", new Vector3(2.95f, 2.37f, 1.1f));
            k.Box(trapdoor.transform, new Vector3(2.95f, 2.37f, 1.1f), new Vector3(0.65f, 0.03f, 0.65f), Color.Lerp(p.b, p.dark, 0.5f));

            AnchorTag sign = k.Anchor("sign", new Vector3(4.5f, 2.36f, 2.9f));
            k.Paper(sign.transform, new Vector3(4.5f, 3.05f, 3.08f), new Vector3(0.7f, 0.45f, 0.04f), Vector3.zero);

            // Dam notice by the pond
            AnchorTag notice = k.Anchor("dam_notice", new Vector3(-5.0f, 0f, 0.4f));
            k.Signpost(notice.transform, new Vector3(-5.0f, 0f, 0.4f), 1.1f, new Vector3(0.75f, 0.5f, 0.05f), 0f);

            Tree(k, new Vector3(5.6f, 0f, -1.6f), 2.2f);
            Tree(k, new Vector3(-5.4f, 0f, -1.9f), 1.8f);
            Tree(k, new Vector3(-0.2f, 0f, 3.6f), 2.0f);
        }

        // =============================================================== level 3

        private static void Bakery(StageKit k)
        {
            StagePalette p = k.pal;
            Frame(k.stage, new Vector3(0.3f, 1.9f, 1.4f), 15f, 24f, -12f);
            k.Plinth(new Vector3(0.5f, -0.2f, 1.0f), new Vector3(12f, 0.4f, 6.5f), p.ground, Color.Lerp(p.ground, p.dark, 0.4f));
            k.Box(null, new Vector3(-0.5f, 0.03f, 1.5f), new Vector3(8.5f, 0.06f, 3.4f), p.b);

            // Walls
            k.Box(null, new Vector3(-2.375f, 1.5f, 3.2f), new Vector3(4.75f, 3f, 0.2f), p.c);
            k.Box(null, new Vector3(1.875f, 2.6f, 3.2f), new Vector3(3.75f, 5.2f, 0.2f), p.a);
            k.Box(null, new Vector3(-4.75f, 1.5f, 1.5f), new Vector3(0.2f, 3f, 3.4f), p.c);
            k.Box(null, new Vector3(3.75f, 2.6f, 2.1f), new Vector3(0.2f, 5.2f, 2.2f), p.a);
            k.Box(null, new Vector3(3.75f, 3.7f, 0.4f), new Vector3(0.2f, 3.0f, 1.2f), p.a);
            k.Box(null, new Vector3(0f, 1.5f, 2.4f), new Vector3(0.2f, 3f, 1.6f), p.a);
            k.Box(null, new Vector3(0f, 1.5f, 0.2f), new Vector3(0.2f, 3f, 0.8f), p.a);
            k.Box(null, new Vector3(0f, 2.5f, 1.1f), new Vector3(0.2f, 1f, 1.0f), p.a);
            k.BoxR(null, new Vector3(1.875f, 5.35f, 2.4f), new Vector3(4.0f, 0.15f, 1.9f), new Vector3(-30f, 0f, 0f), p.dark);

            // Oven room
            AnchorTag oven = k.Anchor("oven", new Vector3(-2.9f, 0.06f, 1.0f));
            k.Box(oven.transform, new Vector3(-3.0f, 0.9f, 2.35f), new Vector3(2.0f, 1.8f, 1.3f), p.c);
            k.Box(oven.transform, new Vector3(-3.0f, 1.9f, 2.35f), new Vector3(1.7f, 0.25f, 1.1f), Color.Lerp(p.c, p.dark, 0.3f));
            k.Cyl(oven.transform, new Vector3(-3.5f, 2.9f, 2.6f), 0.4f, 2.2f, Color.Lerp(p.c, p.dark, 0.3f));
            k.GlowBox(oven.transform, new Vector3(-3.0f, 0.75f, 1.69f), new Vector3(0.9f, 0.5f, 0.04f), C(1f, 0.55f, 0.2f));
            k.Point(oven.transform, new Vector3(-3.0f, 0.9f, 1.3f), C(1f, 0.6f, 0.3f), 3.5f, 1.6f);
            oven.fxOffset = new Vector3(-0.1f, 0.75f, 0.6f);

            AnchorTag vat = k.Anchor("vat", new Vector3(-1.2f, 0.06f, 1.2f));
            k.Cyl(vat.transform, new Vector3(-1.2f, 0.45f, 1.95f), 1.0f, 0.9f, p.b);
            k.GlassCyl(vat.transform, new Vector3(-1.2f, 0.9f, 1.95f), 0.86f, 0.04f, p.water);
            vat.drownHop = new Vector3(0f, 0.85f, 0.75f);
            vat.fxOffset = new Vector3(0f, 0.95f, 0.75f);

            AnchorTag roster = k.Anchor("roster", new Vector3(-1.9f, 0.06f, 2.9f));
            k.Paper(roster.transform, new Vector3(-1.9f, 1.8f, 3.08f), new Vector3(0.7f, 0.9f, 0.04f), Vector3.zero);

            AnchorTag door = k.Anchor("door", new Vector3(0f, 0.06f, 1.1f));
            k.BoxR(door.transform, new Vector3(0.32f, 1.0f, 0.75f), new Vector3(0.06f, 2.0f, 0.9f), new Vector3(0f, -55f, 0f), Color.Lerp(p.b, p.dark, 0.3f));
            door.fxOffset = new Vector3(0f, 0.9f, 0f);

            // Storeroom
            AnchorTag store = k.Anchor("store", new Vector3(1.6f, 0.06f, 1.0f));
            k.Box(store.transform, new Vector3(2.0f, 1.0f, 2.95f), new Vector3(2.6f, 0.06f, 0.4f), p.b);
            k.Box(store.transform, new Vector3(2.0f, 1.8f, 2.95f), new Vector3(2.6f, 0.06f, 0.4f), p.b);
            for (int i = 0; i < 4; i++)
            {
                GameObject sack = k.Ball(store.transform, new Vector3(1.0f + i * 0.55f, 1.2f, 2.95f), 0.4f, C(0.93f, 0.9f, 0.8f));
                sack.transform.localScale = new Vector3(0.45f, 0.35f, 0.35f);
            }
            AnchorTag ledger = k.Anchor("ledger", new Vector3(2.8f, 0.06f, 2.0f));
            k.Box(ledger.transform, new Vector3(2.8f, 0.4f, 2.3f), new Vector3(0.9f, 0.8f, 0.6f), p.b);
            k.Box(ledger.transform, new Vector3(2.8f, 0.84f, 2.25f), new Vector3(0.5f, 0.09f, 0.36f), C(0.55f, 0.18f, 0.2f));
            ledger.fxOffset = new Vector3(0f, 0.9f, 0.25f);

            AnchorTag sign = k.Anchor("sign", new Vector3(0.75f, 0.06f, 2.9f));
            k.Paper(sign.transform, new Vector3(0.75f, 2.3f, 3.08f), new Vector3(0.6f, 0.4f, 0.04f), Vector3.zero);

            // Attic
            k.Box(null, new Vector3(1.875f, 3.05f, 1.5f), new Vector3(3.75f, 0.12f, 3.4f), p.b);
            AnchorTag attic = k.Anchor("attic", new Vector3(2.7f, 3.12f, 1.2f));
            k.Box(attic.transform, new Vector3(3.0f, 3.4f, 2.7f), new Vector3(0.6f, 0.6f, 0.6f), p.b);
            k.Box(attic.transform, new Vector3(2.4f, 3.3f, 2.8f), new Vector3(0.5f, 0.4f, 0.5f), Color.Lerp(p.b, p.a, 0.3f));
            AnchorTag hatch = k.Anchor("hatch", new Vector3(1.6f, 3.12f, 1.0f));
            k.Box(hatch.transform, new Vector3(1.6f, 3.12f, 1.0f), new Vector3(0.7f, 0.03f, 0.7f), Color.Lerp(p.b, p.dark, 0.55f));
            k.Box(hatch.transform, new Vector3(1.6f, 3.15f, 0.75f), new Vector3(0.2f, 0.04f, 0.05f), p.dark);

            // Shop front
            AnchorTag shop = k.Anchor("shop", new Vector3(4.7f, 0f, -0.1f));
            k.Box(shop.transform, new Vector3(5.1f, 0.45f, 0.7f), new Vector3(1.2f, 0.9f, 0.6f), p.b);
            k.BoxR(shop.transform, new Vector3(5.1f, 2.1f, 0.5f), new Vector3(1.6f, 0.08f, 1.1f), new Vector3(15f, 0f, 0f), p.accent);
            for (int i = 0; i < 3; i++)
            {
                GameObject loaf = k.Ball(shop.transform, new Vector3(4.75f + i * 0.33f, 0.98f, 0.7f), 0.28f, C(0.85f, 0.6f, 0.3f));
                loaf.transform.localScale = new Vector3(0.3f, 0.16f, 0.2f);
            }
            k.Point(null, new Vector3(4.8f, 1.8f, 0.2f), C(1f, 0.85f, 0.6f), 3f, 0.8f);

            // People (placed per shift by show: facts)
            k.Npc("baker", new Vector3(-2.5f, 0.06f, 1.2f), C(0.96f, 0.95f, 0.92f), C(1f, 1f, 1f), 0.8f);
            k.Npc("apprentice", new Vector3(2.0f, 0.06f, 1.2f), C(0.72f, 0.56f, 0.42f), C(0.45f, 0.32f, 0.25f), 0.62f);

            k.Point(null, new Vector3(1.8f, 2.6f, 1.2f), C(1f, 0.9f, 0.7f), 4f, 0.7f);
            k.Point(null, new Vector3(1.8f, 4.4f, 1.4f), C(0.9f, 0.85f, 1f), 3f, 0.5f);
        }

        // =============================================================== level 4

        private static void Rails(StageKit k, Transform parent, Vector3 from, Vector3 to, StagePalette p)
        {
            Vector3 d = to - from;
            float len = d.magnitude;
            if (len < 0.01f) return;
            Vector3 dir = d / len;
            float yaw = -Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
            Vector3 n = new Vector3(-dir.z, 0f, dir.x);
            Vector3 mid = (from + to) * 0.5f;
            int count = Mathf.Max(1, Mathf.FloorToInt(len / 0.55f));
            for (int i = 0; i <= count; i++)
            {
                Vector3 pos = Vector3.Lerp(from, to, i / (float)count) + Vector3.up * 0.03f;
                k.BoxR(parent, pos, new Vector3(0.22f, 0.05f, 0.95f), new Vector3(0f, yaw, 0f), p.b);
            }
            k.BoxR(parent, mid + n * 0.3f + Vector3.up * 0.09f, new Vector3(len, 0.07f, 0.07f), new Vector3(0f, yaw, 0f), p.dark);
            k.BoxR(parent, mid - n * 0.3f + Vector3.up * 0.09f, new Vector3(len, 0.07f, 0.07f), new Vector3(0f, yaw, 0f), p.dark);
        }

        private static void Railway(StageKit k)
        {
            StagePalette p = k.pal;
            Frame(k.stage, new Vector3(0.6f, 1.2f, 0.6f), 19f, 32f, 12f);
            k.Plinth(new Vector3(-0.75f, -0.2f, 0.5f), new Vector3(10.5f, 0.4f, 7f), p.ground, Color.Lerp(p.ground, p.dark, 0.4f));
            k.Box(null, new Vector3(5.4f, -1.4f, 0.5f), new Vector3(1.8f, 0.2f, 7f), C(0.1f, 0.1f, 0.14f));
            k.Plinth(new Vector3(6.8f, -0.2f, 0.5f), new Vector3(1.0f, 0.4f, 7f), p.ground, Color.Lerp(p.ground, p.dark, 0.4f));
            k.GlowBall(null, new Vector3(-4.5f, 6.5f, 7f), 0.9f, C(0.95f, 0.95f, 0.85f));

            AnchorTag track = k.Anchor("track", new Vector3(2.6f, 0.12f, 0f));
            Rails(k, track.transform, new Vector3(-6f, 0f, 0f), new Vector3(4.5f, 0f, 0f), p);

            AnchorTag bridge = k.Anchor("bridge", new Vector3(4.6f, 0.12f, 0f));
            k.Box(bridge.transform, new Vector3(4.85f, -0.05f, 0f), new Vector3(0.7f, 0.25f, 1.3f), p.dark);
            k.Box(bridge.transform, new Vector3(5.95f, -0.05f, 0f), new Vector3(0.7f, 0.25f, 1.3f), p.dark);
            k.BoxR(bridge.transform, new Vector3(5.3f, -0.25f, 0.3f), new Vector3(0.6f, 0.07f, 0.07f), new Vector3(0f, 0f, -35f), p.dark);
            k.BoxR(bridge.transform, new Vector3(5.5f, -0.35f, -0.3f), new Vector3(0.6f, 0.07f, 0.07f), new Vector3(0f, 0f, 40f), p.dark);
            Rails(k, bridge.transform, new Vector3(6.3f, 0f, 0f), new Vector3(7.2f, 0f, 0f), p);
            bridge.hasPark = true;
            bridge.park = new Vector3(3.3f, 0.12f, 0f);

            AnchorTag siding = k.Anchor("siding", new Vector3(1.0f, 0.12f, 1.8f));
            Rails(k, siding.transform, new Vector3(-2.0f, 0f, 1.8f), new Vector3(4.0f, 0f, 1.8f), p);
            Rails(k, siding.transform, new Vector3(-3.5f, 0f, 0f), new Vector3(-2.0f, 0f, 1.8f), p);
            k.Box(siding.transform, new Vector3(4.1f, 0.3f, 1.8f), new Vector3(0.2f, 0.5f, 0.9f), p.accent);

            // Express (moves along the line)
            AnchorTag express = k.Anchor("express", new Vector3(-5.0f, 0.12f, 0f));
            Color blue = C(0.26f, 0.33f, 0.52f);
            k.CylR(express.transform, new Vector3(-4.6f, 0.62f, 0f), 0.7f, 1.4f, new Vector3(0f, 0f, 90f), blue);
            k.Box(express.transform, new Vector3(-5.6f, 0.75f, 0f), new Vector3(0.8f, 1.05f, 0.9f), blue);
            k.Box(express.transform, new Vector3(-5.6f, 1.33f, 0f), new Vector3(1.0f, 0.1f, 1.0f), p.dark);
            k.Cyl(express.transform, new Vector3(-4.2f, 1.1f, 0f), 0.22f, 0.4f, p.dark);
            k.Box(express.transform, new Vector3(-4.8f, 0.28f, 0f), new Vector3(2.4f, 0.22f, 0.8f), p.accent);
            k.GlowBall(express.transform, new Vector3(-3.88f, 0.66f, 0f), 0.22f, C(1f, 0.95f, 0.7f));
            k.Point(express.transform, new Vector3(-3.5f, 0.7f, 0f), C(1f, 0.9f, 0.6f), 3f, 1.2f);
            express.hasPark = false;
            express.fxOffset = new Vector3(0.6f, 1.0f, 0f);

            // Freight (on the siding after 9:30)
            AnchorTag freight = k.Anchor("freight", new Vector3(1.0f, 0.12f, 1.8f));
            k.Box(freight.transform, new Vector3(1.9f, 0.6f, 1.8f), new Vector3(1.1f, 0.75f, 0.85f), C(0.55f, 0.36f, 0.28f));
            k.Box(freight.transform, new Vector3(3.1f, 0.6f, 1.8f), new Vector3(1.1f, 0.75f, 0.85f), C(0.48f, 0.33f, 0.27f));
            freight.fxOffset = new Vector3(0.8f, 0.9f, 0f);

            // Signal box with gallery over the points lever
            k.Box(null, new Vector3(-3.3f, 1.1f, -2.4f), new Vector3(1.6f, 2.2f, 1.4f), p.c);
            k.Box(null, new Vector3(-3.3f, 2.9f, -2.4f), new Vector3(1.6f, 1.2f, 1.4f), Color.Lerp(p.c, p.a, 0.3f));
            k.GlowBox(null, new Vector3(-3.3f, 3.0f, -1.69f), new Vector3(1.2f, 0.5f, 0.03f), C(1f, 0.85f, 0.55f));
            k.BoxR(null, new Vector3(-3.3f, 3.7f, -2.4f), new Vector3(1.9f, 0.12f, 1.7f), new Vector3(0f, 0f, 0f), p.dark);
            for (int i = 0; i < 5; i++)
            {
                k.Box(null, new Vector3(-2.35f, 0.2f + i * 0.42f, -2.9f + i * 0.2f), new Vector3(0.4f, 0.08f, 0.3f), p.b);
            }
            AnchorTag box = k.Anchor("box", new Vector3(-3.3f, 2.32f, -1.55f));
            k.Box(box.transform, new Vector3(-3.3f, 2.25f, -1.5f), new Vector3(1.6f, 0.1f, 0.5f), p.b);
            k.Box(box.transform, new Vector3(-3.3f, 2.55f, -1.27f), new Vector3(1.6f, 0.05f, 0.05f), p.dark);

            AnchorTag roster = k.Anchor("roster", new Vector3(-2.85f, 0.06f, -1.55f));
            k.Paper(roster.transform, new Vector3(-2.85f, 1.2f, -1.72f), new Vector3(0.5f, 0.6f, 0.04f), Vector3.zero);

            AnchorTag lever = k.Anchor("lever", new Vector3(-3.4f, 0.06f, -0.75f));
            k.Box(lever.transform, new Vector3(-3.4f, 0.1f, -0.75f), new Vector3(0.4f, 0.12f, 0.25f), p.dark);
            k.BoxR(lever.transform, new Vector3(-3.35f, 0.4f, -0.75f), new Vector3(0.07f, 0.6f, 0.07f), new Vector3(0f, 0f, -20f), p.accent);
            lever.fxOffset = new Vector3(0f, 0.4f, 0f);

            // Platform
            k.Box(null, new Vector3(0.9f, 0.3f, -1.5f), new Vector3(4.0f, 0.6f, 1.6f), p.a);
            k.Box(null, new Vector3(0.9f, 0.61f, -0.75f), new Vector3(4.0f, 0.02f, 0.12f), C(0.9f, 0.85f, 0.4f));
            AnchorTag platform = k.Anchor("platform", new Vector3(0.7f, 0.6f, -1.25f));
            platform.hasPark = true;
            platform.park = new Vector3(-0.5f, 0.12f, 0f);
            for (int i = 0; i < 2; i++)
            {
                float x = -0.8f + i * 3.4f;
                k.Box(null, new Vector3(x, 1.5f, -2.15f), new Vector3(0.08f, 1.8f, 0.08f), p.dark);
                k.GlowBox(null, new Vector3(x, 2.45f, -2.15f), new Vector3(0.22f, 0.22f, 0.22f), C(1f, 0.9f, 0.6f));
                k.Point(null, new Vector3(x, 2.3f, -1.9f), C(1f, 0.85f, 0.6f), 3.2f, 1.1f);
            }

            AnchorTag brazier = k.Anchor("brazier", new Vector3(2.35f, 0.6f, -1.45f));
            k.Cyl(brazier.transform, new Vector3(2.35f, 0.95f, -1.45f), 0.4f, 0.3f, p.dark);
            k.Cyl(brazier.transform, new Vector3(2.35f, 0.75f, -1.45f), 0.08f, 0.3f, p.dark);
            k.GlowBox(brazier.transform, new Vector3(2.35f, 1.1f, -1.45f), new Vector3(0.26f, 0.05f, 0.26f), C(1f, 0.55f, 0.2f));
            k.Point(brazier.transform, new Vector3(2.35f, 1.5f, -1.45f), C(1f, 0.55f, 0.25f), 3.5f, 1.8f);
            FxLibrary.Spawn("fire", new Vector3(2.35f, 1.1f, -1.45f), brazier.transform, Vector3.down);

            AnchorTag timetable = k.Anchor("timetable", new Vector3(-0.6f, 0.6f, -2.05f));
            k.Signpost(timetable.transform, new Vector3(-0.6f, 0.6f, -2.05f), 1.0f, new Vector3(0.75f, 0.5f, 0.04f), 0f);

            AnchorTag rulebook = k.Anchor("rulebook", new Vector3(0.3f, 0.6f, -1.95f));
            k.Box(rulebook.transform, new Vector3(0.45f, 0.82f, -2.05f), new Vector3(1.0f, 0.08f, 0.35f), p.b);
            k.Box(rulebook.transform, new Vector3(0.05f, 0.7f, -2.05f), new Vector3(0.08f, 0.2f, 0.3f), p.b);
            k.Box(rulebook.transform, new Vector3(0.85f, 0.7f, -2.05f), new Vector3(0.08f, 0.2f, 0.3f), p.b);
            k.Box(rulebook.transform, new Vector3(0.25f, 0.9f, -2.05f), new Vector3(0.32f, 0.07f, 0.24f), p.accent);

            AnchorTag telegram = k.Anchor("telegram", new Vector3(1.55f, 0.6f, -2.0f));
            k.Box(telegram.transform, new Vector3(1.55f, 0.95f, -2.05f), new Vector3(0.5f, 0.06f, 0.4f), p.b);
            k.Box(telegram.transform, new Vector3(1.55f, 0.75f, -2.05f), new Vector3(0.07f, 0.35f, 0.07f), p.b);
            k.Box(telegram.transform, new Vector3(1.55f, 0.99f, -2.05f), new Vector3(0.36f, 0.02f, 0.26f), C(0.96f, 0.94f, 0.86f));

            // Water tower with float-driven signal arm
            Vector3 wt = new Vector3(3.3f, 0f, 3.1f);
            for (int i = 0; i < 4; i++)
            {
                float dx = (i % 2 == 0) ? -0.55f : 0.55f;
                float dz = (i < 2) ? -0.55f : 0.55f;
                k.Box(null, wt + new Vector3(dx, 1.3f, dz), new Vector3(0.12f, 2.6f, 0.12f), p.b);
            }
            k.Box(null, wt + new Vector3(0f, 2.65f, 0f), new Vector3(1.6f, 0.1f, 1.6f), p.b);

            AnchorTag tank = k.Anchor("tank", wt + new Vector3(0f, 4.2f, 0f));
            k.GlassCyl(tank.transform, wt + new Vector3(0f, 3.45f, 0f), 1.5f, 1.5f, CA(0.55f, 0.45f, 0.38f, 0.38f));
            k.Cyl(tank.transform, wt + new Vector3(0f, 4.22f, 0f), 1.56f, 0.06f, p.dark);
            k.Cyl(tank.transform, wt + new Vector3(0f, 2.72f, 0f), 1.56f, 0.06f, p.dark);
            GameObject tankWater = k.GlassCyl(tank.transform, wt + new Vector3(0f, 3.2f, 0f), 1.4f, 1.0f, p.water);
            HitBox(tank, wt + new Vector3(0f, 3.45f, 0f), new Vector3(1.5f, 1.5f, 1.5f));
            k.stage.RegisterState("tank_level", "half", delegate (string v)
            {
                float f = 0.5f;
                if (v == "quarter") f = 0.25f;
                else if (v == "three_quarters") f = 0.75f;
                else if (v == "almost_full") f = 0.92f;
                else if (v == "full" || v == "brim") f = 1f;
                float h = Mathf.Max(0.02f, 1.44f * f);
                if (tankWater != null)
                {
                    tankWater.transform.position = wt + new Vector3(0f, 2.73f + h * 0.5f, 0f);
                    tankWater.transform.localScale = new Vector3(1.4f, h * 0.5f, 1.4f);
                }
            });

            AnchorTag tower = k.Anchor("tower", wt + new Vector3(-0.7f, 3.1f, -0.75f));
            k.Box(tower.transform, wt + new Vector3(-0.9f, 2.1f, -0.75f), new Vector3(0.05f, 4.2f, 0.05f), p.dark);
            k.Box(tower.transform, wt + new Vector3(-0.5f, 2.1f, -0.75f), new Vector3(0.05f, 4.2f, 0.05f), p.dark);
            for (int i = 0; i < 9; i++)
            {
                k.Box(tower.transform, wt + new Vector3(-0.7f, 0.3f + i * 0.45f, -0.75f), new Vector3(0.4f, 0.04f, 0.04f), p.dark);
            }

            AnchorTag arm = k.Anchor("signal_arm", new Vector3(4.3f, 0f, 0.9f));
            k.Box(arm.transform, new Vector3(4.3f, 1.6f, 0.9f), new Vector3(0.12f, 3.2f, 0.12f), C(0.75f, 0.75f, 0.75f));
            Transform armPivot = k.Pivot(arm.transform, "armPivot", new Vector3(4.3f, 3.0f, 0.82f));
            GameObject blade = k.Box(armPivot, new Vector3(4.75f, 3.0f, 0.82f), new Vector3(0.9f, 0.16f, 0.05f), p.accent);
            GameObject stripe = k.Box(blade.transform, Vector3.zero, Vector3.one, C(0.95f, 0.95f, 0.95f));
            stripe.transform.localPosition = new Vector3(0.3f, 0f, 0f);
            stripe.transform.localRotation = Quaternion.identity;
            stripe.transform.localScale = new Vector3(0.12f, 1.05f, 1.1f);
            GameObject armLamp = k.GlowBall(arm.transform, new Vector3(4.3f, 3.35f, 0.8f), 0.16f, C(0.35f, 0.3f, 0.3f));
            armPivot.localRotation = Quaternion.Euler(0f, 0f, -80f);
            arm.onToggle = delegate (bool on)
            {
                if (armPivot != null) armPivot.localRotation = Quaternion.Euler(0f, 0f, on ? 0f : -80f);
                if (armLamp != null) armLamp.GetComponent<Renderer>().sharedMaterial = Mats.Glow(on ? C(1f, 0.25f, 0.2f) : C(0.35f, 0.3f, 0.3f));
            };

            // People
            k.Npc("signalman", box.StandPoint, C(0.35f, 0.42f, 0.38f), C(0.2f, 0.22f, 0.2f), 0.72f);
            k.Npc("stationmaster", platform.StandPoint, C(0.30f, 0.30f, 0.45f), C(0.15f, 0.15f, 0.25f), 0.76f);
        }

        // =============================================================== level 5

        private static void ClockTower(StageKit k)
        {
            StagePalette p = k.pal;
            Frame(k.stage, new Vector3(0.1f, 5.0f, 0.8f), 19.5f, 14f, -14f);
            k.Plinth(new Vector3(0f, -0.2f, 0.6f), new Vector3(8f, 0.4f, 5.6f), p.ground, Color.Lerp(p.ground, p.dark, 0.4f));

            // Shell (cutaway front)
            k.Box(null, new Vector3(0f, 4.6f, 2.4f), new Vector3(4.6f, 9.2f, 0.25f), p.a);
            k.Box(null, new Vector3(-2.2f, 4.6f, 1.0f), new Vector3(0.25f, 9.2f, 2.8f), p.a);
            k.Box(null, new Vector3(2.2f, 4.6f, 1.0f), new Vector3(0.25f, 9.2f, 2.8f), p.a);
            k.Box(null, new Vector3(0f, 9.3f, 1.0f), new Vector3(4.8f, 0.25f, 3.0f), p.dark);
            k.Cyl(null, new Vector3(0f, 10.0f, 1.2f), 0.6f, 1.2f, p.accent);
            k.Box(null, new Vector3(0f, 4.1f, 1.0f), new Vector3(4.2f, 0.15f, 2.8f), p.b);
            k.Box(null, new Vector3(0f, 7.3f, 1.0f), new Vector3(4.2f, 0.15f, 2.8f), p.b);
            for (int i = 0; i < 7; i++)
            {
                k.Box(null, new Vector3(-1.85f, 0.3f + i * 0.55f, -0.1f), new Vector3(0.4f, 0.05f, 0.05f), p.b);
            }
            k.Box(null, new Vector3(-2.05f, 2.0f, -0.1f), new Vector3(0.05f, 4.0f, 0.05f), p.b);
            k.Box(null, new Vector3(-1.65f, 2.0f, -0.1f), new Vector3(0.05f, 4.0f, 0.05f), p.b);

            // Front stub carrying the clock face (right half of the gear room)
            k.Box(null, new Vector3(1.4f, 6.35f, -0.4f), new Vector3(1.6f, 1.9f, 0.2f), p.a);
            k.CylR(null, new Vector3(1.4f, 6.35f, -0.52f), 1.35f, 0.06f, new Vector3(90f, 0f, 0f), C(0.95f, 0.93f, 0.86f));
            for (int i = 0; i < 12; i++)
            {
                Vector3 dir = Quaternion.Euler(0f, 0f, i * 30f) * Vector3.up;
                k.Box(null, new Vector3(1.4f, 6.35f, -0.56f) + dir * 0.55f, new Vector3(0.05f, 0.05f, 0.02f), p.dark);
            }
            k.BoxR(null, new Vector3(1.55f, 6.35f, -0.57f), new Vector3(0.3f, 0.05f, 0.02f), Vector3.zero, p.dark);

            AnchorTag face = k.Anchor("face", new Vector3(1.0f, 5.45f, -0.68f));
            k.Box(face.transform, new Vector3(1.4f, 5.36f, -0.68f), new Vector3(1.7f, 0.1f, 0.4f), Color.Lerp(p.a, p.dark, 0.25f));
            face.hasFallLanding = true;
            face.fallLanding = new Vector3(1.2f, 0f, -1.5f);

            AnchorTag hand = k.Anchor("minute_hand", new Vector3(1.4f, 6.35f, -0.62f));
            Transform handPivot = k.Pivot(hand.transform, "handPivot", new Vector3(1.4f, 6.35f, -0.6f));
            k.Box(handPivot, new Vector3(1.4f, 6.62f, -0.6f), new Vector3(0.06f, 0.55f, 0.025f), C(0.72f, 0.45f, 0.3f));
            k.Ball(hand.transform, new Vector3(1.4f, 6.35f, -0.6f), 0.09f, p.dark);
            hand.AddSpin(handPivot, Vector3.forward, -0.15f);
            hand.SetSpin(1f);

            // Gear room
            AnchorTag gears = k.Anchor("gears", new Vector3(0f, 4.18f, 0.85f));
            Transform g1 = Gear(k, gears.transform, new Vector3(-0.7f, 5.4f, 2.1f), 1.6f, p.c, 12);
            Transform g2 = Gear(k, gears.transform, new Vector3(0.55f, 4.95f, 2.05f), 0.95f, Color.Lerp(p.c, p.b, 0.2f), 8);
            Transform g3 = Gear(k, gears.transform, new Vector3(0.25f, 6.25f, 2.05f), 0.65f, p.c, 6);
            k.Box(gears.transform, new Vector3(-0.2f, 5.4f, 2.22f), new Vector3(2.6f, 0.08f, 0.06f), p.dark);
            gears.AddSpin(g1, Vector3.forward, 1f);
            gears.AddSpin(g2, Vector3.forward, -1.7f);
            gears.AddSpin(g3, Vector3.forward, -2.4f);
            gears.SetSpin(1f);
            gears.fxOffset = new Vector3(-0.3f, 1.0f, 1.0f);
            k.Box(null, new Vector3(-1.75f, 4.6f, 1.9f), new Vector3(0.5f, 0.06f, 0.4f), p.b);
            k.GlowBox(null, new Vector3(-1.75f, 4.75f, 1.9f), new Vector3(0.14f, 0.22f, 0.14f), C(1f, 0.7f, 0.35f));
            k.Point(null, new Vector3(-1.5f, 5.0f, 1.5f), C(1f, 0.72f, 0.4f), 4f, 1.6f);

            AnchorTag sheet = k.Anchor("round_sheet", new Vector3(-1.45f, 4.18f, 2.0f));
            k.Paper(sheet.transform, new Vector3(-1.45f, 5.6f, 2.26f), new Vector3(0.6f, 0.8f, 0.04f), Vector3.zero);

            // Pendulum pit
            k.Box(null, new Vector3(0f, 0.2f, 0.4f), new Vector3(2.4f, 0.4f, 0.12f), p.a);
            k.Box(null, new Vector3(0f, 0.2f, 2.1f), new Vector3(2.4f, 0.4f, 0.12f), p.a);
            k.Box(null, new Vector3(-1.2f, 0.2f, 1.25f), new Vector3(0.12f, 0.4f, 1.8f), p.a);
            k.Box(null, new Vector3(1.2f, 0.2f, 1.25f), new Vector3(0.12f, 0.4f, 1.8f), p.a);
            k.Water(null, new Vector3(0f, 0.22f, 1.25f), new Vector3(2.3f, 0.36f, 1.6f), p.water);
            AnchorTag pit = k.Anchor("pit", new Vector3(-1.55f, 0.02f, 0.25f));
            pit.drownHop = new Vector3(0.95f, 0.3f, 0.6f);
            pit.fxOffset = new Vector3(0.9f, 0.4f, 0.6f);

            AnchorTag pendulum = k.Anchor("pendulum", new Vector3(0f, 0.9f, 1.3f));
            Transform pp = k.Pivot(pendulum.transform, "pendulumPivot", new Vector3(0f, 4.0f, 1.3f));
            k.Box(pp, new Vector3(0f, 2.45f, 1.3f), new Vector3(0.06f, 3.1f, 0.06f), p.dark);
            k.CylR(pp, new Vector3(0f, 0.9f, 1.3f), 0.6f, 0.16f, new Vector3(90f, 0f, 0f), p.c);
            pendulum.AddSwing(pp, Vector3.forward, 18f);
            pendulum.SetSpin(1f);
            pendulum.fxOffset = new Vector3(0f, -0.4f, 0f);

            // Belfry
            k.Box(null, new Vector3(-1.5f, 8.05f, 1.6f), new Vector3(0.12f, 1.4f, 0.12f), p.b);
            k.Box(null, new Vector3(-0.1f, 8.05f, 1.6f), new Vector3(0.12f, 1.4f, 0.12f), p.b);
            k.Box(null, new Vector3(-0.8f, 8.75f, 1.6f), new Vector3(1.6f, 0.12f, 0.12f), p.b);
            k.Cyl(null, new Vector3(-0.8f, 8.25f, 1.6f), 0.85f, 0.75f, p.c);
            k.Ball(null, new Vector3(-0.8f, 8.62f, 1.6f), 0.6f, p.c);
            AnchorTag belfry = k.Anchor("belfry", new Vector3(-1.2f, 7.38f, 0.7f));

            AnchorTag hatch = k.Anchor("hatch", new Vector3(0.1f, 7.38f, 0.8f));
            k.Box(hatch.transform, new Vector3(0.1f, 7.385f, 0.8f), new Vector3(0.8f, 0.02f, 0.7f), C(0.08f, 0.07f, 0.08f));
            Transform hinge = k.Pivot(hatch.transform, "hinge", new Vector3(0.1f, 7.41f, 0.45f));
            k.Box(hinge, new Vector3(0.1f, 7.41f, 0.8f), new Vector3(0.82f, 0.06f, 0.7f), Color.Lerp(p.b, p.dark, 0.2f));
            k.stage.RegisterState("hatch", "closed", delegate (string v)
            {
                if (hinge != null) hinge.localRotation = (v == "open") ? Quaternion.Euler(-100f, 0f, 0f) : Quaternion.identity;
            });
            hatch.onToggle = delegate (bool on)
            {
                if (hinge != null) hinge.localRotation = on ? Quaternion.Euler(-100f, 0f, 0f) : Quaternion.identity;
            };

            k.Npc("watchman", new Vector3(0.5f, 4.18f, 0.6f), C(0.34f, 0.38f, 0.55f), C(0.18f, 0.18f, 0.24f), 0.75f);
            k.Point(null, new Vector3(0f, 8.4f, 0.4f), C(0.8f, 0.8f, 1f), 4f, 0.6f);
            k.Point(null, new Vector3(0f, 1.6f, 0.2f), C(0.8f, 0.85f, 1f), 4f, 0.6f);
        }

        // =============================================================== level 6

        private static void Office(StageKit k)
        {
            StagePalette p = k.pal;
            Frame(k.stage, new Vector3(-0.2f, 1.3f, 1.4f), 13f, 28f, -12f);
            k.Plinth(new Vector3(0f, -0.15f, 1.0f), new Vector3(10f, 0.3f, 6f), p.ground, Color.Lerp(p.ground, p.dark, 0.5f));
            k.Box(null, new Vector3(0f, 1.75f, 3.9f), new Vector3(10f, 3.5f, 0.2f), p.a);
            k.Box(null, new Vector3(-5f, 1.75f, 1.0f), new Vector3(0.2f, 3.5f, 6f), p.a);
            k.Box(null, new Vector3(0f, 0.01f, 0.8f), new Vector3(4.2f, 0.02f, 2.6f), C(0.42f, 0.16f, 0.18f));

            AnchorTag desk = k.Anchor("desk", new Vector3(0f, 0f, 0.05f));
            k.Box(desk.transform, new Vector3(0f, 0.8f, 1.0f), new Vector3(2.6f, 0.1f, 1.2f), p.b);
            k.Box(desk.transform, new Vector3(-1.1f, 0.38f, 1.0f), new Vector3(0.35f, 0.76f, 1.1f), Color.Lerp(p.b, p.dark, 0.3f));
            k.Box(desk.transform, new Vector3(1.1f, 0.38f, 1.0f), new Vector3(0.35f, 0.76f, 1.1f), Color.Lerp(p.b, p.dark, 0.3f));
            k.Box(desk.transform, new Vector3(-0.3f, 0.9f, 1.1f), new Vector3(0.5f, 0.1f, 0.36f), p.c);
            k.Box(desk.transform, new Vector3(-0.3f, 0.99f, 1.1f), new Vector3(0.46f, 0.08f, 0.34f), Color.Lerp(p.c, Color.white, 0.2f));
            k.Box(desk.transform, new Vector3(0.35f, 0.92f, 1.2f), new Vector3(0.36f, 0.14f, 0.26f), p.c);
            k.Cyl(desk.transform, new Vector3(0.9f, 0.9f, 1.3f), 0.25f, 0.06f, p.dark);
            k.Box(desk.transform, new Vector3(0.9f, 1.15f, 1.3f), new Vector3(0.04f, 0.5f, 0.04f), p.dark);
            k.GlowBox(desk.transform, new Vector3(0.9f, 1.42f, 1.22f), new Vector3(0.36f, 0.12f, 0.2f), p.accent);
            k.Point(desk.transform, new Vector3(0.8f, 1.35f, 0.9f), C(0.75f, 1f, 0.8f), 4f, 1.8f);
            k.Box(null, new Vector3(0f, 0.5f, 2.0f), new Vector3(0.7f, 0.1f, 0.6f), p.dark);
            k.Box(null, new Vector3(0f, 0.95f, 2.3f), new Vector3(0.7f, 0.9f, 0.1f), p.dark);

            AnchorTag plaque = k.Anchor("keyboard_plaque", new Vector3(-0.75f, 0.85f, 0.55f));
            k.Box(plaque.transform, new Vector3(-0.75f, 0.92f, 0.45f), new Vector3(0.45f, 0.14f, 0.04f), p.c);
            k.Box(plaque.transform, new Vector3(-0.75f, 0.92f, 0.43f), new Vector3(0.34f, 0.04f, 0.01f), p.dark);

            AnchorTag archive = k.Anchor("archive", new Vector3(-3.3f, 0f, 1.7f));
            for (int i = 0; i < 4; i++)
            {
                float x = -4.4f + i * 0.72f;
                k.Box(archive.transform, new Vector3(x, 1.3f, 3.4f), new Vector3(0.66f, 2.6f, 0.7f), Color.Lerp(p.a, p.dark, 0.35f));
                for (int d = 0; d < 4; d++)
                {
                    k.Box(archive.transform, new Vector3(x, 0.4f + d * 0.6f, 3.04f), new Vector3(0.5f, 0.04f, 0.02f), p.c);
                }
            }
            k.Box(archive.transform, new Vector3(-2.9f, 0.15f, 2.4f), new Vector3(0.5f, 0.3f, 0.4f), p.c);
            k.Box(archive.transform, new Vector3(-3.5f, 0.1f, 2.5f), new Vector3(0.4f, 0.2f, 0.35f), p.c);

            AnchorTag door = k.Anchor("door", new Vector3(3.4f, 0f, 2.9f));
            k.Box(door.transform, new Vector3(3.4f, 1.15f, 3.79f), new Vector3(1.1f, 2.3f, 0.04f), C(0.03f, 0.03f, 0.05f));
            k.Box(door.transform, new Vector3(2.8f, 1.2f, 3.75f), new Vector3(0.1f, 2.4f, 0.12f), p.b);
            k.Box(door.transform, new Vector3(4.0f, 1.2f, 3.75f), new Vector3(0.1f, 2.4f, 0.12f), p.b);
            k.Box(door.transform, new Vector3(3.4f, 2.4f, 3.75f), new Vector3(1.3f, 0.1f, 0.12f), p.b);
            k.GlowBox(door.transform, new Vector3(3.4f, 0.03f, 3.7f), new Vector3(1.0f, 0.04f, 0.1f), C(0.9f, 0.9f, 1f));

            AnchorTag clock = k.Anchor("time_clock", new Vector3(2.0f, 0f, 3.2f));
            k.Box(clock.transform, new Vector3(2.0f, 1.5f, 3.68f), new Vector3(0.6f, 0.8f, 0.3f), Color.Lerp(p.a, p.c, 0.3f));
            k.GlowBox(clock.transform, new Vector3(2.0f, 1.65f, 3.52f), new Vector3(0.36f, 0.24f, 0.02f), C(0.85f, 0.95f, 0.8f));
            k.Box(clock.transform, new Vector3(2.0f, 1.3f, 3.52f), new Vector3(0.3f, 0.05f, 0.02f), p.dark);
            k.Box(clock.transform, new Vector3(1.4f, 1.4f, 3.78f), new Vector3(0.3f, 0.6f, 0.04f), p.c);
            clock.hasPark = true;
            clock.park = new Vector3(2.55f, 0f, 3.5f);

            AnchorTag footer = k.Anchor("form_footer", new Vector3(-1.6f, 0f, 3.6f));
            k.Box(footer.transform, new Vector3(-1.6f, 2.0f, 3.8f), new Vector3(0.9f, 1.1f, 0.03f), p.dark);
            k.Paper(footer.transform, new Vector3(-1.6f, 2.0f, 3.77f), new Vector3(0.78f, 0.98f, 0.03f), Vector3.zero);

            AnchorTag scythe = k.Anchor("scythe", new Vector3(1.55f, 0f, 0.45f));
            k.BoxR(scythe.transform, new Vector3(1.62f, 1.1f, 0.45f), new Vector3(0.07f, 2.2f, 0.07f), new Vector3(0f, 0f, -6f), C(0.35f, 0.24f, 0.18f));
            k.BoxR(scythe.transform, new Vector3(1.95f, 2.12f, 0.45f), new Vector3(0.8f, 0.09f, 0.03f), new Vector3(0f, 0f, -22f), C(0.82f, 0.84f, 0.88f));

            k.GlowBall(null, new Vector3(-0.5f, 3.0f, 1.0f), 0.3f, C(0.85f, 0.85f, 0.95f));
            k.Point(null, new Vector3(-0.5f, 2.8f, 1.0f), C(0.85f, 0.85f, 1f), 7f, 1.0f);
        }

        // =============================================================== fallback

        /// <summary>Generic layout for an unknown level: anchors on a ring with a plain block each.</summary>
        public static void Generic(StageKit k, System.Collections.Generic.List<string> anchors)
        {
            Frame(k.stage, new Vector3(0f, 0.8f, 0.5f), 13f, 32f, -15f);
            k.Plinth(new Vector3(0f, -0.2f, 0.5f), new Vector3(9f, 0.4f, 6f), k.pal.ground, k.pal.dark);
        }
    }
}
