using UnityEngine;
using UnityEngine.Rendering;

namespace DeathNotice.Game
{
    /// <summary>
    /// Greybox building blocks. All positions are world positions (the stage root sits at the origin).
    /// Cylinders take a diameter and a height; boxes take a center and a size.
    /// </summary>
    public class StageKit
    {
        public readonly Stage stage;
        public readonly Transform root;
        public readonly StagePalette pal;

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

        /// <summary>Translucent box (water, glass). No collider so it never blocks clue hovering.</summary>
        public GameObject Water(Transform parent, Vector3 pos, Vector3 size, Color c)
        {
            GameObject g = Prim(PrimitiveType.Cube, parent, "water", pos, size, Vector3.zero, Mats.Transparent(c));
            RemoveCollider(g);
            g.AddComponent<ProgramVisual>();
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return g;
        }

        public GameObject GlassCyl(Transform parent, Vector3 pos, float diameter, float height, Color c)
        {
            GameObject g = Prim(PrimitiveType.Cylinder, parent, "glass", pos, new Vector3(diameter, height * 0.5f, diameter), Vector3.zero, Mats.Transparent(c));
            RemoveCollider(g);
            g.AddComponent<ProgramVisual>();
            g.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return g;
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
            AnchorTag t = Anchor(id, pos);
            t.isFigure = true;
            Figure.Create(t.transform, "figure", body, height, false, hat);
            return t;
        }

        /// <summary>A notice board / paper on a post or wall: a flat pale box with dark "lines".</summary>
        public GameObject Paper(Transform parent, Vector3 pos, Vector3 size, Vector3 euler)
        {
            GameObject p = BoxR(parent, pos, size, euler, new Color(0.95f, 0.93f, 0.85f));
            for (int i = 0; i < 3; i++)
            {
                GameObject line = BoxR(p.transform, Vector3.zero, Vector3.one, euler, new Color(0.3f, 0.28f, 0.27f));
                RemoveCollider(line);
                line.transform.localPosition = new Vector3(0f, 0.25f - i * 0.22f, -0.6f);
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = new Vector3(0.7f, 0.06f, 0.2f);
            }
            return p;
        }

        /// <summary>Paper on a stick (sign, timetable, notice).</summary>
        public GameObject Signpost(Transform parent, Vector3 foot, float postHeight, Vector3 boardSize, float yaw)
        {
            Box(parent, foot + Vector3.up * (postHeight * 0.5f), new Vector3(0.08f, postHeight, 0.08f), pal.b);
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

        /// <summary>Wide, flat slab used as the diorama plinth.</summary>
        public void Plinth(Vector3 center, Vector3 size, Color top, Color side)
        {
            Box(root, center, size, side);
            Box(root, center + Vector3.up * (size.y * 0.5f + 0.01f), new Vector3(size.x - 0.05f, 0.02f, size.z - 0.05f), top);
        }
    }
}
