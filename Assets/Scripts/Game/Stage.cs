using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>4-6 soft colors per level plus the "other side" tints.</summary>
    public class StagePalette
    {
        public Color sky = new Color(0.62f, 0.72f, 0.78f);
        public Color ambient = new Color(0.55f, 0.55f, 0.6f);
        public Color sun = new Color(1f, 0.95f, 0.86f);
        public Color ground = new Color(0.55f, 0.6f, 0.5f);
        public Color a = new Color(0.85f, 0.82f, 0.75f);
        public Color b = new Color(0.6f, 0.45f, 0.35f);
        public Color c = new Color(0.45f, 0.5f, 0.6f);
        public Color dark = new Color(0.25f, 0.24f, 0.26f);
        public Color accent = new Color(0.8f, 0.35f, 0.3f);
        public Color water = new Color(0.3f, 0.55f, 0.75f, 0.6f);
        public Color glow = new Color(1f, 0.85f, 0.45f);
        public Color player = new Color(0.92f, 0.9f, 0.82f);
        public Color peekSky = new Color(0.22f, 0.16f, 0.34f);
        public Color peekAmbient = new Color(0.45f, 0.42f, 0.72f);
        public Color peekTint = new Color(0.42f, 0.25f, 0.75f, 0.22f);
    }

    /// <summary>One continuous rotation or swing driven by an anchor's spin speed.</summary>
    public class Spin
    {
        public Transform t;
        public Vector3 axis = Vector3.up;
        public float mult = 1f;
        public bool swing;
        public float amplitude = 22f;
        public Quaternion baseRot;
        public float phase;
    }

    /// <summary>
    /// Marks a named anchor node. The node's position is the anchor's "stand point" (where a figure stands
    /// or an effect happens); visuals are its children.
    /// </summary>
    public class AnchorTag : MonoBehaviour
    {
        public string id = "";
        public bool isFigure;
        public bool hasPark;
        public Vector3 park;
        public bool hasFallLanding;
        public Vector3 fallLanding;
        public Vector3 drownHop = Vector3.zero;
        public Vector3 fxOffset = new Vector3(0f, 0.35f, 0f);
        public Vector3 waterDir = new Vector3(0f, -1f, -0.3f);
        public readonly List<Spin> spins = new List<Spin>();
        public Action<bool> onToggle;
        public Action onLight;
        /// <summary>For toggles that move a part (hinge, signal arm): the greybox transform they rotate. A replacement model's "toggle" part follows it.</summary>
        public Transform toggleMotion;
        /// <summary>Set by a model replacement: this goes self-lit when the anchor lights up.</summary>
        public GameObject lightTarget;
        public float spinSpeed;

        public Vector3 StandPoint { get { return transform.position; } }
        public Vector3 ParkPoint { get { return hasPark ? park : transform.position; } }
        public Vector3 FxPoint { get { return transform.position + fxOffset; } }

        public Spin AddSpin(Transform t, Vector3 axis, float mult)
        {
            var s = new Spin();
            s.t = t;
            s.axis = axis;
            s.mult = mult;
            s.baseRot = t != null ? t.localRotation : Quaternion.identity;
            spins.Add(s);
            return s;
        }

        public Spin AddSwing(Transform t, Vector3 axis, float amplitude)
        {
            Spin s = AddSpin(t, axis, 1f);
            s.swing = true;
            s.amplitude = amplitude;
            return s;
        }

        public void SetSpin(float speed)
        {
            spinSpeed = speed;
        }

        private void Update()
        {
            if (Mathf.Approximately(spinSpeed, 0f)) return;
            float dt = Time.deltaTime;
            for (int i = 0; i < spins.Count; i++)
            {
                Spin s = spins[i];
                if (s.t == null) continue;
                if (s.swing)
                {
                    s.phase += dt * spinSpeed * 2.2f;
                    s.t.localRotation = s.baseRot * Quaternion.AngleAxis(Mathf.Sin(s.phase) * s.amplitude, s.axis);
                }
                else
                {
                    s.t.Rotate(s.axis, spinSpeed * s.mult * 70f * dt, Space.Self);
                }
            }
        }

        /// <summary>Top of the visual bounds, for the floating clue marker.</summary>
        public Vector3 MarkerPoint()
        {
            Renderer[] rs = GetComponentsInChildren<Renderer>();
            bool any = false;
            Bounds b = new Bounds(transform.position, Vector3.zero);
            for (int i = 0; i < rs.Length; i++)
            {
                if (rs[i] == null || !rs[i].enabled || rs[i] is ParticleSystemRenderer) continue;
                if (!any) { b = rs[i].bounds; any = true; }
                else b.Encapsulate(rs[i].bounds);
            }
            if (!any) return transform.position + Vector3.up * 0.5f;
            return new Vector3(b.center.x, b.max.y + 0.25f, b.center.z);
        }

        public void Toggle(bool on)
        {
            if (onToggle != null) { onToggle(on); return; }
            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).gameObject.SetActive(on);
            }
        }

        public void LightUp()
        {
            if (lightTarget != null) ModelSwap.Glow(lightTarget);
            if (onLight != null) { onLight(); return; }
            Light l = GetComponentInChildren<Light>(true);
            if (l == null)
            {
                var go = new GameObject("light");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.up * 0.8f;
                l = go.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 6f;
                l.color = new Color(1f, 0.85f, 0.5f);
            }
            l.gameObject.SetActive(true);
            l.enabled = true;
            l.intensity = Mathf.Max(l.intensity, 3f);
        }
    }

    /// <summary>A little capsule person. Root is at the feet; "Visual" child is what gets squashed and spun.</summary>
    public class Figure : MonoBehaviour
    {
        public Transform visual;
        public Renderer body;
        public Material mat;
        /// <summary>All tintable materials (one for the greybox capsule, several for a model).</summary>
        public readonly List<Material> mats = new List<Material>();
        public Color baseColor;
        public float height = 0.7f;
        public bool transparent;

        public static Figure Create(Transform parent, string name, Color color, float height, bool transparent, Color? hat)
        {
            return Create(parent, name, color, height, transparent, hat, null);
        }

        /// <summary>With a model (Resources/Models/common/player or death): the model replaces capsule, eyes and hat,
        /// keeps its textures, and is tinted with "color" (half-transparent for the ghost). No scaling.</summary>
        public static Figure Create(Transform parent, string name, Color color, float height, bool transparent, Color? hat, GameObject model)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            Figure f = root.AddComponent<Figure>();
            f.height = height;
            f.baseColor = color;
            f.transparent = transparent;

            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            f.visual = vis.transform;

            if (model != null)
            {
                GameObject inst = UnityEngine.Object.Instantiate(model, vis.transform, false);
                inst.name = ModelSwap.ModelChildName;
                inst.SetActive(true);
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                foreach (Collider c in inst.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                foreach (Renderer r in inst.GetComponentsInChildren<Renderer>(true))
                {
                    if (r is ParticleSystemRenderer) continue;
                    Material[] src = r.sharedMaterials;
                    var dst = new Material[Mathf.Max(1, src.Length)];
                    for (int i = 0; i < dst.Length; i++)
                    {
                        Material s = i < src.Length ? src[i] : null;
                        Material m = transparent ? Mats.TransparentUnique(color) : Mats.LitUnique(color);
                        if (m != null && s != null && s.mainTexture != null) m.mainTexture = s.mainTexture;
                        dst[i] = m;
                        f.mats.Add(m);
                    }
                    r.sharedMaterials = dst;
                    r.shadowCastingMode = transparent ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    if (f.body == null) f.body = r;
                }
                f.mat = f.mats.Count > 0 ? f.mats[0] : null;
                return f;
            }

            GameObject bodyGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyGo.name = "Body";
            StageKit.RemoveCollider(bodyGo);
            bodyGo.transform.SetParent(vis.transform, false);
            bodyGo.transform.localPosition = new Vector3(0f, height * 0.5f, 0f);
            bodyGo.transform.localScale = new Vector3(height * 0.46f, height * 0.5f, height * 0.46f);
            f.body = bodyGo.GetComponent<Renderer>();
            f.mat = transparent ? Mats.TransparentUnique(color) : Mats.LitUnique(color);
            f.body.sharedMaterial = f.mat;
            f.mats.Add(f.mat);
            f.body.shadowCastingMode = transparent ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;

            if (!transparent)
            {
                Material eyeMat = Mats.Lit(new Color(0.1f, 0.1f, 0.12f));
                for (int i = -1; i <= 1; i += 2)
                {
                    GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    eye.name = "Eye";
                    StageKit.RemoveCollider(eye);
                    eye.transform.SetParent(vis.transform, false);
                    eye.transform.localPosition = new Vector3(i * height * 0.08f, height * 0.74f, -height * 0.21f);
                    eye.transform.localScale = new Vector3(height * 0.05f, height * 0.08f, height * 0.04f);
                    eye.GetComponent<Renderer>().sharedMaterial = eyeMat;
                }
                if (hat.HasValue)
                {
                    GameObject h = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    h.name = "Hat";
                    StageKit.RemoveCollider(h);
                    h.transform.SetParent(vis.transform, false);
                    h.transform.localPosition = new Vector3(0f, height * 1.02f, 0f);
                    h.transform.localScale = new Vector3(height * 0.42f, height * 0.06f, height * 0.42f);
                    h.GetComponent<Renderer>().sharedMaterial = Mats.Lit(hat.Value);
                }
            }
            return f;
        }

        public void SetColor(Color c)
        {
            for (int i = 0; i < mats.Count; i++) Mats.SetColor(mats[i], c);
        }

        public void ResetPose()
        {
            if (visual == null) return;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            SetColor(baseColor);
        }

        /// <summary>Turn (yaw only) to face a world point.</summary>
        public void Face(Vector3 worldPoint)
        {
            Vector3 d = worldPoint - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            // Figures' eyes look down -Z, so rotate -Z onto d.
            transform.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
        }
    }

    /// <summary>
    /// The built diorama of one level: anchor lookup, the protagonist, the Shift-preview ghost, and the
    /// visible-state handlers used by "show:" facts.
    /// </summary>
    public class Stage : MonoBehaviour
    {
        public LevelData level;
        public StagePalette palette = new StagePalette();
        public Figure player;
        public Figure ghost;
        public Vector3 camFocus = new Vector3(0f, 1.5f, 0.5f);
        public float camDistance = 15f;
        public float camPitch = 30f;
        public float camYaw = -20f;
        public Vector3 npcOffset = new Vector3(0.42f, 0f, 0.18f);

        private readonly Dictionary<string, AnchorTag> anchors = new Dictionary<string, AnchorTag>();
        private readonly Dictionary<string, StateHandler> states = new Dictionary<string, StateHandler>();
        private readonly HashSet<string> showables = new HashSet<string>();
        private readonly HashSet<string> warned = new HashSet<string>();

        private class StateHandler
        {
            public string defaultValue;
            public Action<string> apply;
        }

        public IEnumerable<AnchorTag> AllAnchors { get { return anchors.Values; } }

        public void Register(AnchorTag t)
        {
            if (t == null || string.IsNullOrEmpty(t.id)) return;
            anchors[t.id] = t;
        }

        public bool Has(string id)
        {
            return !string.IsNullOrEmpty(id) && anchors.ContainsKey(id);
        }

        /// <summary>Anchor by id, or null (logs a warning once per id).</summary>
        public AnchorTag Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            AnchorTag t;
            if (anchors.TryGetValue(id, out t) && t != null) return t;
            Warn("anchor:" + id, "[Stage] Missing anchor '" + id + "' in " + (level != null ? level.id : "?"));
            return null;
        }

        public void Warn(string key, string message)
        {
            if (warned.Add(key)) Debug.LogWarning(message);
        }

        public void RegisterState(string key, string defaultValue, Action<string> apply)
        {
            var h = new StateHandler();
            h.defaultValue = defaultValue;
            h.apply = apply;
            states[key] = h;
            SafeApply(key, h, defaultValue);
        }

        private void SafeApply(string key, StateHandler h, string value)
        {
            try { h.apply(value); }
            catch (Exception e) { Warn("state-ex:" + key + "=" + value, "[Stage] State '" + key + "=" + value + "' failed: " + e.Message); }
        }

        /// <summary>Collects the subjects of every "show:x@y" fact. Those objects are only visible on the other side / during playback.</summary>
        public void InitShowables()
        {
            showables.Clear();
            if (level == null) return;
            for (int s = 0; s < level.states.Count; s++)
            {
                List<string> facts = level.GetShowFacts(level.states[s].shift);
                for (int i = 0; i < facts.Count; i++)
                {
                    int at = facts[i].IndexOf('@');
                    if (at > 0) showables.Add(facts[i].Substring(0, at));
                }
            }
            ResetShow();
        }

        /// <summary>Normal view: shift-dependent objects hidden, visible states at their defaults.</summary>
        public void ResetShow()
        {
            foreach (string id in showables)
            {
                AnchorTag t;
                if (anchors.TryGetValue(id, out t) && t != null) t.gameObject.SetActive(false);
            }
            foreach (KeyValuePair<string, StateHandler> kv in states)
            {
                SafeApply(kv.Key, kv.Value, kv.Value.defaultValue);
            }
        }

        /// <summary>Lays out the world as it is during the given shift (reads "show:" facts).</summary>
        public void ApplyShow(string shiftId)
        {
            ResetShow();
            if (level == null || string.IsNullOrEmpty(shiftId)) return;
            List<string> facts = level.GetShowFacts(shiftId);
            for (int i = 0; i < facts.Count; i++)
            {
                string f = facts[i];
                int at = f.IndexOf('@');
                int eq = f.IndexOf('=');
                if (at > 0 && at < f.Length - 1)
                {
                    string who = f.Substring(0, at);
                    string where = f.Substring(at + 1);
                    AnchorTag subject = Find(who);
                    AnchorTag place = Find(where);
                    if (subject == null || place == null) continue;
                    subject.gameObject.SetActive(true);
                    subject.transform.position = subject.isFigure ? NpcPoint(place) : place.ParkPoint;
                }
                else if (eq > 0)
                {
                    string key = f.Substring(0, eq);
                    string value = f.Substring(eq + 1);
                    StateHandler h;
                    if (states.TryGetValue(key, out h)) SafeApply(key, h, value);
                    else Warn("show:" + key, "[Stage] No handler for show fact '" + f + "' (ignored).");
                }
                else
                {
                    Warn("show:" + f, "[Stage] Unrecognised show fact '" + f + "' (ignored).");
                }
            }
        }

        public Vector3 NpcPoint(AnchorTag place)
        {
            return place.StandPoint + npcOffset;
        }

        public void ShowGhost(string placeId)
        {
            if (ghost == null) return;
            AnchorTag p = string.IsNullOrEmpty(placeId) ? null : Find(placeId);
            if (p == null) { ghost.gameObject.SetActive(false); return; }
            ghost.gameObject.SetActive(true);
            ghost.transform.position = p.StandPoint;
        }

        public void HideGhost()
        {
            if (ghost != null) ghost.gameObject.SetActive(false);
        }
    }
}
