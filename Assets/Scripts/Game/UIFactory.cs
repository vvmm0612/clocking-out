using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// A piece of UI text that is either a TextMeshProUGUI or (fallback) a legacy UnityEngine.UI.Text.
    /// Callers never need to know which one they got.
    /// </summary>
    public class Label
    {
        public readonly GameObject gameObject;
        public readonly RectTransform rect;
        private readonly TextMeshProUGUI tmp;
        private readonly Text legacy;

        public Label(GameObject go, TextMeshProUGUI tmp, Text legacy)
        {
            gameObject = go;
            rect = go.GetComponent<RectTransform>();
            this.tmp = tmp;
            this.legacy = legacy;
        }

        public string text
        {
            get { return tmp != null ? tmp.text : (legacy != null ? legacy.text : ""); }
            set
            {
                string v = value ?? "";
                if (tmp != null) tmp.text = v;
                else if (legacy != null) legacy.text = v;
            }
        }

        public Color color
        {
            get { return Graphic != null ? Graphic.color : Color.white; }
            set { if (Graphic != null) Graphic.color = value; }
        }

        public float fontSize
        {
            get { return tmp != null ? tmp.fontSize : (legacy != null ? legacy.fontSize : 0f); }
            set
            {
                if (tmp != null) tmp.fontSize = value;
                else if (legacy != null) legacy.fontSize = Mathf.Max(1, Mathf.RoundToInt(value));
            }
        }

        public Graphic Graphic { get { return tmp != null ? (Graphic)tmp : legacy; } }

        /// <summary>Preferred height for the current rect width (used to size wrapped text).</summary>
        public float PreferredHeight
        {
            get
            {
                if (tmp != null) return tmp.GetPreferredValues(tmp.text, Mathf.Max(1f, rect.rect.width), 0f).y;
                if (legacy != null) return legacy.preferredHeight;
                return 0f;
            }
        }

        public void SetActive(bool on) { if (gameObject != null) gameObject.SetActive(on); }
    }

    /// <summary>Handles to a runtime-built button.</summary>
    public class UIButton
    {
        public Button button;
        public Image image;
        public Label label;
        public RectTransform rect;
        public GameObject gameObject { get { return button != null ? button.gameObject : null; } }

        public bool interactable
        {
            get { return button != null && button.interactable; }
            set { if (button != null) button.interactable = value; }
        }
    }

    /// <summary>Plays the "hover" sfx when the pointer enters an interactable selectable.</summary>
    public class HoverSfx : MonoBehaviour, IPointerEnterHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
            Selectable s = GetComponent<Selectable>();
            if (s == null || s.IsInteractable()) AudioManager.PlaySfx("hover");
        }
    }

    /// <summary>
    /// Central place that builds every piece of runtime UI. All text goes through <see cref="Text"/>,
    /// which uses TextMeshPro when its default font is available and falls back to legacy Text otherwise.
    /// </summary>
    public static class UIFactory
    {
        private static bool initialized;
        private static TMP_FontAsset tmpFont;
        private static Font legacyFont;

        public static readonly Color Ink = new Color(0.16f, 0.14f, 0.13f, 1f);
        public static readonly Color InkSoft = new Color(0.36f, 0.33f, 0.30f, 1f);
        public static readonly Color Paper = new Color(0.95f, 0.92f, 0.84f, 1f);
        public static readonly Color PaperDark = new Color(0.86f, 0.82f, 0.72f, 1f);
        public static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        public static bool UsingTmp
        {
            get { Init(); return tmpFont != null; }
        }

        private static void Init()
        {
            if (initialized) return;
            initialized = true;
            try
            {
                // Only touch TMP_Settings if the settings asset really exists; otherwise TMP logs errors.
                TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
                if (settings != null)
                {
                    tmpFont = TMP_Settings.defaultFontAsset;
                }
                if (tmpFont == null)
                {
                    tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
                }
                if (tmpFont != null && settings == null)
                {
                    // A font without settings is risky (TMP looks up settings on enable). Use legacy instead.
                    tmpFont = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[UIFactory] TextMeshPro unavailable, using legacy Text: " + e.Message);
                tmpFont = null;
            }

            try { legacyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
            catch (Exception) { legacyFont = null; }
            if (legacyFont == null)
            {
                try { legacyFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch (Exception) { legacyFont = null; }
            }
            Debug.Log("[UIFactory] Text backend: " + (tmpFont != null ? "TextMeshPro (" + tmpFont.name + ")" : "legacy UI.Text"));
        }

        // ---------------------------------------------------------------- rect helpers

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            go.layer = 5; // UI
            return go.GetComponent<RectTransform>();
        }

        /// <summary>Stretch to fill the parent with the given insets (left, top, right, bottom).</summary>
        public static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Full-width row hanging from the parent's top edge.</summary>
        public static void TopRow(RectTransform rt, float top, float height, float left, float right)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(left, -top - height);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Fixed-size box anchored at a normalized point of the parent.</summary>
        public static void Box(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        // ---------------------------------------------------------------- widgets

        public static Image Panel(Transform parent, string name, Color color, bool raycast)
        {
            RectTransform rt = Rect(parent, name);
            Image img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Label Text(Transform parent, string name, string content, float size, Color color, TextAnchor align, bool bold)
        {
            Init();
            RectTransform rt = Rect(parent, name);
            GameObject go = rt.gameObject;
            if (tmpFont != null)
            {
                try
                {
                    TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
                    t.font = tmpFont;
                    t.fontSize = size;
                    t.color = color;
                    t.alignment = MapAlign(align);
                    t.enableWordWrapping = true;
                    t.overflowMode = TextOverflowModes.Overflow;
                    t.raycastTarget = false;
                    t.richText = true;
                    if (bold) t.fontStyle = FontStyles.Bold;
                    t.text = content ?? "";
                    return new Label(go, t, null);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[UIFactory] TMP text failed, switching to legacy: " + e.Message);
                    tmpFont = null;
                    TextMeshProUGUI broken = go.GetComponent<TextMeshProUGUI>();
                    if (broken != null) UnityEngine.Object.DestroyImmediate(broken);
                }
            }

            Text lt = go.AddComponent<Text>();
            lt.font = legacyFont;
            lt.fontSize = Mathf.Max(1, Mathf.RoundToInt(size));
            lt.color = color;
            lt.alignment = align;
            lt.horizontalOverflow = HorizontalWrapMode.Wrap;
            lt.verticalOverflow = VerticalWrapMode.Overflow;
            lt.raycastTarget = false;
            lt.supportRichText = true;
            lt.lineSpacing = 1f;
            if (bold) lt.fontStyle = FontStyle.Bold;
            lt.text = content ?? "";
            return new Label(go, null, lt);
        }

        private static TextAlignmentOptions MapAlign(TextAnchor a)
        {
            switch (a)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
            }
            return TextAlignmentOptions.Center;
        }

        public static UIButton Button(Transform parent, string name, string caption, float fontSize,
                                      Color bg, Color fg, UnityAction onClick)
        {
            var b = new UIButton();
            b.image = Panel(parent, name, bg, true);
            b.rect = b.image.rectTransform;
            b.button = b.image.gameObject.AddComponent<Button>();
            b.button.targetGraphic = b.image;
            ColorBlock cb = b.button.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.08f, 1.08f, 1.08f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(0.75f, 0.75f, 0.75f, 0.55f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = 0.08f;
            b.button.colors = cb;
            Navigation nav = b.button.navigation;
            nav.mode = Navigation.Mode.None; // keeps Space free for the beat player
            b.button.navigation = nav;

            b.label = Text(b.rect, "Label", caption, fontSize, fg, TextAnchor.MiddleCenter, true);
            Stretch(b.label.rect, 6f, 2f, 6f, 2f);

            b.image.gameObject.AddComponent<HoverSfx>();
            b.image.gameObject.AddComponent<ButtonFx>();
            b.button.onClick.AddListener(delegate { AudioManager.PlaySfx("click"); });
            if (onClick != null) b.button.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>A 0..1 slider built from plain Images.</summary>
        public static Slider Slider(Transform parent, string name, float value, UnityAction<float> onChange)
        {
            RectTransform root = Rect(parent, name);
            Slider s = root.gameObject.AddComponent<Slider>();

            Image bg = Panel(root, "Background", new Color(0f, 0f, 0f, 0.25f), true);
            Stretch(bg.rectTransform, 0f, 8f, 0f, 8f);

            RectTransform fillArea = Rect(root, "Fill Area");
            Stretch(fillArea, 6f, 8f, 6f, 8f);
            Image fill = Panel(fillArea, "Fill", new Color(0.55f, 0.18f, 0.16f, 1f), false);
            fill.rectTransform.sizeDelta = Vector2.zero;

            RectTransform handleArea = Rect(root, "Handle Slide Area");
            Stretch(handleArea, 10f, 0f, 10f, 0f);
            Image handle = Panel(handleArea, "Handle", Ink, true);
            handle.rectTransform.sizeDelta = new Vector2(18f, 0f);

            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            s.minValue = 0f;
            s.maxValue = 1f;
            s.wholeNumbers = false;
            Navigation nav = s.navigation;
            nav.mode = Navigation.Mode.None;
            s.navigation = nav;
            s.value = Mathf.Clamp01(value);
            if (onChange != null) s.onValueChanged.AddListener(onChange);
            return s;
        }

        /// <summary>A full-screen, invisible raycast blocker; click calls onClick (may be null).</summary>
        public static Image Blocker(Transform parent, string name, Color tint, UnityAction onClick)
        {
            Image img = Panel(parent, name, tint, true);
            Stretch(img.rectTransform, 0f, 0f, 0f, 0f);
            if (onClick != null)
            {
                Button b = img.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                Navigation nav = b.navigation;
                nav.mode = Navigation.Mode.None;
                b.navigation = nav;
                b.onClick.AddListener(onClick);
            }
            return img;
        }

        /// <summary>Vertical scroll list. Returns the content rect; add children with a LayoutElement or text.</summary>
        public static RectTransform ScrollList(Transform parent, string name, out ScrollRect scroll)
        {
            RectTransform root = Rect(parent, name);
            Image bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.04f);
            bg.raycastTarget = true;
            scroll = root.gameObject.AddComponent<ScrollRect>();

            RectTransform viewport = Rect(root, "Viewport");
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image vpImg = viewport.gameObject.AddComponent<Image>();
            vpImg.color = Clear;
            vpImg.raycastTarget = true;

            RectTransform content = Rect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);
            VerticalLayoutGroup vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 6f;
            vlg.padding = new RectOffset(8, 8, 6, 6);
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;
            scroll.inertia = false;
            return content;
        }

        /// <summary>Camera of the main canvas: null for Screen Space - Overlay (the normal case).</summary>
        public static Camera CanvasCamera
        {
            get
            {
                GameRoot r = GameRoot.Instance;
                if (r == null || r.Canvas == null || r.Canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;
                return r.Canvas.worldCamera;
            }
        }

        /// <summary>Converts a screen-space point to local coordinates of the given rect.</summary>
        public static Vector2 ScreenToLocal(RectTransform rt, Vector2 screen)
        {
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screen, CanvasCamera, out local);
            return local;
        }

        /// <summary>World position of a UI element to screen pixels (identity for overlay canvases).</summary>
        public static Vector2 WorldToScreen(Vector3 world)
        {
            return RectTransformUtility.WorldToScreenPoint(CanvasCamera, world);
        }

        private static Sprite circle;

        /// <summary>A white anti-aliased disc (procedural, so nothing extra ships with the build).</summary>
        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[n * n];
            float r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - n * 0.5f;
                    float dy = y + 0.5f - n * 0.5f;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }

        public static Image Disc(Transform parent, string name, Color color, Vector2 center, float diameter)
        {
            Image img = Panel(parent, name, color, false);
            img.sprite = Circle();
            Box(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), center, new Vector2(diameter, diameter));
            return img;
        }

        /// <summary>
        /// Turns a flat panel into paper: tiled grain multiplied by its colour, plus a soft darkening toward the
        /// edges (an overlay child, so add it before the panel's content).
        /// </summary>
        public static void MakePaper(Image img, float edgeAlpha)
        {
            if (img == null) return;
            img.sprite = UITextures.PaperGrain();
            img.type = Image.Type.Tiled;
            img.pixelsPerUnitMultiplier = 1.4f;
            Image e = Panel(img.rectTransform, "EdgeShade", new Color(0.36f, 0.25f, 0.14f, edgeAlpha), false);
            e.sprite = UITextures.EdgeShade();
            e.type = Image.Type.Sliced;
            e.pixelsPerUnitMultiplier = 0.6f;
            Stretch(e.rectTransform, 0f, 0f, 0f, 0f);
        }

        /// <summary>Heading rule: a thick line over a thin one, with a diamond at each end.</summary>
        public static void OrnateRule(RectTransform parent, float top, float inset, Color c)
        {
            Image thick = Panel(parent, "Rule", c, false);
            TopRow(thick.rectTransform, top, 2f, inset, inset);
            Image thin = Panel(parent, "RuleThin", new Color(c.r, c.g, c.b, c.a * 0.7f), false);
            TopRow(thin.rectTransform, top + 4f, 1f, inset + 10f, inset + 10f);
            for (int i = 0; i < 2; i++)
            {
                Image d = Panel(parent, "RuleDiamond", c, false);
                d.sprite = UITextures.Diamond();
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(i, 1f);
                d.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                d.rectTransform.sizeDelta = new Vector2(9f, 9f);
                d.rectTransform.anchoredPosition = new Vector2(i == 0 ? inset : -inset, -top - 1f);
            }
        }

        /// <summary>A translucent dark pill behind a label (HUD text over bright skies).</summary>
        public static Image Backing(RectTransform label, Color c, Vector2 pad)
        {
            Image bg = Panel(label.parent, label.name + "Backing", c, false);
            RectTransform r = bg.rectTransform;
            r.SetSiblingIndex(label.GetSiblingIndex());
            r.anchorMin = label.anchorMin;
            r.anchorMax = label.anchorMax;
            r.pivot = label.pivot;
            r.anchoredPosition = label.anchoredPosition + new Vector2(-pad.x * (1f - 2f * label.pivot.x) * 0f, 0f);
            r.offsetMin = label.offsetMin - pad;
            r.offsetMax = label.offsetMax + pad;
            return bg;
        }

        public static void DestroyChildren(Transform t)
        {
            if (t == null) return;
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
            }
        }
    }
}
