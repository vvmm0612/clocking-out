using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DeathNotice.Core;
using DeathNotice.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeathNotice.Tests.PlayMode
{
    /// <summary>
    /// Unattended playthrough of the real game (GameRoot / GameFlow / LevelController, booted by the
    /// runtime like in a build): title -> intro -> every case (wrong answers first, then the solution)
    /// -> outro -> credits -> title. Drives the UI through its own buttons, Shift through the injectable
    /// <see cref="GameInput"/>, and fails on any error log, exception or step that times out.
    /// Screenshots go to Logs/playtest/. Run with tools/test-playmode.ps1.
    /// </summary>
    public class PlaythroughTests
    {
        private const float TimeScale = 4f;
        private const float StepTimeout = 30f; // real seconds
        private const string CauseField = "cause";

        private class FakeInput : IGameInput
        {
            public bool shift;
            public Vector3 mouse = new Vector3(-9999f, -9999f, 0f);
            public int clickFrame = -1;
            public bool GetKey(KeyCode key) { return shift && (key == KeyCode.LeftShift || key == KeyCode.RightShift); }
            public bool GetKeyDown(KeyCode key) { return false; }
            public bool GetMouseButton(int button) { return false; }
            public bool GetMouseButtonDown(int button) { return button == 0 && Time.frameCount == clickFrame; }
            public Vector3 MousePosition { get { return mouse; } }
        }

        private FakeInput input;
        private PlaytestCapture capture;
        private readonly List<string> errors = new List<string>();
        private readonly List<string> warnings = new List<string>();
        private readonly List<string> findings = new List<string>();
        private GameRoot root;
        private GameFlow flow;
        private LevelController lc;
        private string folder;

        // ---------------------------------------------------------------- setup

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            errors.Clear();
            warnings.Clear();
            findings.Clear();
            Application.logMessageReceived += OnLog;
            input = new FakeInput();
            GameInput.Source = input;

            float until = Time.realtimeSinceStartup + StepTimeout;
            while ((GameRoot.Instance == null || GameRoot.Instance.GetComponent<GameFlow>() == null ||
                    GameRoot.Instance.GetComponent<GameFlow>().Level == null) && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }
            root = GameRoot.Instance;
            Assert.IsNotNull(root, "GameRoot did not boot");
            flow = root.GetComponent<GameFlow>();
            lc = flow.Level;
            Assert.IsNotNull(lc, "GameFlow did not start");

            folder = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "playtest");
            Directory.CreateDirectory(folder);
            if (PlaytestCapture.GraphicsAvailable)
            {
                capture = new PlaytestCapture();
                capture.Begin(root, 1280, 720, folder);
            }
            Time.timeScale = TimeScale;

            // Fresh player: no progress, tips not seen yet.
            var ids = new List<string>();
            foreach (LevelData l in LevelLoader.LoadAllFromResources()) ids.Add(l.id);
            Progress.ResetAll(ids);
            Tutorial.ResetSeen();
            AudioManager.ResetCounters();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1f;
            GameInput.Source = null;
            if (capture != null) capture.End();
            capture = null;
            Application.logMessageReceived -= OnLog;
            yield return null;
        }

        private void OnLog(string msg, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(type + ": " + msg + "\n" + stack);
            else if (type == LogType.Warning) warnings.Add(msg);
        }

        // ---------------------------------------------------------------- helpers

        private IEnumerator WaitUntil(Func<bool> cond, string what, float timeout = StepTimeout)
        {
            float until = Time.realtimeSinceStartup + timeout;
            while (!cond())
            {
                if (errors.Count > 0) Assert.Fail("Error while waiting for " + what + ":\n" + errors[0]);
                if (Time.realtimeSinceStartup > until) Assert.Fail("Timed out (" + timeout + "s) waiting for: " + what + "  [screen='" + flow.ScreenName + "' phase=" + lc.PhaseName + "]");
                yield return null;
            }
        }

        private IEnumerator Frames(int n)
        {
            for (int i = 0; i < n; i++) yield return null;
        }

        private IEnumerator RealSeconds(float s)
        {
            float until = Time.realtimeSinceStartup + s;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        private void Shot(string name)
        {
            if (capture == null) return;
            string p = capture.Save(name);
            Debug.Log("[Playtest] screenshot " + p);
        }

        private void AssertNoErrors(string where)
        {
            if (errors.Count > 0) Assert.Fail("Errors logged (" + where + "):\n" + string.Join("\n---\n", errors.ToArray()));
        }

        private static void Click(UIButton b, string what)
        {
            Assert.IsNotNull(b, what + ": button missing");
            Assert.IsTrue(b.gameObject.activeInHierarchy, what + ": button not visible");
            Assert.IsTrue(b.interactable, what + ": button not interactable");
            b.button.onClick.Invoke();
        }

        /// <summary>Clicks a click-anywhere screen (title / story / credits) once its input guard has passed.</summary>
        private IEnumerator ClickScreen(string expectedName)
        {
            yield return WaitUntil(() => flow.ScreenName == expectedName && !flow.Transitioning, "screen " + expectedName);
            yield return RealSeconds(0.4f);
            Button b = flow.ScreenRect.GetComponent<Button>();
            Assert.IsNotNull(b, expectedName + " is not clickable");
            b.onClick.Invoke();
            yield return null;
        }

        private IEnumerator WaitEditing(string levelId)
        {
            yield return WaitUntil(() => lc.InLevel && lc.PhaseName == "Editing" && !flow.Transitioning &&
                                         lc.CurrentStage != null && lc.CurrentStage.level != null && lc.CurrentStage.level.id == levelId,
                "editing " + levelId);
        }

        private Button FindPopupOption(string optionId)
        {
            Transform t = FindDeep(root.PopupLayer, "Option_" + optionId);
            return t != null ? t.GetComponent<Button>() : null;
        }

        private static Transform FindDeep(Transform parent, string name)
        {
            if (parent == null) return null;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform c = parent.GetChild(i);
                if (c.name == name && c.gameObject.activeInHierarchy) return c;
                Transform d = FindDeep(c, name);
                if (d != null) return d;
            }
            return null;
        }

        /// <summary>Fills the form through the dropdowns, exactly like a player.</summary>
        private IEnumerator Fill(LevelData level, Dictionary<string, string> choice)
        {
            NoticeForm form = lc.Form;
            foreach (FieldData f in level.fields)
            {
                string want = choice[f.id];
                Click(form.RowButton(f.id), "field " + f.id);
                yield return null;
                Assert.IsTrue(form.PopupOpen, "dropdown for " + f.id + " did not open");
                Button opt = FindPopupOption(want);
                Assert.IsNotNull(opt, level.id + ": option '" + want + "' not offered in the " + f.id + " dropdown (shift held: " + input.shift + ")");
                opt.onClick.Invoke();
                yield return null;
                Assert.IsFalse(form.PopupOpen, "dropdown for " + f.id + " did not close");
                Assert.AreEqual(want, form.Get(f.id), level.id + ": " + f.id + " not set");
            }
        }

        private static string Describe(LevelData level, Dictionary<string, string> c)
        {
            var parts = new List<string>();
            foreach (FieldData f in level.fields) parts.Add(c[f.id]);
            return string.Join("+", parts.ToArray());
        }

        /// <summary>Wrong answers to file first: a reject rule, a "died but the wish failed", and one more if there is one.</summary>
        private static List<Dictionary<string, string>> PickWrong(LevelData level)
        {
            var result = new List<Dictionary<string, string>>();
            Dictionary<string, string> rejectRule = null, died = null, other = null;
            string firstRejectBy = null;
            foreach (Dictionary<string, string> c in Simulator.EnumerateCombos(level))
            {
                SimResult r = Simulator.Simulate(level, c);
                if (r.approved) continue;
                if (r.rejectedBy != null)
                {
                    if (rejectRule == null) { rejectRule = c; firstRejectBy = r.rejectedBy; }
                    else if (other == null && r.rejectedBy != firstRejectBy) other = c;
                }
                else if (died == null) died = c;
            }
            if (rejectRule != null) result.Add(rejectRule);
            if (died != null) result.Add(died);
            if (other != null) result.Add(other);
            return result;
        }

        private IEnumerator Submit(LevelData level)
        {
            NoticeForm form = lc.Form;
            Assert.IsTrue(form.IsComplete, level.id + ": form incomplete");
            int submits = AudioManager.SfxPlayCount("submit");
            Click(form.SubmitButton, "SUBMIT");
            yield return null;
            Assert.AreEqual("Playing", lc.PhaseName, level.id + ": submit did not start playback");
            Assert.AreEqual(submits + 1, AudioManager.SfxPlayCount("submit"), level.id + ": submit sfx not played");
        }

        // ---------------------------------------------------------------- clue reachability

        /// <summary>Points the (fake) mouse at each clue object and clicks it; reports clues the camera cannot hit.</summary>
        private IEnumerator TryClues(LevelData level)
        {
            Stage stage = lc.CurrentStage;
            Camera cam = root.Cam;
            int before = lc.Form.NoteCount;
            var tried = new HashSet<string>();
            int ok = 0;
            foreach (ClueData c in level.clues)
            {
                if (c.minRejects > lc.Submissions || !tried.Add(c.anchor)) continue;
                AnchorTag a = stage.Find(c.anchor);
                if (a == null || !a.gameObject.activeInHierarchy) continue;
                Vector3? screen = HoverPoint(cam, a);
                if (!screen.HasValue)
                {
                    findings.Add(level.id + ": clue '" + c.anchor + "' cannot be hovered from the camera (" + Blocker(cam, a) + ")");
                    continue;
                }
                input.mouse = screen.Value;
                yield return Frames(2);
                input.clickFrame = Time.frameCount + 1;
                yield return Frames(2);
                ok++;
            }
            input.mouse = new Vector3(-9999f, -9999f, 0f);
            yield return Frames(2);
            int added = lc.Form.NoteCount - before;
            Debug.Log("[Playtest] " + level.id + ": " + ok + " clue anchors reachable, " + added + " notes added");
            Assert.Greater(added, 0, level.id + ": clicking clues added no notes");
        }

        private static string Blocker(Camera cam, AnchorTag a)
        {
            var cols = a.GetComponentsInChildren<Collider>();
            if (cols.Length == 0) return "no collider";
            Vector3 s = cam.WorldToScreenPoint(cols[0].bounds.center);
            if (s.z <= 0f || !cam.pixelRect.Contains(new Vector2(s.x, s.y))) return "off screen";
            RaycastHit hit;
            if (!Physics.Raycast(cam.ScreenPointToRay(s), out hit, 200f)) return "ray hits nothing";
            Transform t = hit.collider.transform;
            string path = t.name;
            while (t.parent != null && t.parent.GetComponent<Stage>() == null) { t = t.parent; path = t.name + "/" + path; }
            return "blocked by " + path;
        }

        private static Vector3? HoverPoint(Camera cam, AnchorTag a)
        {
            var cols = a.GetComponentsInChildren<Collider>();
            for (int ci = 0; ci < cols.Length; ci++)
            {
                Bounds b = cols[ci].bounds;
                for (int i = 0; i < 27; i++)
                {
                    Vector3 f = new Vector3((i % 3) - 1, ((i / 3) % 3) - 1, (i / 9) - 1) * 0.35f;
                    // center first
                    if (i == 0) f = Vector3.zero;
                    Vector3 p = b.center + Vector3.Scale(b.extents, f);
                    Vector3 s = cam.WorldToScreenPoint(p);
                    if (s.z <= 0f || !cam.pixelRect.Contains(new Vector2(s.x, s.y))) continue;
                    RaycastHit hit;
                    if (Physics.Raycast(cam.ScreenPointToRay(s), out hit, 200f) && hit.collider.GetComponentInParent<AnchorTag>() == a) return s;
                }
            }
            return null;
        }

        // ---------------------------------------------------------------- one case

        private IEnumerator PlayCase(LevelData level, int index)
        {
            string tag = "L" + (index + 1);
            yield return WaitEditing(level.id);
            Assert.AreEqual("level", AudioManager.Instance.CurrentBgm, level.id + ": level music not playing");
            yield return Frames(3);

            // First-time tips
            Tutorial tips = lc.Tips;
            if (index == 0) Assert.IsTrue(tips.Visible, "case 1 tips not shown");
            if (index == 1) Assert.IsTrue(tips.Visible, "Shift tip not shown on case 2");
            Shot(tag + "_00_start");
            if (tips.Visible) { Click(tips.SkipButton.gameObject.activeSelf ? tips.SkipButton : tips.NextButton, "skip tips"); yield return null; }
            Assert.IsFalse(tips.Visible, "tips did not close");

            bool shiftOnly = level.HasShiftOnlyOptions();
            if (shiftOnly)
            {
                // Without Shift the hidden option must not be offered.
                Click(lc.Form.RowButton(CauseField), "cause dropdown");
                yield return null;
                foreach (OptionData o in level.FindField(CauseField).options)
                {
                    if (o.shiftOnly) Assert.IsNull(FindPopupOption(o.id), "shiftOnly option visible without Shift");
                    else Assert.IsNotNull(FindPopupOption(o.id), "normal option missing: " + o.id);
                }
                // Close it the way a player does: click outside the list.
                Transform blocker = FindDeep(root.PopupLayer, "Blocker");
                if (blocker != null) blocker.GetComponent<Button>().onClick.Invoke();
                yield return null;
            }

            // Wrong answers
            List<Dictionary<string, string>> wrong = PickWrong(level);
            Assert.GreaterOrEqual(wrong.Count, 1, level.id + ": no wrong combination to test");
            for (int w = 0; w < wrong.Count; w++)
            {
                Dictionary<string, string> c = wrong[w];
                yield return Fill(level, c);
                int rejects = AudioManager.SfxPlayCount("reject");
                yield return Submit(level);
                yield return WaitUntil(() => lc.PhaseName == "Result" && lc.RejectCardVisible, level.id + " REJECTED for " + Describe(level, c));
                Assert.IsFalse(lc.LastResult.approved, level.id + ": expected rejection for " + Describe(level, c));
                yield return WaitUntil(() => AudioManager.SfxPlayCount("reject") > rejects, level.id + ": reject sfx");
                Debug.Log("[Playtest] " + level.id + " " + Describe(level, c) + " -> REJECTED (" + (lc.LastResult.rejectedBy ?? "wish not met") + "): " + lc.LastResult.verdict);
                if (w == 0) Shot(tag + "_01_rejected");
                Click(lc.ReviseButton, "Revise");
                yield return WaitEditing(level.id);
                AssertNoErrors(level.id + " reject " + w);
            }

            yield return TryClues(level);

            // The solution
            Dictionary<string, string> sol = level.solutions[0].ToDictionary();
            if (shiftOnly)
            {
                input.shift = true;
                yield return WaitUntil(() => lc.Form.SupervisorMode, "supervisor mode while Shift held");
                Shot(tag + "_02_shift_held");
                yield return Fill(level, sol);
                input.shift = false;
                yield return WaitUntil(() => !lc.Form.SupervisorMode, "supervisor mode off after Shift released");
                Assert.AreEqual(sol[CauseField], lc.Form.Get(CauseField), "shiftOnly choice lost after releasing Shift");
            }
            else
            {
                yield return Fill(level, sol);
                if (level.FindField(Simulator.ShiftField) != null)
                {
                    input.shift = true;
                    yield return WaitUntil(() => lc.Peeking, "Shift preview");
                    yield return Frames(3);
                    Shot(tag + "_02_peek");
                    input.shift = false;
                    yield return WaitUntil(() => !lc.Peeking, "Shift preview closed");
                }
            }

            int approves = AudioManager.SfxPlayCount("approve");
            int stamps = AudioManager.SfxPlayCount("stamp");
            yield return Submit(level);
            yield return WaitUntil(() => lc.Beats.EventIndex >= lc.Beats.EventCount - 1 || lc.PhaseName != "Playing", level.id + " playback reaches last event");
            if (lc.PhaseName == "Playing")
            {
                float t = 0f;
                while (t < 0.9f && lc.PhaseName == "Playing") { t += Time.deltaTime; yield return null; }
                Shot(tag + "_03_playback");
            }
            yield return WaitUntil(() => lc.Certificate != null, level.id + " APPROVED + certificate");
            Assert.IsTrue(lc.LastResult.approved, level.id + ": solution not approved");
            Assert.Greater(AudioManager.SfxPlayCount("approve"), approves, level.id + ": approve sfx");
            Assert.Greater(AudioManager.SfxPlayCount("stamp"), stamps, level.id + ": stamp sfx");
            yield return Frames(3);
            Shot(tag + "_04_certificate");
            Assert.AreEqual(wrong.Count + 1, lc.Submissions);
            Assert.AreEqual(wrong.Count + 1, Progress.GetBest(level.id), level.id + ": best submissions not recorded");

            Transform next = FindDeep(lc.Certificate, "Next");
            Assert.IsNotNull(next, "certificate Next button");
            next.GetComponent<Button>().onClick.Invoke();
            yield return null;
            AssertNoErrors(level.id);
        }

        // ---------------------------------------------------------------- tests

        [UnityTest, Timeout(1200000)]
        public IEnumerator A_FullPlaythrough()
        {
            List<LevelData> levels = LevelLoader.LoadAllFromResources();
            Assert.AreEqual(6, levels.Count, "expected 6 cases");
            Assert.AreEqual(levels.Count, flow.LevelCount);

            flow.ShowTitle();
            yield return Frames(2);
            Assert.AreEqual("Title", flow.ScreenName);
            Assert.AreEqual(0, AudioManager.BgmHistory.Count, "music must not start before the first click");
            Shot("00_title");

            yield return ClickScreen("Title");
            yield return WaitUntil(() => AudioManager.Instance.CurrentBgm == "title", "title music after the first click");
            yield return WaitUntil(() => flow.ScreenName == "Story" && !flow.Transitioning, "intro story");
            Shot("00_intro");
            yield return ClickScreen("Story");

            for (int i = 0; i < levels.Count; i++)
            {
                yield return PlayCase(levels[i], i);
                LevelData lv = levels[i];
                if (!string.IsNullOrEmpty(lv.outro))
                {
                    yield return WaitUntil(() => flow.ScreenName == "Story" && !flow.Transitioning, lv.id + " outro");
                    Shot("L" + (i + 1) + "_05_outro");
                    yield return ClickScreen("Story");
                }
                if (i + 1 < levels.Count && !string.IsNullOrEmpty(levels[i + 1].intro))
                {
                    yield return WaitUntil(() => flow.ScreenName == "Story" && !flow.Transitioning, levels[i + 1].id + " intro");
                    yield return ClickScreen("Story");
                }
            }

            yield return WaitUntil(() => flow.ScreenName == "Credits" && !flow.Transitioning, "credits");
            yield return WaitUntil(() => AudioManager.Instance.CurrentBgm == "credits", "credits music");
            Shot("99_credits");
            yield return ClickScreen("Credits");
            yield return WaitUntil(() => flow.ScreenName == "Title" && !flow.Transitioning, "back to title");
            Assert.IsFalse(lc.InLevel);
            Assert.AreEqual(levels.Count, Progress.Cleared);
            yield return Frames(2);
            Shot("99_title_after");

            string[] expectBgm = { "title", "level", "credits" };
            foreach (string b in expectBgm) Assert.Contains(b, (System.Collections.ICollection)AudioManager.BgmHistory, "music '" + b + "' never played");
            Debug.Log("[Playtest] bgm history: " + string.Join(" > ", new List<string>(AudioManager.BgmHistory).ToArray()) + "; sfx plays: " + AudioManager.TotalSfxPlays);
            Report();
            AssertNoErrors("end");
            Assert.IsEmpty(findings, "Clues the player cannot reach:\n" + string.Join("\n", findings.ToArray()));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator B_LayoutAtWindowShapes()
        {
            if (capture == null) { Assert.Ignore("needs graphics (run without -nographics)"); yield break; }
            int[,] sizes = { { 1280, 720 }, { 1920, 1080 }, { 1280, 800 }, { 1024, 768 }, { 800, 600 }, { 640, 900 }, { 390, 844 } };
            List<LevelData> levels = LevelLoader.LoadAllFromResources();
            LevelData lv = levels[3]; // three fields, long title
            Tutorial.ResetSeen();
            flow.ShowTitle();
            yield return Frames(2);
            flow.StartLevel(3);
            yield return WaitEditing(lv.id);
            var problems = new List<string>();
            for (int i = 0; i < sizes.GetLength(0); i++)
            {
                int w = sizes[i, 0], h = sizes[i, 1];
                capture.Resize(w, h);
                yield return Frames(4);
                string name = "layout_" + w + "x" + h;
                problems.AddRange(LayoutCheck.Run(root, lc, name));
                Shot(name);
            }
            // Certificate and title at the narrowest shape
            capture.Resize(390, 844);
            yield return Frames(3);
            yield return Fill(lv, lv.solutions[0].ToDictionary());
            yield return Submit(lv);
            lc.Beats.SkipAll();
            yield return WaitUntil(() => lc.Certificate != null, "certificate");
            yield return Frames(3);
            problems.AddRange(LayoutCheck.InsideCanvas(root, lc.Certificate.Find("Sheet") as RectTransform, "certificate@390x844"));
            Shot("layout_390x844_certificate");
            capture.Resize(1280, 720);
            flow.ShowTitle();
            yield return Frames(3);
            foreach (string p in problems) Debug.LogWarning("[Layout] " + p);
            Assert.IsEmpty(problems, "Layout problems:\n" + string.Join("\n", problems.ToArray()));
            AssertNoErrors("layout");
        }

        private void Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[Playtest] findings: " + findings.Count);
            foreach (string f in findings) sb.AppendLine("  - " + f);
            sb.AppendLine("[Playtest] warnings logged: " + warnings.Count);
            var seen = new HashSet<string>();
            foreach (string w in warnings) if (seen.Add(w)) sb.AppendLine("  - " + w);
            Debug.Log(sb.ToString());
            File.WriteAllText(Path.Combine(folder, "report.txt"), sb.ToString());
        }
    }
}
