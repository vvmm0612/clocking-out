using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// Runtime entry point. Spawned automatically after the first scene loads; builds the camera,
    /// light, EventSystem, Canvas and UI layers, then hands over to <see cref="GameFlow"/>.
    /// The Main scene itself stays empty.
    /// </summary>
    public class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }

        /// <summary>Design height of the UI; the canvas scales by height.</summary>
        public const float RefWidth = 1280f;
        public const float RefHeight = 720f;
        public const float PaperWidth = 500f;

        public Camera Cam { get; private set; }
        public CameraRig Rig { get; private set; }
        public Light Sun { get; private set; }
        public Canvas Canvas { get; private set; }
        public RectTransform CanvasRect { get; private set; }
        public RectTransform StageOverlay { get; private set; }
        public RectTransform PaperArea { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform ModalLayer { get; private set; }
        public RectTransform PopupLayer { get; private set; }
        /// <summary>Full-screen black overlay used for fade transitions (alpha 0 = invisible).</summary>
        public CanvasGroup Fader { get; private set; }
        public CanvasScaler Scaler { get; private set; }

        /// <summary>
        /// The canvas is never narrower than this many design units: 720 tall on wide screens, and on
        /// narrower ones (4:3, portrait) it matches this width instead, so the 500-unit paper and the HUD
        /// on its left never overlap.
        /// </summary>
        public const float MinCanvasWidth = 1100f;
        public Transform World { get; private set; }

        private float lastCanvasWidth = -1f;

        public static bool ShiftHeld
        {
            get { return GameInput.ShiftHeld; }
        }

        /// <summary>True while a modal panel (settings, menu, certificate) is up.</summary>
        public static bool ModalOpen
        {
            get
            {
                if (Instance == null || Instance.ModalLayer == null) return false;
                Transform t = Instance.ModalLayer;
                for (int i = 0; i < t.childCount; i++)
                {
                    if (t.GetChild(i).gameObject.activeSelf) return true;
                }
                return false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (Instance != null || FindObjectOfType<GameRoot>() != null) return;
            var go = new GameObject("GameRoot");
            go.AddComponent<GameRoot>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            KeepStrippableTypes();

            BuildWorld();
            BuildUI();
            gameObject.AddComponent<AudioManager>();
            gameObject.AddComponent<GameFlow>();
        }

        /// <summary>
        /// CreatePrimitive adds Sphere/Capsule/Mesh colliders by native class name only; referencing the
        /// types here (plus Assets/link.xml) keeps engine-code stripping from removing them in WebGL.
        /// </summary>
        private static System.Type[] KeepStrippableTypes()
        {
            return new System.Type[] { typeof(BoxCollider), typeof(SphereCollider), typeof(CapsuleCollider), typeof(MeshCollider), typeof(MeshFilter), typeof(MeshRenderer) };
        }

        private void BuildWorld()
        {
            // Disable any camera / light left in the scene so ours is the only one.
            Camera[] cams = FindObjectsOfType<Camera>();
            for (int i = 0; i < cams.Length; i++) cams[i].gameObject.SetActive(false);
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            for (int i = 0; i < listeners.Length; i++) listeners[i].enabled = false;

            var worldGo = new GameObject("World");
            worldGo.transform.SetParent(transform, false);
            World = worldGo.transform;

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(transform, false);
            Cam = camGo.AddComponent<Camera>();
            Cam.clearFlags = CameraClearFlags.SolidColor;
            Cam.backgroundColor = new Color(0.1f, 0.09f, 0.11f);
            Cam.fieldOfView = 38f;
            Cam.nearClipPlane = 0.1f;
            Cam.farClipPlane = 200f;
            camGo.AddComponent<AudioListener>();
            Rig = camGo.AddComponent<CameraRig>();
            Rig.cam = Cam;

            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(transform, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            Sun = sunGo.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.intensity = 1.1f;
            Sun.shadows = LightShadows.Soft;
            Sun.shadowStrength = 1f; // the toon shader turns "in shadow" into the level's shade colour, never black
            Sun.shadowBias = 0.04f;
            Sun.shadowNormalBias = 0.35f;

            BuildSky(camGo.transform);

            RenderSettings.fog = false; // the toon shaders do their own distance / height haze (_CO_Fog*)
            RenderSettings.ambientMode = AmbientMode.Flat;
            SetAmbient(new Color(0.5f, 0.5f, 0.55f));
            ApplyLighting(new StagePalette(), false);
        }

        private Material skyMat;

        /// <summary>Backdrop: a unit quad the Sky shader maps straight onto the viewport (drawn first, behind everything).</summary>
        private void BuildSky(Transform camT)
        {
            skyMat = Mats.Sky();
            if (skyMat == null) return;
            var go = new GameObject("Sky");
            go.transform.SetParent(camT, false);
            go.transform.localPosition = Vector3.forward * 10f;
            var mesh = new Mesh();
            mesh.name = "SkyQuad";
            mesh.vertices = new[] { new Vector3(-1, -1, 0), new Vector3(1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0) };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f); // never culled
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = skyMat;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Vector4 Lin(Color c) { Color l = c.linear; return new Vector4(l.r, l.g, l.b, c.a); }

        private static readonly int ShadowTintId = Shader.PropertyToID("_CO_ShadowTint");
        private static readonly int RimId = Shader.PropertyToID("_CO_RimColor");
        private static readonly int FogColorId = Shader.PropertyToID("_CO_FogColor");
        private static readonly int FogParamsId = Shader.PropertyToID("_CO_FogParams");
        private static readonly int OutlineTintId = Shader.PropertyToID("_CO_OutlineTint");
        private static readonly int OutlinePxId = Shader.PropertyToID("_CO_OutlinePx");

        /// <summary>Haze distances follow the camera distance; height haze swallows anything below the stand.</summary>
        public void SetFogRange(float cameraDistance, float groundY)
        {
            float d = Mathf.Max(4f, cameraDistance);
            Shader.SetGlobalVector(FogParamsId, new Vector4(d * 1.05f, d * 2.4f, groundY - 0.9f, 2.5f));
            var urp = GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
            if (urp != null) urp.shadowDistance = Mathf.Clamp(d * 1.8f, 15f, 60f);
        }

        public void SetAmbient(Color c)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = c;
            var sh = new SphericalHarmonicsL2();
            sh.AddAmbientLight(c);
            RenderSettings.ambientProbe = sh;
        }

        /// <summary>Sky, ambient and sun for a stage palette (or its "other side" variant).</summary>
        public void ApplyLighting(StagePalette p, bool otherSide)
        {
            if (p == null) return;
            if (Cam != null) Cam.backgroundColor = otherSide ? p.peekSky : p.skyBottom;
            SetAmbient(otherSide ? p.peekAmbient : p.ambient);
            if (Sun != null)
            {
                Sun.color = otherSide ? new Color(0.72f, 0.62f, 1f) : p.sun;
                Sun.intensity = otherSide ? 0.75f : p.sunIntensity;
                Sun.transform.rotation = Quaternion.Euler(p.sunAngles.x, p.sunAngles.y, 0f);
            }

            // "The other side" (Shift peek): everything goes violet and dreamy, same layout.
            Color shadow = otherSide ? new Color(0.42f, 0.32f, 0.66f) : p.shadowTint;
            Color rim = otherSide ? new Color(0.85f, 0.7f, 1f, 0.8f) : p.rim;
            Color top = otherSide ? new Color(0.16f, 0.09f, 0.3f) : p.skyTop;
            Color mid = otherSide ? new Color(0.5f, 0.32f, 0.7f) : p.sky;
            Color bottom = otherSide ? new Color(0.24f, 0.14f, 0.38f) : p.skyBottom;
            Color glow = otherSide ? new Color(0.85f, 0.6f, 1f, 0.35f) : p.skyGlow;
            Color fog = otherSide ? new Color(0.4f, 0.27f, 0.6f, 0.6f) : p.fog;

            Shader.SetGlobalVector(ShadowTintId, Lin(shadow));
            Shader.SetGlobalVector(RimId, Lin(rim));
            Shader.SetGlobalVector(FogColorId, Lin(fog));
            Color dark = p.dark;
            Shader.SetGlobalVector(OutlineTintId, new Vector4(dark.linear.r, dark.linear.g, dark.linear.b, p.outlineToDark));
            Shader.SetGlobalFloat(OutlinePxId, 1.5f);
            if (Shader.GetGlobalVector(FogParamsId) == Vector4.zero) SetFogRange(15f, -1f);

            if (skyMat != null)
            {
                skyMat.SetColor("_Top", top);
                skyMat.SetColor("_Mid", mid);
                skyMat.SetColor("_Bottom", bottom);
                skyMat.SetFloat("_MidHeight", p.skyMid);
                skyMat.SetColor("_Glow", glow);
                skyMat.SetVector("_GlowPos", new Vector4(p.skyGlowPos.x, p.skyGlowPos.y, 0f, 0f));
            }
        }

        private void BuildUI()
        {
            if (EventSystem.current == null && FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(transform, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("Canvas");
            canvasGo.layer = 5;
            canvasGo.transform.SetParent(transform, false);
            Canvas = canvasGo.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            Scaler = scaler;
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();
            CanvasRect = canvasGo.GetComponent<RectTransform>();

            StageOverlay = UIFactory.Rect(CanvasRect, "StageOverlay");
            UIFactory.Stretch(StageOverlay, 0f, 0f, PaperWidth, 0f);

            PaperArea = UIFactory.Rect(CanvasRect, "PaperArea");
            PaperArea.anchorMin = new Vector2(1f, 0f);
            PaperArea.anchorMax = new Vector2(1f, 1f);
            PaperArea.pivot = new Vector2(1f, 0.5f);
            PaperArea.anchoredPosition = Vector2.zero;
            PaperArea.sizeDelta = new Vector2(PaperWidth, 0f);

            ScreenLayer = UIFactory.Rect(CanvasRect, "Screens");
            UIFactory.Stretch(ScreenLayer, 0f, 0f, 0f, 0f);
            ModalLayer = UIFactory.Rect(CanvasRect, "Modals");
            UIFactory.Stretch(ModalLayer, 0f, 0f, 0f, 0f);
            PopupLayer = UIFactory.Rect(CanvasRect, "Popups");
            UIFactory.Stretch(PopupLayer, 0f, 0f, 0f, 0f);

            Image fade = UIFactory.Panel(CanvasRect, "Fader", new Color(0.04f, 0.035f, 0.045f, 1f), true);
            UIFactory.Stretch(fade.rectTransform, 0f, 0f, 0f, 0f);
            Fader = fade.gameObject.AddComponent<CanvasGroup>();
            Fader.alpha = 0f;
            Fader.blocksRaycasts = false;
            Fader.interactable = false;
            UpdateScaler();
        }

        private void Update()
        {
            UpdateLayout();
        }

        /// <summary>Keeps the paper at a fixed design width on the right and the 3D view in the rest.</summary>
        /// <summary>Match height on wide screens, width (MinCanvasWidth) on narrow ones.</summary>
        private void UpdateScaler()
        {
            if (Scaler == null || Canvas == null) return;
            Vector2 px = Canvas.renderingDisplaySize;
            if (px.x < 1f || px.y < 1f) px = new Vector2(Screen.width, Screen.height);
            if (px.x < 1f || px.y < 1f) return;
            bool narrow = px.x / px.y < MinCanvasWidth / RefHeight;
            Vector2 refRes = new Vector2(MinCanvasWidth, RefHeight);
            float match = narrow ? 0f : 1f;
            if (Scaler.referenceResolution != refRes) Scaler.referenceResolution = refRes;
            if (!Mathf.Approximately(Scaler.matchWidthOrHeight, match)) Scaler.matchWidthOrHeight = match;
        }

        private void UpdateLayout()
        {
            UpdateScaler();
            if (CanvasRect == null) return;
            float canvasW = CanvasRect.rect.width;
            if (canvasW <= 1f) return;
            if (Mathf.Abs(canvasW - lastCanvasWidth) < 0.5f) return;
            lastCanvasWidth = canvasW;
            float paperW = Mathf.Min(PaperWidth, canvasW * 0.5f);
            PaperArea.sizeDelta = new Vector2(paperW, 0f);
            UIFactory.Stretch(StageOverlay, 0f, 0f, paperW, 0f);
            if (Rig != null) Rig.stageFraction = 1f - paperW / canvasW;
        }
    }
}
