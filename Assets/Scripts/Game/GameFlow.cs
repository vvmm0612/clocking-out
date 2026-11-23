using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// Title screen -> intro -> levels -> outro -> credits -> title. Also owns the settings and pause modals.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        private GameRoot root;
        private LevelController levelCtl;
        private List<LevelData> levels = new List<LevelData>();
        private RectTransform screen;
        private RectTransform modal;
        private bool clickedToStart;
        private float screenShownAt;
        private bool transitioning;

        public const float FadeOutSeconds = 0.25f;
        public const float FadeInSeconds = 0.3f;

        /// <summary>Name of the full-screen page being shown ("Title", "Story", "Credits"), or "" inside a level.</summary>
        public string ScreenName { get { return screen != null ? screen.name : ""; } }
        public RectTransform ScreenRect { get { return screen; } }
        public bool Transitioning { get { return transitioning; } }
        public LevelController Level { get { return levelCtl; } }
        public int LevelCount { get { return levels.Count; } }

        private static readonly Color ScreenBg = new Color(0.09f, 0.08f, 0.10f, 1f);
        private static readonly Color ScreenInk = new Color(0.95f, 0.92f, 0.84f, 1f);
        private static readonly Color ScreenInkSoft = new Color(0.70f, 0.66f, 0.60f, 1f);
        private static readonly Color Blood = new Color(0.62f, 0.2f, 0.17f, 1f);

        private void Start()
        {
            root = GetComponent<GameRoot>();
            if (root == null) root = GameRoot.Instance;
            levelCtl = gameObject.AddComponent<LevelController>();
            levelCtl.Init(root, this);
            LoadLevels();
            ShowTitle();
        }

        private void LoadLevels()
        {
            try
            {
                levels = LevelLoader.LoadAllFromResources();
            }
            catch (Exception e)
            {
                Debug.LogError("[GameFlow] Loading levels failed: " + e);
                levels = new List<LevelData>();
            }
            Debug.Log("[GameFlow] Loaded " + levels.Count + " levels.");
        }

        private void Update()
        {
            if (GameInput.GetKeyDown(KeyCode.Escape) && modal != null && GameRoot.ModalOpen)
            {
                CloseModal();
            }
        }

        // ---------------------------------------------------------------- transitions

        /// <summary>Fade to black, run the action (which swaps screens / levels), fade back in.</summary>
        public void Go(UnityAction action)
        {
            if (action == null) return;
            if (transitioning || root == null || root.Fader == null) { if (!transitioning) action(); return; }
            StartCoroutine(GoRoutine(action));
        }

        private System.Collections.IEnumerator GoRoutine(UnityAction action)
        {
            transitioning = true;
            CanvasGroup g = root.Fader;
            g.blocksRaycasts = true;
            float t = 0f;
            while (t < FadeOutSeconds)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Clamp01(t / FadeOutSeconds);
                yield return null;
            }
            g.alpha = 1f;
            try { action(); }
            catch (Exception e) { Debug.LogError("[GameFlow] Transition action failed: " + e); }
            yield return null;
            t = 0f;
            while (t < FadeInSeconds)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = 1f - Mathf.Clamp01(t / FadeInSeconds);
                yield return null;
            }
            g.alpha = 0f;
            g.blocksRaycasts = false;
            transitioning = false;
        }

        // ---------------------------------------------------------------- screens

        private RectTransform NewScreen(string name)
        {
            CloseScreen();
            Image bg = UIFactory.Panel(root.ScreenLayer, name, ScreenBg, true);
            screen = bg.rectTransform;
            UIFactory.Stretch(screen, 0f, 0f, 0f, 0f);
            screenShownAt = Time.unscaledTime;
            return screen;
        }

        private void CloseScreen()
        {
            if (screen != null) Destroy(screen.gameObject);
            screen = null;
            DestroyShowcase();
        }

        // ---------------------------------------------------------------- title showcase

        private Stage showcase;

        /// <summary>The title's backdrop: case 1's diorama on a slowly turning stand, lamp lit, framed on the right.</summary>
        private void BuildShowcase()
        {
            if (showcase != null || levels.Count == 0 || root == null) return;
            try
            {
                showcase = StageBuilder.Build(levels[0], root.World);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[GameFlow] Title showcase failed: " + e.Message);
                showcase = null;
                return;
            }
            showcase.name = "TitleShowcase";
            AnchorTag lamp = showcase.Find("lamp");
            if (lamp != null) lamp.LightUp();
            root.ApplyLighting(showcase.palette, false);
            Bounds b = showcase.fitBounds;
            // The turning stand sweeps a cylinder; frame most of it (the sea tray's corners may brush the edge).
            float r = new Vector2(b.extents.x, b.extents.z).magnitude * 0.78f;
            Bounds sweep = new Bounds(b.center, new Vector3(r * 2f, b.size.y, r * 2f));
            Turntable tt = showcase.gameObject.AddComponent<Turntable>();
            tt.center = b.center;
            root.Rig.fullScreen = true;
            root.Rig.window = Rect.MinMaxRect(-0.02f, -0.92f, 0.98f, 0.78f);
            root.Rig.FitTo(sweep, 21f, -8f, 0.86f);
            root.SetFogRange(root.Rig.Distance, b.min.y);
        }

        private void DestroyShowcase()
        {
            if (showcase != null) Destroy(showcase.gameObject);
            showcase = null;
            if (root != null && root.Rig != null) root.Rig.fullScreen = false;
        }

        /// <summary>For the press-kit tool: the title's diorama (null when not on the title).</summary>
        public Stage Showcase { get { return showcase; } }

        /// <summary>Makes the whole screen a button (with a short delay so a held click does not skip it).</summary>
        private void ClickAnywhere(RectTransform target, UnityAction action)
        {
            Button b = target.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            Navigation nav = b.navigation;
            nav.mode = Navigation.Mode.None;
            b.navigation = nav;
            b.onClick.AddListener(delegate
            {
                if (Time.unscaledTime - screenShownAt < 0.35f) return;
                if (GameRoot.ModalOpen) return;
                action();
            });
        }

        public void ShowTitle()
        {
            CloseModal();
            levelCtl.Exit();
            AudioManager.PlayBgm(clickedToStart ? "title" : "");
            RectTransform s = NewScreen("Title");
            // See-through: the turning diorama behind is the title art. The screen still catches clicks.
            s.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            BuildShowcase();

            // Left column shade so the lettering reads over any sky.
            Image shade = UIFactory.Panel(s, "Shade", new Color(0.06f, 0.05f, 0.08f, 0.78f), false);
            shade.sprite = UITextures.FadeRight();
            shade.rectTransform.anchorMin = new Vector2(0f, 0f);
            shade.rectTransform.anchorMax = new Vector2(0.62f, 1f);
            shade.rectTransform.offsetMin = Vector2.zero;
            shade.rectTransform.offsetMax = Vector2.zero;
            Image bottomShade = UIFactory.Panel(s, "BottomShade", new Color(0.06f, 0.05f, 0.08f, 0.55f), false);
            bottomShade.sprite = UITextures.Fade();
            bottomShade.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottomShade.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottomShade.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottomShade.rectTransform.sizeDelta = new Vector2(0f, 120f);
            bottomShade.rectTransform.anchoredPosition = Vector2.zero;

            const float left = 84f;
            Label kicker = UIFactory.Text(s, "Kicker", "DEPARTMENT OF FINAL ARRANGEMENTS  ·  FORM 1-D", 14f, new Color(0.86f, 0.62f, 0.5f, 1f), TextAnchor.MiddleLeft, true);
            UIFactory.Box(kicker.rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(left + 2f, 296f), new Vector2(640f, 22f));
            Label title = UIFactory.Text(s, "Title", "CLOCKING\nOUT", 96f, ScreenInk, TextAnchor.MiddleLeft, true);
            UIFactory.Box(title.rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(left - 4f, 172f), new Vector2(700f, 220f));
            Shadow ts = title.gameObject.AddComponent<Shadow>();
            ts.effectColor = new Color(0f, 0f, 0f, 0.55f);
            ts.effectDistance = new Vector2(3f, -4f);

            Image band = UIFactory.Panel(s, "Band", Blood, false);
            UIFactory.Box(band.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(left, 50f), new Vector2(430f, 6f));
            Image dia = UIFactory.Panel(s, "BandEnd", Blood, false);
            dia.sprite = UITextures.Diamond();
            UIFactory.Box(dia.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(left + 438f, 53f), new Vector2(16f, 16f));
            Label sub = UIFactory.Text(s, "Subtitle", "A paperwork puzzle about dying on schedule", 24f, new Color(0.9f, 0.85f, 0.76f, 1f), TextAnchor.MiddleLeft, false);
            UIFactory.Box(sub.rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(left, 16f), new Vector2(700f, 36f));

            Label foot = UIFactory.Text(s, "Footer", "Game Off 2026  ·  theme: SHIFT", 14f, ScreenInkSoft, TextAnchor.MiddleLeft, false);
            UIFactory.Box(foot.rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(left, 18f), new Vector2(700f, 24f));

            ReaperUI.Create(s, 330f, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, -14f), false);

            if (!clickedToStart)
            {
                Label click = UIFactory.Text(s, "Click", "Click to start", 26f, ScreenInk, TextAnchor.MiddleLeft, true);
                UIFactory.Box(click.rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(left, -60f), new Vector2(600f, 40f));
                click.gameObject.AddComponent<Pulse>();
                ClickAnywhere(s, OnFirstClick);
                return;
            }
            BuildTitleMenu(s);
        }

        private void OnFirstClick()
        {
            clickedToStart = true;
            AudioManager.PlaySfx("click");
            AudioManager.PlayBgm("title");
            if (!Progress.HasSave) Go(delegate { StartLevel(0); });
            else ShowTitle();
        }

        private void BuildTitleMenu(RectTransform s)
        {
            float y = -64f;
            const float left = 84f;
            Vector2 a = new Vector2(0f, 0.5f);
            Vector2 pv = new Vector2(0f, 0.5f);
            Color btnBg = new Color(0.95f, 0.92f, 0.84f, 1f);
            int cleared = Mathf.Min(Progress.Cleared, Mathf.Max(0, levels.Count));
            if (Progress.HasSave && levels.Count > 0)
            {
                int next = Mathf.Clamp(cleared, 0, levels.Count - 1);
                string label = cleared >= levels.Count ? "Continue (all cases closed)" : "Continue — Case " + (next + 1);
                UIButton cont = UIFactory.Button(s, "Continue", label, 22f, Blood, ScreenInk, delegate { Go(delegate { StartLevel(next); }); });
                UIFactory.Box(cont.rect, a, pv, new Vector2(left, y), new Vector2(360f, 50f));
                y -= 62f;
            }
            UIButton start = UIFactory.Button(s, "NewGame", Progress.HasSave ? "Start from Case 1" : "Start", 20f, btnBg, UIFactory.Ink, delegate { Go(delegate { StartLevel(0); }); });
            UIFactory.Box(start.rect, a, pv, new Vector2(left, y), new Vector2(360f, 44f));
            y -= 56f;
            UIButton settings = UIFactory.Button(s, "Settings", "Settings", 20f, btnBg, UIFactory.Ink, OpenSettings);
            UIFactory.Box(settings.rect, a, pv, new Vector2(left, y), new Vector2(360f, 44f));
            y -= 60f;

            if (Progress.HasSave && levels.Count > 0)
            {
                Label sel = UIFactory.Text(s, "SelectLabel", "CASE FILES", 15f, ScreenInkSoft, TextAnchor.MiddleLeft, true);
                UIFactory.Box(sel.rect, a, pv, new Vector2(left, y), new Vector2(400f, 22f));
                y -= 44f;
                float w = 66f;
                float gap = 8f;
                for (int i = 0; i < levels.Count; i++)
                {
                    int idx = i;
                    bool unlocked = i <= cleared;
                    int best = Progress.GetBest(levels[i].id);
                    string seal = best > 0 ? "\n" + Progress.SealName(best) : "";
                    Color bg = best > 0 ? Progress.SealColor(best) : (unlocked ? btnBg : new Color(0.3f, 0.28f, 0.3f, 1f));
                    UIButton b = UIFactory.Button(s, "Case" + (i + 1), (i + 1) + seal, best > 0 ? 11f : 22f, bg, UIFactory.Ink, delegate { Go(delegate { StartLevel(idx); }); });
                    UIFactory.Box(b.rect, a, pv, new Vector2(left + i * (w + gap), y), new Vector2(w, 56f));
                    b.interactable = unlocked;
                }
            }
        }

        public void StartLevel(int index)
        {
            if (levels.Count == 0)
            {
                ShowMessage("No case files were found.\n(Resources/Levels is empty.)", ShowTitle);
                return;
            }
            index = Mathf.Clamp(index, 0, levels.Count - 1);
            LevelData lvl = levels[index];
            int captured = index;
            if (!string.IsNullOrEmpty(lvl.intro)) ShowStory(lvl.intro, "— Death", delegate { BeginLevel(captured); });
            else BeginLevel(index);
        }

        private void BeginLevel(int index)
        {
            CloseScreen();
            CloseModal();
            levelCtl.Begin(levels[index], index, levels.Count);
        }

        /// <summary>Called by the level after the certificate's Next button.</summary>
        public void OnLevelApproved(int index)
        {
            LevelData lvl = (index >= 0 && index < levels.Count) ? levels[index] : null;
            bool last = index >= levels.Count - 1;
            UnityAction next = delegate
            {
                if (last) ShowCredits();
                else StartLevel(index + 1);
            };
            if (lvl != null && !string.IsNullOrEmpty(lvl.outro)) Go(delegate { ShowStory(lvl.outro, "", next); });
            else Go(next);
        }

        private void ShowStory(string text, string speaker, UnityAction onContinue)
        {
            levelCtl.Exit();
            RectTransform s = NewScreen("Story");
            Label body = UIFactory.Text(s, "Text", text, 30f, ScreenInk, TextAnchor.MiddleCenter, false);
            UIFactory.Box(body.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 30f), new Vector2(900f, 300f));
            if (!string.IsNullOrEmpty(speaker))
            {
                Label who = UIFactory.Text(s, "Speaker", speaker, 20f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
                UIFactory.Box(who.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(600f, 30f));
            }
            Label hint = UIFactory.Text(s, "Hint", "Click to continue", 16f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.Box(hint.rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 24f));
            hint.gameObject.AddComponent<Pulse>();
            ClickAnywhere(s, delegate { AudioManager.PlaySfx("page"); Go(onContinue); });
            AudioManager.PlaySfx("page");
        }

        private void ShowMessage(string text, UnityAction onContinue)
        {
            ShowStory(text, "", onContinue);
        }

        private void ShowCredits()
        {
            levelCtl.Exit();
            AudioManager.PlayBgm("credits");
            RectTransform s = NewScreen("Credits");
            Label title = UIFactory.Text(s, "Title", "CLOCKING OUT", 56f, ScreenInk, TextAnchor.MiddleCenter, true);
            UIFactory.Box(title.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 200f), new Vector2(900f, 80f));
            Label by = UIFactory.Text(s, "Byline", "A game by vvmm0612 — made for Game Off 2026 (theme: SHIFT)", 24f, ScreenInk, TextAnchor.MiddleCenter, false);
            UIFactory.Box(by.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(1000f, 36f));
            Label ai = UIFactory.Text(s, "AiNote", "Code, writing, art and audio were made with the help of generative AI tools. See the repository for details.", 17f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.Box(ai.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 72f), new Vector2(1000f, 30f));
            string body = "Built with Unity\n\nThank you for filing with us.\nYour shift begins now.";
            Label text = UIFactory.Text(s, "Body", body, 22f, ScreenInkSoft, TextAnchor.UpperCenter, false);
            UIFactory.Box(text.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(900f, 180f));
            Label hint = UIFactory.Text(s, "Hint", "Click to return to the title", 16f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.Box(hint.rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 24f));
            hint.gameObject.AddComponent<Pulse>();
            ClickAnywhere(s, delegate { Go(ShowTitle); });
        }

        // ---------------------------------------------------------------- modals

        private RectTransform NewModal(string name, Vector2 size, out RectTransform panel)
        {
            CloseModal();
            Image dim = UIFactory.Blocker(root.ModalLayer, name, new Color(0f, 0f, 0f, 0.55f), null);
            modal = dim.rectTransform;
            Image p = UIFactory.Panel(modal, "Panel", UIFactory.Paper, true);
            panel = p.rectTransform;
            UIFactory.Box(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            ResultUI.Border(panel, 3f, UIFactory.Ink);
            return modal;
        }

        public void CloseModal()
        {
            if (modal != null) Destroy(modal.gameObject);
            modal = null;
        }

        public void OpenSettings()
        {
            RectTransform panel;
            NewModal("Settings", new Vector2(460f, 300f), out panel);
            Label title = UIFactory.Text(panel, "Title", "SETTINGS", 26f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(title.rect, 18f, 34f, 20f, 20f);

            AudioManager am = AudioManager.Instance;
            AddSlider(panel, "Music", 80f, am != null ? am.MusicVolume : 0.6f, delegate (float v) { if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(v); });
            AddSlider(panel, "Sound effects", 140f, am != null ? am.SfxVolume : 0.8f, delegate (float v)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.SetSfxVolume(v);
            });

            UIButton close = UIFactory.Button(panel, "Close", "Close", 20f, Blood, ScreenInk, CloseModal);
            UIFactory.Box(close.rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(180f, 44f));
        }

        private void AddSlider(RectTransform panel, string label, float top, float value, UnityAction<float> onChange)
        {
            Label l = UIFactory.Text(panel, label, label, 18f, UIFactory.Ink, TextAnchor.MiddleLeft, false);
            UIFactory.TopRow(l.rect, top, 36f, 30f, 260f);
            Slider s = UIFactory.Slider(panel, label + "Slider", value, onChange);
            RectTransform rt = s.GetComponent<RectTransform>();
            UIFactory.TopRow(rt, top, 36f, 190f, 30f);
            s.onValueChanged.AddListener(delegate { AudioManager.PlaySfx("tick_ui"); });
        }

        public void OpenPauseMenu()
        {
            RectTransform panel;
            NewModal("Pause", new Vector2(400f, 330f), out panel);
            Label title = UIFactory.Text(panel, "Title", "PAUSED", 26f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(title.rect, 18f, 34f, 20f, 20f);
            Color bg = UIFactory.PaperDark;
            string[] names = { "Resume", "Settings", "Restart case", "Back to title" };
            UnityAction[] actions =
            {
                CloseModal,
                OpenSettings,
                delegate { CloseModal(); levelCtl.RestartLevel(); },
                delegate { Go(ShowTitle); }
            };
            for (int i = 0; i < names.Length; i++)
            {
                UIButton b = UIFactory.Button(panel, names[i], names[i], 20f, i == 0 ? Blood : bg, i == 0 ? ScreenInk : UIFactory.Ink, actions[i]);
                UIFactory.TopRow(b.rect, 70f + i * 60f, 46f, 60f, 60f);
            }
        }
    }

    /// <summary>Turns a transform slowly around a vertical axis through a point (the title diorama).</summary>
    public class Turntable : MonoBehaviour
    {
        public Vector3 center;
        public float degreesPerSecond = 7f;

        private void Update()
        {
            transform.RotateAround(center, Vector3.up, degreesPerSecond * Time.unscaledDeltaTime);
        }
    }

    /// <summary>Gently pulses a UI element's alpha (for "Click to start").</summary>
    public class Pulse : MonoBehaviour
    {
        private CanvasGroup group;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
        }

        private void Update()
        {
            group.alpha = 0.45f + 0.55f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f));
        }
    }
}
