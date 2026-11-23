using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Runtime materials. Shaders are never looked up by name in a build: ProjectSetup writes template
    /// materials to Assets/Resources/Materials (Lit, Unlit, Sprite), which guarantees their shaders ship.
    /// Shader.Find is only a fallback for running in the editor before ProjectSetup has been executed.
    /// </summary>
    public static class Mats
    {
        private static bool initialized;
        private static Material litTemplate;
        private static Material unlitTemplate;
        private static Material spriteTemplate;
        private static Material particleMat;
        private static Texture2D softDot;
        private static readonly Dictionary<int, Material> litCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> unlitCache = new Dictionary<int, Material>();
        private static readonly Dictionary<int, Material> transCache = new Dictionary<int, Material>();

        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");

        private static void Init()
        {
            if (initialized) return;
            initialized = true;
            litTemplate = LoadTemplate("Materials/Lit", "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard");
            unlitTemplate = LoadTemplate("Materials/Unlit", "Universal Render Pipeline/Unlit", "Unlit/Color", null);
            spriteTemplate = LoadTemplate("Materials/Sprite", "Sprites/Default", "UI/Default", null);
            if (unlitTemplate == null) unlitTemplate = litTemplate;
            if (spriteTemplate == null) spriteTemplate = unlitTemplate;
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
                    Debug.LogWarning("[Mats] Resources/" + resourcePath + " missing; using Shader.Find(\"" + names[i] + "\"). Run ProjectSetup.Run before building.");
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

        private static Material Make(Material template, Color c)
        {
            if (template == null) return null;
            var m = new Material(template);
            SetColor(m, c);
            return m;
        }

        /// <summary>Shared opaque lit material for a color.</summary>
        public static Material Lit(Color c)
        {
            Init();
            int k = Key(c);
            Material m;
            if (litCache.TryGetValue(k, out m) && m != null) return m;
            m = Make(litTemplate, c);
            if (m != null)
            {
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
                if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", 0.15f);
            }
            litCache[k] = m;
            return m;
        }

        /// <summary>A private lit material (for things whose color animates).</summary>
        public static Material LitUnique(Color c)
        {
            Init();
            Material m = Make(litTemplate, c);
            if (m != null && m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
            return m;
        }

        private static readonly Dictionary<int, Material> modelCache = new Dictionary<int, Material>();

        /// <summary>
        /// A model's material moved onto the shipped Lit template: same color and main texture, but a shader
        /// that is guaranteed to be in the build (imported FBX materials may reference stripped shaders).
        /// Cached per source material.
        /// </summary>
        public static Material FromModel(Material src)
        {
            Init();
            if (src == null) return Lit(Color.white);
            int k = src.GetInstanceID();
            Material m;
            if (modelCache.TryGetValue(k, out m) && m != null) return m;
            m = Make(litTemplate, GetColor(src));
            if (m != null)
            {
                Texture tex = null;
                if (src.HasProperty("_BaseMap")) tex = src.GetTexture("_BaseMap");
                if (tex == null && src.HasProperty("_MainTex")) tex = src.GetTexture("_MainTex");
                if (tex != null) m.mainTexture = tex;
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.15f);
                m.name = src.name + " (project)";
            }
            modelCache[k] = m;
            return m;
        }

        /// <summary>Shared unlit (self-lit, "glowing") material.</summary>
        public static Material Glow(Color c)
        {
            Init();
            int k = Key(c);
            Material m;
            if (unlitCache.TryGetValue(k, out m) && m != null) return m;
            m = Make(unlitTemplate, c);
            unlitCache[k] = m;
            return m;
        }

        /// <summary>Alpha-blended material (Sprites/Default based). Shared per color.</summary>
        public static Material Transparent(Color c)
        {
            Init();
            int k = Key(c);
            Material m;
            if (transCache.TryGetValue(k, out m) && m != null) return m;
            m = Make(spriteTemplate, c);
            transCache[k] = m;
            return m;
        }

        public static Material TransparentUnique(Color c)
        {
            Init();
            return Make(spriteTemplate, c);
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
