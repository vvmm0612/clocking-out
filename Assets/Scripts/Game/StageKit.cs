using UnityEngine;
using UnityEngine.Rendering;

namespace DeathNotice.Game
{
    /// <summary>
    /// Greybox building blocks. All positions are world positions (the stage root sits at the origin).
    /// Cylinders take a diameter and a height; boxes take a center and a size.
    /// Box / Cyl / Ball have colliders (they are floors, walls and clue objects); the D* "decoration"
    /// variants have none, so dressing never blocks a clue or catches a falling figure.
    /// </summary>
    public class StageKit
    {
        public readonly Stage stage;
        public readonly Transform root;
        public readonly StagePalette pal;

        public static readonly Color StandWood = new Color(0.30f, 0.21f, 0.17f);
        public static readonly Color StandTrim = new Color(0.52f, 0.38f, 0.27f);
        public static readonly Color Brass = new Color(0.86f, 0.68f, 0.34f);

        public StageKit(Stage stage)
        {
            this.stage = stage;
            root = stage.transform;
            pal = stage.palette;
        }

        public static void RemoveCollider(GameObject go)
        {
            Collider[] cs = go.GetComponents<Collider>();
            for (int i = 0; i < cs.Length; i++)
            {
                // Disable first: Destroy is deferred to the end of the frame, and raycasts must not see it meanwhile.
                cs[i].enabled = false;
                if (Application.isPlaying) Object.Destroy(cs[i]);
                else Object.DestroyImmediate(cs[i]); // edit-mode tests build stages too
            }
        }

        private GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : root, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            ToonMesh.Apply(go.GetComponent<MeshFilter>());
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
            if (type == PrimitiveType.Cylinder)
            {
                // The primitive's CapsuleCollider turns flat discs (gallery floor, clock face) into big
                // spheres that swallow the clue objects around them. A box matches the visual.
                RemoveCollider(go);
                go.AddComponent<BoxCollider>();
            }
            return go;
        }

        private GameObject Deco(GameObject go)
        {
            RemoveCollider(go);
            // Tiny dressing (tufts, pebbles, trims) does not cast shadows: saves shadow-map draws on WebGL.
            Vector3 sc = go.transform.lossyScale;
            if (Mathf.Max(sc.x, Mathf.Max(sc.y, sc.z)) < 0.16f)
            {
                Renderer r = go.GetComponent<Renderer>();
                if (r != null) r.shadowCastingMode = ShadowCastingMode.Off;
            }
            return go;
        }

        public GameObject Box(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            return Prim(PrimitiveType.Cube, parent, "box", pos, size, Vector3.zero, Mats.Lit(c));
        }

        public GameObject BoxR(Transform parent, Vector3 pos, Vector3 size, Vector3 euler, Color c)
        {
            return Prim(PrimitiveType.Cube, parent, "box", pos, size, euler, Mats.Lit(c));
        }

        /// <summary>Upright cylinder centered at pos.</summary>
        public GameObject Cyl(Transform parent, Vector3 pos, float diameter, float height, Color c)
        {
            return Prim(PrimitiveType.Cylinder, parent, "cyl", pos, new Vector3(diameter, height * 0.5f, diameter), Vector3.zero, Mats.Lit(c));
        }

        public GameObject CylR(Transform parent, Vector3 pos, float diameter, float height, Vector3 euler, Color c)
        {
            return Prim(PrimitiveType.Cylinder, parent, "cyl", pos, new Vector3(diameter, height * 0.5f, diameter), euler, Mats.Lit(c));
        }

        public GameObject Ball(Transform parent, Vector3 pos, float diameter, Color c)
        {
            return Prim(PrimitiveType.Sphere, parent, "ball", pos, Vector3.one * diameter, Vector3.zero, Mats.Lit(c));
        }

        // ---- decoration (no colliders)

        public GameObject DBox(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            return Deco(Prim(PrimitiveType.Cube, parent, "deco", pos, size, Vector3.zero, Mats.Lit(c)));
        }

        public GameObject DBoxR(Transform parent, Vector3 pos, Vector3 size, Vector3 euler, Color c)
        {
            return Deco(Prim(PrimitiveType.Cube, parent, "deco", pos, size, euler, Mats.Lit(c)));
        }

        public GameObject DCyl(Transform parent, Vector3 pos, float diameter, float height, Color c)
        {
            return Deco(Prim(PrimitiveType.Cylinder, parent, "deco", pos, new Vector3(diameter, height * 0.5f, diameter), Vector3.zero, Mats.Lit(c)));
        }

