using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Marks greybox objects the program owns (water surfaces, glass, gauge needles, light beams): they are
    /// not "appearance", so a model replacement keeps them visible and working.
    /// </summary>
    public class ProgramVisual : MonoBehaviour { }

    /// <summary>Copies another transform's local rotation change onto this one (model part following a hidden greybox hinge).</summary>
    public class RotationFollower : MonoBehaviour
    {
        public Transform source;
        private Quaternion sourceBase;
        private Quaternion selfBase;

        public void Bind(Transform src)
        {
            source = src;
            sourceBase = src != null ? src.localRotation : Quaternion.identity;
            selfBase = transform.localRotation;
        }

        public void Sync()
        {
            if (source == null) return;
            transform.localRotation = selfBase * (Quaternion.Inverse(sourceBase) * source.localRotation);
        }

        private void LateUpdate() { Sync(); }
    }

    /// <summary>
    /// Replaces greybox appearance with authored models (docs/art-brief.md, "模型替换规则"):
    /// only the look changes. The anchor's transform, behaviours (spin / toggle / light), clue marker and
    /// program-owned visuals stay; the greybox renderers and colliders are switched off and one BoxCollider
    /// fitted to the model is added on the anchor. Model origin = anchor origin, no automatic scaling.
    /// Named children route behaviours: "spin*" (continuous rotation), "toggle" (on/off or hinge), "light"
    /// (glows when lit); without them the behaviour applies to the whole model.
    /// </summary>
    public static class ModelSwap
    {
        public const string ModelChildName = "model";
        public const string SpinPart = "spin";
        public const string TogglePart = "toggle";
        public const string LightPart = "light";

        private static bool IsProgramOwned(Component c)
        {
            return c.GetComponentInParent<ProgramVisual>(true) != null;
        }

        /// <summary>Disables greybox renderers / colliders under root, skipping program visuals and anything under "skip".</summary>
        public static void HideGreybox(Transform root, Transform skip, Func<Transform, bool> exclude)
        {
            foreach (Renderer r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || IsProgramOwned(r)) continue;
                if (skip != null && r.transform.IsChildOf(skip)) continue;
                if (exclude != null && exclude(r.transform)) continue;
                r.enabled = false;
            }
            foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
            {
                if (IsProgramOwned(c)) continue;
                if (skip != null && c.transform.IsChildOf(skip)) continue;
                if (exclude != null && exclude(c.transform)) continue;
                c.enabled = false;
            }
        }

        /// <summary>Instantiates a model at parent's origin (no scaling) with the project's materials.</summary>
        public static GameObject Instantiate(GameObject prefab, Transform parent, string name)
        {
            GameObject inst = UnityEngine.Object.Instantiate(prefab, parent, false);
            inst.name = name;
            inst.SetActive(true);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;
            ConvertMaterials(inst);
            return inst;
        }

        /// <summary>Keeps each material's color and main texture but moves it onto the shipped Lit template.</summary>
        public static void ConvertMaterials(GameObject go)
        {
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] src = r.sharedMaterials;
                var dst = new Material[Mathf.Max(1, src.Length)];
                for (int i = 0; i < dst.Length; i++) dst[i] = Mats.FromModel(i < src.Length ? src[i] : null);
                r.sharedMaterials = dst;
            }
        }

        /// <summary>Descendants named exactly "name" or starting with "name" + '_' / digit, in hierarchy order.</summary>
        public static List<Transform> FindParts(Transform root, string name)
        {
            var list = new List<Transform>();
            Collect(root, name, list);
            return list;
        }

        private static void Collect(Transform t, string name, List<Transform> list)
        {
            for (int i = 0; i < t.childCount; i++)
            {
                Transform c = t.GetChild(i);
                string n = c.name.ToLowerInvariant();
                if (n == name || (n.StartsWith(name) && n.Length > name.Length && (n[name.Length] == '_' || char.IsDigit(n[name.Length]))))
                    list.Add(c);
                Collect(c, name, list);
            }
        }

        public static Transform FindPart(Transform root, string name)
        {
            List<Transform> l = FindParts(root, name);
            return l.Count > 0 ? l[0] : null;
        }

        /// <summary>Renderer bounds of go in the local space of "space" (8 corners of each world AABB).</summary>
        public static bool LocalBounds(GameObject go, Transform space, out Bounds b)
        {
            b = new Bounds();
            bool any = false;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Bounds w = r.bounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = w.center + Vector3.Scale(w.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 p = space.InverseTransformPoint(corner);
                    if (!any) { b = new Bounds(p, Vector3.zero); any = true; }
                    else b.Encapsulate(p);
                }
            }
            return any;
        }

        /// <summary>Which behaviours an anchor has (from the layout and from the level's beats).</summary>
        public struct Uses
        {
            public bool spin, toggle, light;
        }

        /// <summary>Swaps one anchor's look for the model. Returns the model instance (or null if prefab is null).</summary>
        public static GameObject ApplyAnchor(AnchorTag t, GameObject prefab, Uses uses)
        {
            if (t == null || prefab == null) return null;
            HideGreybox(t.transform, null, null);
            GameObject inst = Instantiate(prefab, t.transform, ModelChildName);
            foreach (Collider c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;

            Bounds b;
            if (LocalBounds(inst, t.transform, out b))
            {
                BoxCollider bc = t.gameObject.AddComponent<BoxCollider>();
                bc.center = b.center;
                bc.size = Vector3.Max(b.size, new Vector3(0.05f, 0.05f, 0.05f));
            }

            // spin: one model part per original spin (the parts' own pivots are the rotation centres).
            if (t.spins.Count > 0 || uses.spin)
            {
                var originals = new List<Spin>(t.spins);
                if (originals.Count == 0) { var d = new Spin(); d.axis = Vector3.up; originals.Add(d); }
                List<Transform> parts = FindParts(inst.transform, SpinPart);
                parts.Sort(delegate (Transform a, Transform b) { return string.CompareOrdinal(a.name, b.name); });
                if (parts.Count == 0)
                {
                    CopySpin(t, originals[0], inst.transform);
                }
                else
                {
                    for (int i = 0; i < parts.Count; i++) CopySpin(t, originals[Mathf.Min(i, originals.Count - 1)], parts[i]);
                }
            }

            // toggle: hinge-style toggles follow the hidden greybox hinge; on/off toggles show/hide the part.
            Transform toggleTarget = FindPart(inst.transform, TogglePart);
            if (toggleTarget == null) toggleTarget = inst.transform;
            if (t.toggleMotion != null)
            {
                RotationFollower f = toggleTarget.gameObject.AddComponent<RotationFollower>();
                f.Bind(t.toggleMotion);
            }
            else if (t.onToggle == null && (uses.toggle || toggleTarget != inst.transform))
            {
                Transform anchor = t.transform;
                GameObject target = toggleTarget.gameObject;
                GameObject model = inst;
                t.onToggle = delegate (bool on)
                {
                    // Same as the greybox default (every child), but the model only via its toggle part.
                    for (int i = 0; i < anchor.childCount; i++)
                    {
                        GameObject c = anchor.GetChild(i).gameObject;
                        if (c != model) c.SetActive(on);
                    }
                    if (target != null) target.SetActive(on);
                };
            }

            // light: the "light" part (or the whole model) turns self-lit when the anchor lights up.
            if (uses.light || t.onLight != null)
            {
                Transform lt = FindPart(inst.transform, LightPart);
                t.lightTarget = lt != null ? lt.gameObject : inst;
            }
            Debug.Log("[ModelSwap] " + t.id + ": model " + prefab.name + " (spin parts " + FindParts(inst.transform, SpinPart).Count +
                      ", toggle part " + (FindPart(inst.transform, TogglePart) != null) + ", light part " + (FindPart(inst.transform, LightPart) != null) + ")");
            return inst;
        }

        private static void CopySpin(AnchorTag t, Spin s, Transform target)
        {
            Spin n = t.AddSpin(target, s.axis, s.mult);
            n.swing = s.swing;
            n.amplitude = s.amplitude;
        }

        /// <summary>Whole-level scenery: hides every greybox piece that is not under an anchor, adds the set model.</summary>
        public static GameObject ApplySet(Stage stage, GameObject prefab)
        {
            if (stage == null || prefab == null) return null;
            Transform root = stage.transform;
            HideGreybox(root, null, delegate (Transform x) { return x.GetComponentInParent<AnchorTag>(true) != null; });
            GameObject inst = Instantiate(prefab, root, "_set");
            foreach (MeshFilter mf in inst.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.GetComponent<Collider>() != null || mf.sharedMesh == null) continue;
                if (mf.sharedMesh.isReadable)
                {
                    MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                }
                else
                {
                    mf.gameObject.AddComponent<BoxCollider>();
                }
            }
            Debug.Log("[ModelSwap] " + (stage.level != null ? stage.level.id : "?") + ": scenery replaced by " + prefab.name);
            return inst;
        }

        /// <summary>Makes every renderer under go self-lit, keeping its color.</summary>
        public static void Glow(GameObject go)
        {
            if (go == null) return;
            foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] ms = r.sharedMaterials;
                for (int i = 0; i < ms.Length; i++) ms[i] = Mats.Glow(Mats.GetColor(ms[i]));
                r.sharedMaterials = ms;
            }
        }
    }
}
