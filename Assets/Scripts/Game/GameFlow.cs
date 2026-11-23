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
            if (Input.GetKeyDown(KeyCode.Escape) && modal != null && GameRoot.ModalOpen)
            {
                CloseModal();
            }
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
        }

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

            Image band = UIFactory.Panel(s, "Band", Blood, false);
            UIFactory.Box(band.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 128f), new Vector2(560f, 6f));

            Label title = UIFactory.Text(s, "Title", "CLOCKING OUT", 76f, ScreenInk, TextAnchor.MiddleCenter, true);
            UIFactory.Box(title.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 190f), new Vector2(1000f, 100f));
            Label sub = UIFactory.Text(s, "Subtitle", "A paperwork puzzle about dying on schedule", 22f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.Box(sub.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 98f), new Vector2(900f, 36f));

            Label foot = UIFactory.Text(s, "Footer", "Game Off 2026  ·  theme: SHIFT", 14f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.Box(foot.rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(700f, 24f));

            if (!clickedToStart)
            {
                Label click = UIFactory.Text(s, "Click", "Click to start", 26f, ScreenInk, TextAnchor.MiddleCenter, false);
                UIFactory.Box(click.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f), new Vector2(600f, 40f));
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
            if (!Progress.HasSave) StartLevel(0);
            else ShowTitle();
        }

        private void BuildTitleMenu(RectTransform s)
        {
            float y = 10f;
            Color btnBg = new Color(0.95f, 0.92f, 0.84f, 1f);
            int cleared = Mathf.Min(Progress.Cleared, Mathf.Max(0, levels.Count));
            if (Progress.HasSave && levels.Count > 0)
            {
                int next = Mathf.Clamp(cleared, 0, levels.Count - 1);
                string label = cleared >= levels.Count ? "Continue (all cases closed)" : "Continue — Case " + (next + 1);
                UIButton cont = UIFactory.Button(s, "Continue", label, 22f, Blood, ScreenInk, delegate { StartLevel(next); });
                UIFactory.Box(cont.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(360f, 50f));
                y -= 62f;
            }
            UIButton start = UIFactory.Button(s, "NewGame", Progress.HasSave ? "Start from Case 1" : "Start", 20f, btnBg, UIFactory.Ink, delegate { StartLevel(0); });
            UIFactory.Box(start.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(360f, 44f));
            y -= 56f;
            UIButton settings = UIFactory.Button(s, "Settings", "Settings", 20f, btnBg, UIFactory.Ink, OpenSettings);
            UIFactory.Box(settings.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(360f, 44f));
            y -= 70f;

            if (Progress.HasSave && levels.Count > 0)
            {
                Label sel = UIFactory.Text(s, "SelectLabel", "CASE FILES", 15f, ScreenInkSoft, TextAnchor.MiddleCenter, true);
                UIFactory.Box(sel.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(400f, 22f));
                y -= 44f;
                float w = 72f;
                float gap = 10f;
                float total = levels.Count * w + (levels.Count - 1) * gap;
                for (int i = 0; i < levels.Count; i++)
                {
                    int idx = i;
                    bool unlocked = i <= cleared;
                    int best = Progress.GetBest(levels[i].id);
                    string seal = best > 0 ? "\n" + Progress.SealName(best) : "";
                    Color bg = best > 0 ? Progress.SealColor(best) : (unlocked ? btnBg : new Color(0.3f, 0.28f, 0.3f, 1f));
                    UIButton b = UIFactory.Button(s, "Case" + (i + 1), (i + 1) + seal, best > 0 ? 15f : 22f, bg, UIFactory.Ink, delegate { StartLevel(idx); });
                    UIFactory.Box(b.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-total * 0.5f + w * 0.5f + i * (w + gap), y), new Vector2(w, 56f));
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
            if (lvl != null && !string.IsNullOrEmpty(lvl.outro)) ShowStory(lvl.outro, "", next);
            else next();
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
            ClickAnywhere(s, delegate { AudioManager.PlaySfx("page"); onContinue(); });
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
            string body =
                "Made in eight days for Game Off 2026 (theme: SHIFT)\n\n" +
                "Design, code and greybox art\nthe Clocking Out team\n\n" +
                "Built with Unity\n\n" +
                "Thank you for filing with us.\nYour shift begins now.";
            Label text = UIFactory.Text(s, "Body", body, 22f, ScreenInkSoft, TextAnchor.UpperCenter, false);
            UIFactory.Box(text.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(900f, 360f));
            Label hint = UIFactory.Text(s, "Hint", "Click to return to the title", 16f, ScreenInkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.Box(hint.rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(600f, 24f));
            hint.gameObject.AddComponent<Pulse>();
            ClickAnywhere(s, ShowTitle);
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
                ShowTitle
            };
            for (int i = 0; i < names.Length; i++)
            {
                UIButton b = UIFactory.Button(panel, names[i], names[i], 20f, i == 0 ? Blood : bg, i == 0 ? ScreenInk : UIFactory.Ink, actions[i]);
                UIFactory.TopRow(b.rect, 70f + i * 60f, 46f, 60f, 60f);
            }
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