        public GameObject DCylR(Transform parent, Vector3 pos, float diameter, float height, Vector3 euler, Color c)
        {
            return Deco(Prim(PrimitiveType.Cylinder, parent, "deco", pos, new Vector3(diameter, height * 0.5f, diameter), euler, Mats.Lit(c)));
        }

        public GameObject DBall(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            return Deco(Prim(PrimitiveType.Sphere, parent, "deco", pos, size, Vector3.zero, Mats.Lit(c)));
        }

        /// <summary>Self-lit box (flames, lamps, windows).</summary>
        public GameObject GlowBox(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            GameObject g = Prim(PrimitiveType.Cube, parent, "glow", pos, size, Vector3.zero, Mats.Glow(c));
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        public GameObject GlowBall(Transform parent, Vector3 pos, float diameter, Color c)
        {
            GameObject g = Prim(PrimitiveType.Sphere, parent, "glow", pos, Vector3.one * diameter, Vector3.zero, Mats.Glow(c));
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        private GameObject SeeThrough(PrimitiveType type, Transform parent, string name, Vector3 pos, Vector3 scale, Material m)
        {
            GameObject g = Prim(type, parent, name, pos, scale, Vector3.zero, m);
            RemoveCollider(g);
            g.AddComponent<ProgramVisual>();
            Renderer r = g.GetComponent<Renderer>();
            r.shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        /// <summary>Water volume (swell + highlight bands). No collider so it never blocks clue hovering.</summary>
        public GameObject Water(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            return SeeThrough(PrimitiveType.Cube, parent, "water", pos, size, Mats.Water(c));
        }

        /// <summary>Round water surface (vat, tank).</summary>
        public GameObject WaterCyl(Transform parent, Vector3 pos, float diameter, float height, Color c)
        {
            return SeeThrough(PrimitiveType.Cylinder, parent, "water", pos, new Vector3(diameter, height * 0.5f, diameter), Mats.Water(c));
        }

        public GameObject GlassCyl(Transform parent, Vector3 pos, float diameter, float height, Color c)
        {
            return SeeThrough(PrimitiveType.Cylinder, parent, "glass", pos, new Vector3(diameter, height * 0.5f, diameter), Mats.Glass(c));
        }

        /// <summary>Self-lit translucent shaft (lighthouse beam).</summary>
        public GameObject Beam(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            return SeeThrough(PrimitiveType.Cube, parent, "beam", pos, size, Mats.Beam(c));
        }

        public Light Point(Transform parent, Vector3 pos, Color c, float range, float intensity)
        {
            var go = new GameObject("light");
            go.transform.SetParent(parent != null ? parent : root, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = c;
            l.range = range;
            l.intensity = intensity;
            l.shadows = LightShadows.None;
            return l;
        }

        public AnchorTag Anchor(string id, Vector3 pos)
        {
            var go = new GameObject(id);
            go.transform.SetParent(root, false);
            go.transform.position = pos;
            AnchorTag t = go.AddComponent<AnchorTag>();
            t.id = id;
            stage.Register(t);
            return t;
        }

        /// <summary>An anchor that is a person (NPC). Visible only on the other side / during playback.</summary>
        public AnchorTag Npc(string id, Vector3 pos, Color body, Color? hat, float height)
        {
            return Npc(id, pos, body, hat, height, FigureStyle.Plain);
        }

        public AnchorTag Npc(string id, Vector3 pos, Color body, Color? hat, float height, FigureStyle style)
        {
            AnchorTag t = Anchor(id, pos);
            t.isFigure = true;
            Figure.Create(t.transform, "figure", body, height, false, hat, null, style);
            return t;
        }

        /// <summary>A notice board / paper on a post or wall: a flat pale sheet in a thin dark frame, with "lines".</summary>
        public GameObject Paper(Transform parent, Vector3 pos, Vector3 size, Vector3 euler)
        {
            GameObject p = BoxR(parent, pos, size, euler, new Color(0.95f, 0.93f, 0.85f));
            // frame (slightly larger, behind)
            GameObject frame = DBoxR(p.transform, Vector3.zero, Vector3.one, euler, Color.Lerp(pal.b, pal.dark, 0.35f));
            frame.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            frame.transform.localRotation = Quaternion.identity;
            frame.transform.localScale = new Vector3(1.12f, 1.12f, 0.6f);
            for (int i = 0; i < 3; i++)
            {
                GameObject line = DBoxR(p.transform, Vector3.zero, Vector3.one, euler, new Color(0.3f, 0.28f, 0.27f));
                line.transform.localPosition = new Vector3(i == 0 ? -0.05f : 0f, 0.25f - i * 0.22f, -0.6f);
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = new Vector3(i == 0 ? 0.55f : 0.7f, 0.06f, 0.2f);
                line.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            // a red pin
            GameObject pin = DBall(p.transform, Vector3.zero, Vector3.one, pal.accent);
            pin.transform.localPosition = new Vector3(0f, 0.42f, -0.8f);
            pin.transform.localScale = new Vector3(0.12f, 0.12f * size.x / Mathf.Max(0.01f, size.y), 1.2f);
            return p;
        }

        /// <summary>Paper on a stick (sign, timetable, notice).</summary>
        public GameObject Signpost(Transform parent, Vector3 foot, float postHeight, Vector3 boardSize, float yaw)
        {
            Box(parent, foot + Vector3.up * (postHeight * 0.5f), new Vector3(0.08f, postHeight, 0.08f), pal.b);
            DBox(parent, foot + Vector3.up * 0.04f, new Vector3(0.2f, 0.08f, 0.2f), Color.Lerp(pal.b, pal.dark, 0.4f));
            return Paper(parent, foot + Vector3.up * postHeight + new Vector3(0f, 0f, -0.06f), boardSize, new Vector3(0f, yaw, 0f));
        }

        /// <summary>A pivot transform (for hinged / spinning parts).</summary>
        public Transform Pivot(Transform parent, string name, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : root, false);
            go.transform.position = pos;
            return go.transform;
        }

        // ---------------------------------------------------------------- diorama dressing

        /// <summary>Ground slab of the diorama, standing on a wooden display stand.</summary>
        public void Plinth(Vector3 center, Vector3 size, Color top, Color side)
        {
            PlinthCore(center, size, top, side);
            Stand(new Vector3(center.x, center.y - size.y * 0.5f, center.z), new Vector2(size.x, size.z));
        }

        /// <summary>The ground slab only (an island rising out of the sea, which has its own stand).</summary>
        public void PlinthCore(Vector3 center, Vector3 size, Color top, Color side)
        {
            Box(root, center, size, side);
            Box(root, center + Vector3.up * (size.y * 0.5f + 0.01f), new Vector3(size.x - 0.05f, 0.02f, size.z - 0.05f), top);
            // bevel: a lighter lip around the top edge
            Color lip = Color.Lerp(top, Color.white, 0.18f);
            float y = center.y + size.y * 0.5f - 0.03f;
            DBox(root, new Vector3(center.x, y, center.z - size.z * 0.5f), new Vector3(size.x + 0.04f, 0.07f, 0.06f), lip);
            DBox(root, new Vector3(center.x, y, center.z + size.z * 0.5f), new Vector3(size.x + 0.04f, 0.07f, 0.06f), lip);
            DBox(root, new Vector3(center.x - size.x * 0.5f, y, center.z), new Vector3(0.06f, 0.07f, size.z + 0.04f), lip);
            DBox(root, new Vector3(center.x + size.x * 0.5f, y, center.z), new Vector3(0.06f, 0.07f, size.z + 0.04f), lip);
        }

        /// <summary>
        /// Display stand under a slab whose bottom face is at topCenter: a trim lip, a thick dark wooden base,
        /// a brass plate on the front and a soft contact shadow. Makes the level read as a model on a table.
        /// </summary>
        public void Stand(Vector3 topCenter, Vector2 xz)
        {
            Transform t = Pivot(root, "stand", topCenter);
            DBox(t, topCenter + new Vector3(0f, -0.05f, 0f), new Vector3(xz.x + 0.22f, 0.1f, xz.y + 0.22f), StandTrim);
            Box(t, topCenter + new Vector3(0f, -0.36f, 0f), new Vector3(xz.x + 0.5f, 0.52f, xz.y + 0.5f), StandWood);
            DBox(t, topCenter + new Vector3(0f, -0.64f, 0f), new Vector3(xz.x + 0.62f, 0.06f, xz.y + 0.62f), Color.Lerp(StandWood, Color.black, 0.25f));
            // brass name plate on the camera-facing side
            DBox(t, topCenter + new Vector3(0f, -0.36f, -(xz.y + 0.5f) * 0.5f - 0.015f), new Vector3(Mathf.Min(1.6f, xz.x * 0.3f), 0.2f, 0.03f), Brass);
            // contact shadow
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "contact_shadow";
            RemoveCollider(q);
            q.transform.SetParent(t, false);
            q.transform.position = topCenter + new Vector3(0.3f, -0.68f, 0.3f);
            q.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            q.transform.localScale = new Vector3(xz.x * 1.5f, xz.y * 1.6f, 1f);
            Material sm = Mats.Shadow(0.4f);
            Renderer qr = q.GetComponent<Renderer>();
            qr.sharedMaterial = sm;
            qr.shadowCastingMode = ShadowCastingMode.Off;
            q.AddComponent<NoFit>();
        }

        /// <summary>A lit window on a wall facing -Z (toward the camera): dark frame, glowing pane, cross bars, sill.</summary>
        public void Window(Transform parent, Vector3 center, Vector2 size, Color glow)
        {
            DBox(parent, center + new Vector3(0f, 0f, 0.005f), new Vector3(size.x + 0.1f, size.y + 0.1f, 0.05f), Color.Lerp(pal.b, pal.dark, 0.45f));
            GameObject pane = GlowBox(parent, center + new Vector3(0f, 0f, -0.02f), new Vector3(size.x, size.y, 0.03f), glow);
            RemoveCollider(pane);
            DBox(parent, center + new Vector3(0f, 0f, -0.04f), new Vector3(0.035f, size.y, 0.02f), Color.Lerp(pal.b, pal.dark, 0.45f));
            DBox(parent, center + new Vector3(0f, 0f, -0.04f), new Vector3(size.x, 0.035f, 0.02f), Color.Lerp(pal.b, pal.dark, 0.45f));
            DBox(parent, center + new Vector3(0f, -size.y * 0.5f - 0.06f, -0.05f), new Vector3(size.x + 0.18f, 0.05f, 0.12f), Color.Lerp(pal.a, Color.white, 0.2f));
        }

        public void Rock(Vector3 pos, float size, Color c)
        {
            DBall(null, pos, new Vector3(size, size * 0.62f, size * 0.85f), c).transform.rotation = Quaternion.Euler(0f, pos.x * 47f, 8f);
        }

        /// <summary>A tuft of grass: three thin blades leaning out.</summary>
        public void Grass(Vector3 foot, Color c)
        {
            for (int i = 0; i < 3; i++)
            {
                float a = i * 120f + foot.x * 30f;
                DBoxR(null, foot + Quaternion.Euler(0f, a, 0f) * new Vector3(0.04f, 0.09f, 0f), new Vector3(0.04f, 0.2f, 0.02f),
                    new Vector3(0f, a, 18f), Color.Lerp(c, Color.white, i * 0.08f)).GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        /// <summary>Wooden crate with darker edge battens.</summary>
        public void Crate(Vector3 foot, float s, float yaw)
        {
            Color w = Color.Lerp(pal.b, pal.a, 0.25f);
            Color e = Color.Lerp(pal.b, pal.dark, 0.35f);
            Transform t = Pivot(null, "crate", foot);
            DBox(t, foot + Vector3.up * s * 0.5f, Vector3.one * s, w);
            DBox(t, foot + Vector3.up * s * 0.5f, new Vector3(s * 1.04f, s * 0.16f, s * 1.04f), e);
            DBox(t, foot + Vector3.up * s * 0.5f, new Vector3(s * 0.16f, s * 1.04f, s * 1.04f), e);
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
        }

        public void Barrel(Vector3 foot, float d, float h)
        {
            Color w = Color.Lerp(pal.b, pal.a, 0.15f);
            DCyl(null, foot + Vector3.up * h * 0.5f, d, h, w);
            DCyl(null, foot + Vector3.up * h * 0.22f, d * 1.05f, h * 0.07f, pal.dark);
            DCyl(null, foot + Vector3.up * h * 0.78f, d * 1.05f, h * 0.07f, pal.dark);
        }

        public void Bush(Vector3 foot, float s, Color c)
        {
            DBall(null, foot + new Vector3(0f, s * 0.35f, 0f), new Vector3(s, s * 0.75f, s * 0.9f), c);
            DBall(null, foot + new Vector3(s * 0.38f, s * 0.25f, -s * 0.1f), new Vector3(s * 0.65f, s * 0.55f, s * 0.6f), Color.Lerp(c, Color.white, 0.1f));
        }

        /// <summary>Slow chimney smoke.</summary>
        public void Smoke(Vector3 pos)
        {
            FxLibrary.Spawn("chimney", pos, root, Vector3.up);
        }

        /// <summary>Marks an object (and its children) as not framed by the camera.</summary>
        public static GameObject NoFrame(GameObject go)
        {
            if (go != null && go.GetComponent<NoFit>() == null) go.AddComponent<NoFit>();
            return go;
        }
    }
}
