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
        public Transform World { get; private set; }

        private float lastCanvasWidth = -1f;

        public static bool ShiftHeld
        {
            get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); }
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
            Sun.shadowStrength = 0.6f;

            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            SetAmbient(new Color(0.5f, 0.5f, 0.55f));
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
            if (Cam != null) Cam.backgroundColor = otherSide ? p.peekSky : p.sky;
            SetAmbient(otherSide ? p.peekAmbient : p.ambient);
            if (Sun != null)
            {
                Sun.color = otherSide ? new Color(0.62f, 0.55f, 0.95f) : p.sun;
                Sun.intensity = otherSide ? 0.55f : 1.1f;
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
        }

        private void Update()
        {
            UpdateLayout();
        }

        /// <summary>Keeps the paper at a fixed design width on the right and the 3D view in the rest.</summary>
        private void UpdateLayout()
        {
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
