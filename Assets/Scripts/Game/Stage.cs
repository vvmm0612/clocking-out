using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>4-6 soft colors per level plus the "other side" tints, and the level's light / sky / fog mood.</summary>
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
        /// <summary>Flat wash over the 3D view while peeking (grey-violet: cools and slightly desaturates).</summary>
        public Color peekTint = new Color(0.5f, 0.46f, 0.64f, 0.12f);

        // ---- mood (GameRoot.ApplyLighting)
        /// <summary>Sky gradient: top, horizon band (at skyMid 0..1 of the view height), bottom.</summary>
        public Color skyTop = new Color(0.45f, 0.55f, 0.7f);
        public Color skyBottom = new Color(0.5f, 0.55f, 0.6f);
        public float skyMid = 0.42f;
        /// <summary>Soft glow in the sky (sun / moon / lamp haze); alpha = strength. Position in viewport 0..1.</summary>
        public Color skyGlow = new Color(1f, 0.9f, 0.7f, 0.25f);
        public Vector2 skyGlowPos = new Vector2(0.75f, 0.6f);
        /// <summary>Main light: pitch / yaw (degrees) and intensity. Keep yaw within about -75..25 so the lit faces point at the camera.</summary>
        public Vector2 sunAngles = new Vector2(48f, -35f);
        public float sunIntensity = 1.1f;
        /// <summary>What the shaded side is multiplied by (cool violet, never black).</summary>
        public Color shadowTint = new Color(0.55f, 0.52f, 0.68f);
        /// <summary>Rim light colour; alpha = strength.</summary>
        public Color rim = new Color(1f, 0.95f, 0.85f, 0.35f);
        /// <summary>Fog / far haze colour; alpha = max amount.</summary>
        public Color fog = new Color(0.7f, 0.75f, 0.8f, 0.55f);
        /// <summary>How much the outline leans toward "dark" instead of the darkened albedo.</summary>
        public float outlineToDark = 0.35f;
    }

    /// <summary>Marks scenery the camera should not frame (open sea, tabletop, far moon).</summary>
    public class NoFit : MonoBehaviour { }

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

    /// <summary>Look of a greybox person: body shape, hat, apron... (models replace all of it).</summary>
    public enum FigureStyle { Plain, Player, Ghost, Baker, Apprentice, Signalman, Stationmaster, Watchman, Reaper }

    /// <summary>
    /// A little toy person: round head, stubby body, two dot eyes and a job marker (hat, apron, lantern).
    /// Root is at the feet; "Visual" child is what gets squashed and spun.
    /// </summary>
    public class Figure : MonoBehaviour
    {
        public Transform visual;
        public Renderer body;
        public Material mat;
        /// <summary>All tintable materials (one per coloured part for the greybox, several for a model).</summary>
        public readonly List<Material> mats = new List<Material>();
        /// <summary>Each tintable material's own colour (what ResetPose goes back to).</summary>
        public readonly List<Color> baseColors = new List<Color>();
        public Color baseColor;
        public float height = 0.7f;
        public bool transparent;

        public static Figure Create(Transform parent, string name, Color color, float height, bool transparent, Color? hat)
        {
            return Create(parent, name, color, height, transparent, hat, null, transparent ? FigureStyle.Ghost : FigureStyle.Plain);
        }

        public static Figure Create(Transform parent, string name, Color color, float height, bool transparent, Color? hat, GameObject model)
        {
            return Create(parent, name, color, height, transparent, hat, model, transparent ? FigureStyle.Ghost : FigureStyle.Plain);
        }

        /// <summary>With a model (Resources/Models/common/player or death): the model replaces the greybox person,
        /// keeps its textures, and is tinted with "color" (half-transparent for the ghost). No scaling.</summary>
        public static Figure Create(Transform parent, string name, Color color, float height, bool transparent, Color? hat, GameObject model, FigureStyle style)
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
                        Material m = transparent ? Mats.Ghost(color) : Mats.LitUnique(color);
                        if (m != null && s != null && s.mainTexture != null) m.mainTexture = s.mainTexture;
                        dst[i] = m;
                        f.mats.Add(m);
                        f.baseColors.Add(color);
                    }
                    r.sharedMaterials = dst;
                    r.shadowCastingMode = transparent ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && !transparent) ToonMesh.Apply(mf);
                    if (f.body == null) f.body = r;
                }
                f.mat = f.mats.Count > 0 ? f.mats[0] : null;
                return f;
            }

            f.BuildGreybox(color, hat, style);
            return f;
        }

        // ---------------------------------------------------------------- greybox person

        private Material ghostMat;

        private Material PartMat(Color c)
        {
            if (transparent)
            {
                if (ghostMat == null)
                {
                    ghostMat = Mats.Ghost(baseColor);
                    mats.Add(ghostMat);
                    baseColors.Add(baseColor);
                }
                return ghostMat;
            }
            Material m = Mats.LitUnique(c);
            mats.Add(m);
            baseColors.Add(c);
            return m;
        }

        private GameObject Part(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Vector3 euler, Material m, bool shadows)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            StageKit.RemoveCollider(go);
            go.transform.SetParent(visual, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            if (!transparent) ToonMesh.Apply(go.GetComponent<MeshFilter>());
            Renderer r = go.GetComponent<Renderer>();
            r.sharedMaterial = m;
            r.shadowCastingMode = shadows && !transparent ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        private static Color Skin { get { return new Color(0.98f, 0.86f, 0.74f); } }

        private void BuildGreybox(Color color, Color? hat, FigureStyle style)
        {
            float h = height;
            bool reaper = style == FigureStyle.Reaper;
            bool wide = style == FigureStyle.Baker;
            bool small = style == FigureStyle.Apprentice;
            float bw = wide ? 0.6f : small ? 0.42f : 0.48f;      // body width (x h)
            float bodyTop = reaper ? 0.66f : 0.56f;

            Material bodyMat = PartMat(color);
            // Body: a capsule squashed into a stubby bean, feet at 0.
            GameObject b = Part(PrimitiveType.Capsule, "Body", new Vector3(0f, bodyTop * 0.5f * h, 0f),
                new Vector3(bw * h, bodyTop * 0.5f * h, bw * 0.9f * h), Vector3.zero, bodyMat, true);
            body = b.GetComponent<Renderer>();
            mat = bodyMat;

            float headD = reaper ? 0.42f : 0.46f;
            float headY = bodyTop + headD * 0.42f;
            Color headColor = reaper ? color : (transparent ? color : Skin);
            Part(PrimitiveType.Sphere, "Head", new Vector3(0f, headY * h, 0f), Vector3.one * headD * h, Vector3.zero, PartMat(headColor), true);

            // Little arms
            for (int i = -1; i <= 1; i += 2)
            {
                Part(PrimitiveType.Sphere, "Arm", new Vector3(i * (bw * 0.5f + 0.02f) * h, bodyTop * 0.55f * h, 0f),
                    new Vector3(0.16f, 0.22f, 0.16f) * h, new Vector3(0f, 0f, i * 18f), bodyMat, true);
            }

            if (transparent) return;

            float eyeZ = -headD * 0.47f * h;
            if (reaper)
            {
                // Hood (a bigger dark shell) with a black face hole and two glowing eyes.
                Material hood = PartMat(Color.Lerp(color, Color.black, 0.2f));
                Part(PrimitiveType.Sphere, "Hood", new Vector3(0f, (headY + 0.03f) * h, 0.03f * h), new Vector3(0.5f, 0.56f, 0.5f) * h, Vector3.zero, hood, true);
                Part(PrimitiveType.Sphere, "Face", new Vector3(0f, headY * h, -0.12f * h), new Vector3(0.3f, 0.34f, 0.2f) * h, Vector3.zero, Mats.Lit(new Color(0.02f, 0.02f, 0.03f)), false);
                for (int i = -1; i <= 1; i += 2)
                    Part(PrimitiveType.Sphere, "Eye", new Vector3(i * 0.065f * h, (headY + 0.01f) * h, -0.21f * h), Vector3.one * 0.06f * h, Vector3.zero, Mats.Glow(new Color(0.62f, 1f, 0.78f)), false);
                return;
            }

            Material eyeMat = Mats.Lit(new Color(0.08f, 0.07f, 0.09f));
            for (int i = -1; i <= 1; i += 2)
            {
                Part(PrimitiveType.Sphere, "Eye", new Vector3(i * 0.085f * h, (headY + 0.01f) * h, eyeZ),
                    new Vector3(0.06f, 0.085f, 0.05f) * h, Vector3.zero, eyeMat, false);
            }

            Color hc = hat.HasValue ? hat.Value : Color.Lerp(color, Color.black, 0.4f);
            float top = (headY + headD * 0.5f) * h;
            switch (style)
            {
                case FigureStyle.Player:
                {
                    // Red scarf with a tail and a mustard knit cap: the protagonist reads apart from every NPC.
                    Material scarf = PartMat(new Color(0.84f, 0.24f, 0.2f));
                    Part(PrimitiveType.Cylinder, "Scarf", new Vector3(0f, bodyTop * h, 0f), new Vector3(bw * 1.02f, 0.045f, bw * 0.95f) * h, Vector3.zero, scarf, true);
                    Part(PrimitiveType.Cube, "ScarfTail", new Vector3(0.12f * h, (bodyTop - 0.12f) * h, -bw * 0.42f * h), new Vector3(0.09f, 0.2f, 0.04f) * h, new Vector3(0f, 0f, -12f), scarf, true);
                    Material cap = PartMat(new Color(0.95f, 0.72f, 0.3f));
                    Part(PrimitiveType.Sphere, "Cap", new Vector3(0f, top - 0.07f * h, 0.01f * h), new Vector3(0.44f, 0.26f, 0.44f) * h, Vector3.zero, cap, true);
                    Part(PrimitiveType.Sphere, "Pompom", new Vector3(0f, top + 0.05f * h, 0.01f * h), Vector3.one * 0.1f * h, Vector3.zero, cap, true);
                    break;
                }
                case FigureStyle.Baker:
                {
                    Material white = PartMat(hat.HasValue ? hat.Value : Color.white);
                    Part(PrimitiveType.Cylinder, "Toque", new Vector3(0f, top + 0.06f * h, 0f), new Vector3(0.3f, 0.11f, 0.3f) * h, Vector3.zero, white, true);
                    Part(PrimitiveType.Sphere, "ToquePuff", new Vector3(0f, top + 0.2f * h, 0f), new Vector3(0.4f, 0.22f, 0.4f) * h, Vector3.zero, white, true);
                    Part(PrimitiveType.Cube, "Apron", new Vector3(0f, bodyTop * 0.4f * h, -bw * 0.43f * h), new Vector3(bw * 0.7f, bodyTop * 0.62f, 0.03f) * h, Vector3.zero, PartMat(new Color(0.86f, 0.84f, 0.78f)), false);
                    break;
                }
                case FigureStyle.Apprentice:
                {
                    Material capM = PartMat(hc);
                    Part(PrimitiveType.Sphere, "Cap", new Vector3(0f, top - 0.06f * h, 0.01f * h), new Vector3(0.44f, 0.22f, 0.44f) * h, Vector3.zero, capM, true);
                    Part(PrimitiveType.Cube, "Brim", new Vector3(0f, top - 0.1f * h, -0.2f * h), new Vector3(0.3f, 0.03f, 0.16f) * h, Vector3.zero, capM, false);
                    // Flour sack on the shoulder
                    Part(PrimitiveType.Sphere, "Sack", new Vector3(0.22f * h, (bodyTop + 0.02f) * h, 0.05f * h), new Vector3(0.3f, 0.2f, 0.22f) * h, new Vector3(0f, 0f, 20f), PartMat(new Color(0.93f, 0.9f, 0.8f)), true);
                    break;
                }
                case FigureStyle.Signalman:
                {
                    Material capM = PartMat(hc);
                    Part(PrimitiveType.Cylinder, "Cap", new Vector3(0f, top - 0.03f * h, 0f), new Vector3(0.44f, 0.05f, 0.44f) * h, Vector3.zero, capM, true);
                    Part(PrimitiveType.Cube, "Brim", new Vector3(0f, top - 0.06f * h, -0.2f * h), new Vector3(0.28f, 0.025f, 0.14f) * h, Vector3.zero, capM, false);
                    // Red flag in hand
                    Part(PrimitiveType.Cube, "FlagStick", new Vector3(0.3f * h, bodyTop * 0.75f * h, -0.05f * h), new Vector3(0.025f, 0.4f, 0.025f) * h, Vector3.zero, PartMat(new Color(0.4f, 0.3f, 0.22f)), false);
                    Part(PrimitiveType.Cube, "Flag", new Vector3(0.39f * h, (bodyTop * 0.75f + 0.14f) * h, -0.05f * h), new Vector3(0.16f, 0.11f, 0.015f) * h, Vector3.zero, PartMat(new Color(0.85f, 0.22f, 0.2f)), false);
                    break;
                }
                case FigureStyle.Stationmaster:
                {
                    Material capM = PartMat(hc);
                    Part(PrimitiveType.Cylinder, "Cap", new Vector3(0f, top + 0.02f * h, 0f), new Vector3(0.46f, 0.07f, 0.46f) * h, Vector3.zero, capM, true);
                    Part(PrimitiveType.Cube, "Visor", new Vector3(0f, top - 0.05f * h, -0.22f * h), new Vector3(0.32f, 0.025f, 0.14f) * h, new Vector3(-12f, 0f, 0f), PartMat(new Color(0.08f, 0.08f, 0.1f)), false);
                    Part(PrimitiveType.Cube, "Band", new Vector3(0f, top, -0.225f * h), new Vector3(0.3f, 0.04f, 0.02f) * h, Vector3.zero, Mats.Glow(new Color(0.95f, 0.78f, 0.35f)), false);
                    Material gold = Mats.Lit(new Color(0.95f, 0.78f, 0.35f));
                    for (int i = 0; i < 3; i++)
                        Part(PrimitiveType.Sphere, "Button", new Vector3(0f, (0.16f + i * 0.12f) * h, -bw * 0.45f * h), Vector3.one * 0.045f * h, Vector3.zero, gold, false);
                    break;
                }
                case FigureStyle.Watchman:
                {
                    Material hatM = PartMat(hc);
                    Part(PrimitiveType.Cylinder, "HatBrim", new Vector3(0f, top - 0.06f * h, 0f), new Vector3(0.66f, 0.02f, 0.66f) * h, Vector3.zero, hatM, true);
                    Part(PrimitiveType.Cylinder, "HatCrown", new Vector3(0f, top + 0.02f * h, 0f), new Vector3(0.36f, 0.08f, 0.36f) * h, Vector3.zero, hatM, true);
                    // Long coat hem
                    Part(PrimitiveType.Cylinder, "Coat", new Vector3(0f, 0.1f * h, 0f), new Vector3(bw * 1.12f, 0.1f, bw * 1.0f) * h, Vector3.zero, PartMat(Color.Lerp(color, Color.black, 0.2f)), true);
                    // Hand lantern
                    Part(PrimitiveType.Cube, "Lantern", new Vector3(0.33f * h, 0.2f * h, -0.06f * h), new Vector3(0.1f, 0.14f, 0.1f) * h, Vector3.zero, Mats.Glow(new Color(1f, 0.78f, 0.4f)), false);
                    break;
                }
                default:
                {
                    if (hat.HasValue)
                    {
                        Part(PrimitiveType.Cylinder, "Hat", new Vector3(0f, top - 0.02f * h, 0f), new Vector3(0.42f, 0.05f, 0.42f) * h, Vector3.zero, PartMat(hat.Value), true);
                    }
                    break;
                }
            }
        }

        // ---------------------------------------------------------------- tint

        /// <summary>Every tintable part set to one colour.</summary>
        public void SetColor(Color c)
        {
            for (int i = 0; i < mats.Count; i++) Mats.SetColor(mats[i], c);
        }

        /// <summary>Each part moves from its own colour toward a, then toward b (burn: toward red-hot, then ash).</summary>
        public void Blend(Color a, float ka, Color b, float kb)
        {
            for (int i = 0; i < mats.Count; i++)
            {
                Color own = i < baseColors.Count ? baseColors[i] : baseColor;
                Color c = Color.Lerp(Color.Lerp(own, a, ka), b, kb);
                c.a = own.a;
                Mats.SetColor(mats[i], c);
            }
        }

        public void RestoreColors()
        {
            for (int i = 0; i < mats.Count; i++) Mats.SetColor(mats[i], i < baseColors.Count ? baseColors[i] : baseColor);
        }

        public void ResetPose()
        {
            if (visual == null) return;
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            RestoreColors();
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
        /// <summary>What the camera frames (CameraRig.FitTo): the diorama without sea, table or sky props.</summary>
        public Bounds fitBounds;
        public bool hasFitBounds;
        /// <summary>Fraction of the 3D view's height the framed diorama should fill.</summary>
        public float camFill = 0.8f;
        /// <summary>Optional hand-picked framing box (a tall level frames its building, letting the plinth run off the bottom).</summary>
        public bool hasFitOverride;
        public Bounds fitOverride;
        /// <summary>Size multiplier for the player and ghost figures (tall, deep levels make people bigger to read).</summary>
        public float figureScale = 1f;

        /// <summary>Bounds of every visible renderer except particles and objects marked <see cref="NoFit"/>.</summary>
        public void ComputeFitBounds()
        {
            if (hasFitOverride) { fitBounds = fitOverride; hasFitBounds = true; return; }
            hasFitBounds = false;
            foreach (Renderer r in GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                if (r.GetComponentInParent<NoFit>() != null) continue;
                if (!hasFitBounds) { fitBounds = r.bounds; hasFitBounds = true; }
                else fitBounds.Encapsulate(r.bounds);
            }
        }

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

        // ---------------------------------------------------------------- Shift-peek halos

        private readonly List<GameObject> halos = new List<GameObject>();
        private static Mesh haloQuad;

        /// <summary>On the other side, people (NPCs and your ghost) get a soft glow behind them so they read at a glance.</summary>
        public void SetPeekHalos(bool on)
        {
            for (int i = 0; i < halos.Count; i++) if (halos[i] != null) Destroy(halos[i]);
            halos.Clear();
            if (!on) return;
            foreach (AnchorTag t in anchors.Values)
            {
                if (t != null && t.isFigure && t.gameObject.activeInHierarchy) AddHalo(t.transform, new Color(1f, 0.9f, 0.7f, 0.6f));
            }
            if (ghost != null && ghost.gameObject.activeInHierarchy) AddHalo(ghost.transform, new Color(0.7f, 0.92f, 1f, 0.7f));
        }

        private void AddHalo(Transform who, Color c)
        {
            bool any = false;
            Bounds b = new Bounds(who.position, Vector3.zero);
            foreach (Renderer r in who.GetComponentsInChildren<Renderer>(false))
            {
                if (r == null || !r.enabled || r is ParticleSystemRenderer) continue;
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            }
            if (!any) b = new Bounds(who.position + Vector3.up * 0.35f, new Vector3(0.4f, 0.7f, 0.4f));

            if (haloQuad == null)
            {
                haloQuad = new Mesh();
                haloQuad.name = "HaloQuad";
                haloQuad.vertices = new[] { new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f), new Vector3(0.5f, 0.5f, 0f) };
                haloQuad.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
                haloQuad.colors = new[] { Color.white, Color.white, Color.white, Color.white };
                haloQuad.triangles = new[] { 0, 2, 1, 2, 3, 1 };
                haloQuad.RecalculateBounds();
            }

            var go = new GameObject("PeekHalo");
            go.transform.SetParent(who, false);
            go.transform.position = b.center;
            float size = Mathf.Max(b.size.y, Mathf.Max(b.size.x, b.size.z)) * 2.1f;
            Vector3 ls = who.lossyScale;
            float s = Mathf.Max(0.0001f, Mathf.Max(ls.x, Mathf.Max(ls.y, ls.z)));
            go.transform.localScale = Vector3.one * (size / s);
            go.AddComponent<NoFit>();
            go.AddComponent<MeshFilter>().sharedMesh = haloQuad;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Mats.Halo(c);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            go.AddComponent<FaceCamera>();
            halos.Add(go);
        }
    }

    /// <summary>Keeps a flat quad turned to the main camera.</summary>
    public class FaceCamera : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;
        }
    }
}
