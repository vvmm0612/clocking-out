using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Runtime materials. Shaders are never looked up by name in a build: ProjectSetup / VisualSetup write
    /// template materials to Assets/Resources/Materials (Toon, ToonTransparent, ParticleAdd, Sky, plus the
    /// older Lit / Unlit / Sprite), which guarantees their shaders ship. Shader.Find is only a fallback for
    /// running in the editor before the setup has been executed.
    ///   Lit / LitUnique / FromModel : ClockingOut/Toon (cel shading + outline)
    ///   Glow                        : ClockingOut/Toon with _Emission = 1 (windows, lamps, flames' cores)
    ///   Water / Glass / Ghost / Beam: ClockingOut/ToonTransparent with the matching effect switches
    ///   Transparent                 : plain ToonTransparent (anything else see-through)
    ///   Particle / ParticleAdd      : Sprites/Default (alpha) / ClockingOut/ParticleAdd (fire, sparks)
    /// </summary>
    public static class Mats
    {
        private static bool initialized;
        private static Material toonTemplate;
        private static Material transTemplate;
        private static Material spriteTemplate;
        private static Material addTemplate;
        private static Material skyTemplate;
        private static Material particleMat;
        private static Material particleAddMat;
        private static Texture2D softDot;
        private static readonly Dictionary<int, Material> litCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> glowCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> transCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> waterCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> glassCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> modelCache = new Dictionary<int, Material>();

        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int EmissionId = Shader.PropertyToID("_Emission");

        private static void Init()
        {
            if (initialized) return;
            initialized = true;
            toonTemplate = LoadTemplate("Materials/Toon", "ClockingOut/Toon", "Universal Render Pipeline/Lit", "Standard");
            transTemplate = LoadTemplate("Materials/ToonTransparent", "ClockingOut/ToonTransparent", "Sprites/Default", null);
            spriteTemplate = LoadTemplate("Materials/Sprite", "Sprites/Default", "UI/Default", null);
            addTemplate = LoadTemplate("Materials/ParticleAdd", "ClockingOut/ParticleAdd", null, null);
            skyTemplate = LoadTemplate("Materials/Sky", "ClockingOut/Sky", null, null);
            if (transTemplate == null) transTemplate = spriteTemplate;
            if (spriteTemplate == null) spriteTemplate = transTemplate;
            if (addTemplate == null) addTemplate = spriteTemplate;
        }

        private static Material LoadTemplate(string resourcePath, string shaderA, string shaderB, string shaderC)
        {
            Material m = Resources.Load<Material>(resourcePath);
            if (m != null) return m;
            string[] names = { shaderA, shaderB, shaderC };
            for (int i = 0; i < names.Length; i++)
            {
                if (string.IsNullOrEmpty(names[i])) continue;
                Shader sh = Shader.Find(names[i]);
                if (sh != null)
                {
                    Debug.LogWarning("[Mats] Resources/" + resourcePath + " missing; using Shader.Find(\"" + names[i] + "\"). Run VisualSetup.Run before building.");
                    return new Material(sh);
                }
            }
            Debug.LogError("[Mats] No shader available for " + resourcePath);
            return null;
        }

        private static int Key(Color c)
        {
            Color32 k = c;
            return (k.r << 24) | (k.g << 16) | (k.b << 8) | k.a;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
            if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
        }

        public static Color GetColor(Material m)
        {
            if (m == null) return Color.white;
            if (m.HasProperty(BaseColorId)) return m.GetColor(BaseColorId);
            if (m.HasProperty(ColorId)) return m.GetColor(ColorId);
            return Color.white;
        }

        private static void SetF(Material m, string name, float v)
        {
            if (m != null && m.HasProperty(name)) m.SetFloat(name, v);
        }

        private static Material Make(Material template, Color c)
        {
            if (template == null) return null;
            var m = new Material(template);
            SetColor(m, c);
            return m;
        }

        private static Material Cached(Dictionary<int, Material> cache, Color c, System.Func<Color, Material> make)
        {
            Init();
            int k = Key(c);
            Material m;
            if (cache.TryGetValue(k, out m) && m != null) return m;
            m = make(c);
            cache[k] = m;
            return m;
        }

        /// <summary>Shared opaque cel-shaded material for a color.</summary>
        public static Material Lit(Color c)
        {
            return Cached(litCache, c, delegate (Color x) { return Make(toonTemplate, x); });
        }

        /// <summary>A private cel-shaded material (for things whose color animates).</summary>
        public static Material LitUnique(Color c)
        {
            Init();
            return Make(toonTemplate, c);
        }

        /// <summary>
        /// A model's material moved onto the shipped toon template: same color and main texture, but a shader
        /// that is guaranteed to be in the build. Cached per source material.
        /// </summary>
        public static Material FromModel(Material src)
        {
            Init();
            if (src == null) return Lit(Color.white);
            int k = src.GetInstanceID();
            Material m;
            if (modelCache.TryGetValue(k, out m) && m != null) return m;
            m = Make(toonTemplate, GetColor(src));
            if (m != null)
            {
                Texture tex = null;
                if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                if (tex != null) m.mainTexture = tex;
                m.name = src.name + " (project)";
            }
            modelCache[k] = m;
            return m;
        }

        /// <summary>Shared self-lit ("glowing") material: toon shader at full emission, keeps its outline.</summary>
        public static Material Glow(Color c)
        {
            return Cached(glowCache, c, delegate (Color x)
            {
                Material m = Make(toonTemplate, x);
                SetF(m, "_Emission", 1f);
                SetF(m, "_OutlineDarken", 0.55f);
                return m;
            });
        }

        /// <summary>Plain alpha-blended cel material. Shared per color.</summary>
        public static Material Transparent(Color c)
        {
            return Cached(transCache, c, delegate (Color x) { return Make(transTemplate, x); });
        }

        public static Material TransparentUnique(Color c)
        {
            Init();
            return Make(transTemplate, c);
        }

        /// <summary>Water: slow swell on the top face, drifting highlight bands, darker sides.</summary>
        public static Material Water(Color c)
        {
            return Cached(waterCache, c, delegate (Color x)
            {
                Material m = Make(transTemplate, x);
                SetF(m, "_Wave", 0.05f);
                SetF(m, "_Stripes", 0.7f);
                SetF(m, "_Fresnel", 0.35f);
                SetF(m, "_Rim", 0.6f);
                return m;
            });
        }

        /// <summary>Glass: faint body, opaque-ish edges and a glint band.</summary>
        public static Material Glass(Color c)
        {
            return Cached(glassCache, c, delegate (Color x)
            {
                Material m = Make(transTemplate, x);
                SetF(m, "_Fresnel", 0.9f);
                SetF(m, "_Glint", 0.6f);
                SetF(m, "_Rim", 1.4f);
                return m;
            });
        }

        /// <summary>Shift-preview ghost: see-through with glowing edges and a slow shimmer (private, it is tinted).</summary>
        public static Material Ghost(Color c)
        {
            Init();
            Material m = Make(transTemplate, c);
            SetF(m, "_Fresnel", 0.8f);
            SetF(m, "_Flicker", 0.8f);
            SetF(m, "_Rim", 2f);
            SetF(m, "_Emission", 0.35f);
            return m;
        }

        /// <summary>Light beam: self-lit, edges fade out.</summary>
        public static Material Beam(Color c)
        {
            Init();
            Material m = Make(transTemplate, c);
            SetF(m, "_Emission", 1f);
            SetF(m, "_Fresnel", 0f);
            SetF(m, "_Rim", 0f);
            return m;
        }

        /// <summary>Unlit soft blob (contact shadow under a stand): no lighting, no haze, no fresnel.</summary>
        public static Material Shadow(float alpha)
        {
            Init();
            Material m = Make(spriteTemplate, new Color(0f, 0f, 0f, alpha));
            if (m != null) m.mainTexture = SoftDot();
            return m;
        }

        /// <summary>Soft round particle material (vertex colors come from the particle system).</summary>
        public static Material Particle()
        {
            Init();
            if (particleMat != null) return particleMat;
            particleMat = Make(spriteTemplate, Color.white);
            if (particleMat != null) particleMat.mainTexture = SoftDot();
            return particleMat;
        }

        /// <summary>Additive soft particle (fire, sparks).</summary>
        public static Material ParticleAdd()
        {
            Init();
            if (particleAddMat != null) return particleAddMat;
            particleAddMat = Make(addTemplate, Color.white);
            if (particleAddMat != null) particleAddMat.mainTexture = SoftDot();
            return particleAddMat;
        }

        /// <summary>A private instance of the sky backdrop material.</summary>
        public static Material Sky()
        {
            Init();
            return skyTemplate != null ? new Material(skyTemplate) : null;
        }

        public static Texture2D SoftDot()
        {
            if (softDot != null) return softDot;
            const int n = 32;
            softDot = new Texture2D(n, n, TextureFormat.RGBA32, false);
            softDot.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            softDot.SetPixels32(px);
            softDot.Apply(false, true);
            return softDot;
        }
    }
}
