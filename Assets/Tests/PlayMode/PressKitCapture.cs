using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DeathNotice.Core;
using DeathNotice.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace DeathNotice.Tests.PlayMode
{
    /// <summary>
    /// Press kit from the real game (batchmode, GPU on): five 1920x1080 screenshots and the 630x500 itch.io
    /// cover, written to docs/press/. Explicit: it only runs when asked for by name, e.g.
    ///   powershell -ExecutionPolicy Bypass -File tools\test-playmode.ps1 -Filter "DeathNotice.Tests.PlayMode.PressKitCapture"
    /// </summary>
    [Explicit("Writes docs/press; run on purpose with -Filter DeathNotice.Tests.PlayMode.PressKitCapture")]
    public class PressKitCapture
    {
        private class FakeInput : IGameInput
        {
            public bool shift;
            public Vector3 mouse = new Vector3(-9999f, -9999f, 0f);
            public bool GetKey(KeyCode key) { return shift && (key == KeyCode.LeftShift || key == KeyCode.RightShift); }
            public bool GetKeyDown(KeyCode key) { return false; }
            public bool GetMouseButton(int button) { return false; }
            public bool GetMouseButtonDown(int button) { return false; }
            public Vector3 MousePosition { get { return mouse; } }
        }

        private FakeInput input;
        private PlaytestCapture capture;
        private GameRoot root;
        private GameFlow flow;
        private LevelController lc;
        private string folder;
        private readonly List<string> errors = new List<string>();

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg);
        }

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Application.logMessageReceived += OnLog;
            input = new FakeInput();
            GameInput.Source = input;
            float until = Time.realtimeSinceStartup + 30f;
            while ((GameRoot.Instance == null || GameRoot.Instance.GetComponent<GameFlow>() == null ||
                    GameRoot.Instance.GetComponent<GameFlow>().Level == null) && Time.realtimeSinceStartup < until)
                yield return null;
            root = GameRoot.Instance;
            Assert.IsNotNull(root);
            flow = root.GetComponent<GameFlow>();
            lc = flow.Level;
            if (!PlaytestCapture.GraphicsAvailable) Assert.Ignore("needs graphics");
            folder = Path.Combine(Directory.GetCurrentDirectory(), "docs", "press");
            Directory.CreateDirectory(folder);
            capture = new PlaytestCapture();
            capture.Begin(root, 1920, 1080, folder);
            Time.timeScale = 1f;
            var ids = new List<string>();
            foreach (LevelData l in LevelLoader.LoadAllFromResources()) ids.Add(l.id);
            Progress.ResetAll(ids);
            // No first-time tips in the press shots.
            PlayerPrefs.SetInt(Tutorial.KeyCase1, 1);
            PlayerPrefs.SetInt(Tutorial.KeyShift, 1);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            GameInput.Source = null;
            if (capture != null) capture.End();
            capture = null;
            Tutorial.ResetSeen();
            Application.logMessageReceived -= OnLog;
            yield return null;
        }

        private IEnumerator Until(Func<bool> cond, string what, float timeout = 40f)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (!cond())
            {
                if (Time.realtimeSinceStartup > until) Assert.Fail("Timed out waiting for " + what);
                yield return null;
            }
        }

        private IEnumerator Seconds(float s)
        {
            float until = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        /// <summary>Starts case i and clicks through its intro, ending in Editing.</summary>
        private IEnumerator OpenCase(int i, string levelId)
        {
            flow.StartLevel(i);
            yield return Seconds(0.1f);
            if (flow.ScreenName == "Story")
            {
                yield return Until(() => !flow.Transitioning, "story");
                yield return Seconds(0.45f);
                flow.ScreenRect.GetComponent<Button>().onClick.Invoke();
            }
            yield return Until(() => lc.InLevel && lc.PhaseName == "Editing" && !flow.Transitioning && lc.CurrentStage != null && lc.CurrentStage.level.id == levelId, "editing " + levelId);
            yield return Seconds(1.2f);
        }

        private void TakeNotes(LevelData level, int max)
        {
            int n = 0;
            foreach (ClueData c in level.clues) { if (c.minRejects == 0 && n < max && lc.Form.AddNote(c.text)) n++; }
        }

        private void Shot(string name)
        {
            string p = capture.Save(name);
            Debug.Log("[PressKit] " + p);
        }

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureAll()
        {
            List<LevelData> levels = LevelLoader.LoadAllFromResources();

            // 1. Title (before the first click: big title, "Click to start", the turning lighthouse)
            flow.ShowTitle();
            yield return Seconds(3.5f);
            Shot("01_title");

            // 2. Case 1, form half filled, a few notes taken, the Place drop-down open
            yield return OpenCase(0, levels[0].id);
            LevelData l1 = levels[0];
            Dictionary<string, string> sol1 = l1.solutions[0].ToDictionary();
            TakeNotes(l1, 3);
            lc.Form.Set("cause", sol1["cause"]);
            lc.Form.RowButton(Simulator.PlaceField).button.onClick.Invoke();
            yield return Seconds(0.6f);
            Shot("02_case1_filling");

            // 3. Shift preview at the railway halt at night (ghost at the chosen place, people where they are then)
            int halt = levels.FindIndex(x => x.id == "level_04");
            yield return OpenCase(halt, "level_04");
            LevelData l4 = levels[halt];
            TakeNotes(l4, 4);
            Dictionary<string, string> sol4 = l4.solutions[0].ToDictionary();
            foreach (FieldData f in l4.fields) lc.Form.Set(f.id, sol4[f.id]);
            input.shift = true;
            yield return Until(() => lc.Peeking, "peek");
            yield return Seconds(1.0f);
            Shot("03_shift_preview");
            input.shift = false;
            yield return Until(() => !lc.Peeking, "peek off");

            // 4. Playback with a caption: the bakery fire
            int bakery = levels.FindIndex(x => x.id == "level_03");
            yield return OpenCase(bakery, "level_03");
            LevelData l3 = levels[bakery];
            TakeNotes(l3, 5);
            Dictionary<string, string> sol3 = l3.solutions[0].ToDictionary();
            foreach (FieldData f in l3.fields) lc.Form.Set(f.id, sol3[f.id]);
            lc.Form.SubmitButton.button.onClick.Invoke();
            yield return Until(() => lc.Beats.EventIndex >= Mathf.Max(0, lc.Beats.EventCount - 1) || lc.PhaseName != "Playing", "last event");
            yield return Seconds(1.3f);
            Shot("04_playback_caption");
            lc.Beats.SkipAll();

            // 5. Death certificate, first-time filing = GOLD (the mill)
            int mill = levels.FindIndex(x => x.id == "level_02");
            yield return OpenCase(mill, "level_02");
            LevelData l2 = levels[mill];
            Dictionary<string, string> sol2 = l2.solutions[0].ToDictionary();
            foreach (FieldData f in l2.fields) lc.Form.Set(f.id, sol2[f.id]);
            lc.Form.SubmitButton.button.onClick.Invoke();
            lc.Beats.SkipAll();
            yield return Until(() => lc.Certificate != null, "certificate");
            yield return Seconds(0.8f);
            Shot("05_certificate_gold");

            // 6. Cover 630x500 (rendered at 2x and downsampled): the lighthouse diorama, title and tagline
            flow.ShowTitle();
            yield return Seconds(0.3f);
            Stage show = flow.Showcase;
            Assert.IsNotNull(show, "title showcase");
            Turntable tt = show.GetComponent<Turntable>();
            if (tt != null) tt.degreesPerSecond = 0f;
            for (int i = 0; i < flow.ScreenRect.childCount; i++) flow.ScreenRect.GetChild(i).gameObject.SetActive(false);
            BuildCoverUi(flow.ScreenRect);
            // Stand turned to a good three-quarter view, framed above the lettering and left of the Reaper.
            show.transform.RotateAround(show.fitBounds.center, Vector3.up, 30f);
            show.ComputeFitBounds();
            // The box is the turned stand's axis-aligned bounds, so its corners are empty air: let them overhang.
            root.Rig.window = Rect.MinMaxRect(-1.12f, -0.34f, 0.74f, 1.1f);
            Bounds tight = show.fitBounds;
            tight.size = Vector3.Scale(tight.size, new Vector3(0.72f, 0.8f, 0.72f));
            root.Rig.FitTo(tight, 22f, -14f, 0.95f);
            root.Rig.sway = 0f;
            capture.Resize(1260, 1000);
            yield return Seconds(0.6f);
            string big = capture.Save("cover_2x");
            WriteDownsampled(big, Path.Combine(folder, "cover_630x500.png"));
            File.Delete(big);
            root.Rig.sway = 1f;
            capture.Resize(1920, 1080);
            flow.ShowTitle();
            yield return null;
            Assert.IsEmpty(errors, "errors while capturing:\n" + string.Join("\n", errors.ToArray()));
        }

        private static void BuildCoverUi(RectTransform s)
        {
            // Back to front: the Reaper on the right, a dark fade at the bottom, then the lettering.
            ReaperUI.Create(s, 500f, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(46f, 70f), false);
            Image fade = UIFactory.Panel(s, "CoverFade", new Color(0.07f, 0.05f, 0.08f, 0.94f), false);
            fade.sprite = UITextures.Fade();
            fade.rectTransform.anchorMin = new Vector2(0f, 0f);
            fade.rectTransform.anchorMax = new Vector2(1f, 0.46f);
            fade.rectTransform.offsetMin = Vector2.zero;
            fade.rectTransform.offsetMax = Vector2.zero;
            Label title = UIFactory.Text(s, "CoverTitle", "CLOCKING OUT", 112f, new Color(0.97f, 0.93f, 0.84f), TextAnchor.MiddleCenter, true);
            title.rect.anchorMin = new Vector2(0f, 0f);
            title.rect.anchorMax = new Vector2(1f, 0f);
            title.rect.pivot = new Vector2(0.5f, 0f);
            title.rect.offsetMin = new Vector2(10f, 150f);
            title.rect.offsetMax = new Vector2(-10f, 290f);
            Shadow sh = title.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.7f);
            sh.effectDistance = new Vector2(4f, -5f);
            Image band = UIFactory.Panel(s, "CoverBand", new Color(0.62f, 0.2f, 0.17f, 1f), false);
            UIFactory.Box(band.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 138f), new Vector2(620f, 8f));
            Label tag = UIFactory.Text(s, "CoverTag", "A paperwork puzzle about dying on schedule", 42f, new Color(0.93f, 0.86f, 0.76f), TextAnchor.MiddleCenter, false);
            tag.rect.anchorMin = new Vector2(0f, 0f);
            tag.rect.anchorMax = new Vector2(1f, 0f);
            tag.rect.pivot = new Vector2(0.5f, 0f);
            tag.rect.offsetMin = new Vector2(10f, 54f);
            tag.rect.offsetMax = new Vector2(-10f, 118f);
        }

        /// <summary>2x2 box filter: supersampled, so thin outlines and the lettering stay clean at 630x500.</summary>
        private static void WriteDownsampled(string src, string dst)
        {
            var big = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            big.LoadImage(File.ReadAllBytes(src));
            int w = big.width / 2, h = big.height / 2;
            var small = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color32[] a = big.GetPixels32();
            var o = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int i0 = (y * 2) * big.width + x * 2, i1 = i0 + 1, i2 = i0 + big.width, i3 = i2 + 1;
                    o[y * w + x] = new Color32(
                        (byte)((a[i0].r + a[i1].r + a[i2].r + a[i3].r) / 4),
                        (byte)((a[i0].g + a[i1].g + a[i2].g + a[i3].g) / 4),
                        (byte)((a[i0].b + a[i1].b + a[i2].b + a[i3].b) / 4), 255);
                }
            }
            small.SetPixels32(o);
            small.Apply();
            File.WriteAllBytes(dst, small.EncodeToPNG());
            UnityEngine.Object.Destroy(big);
            UnityEngine.Object.Destroy(small);
        }
    }
}
