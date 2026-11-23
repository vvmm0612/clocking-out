using System;
using System.Collections;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// One case (level) from start to stamp: builds the stage and form, handles the Shift peek, clue
    /// hovering, submission, playback, and the approved / rejected outcomes.
    /// </summary>
    public class LevelController : MonoBehaviour
    {
        private enum Phase { Idle, Editing, Playing, Result }

        private GameRoot root;
        private GameFlow flow;
        private NoticeForm form;
        private PopupList popup;
        private ClueSystem clues;
        private BeatPlayer beats;

        private RectTransform hud;
        private Image peekTint;
        private Label peekLabel;
        private Label peekSub;
        private Label hintLabel;
        private Label caseLabel;
        private RectTransform captionBar;
        private Label captionText;
        private Label captionHelp;
        private UIButton skipButton;
        private UIButton menuButton;
        private UIButton resetButton;
        private ResultUI.RejectCard rejectCard;
        private RectTransform certificate;
        private Label debugLabel;

        private LevelData level;
        private int levelIndex;
        private int levelCount;
        private Stage stage;
        private Phase phase = Phase.Idle;
        private int submissions;
        private int rejections;
        private bool peeking;
        private int peekVersion = -1;
        private bool shiftOnlyLevel;
        private Tutorial tutorial;
        private ReaperUI reaper;
        private const float CaptionHeight = 104f;

        public bool InLevel { get { return phase != Phase.Idle; } }

        // ---- read-only handles for the automated playtest (Assets/Tests/PlayMode)
        public string PhaseName { get { return phase.ToString(); } }
        public NoticeForm Form { get { return form; } }
        public Stage CurrentStage { get { return stage; } }
        public BeatPlayer Beats { get { return beats; } }
        public SimResult LastResult { get; private set; }
        public int Submissions { get { return submissions; } }
        public bool Peeking { get { return peeking; } }
        public bool RejectCardVisible { get { return rejectCard != null && rejectCard.root.gameObject.activeSelf; } }
        public UIButton ReviseButton { get { return rejectCard != null ? rejectCard.revise : null; } }
        public RectTransform Certificate { get { return certificate; } }
        public Tutorial Tips { get { return tutorial; } }
        public RectTransform Hud { get { return hud; } }

        public void Init(GameRoot gameRoot, GameFlow gameFlow)
        {
            root = gameRoot;
            flow = gameFlow;
            popup = new PopupList(root.PopupLayer);
            form = new NoticeForm(root.PaperArea, popup);
            form.onSubmit = Submit;
            form.onChanged = delegate { if (peeking) ApplyPeek(); };

            hud = root.StageOverlay;
            // Dark fade along the top of the 3D view: the white HUD text stays readable on pale skies.
            Image topShade = UIFactory.Panel(hud, "TopShade", new Color(0.06f, 0.05f, 0.08f, 0.62f), false);
            topShade.sprite = UITextures.FadeDown();
            UIFactory.TopRow(topShade.rectTransform, 0f, 78f, 0f, 0f);
            peekTint = UIFactory.Panel(hud, "PeekTint", new Color(0.4f, 0.25f, 0.75f, 0.22f), false);
            UIFactory.Stretch(peekTint.rectTransform, 0f, 0f, 0f, 0f);
            peekTint.gameObject.SetActive(false);

            RectTransform markerLayer = UIFactory.Rect(hud, "Markers");
            UIFactory.Stretch(markerLayer, 0f, 0f, 0f, 0f);

            menuButton = UIFactory.Button(hud, "Menu", "Menu", 16f, new Color(0.14f, 0.12f, 0.12f, 0.85f), new Color(0.96f, 0.93f, 0.86f), delegate { flow.OpenPauseMenu(); });
            UIFactory.Box(menuButton.rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -12f), new Vector2(84f, 34f));
            resetButton = UIFactory.Button(hud, "Reset", "Reset", 16f, new Color(0.14f, 0.12f, 0.12f, 0.85f), new Color(0.96f, 0.93f, 0.86f), RestartLevel);
            UIFactory.Box(resetButton.rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(102f, -12f), new Vector2(84f, 34f));
            caseLabel = UIFactory.Text(hud, "Case", "", 17f, new Color(1f, 0.98f, 0.94f, 1f), TextAnchor.MiddleLeft, true);
            UIFactory.TopRow(caseLabel.rect, 12f, 34f, 198f, 12f);
            AddShadow(caseLabel);

            // Bottom-left corner; the caption bar covers it during playback, when Shift does nothing anyway.
            hintLabel = UIFactory.Text(hud, "Hint", "Hold SHIFT to peek at the other side", 16f, new Color(1f, 0.97f, 0.9f, 0.98f), TextAnchor.MiddleCenter, true);
            UIFactory.Box(hintLabel.rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f), new Vector2(330f, 28f));
            AddShadow(hintLabel);
            // Translucent pill behind it (it sits on the brightest part of most skies / seas).
            Image hintBack = UIFactory.Panel(hintLabel.rect, "Backing", new Color(0.07f, 0.06f, 0.09f, 0.55f), false);
            UIFactory.Stretch(hintBack.rectTransform, -2f, -1f, -2f, -1f);
            hintBack.rectTransform.SetAsFirstSibling();

            peekLabel = UIFactory.Text(hud, "PeekLabel", "", 26f, new Color(0.92f, 0.88f, 1f, 1f), TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(peekLabel.rect, 64f, 36f, 20f, 20f);
            AddShadow(peekLabel);
            AddOutline(peekLabel);
            peekSub = UIFactory.Text(hud, "PeekSub", "", 16f, new Color(0.92f, 0.88f, 1f, 0.95f), TextAnchor.MiddleCenter, false);
            UIFactory.TopRow(peekSub.rect, 100f, 48f, 30f, 30f);
            AddShadow(peekSub);
            peekLabel.SetActive(false);
            peekSub.SetActive(false);

            // Opaque strip along the bottom of the stage; while it is up the 3D view is squeezed above it
            // (CameraRig.bottomInset) so nothing that happens at the bottom of a diorama is hidden.
            Image bar = UIFactory.Panel(hud, "CaptionBar", new Color(0.09f, 0.08f, 0.09f, 1f), false);
            captionBar = bar.rectTransform;
            captionBar.anchorMin = new Vector2(0f, 0f);
            captionBar.anchorMax = new Vector2(1f, 0f);
            captionBar.pivot = new Vector2(0.5f, 0f);
            captionBar.offsetMin = new Vector2(0f, 0f);
            captionBar.offsetMax = new Vector2(0f, CaptionHeight);
            Image barEdge = UIFactory.Panel(captionBar, "Edge", new Color(0.62f, 0.2f, 0.17f, 1f), false);
            UIFactory.TopRow(barEdge.rectTransform, 0f, 3f, 0f, 0f);
            captionText = UIFactory.Text(captionBar, "Caption", "", 21f, new Color(1f, 0.97f, 0.9f, 1f), TextAnchor.MiddleCenter, false);
            UIFactory.Stretch(captionText.rect, 20f, 8f, 20f, 24f);
            captionHelp = UIFactory.Text(captionBar, "Help", "click / space: next    hold: faster", 12f, new Color(1f, 1f, 1f, 0.5f), TextAnchor.LowerCenter, false);
            UIFactory.Stretch(captionHelp.rect, 10f, 4f, 10f, 4f);
            captionBar.gameObject.SetActive(false);

            skipButton = UIFactory.Button(hud, "Skip", "Skip >>", 16f, new Color(0.14f, 0.12f, 0.12f, 0.9f), new Color(0.96f, 0.93f, 0.86f), delegate { if (beats != null) beats.SkipAll(); });
            UIFactory.Box(skipButton.rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-14f, CaptionHeight + 10f), new Vector2(100f, 34f));
            skipButton.gameObject.SetActive(false);

            rejectCard = ResultUI.BuildRejectCard(hud, Revise);

            RectTransform tipLayer = UIFactory.Rect(hud, "Tooltips");
            UIFactory.Stretch(tipLayer, 0f, 0f, 0f, 0f);

            debugLabel = UIFactory.Text(hud, "Debug", "", 15f, new Color(1f, 1f, 0.6f, 1f), TextAnchor.UpperLeft, false);
            UIFactory.Box(debugLabel.rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(14f, -56f), new Vector2(520f, 140f));
            AddShadow(debugLabel);
            debugLabel.SetActive(false);

            clues = gameObject.AddComponent<ClueSystem>();
            clues.cam = root.Cam;
            clues.form = form;
            clues.Init(hud, markerLayer, tipLayer);

            tutorial = new Tutorial(hud);

            beats = gameObject.AddComponent<BeatPlayer>();
            beats.rig = root.Rig;
            beats.showCaption = ShowCaption;

            // The Reaper stands at the bottom-left corner of the paper, facing it; drawn over the paper's
            // margin so his stamping arm comes down onto the form.
            reaper = ReaperUI.Create(root.PaperArea, 196f, new Vector2(0f, 0f), new Vector2(0.74f, 0f), new Vector2(0f, -6f), true);

            SetHudVisible(false);
        }

        private static void AddOutline(Label l)
        {
            if (l == null || l.Graphic == null) return;
            Outline o = l.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.08f, 0.05f, 0.14f, 0.6f);
            o.effectDistance = new Vector2(1.2f, -1.2f);
        }

        private static void AddShadow(Label l)
        {
            if (l == null || l.Graphic == null) return;
            Shadow s = l.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0f, 0f, 0f, 0.7f);
            s.effectDistance = new Vector2(1.5f, -1.5f);
        }

        private void SetHudVisible(bool on)
        {
            if (hud != null) hud.gameObject.SetActive(on);
            // The paper only exists inside a case (the title screen shows the 3D scene behind it).
            if (root != null && root.PaperArea != null && root.PaperArea.gameObject.activeSelf != on) root.PaperArea.gameObject.SetActive(on);
        }

        // ---------------------------------------------------------------- lifecycle

        public void Begin(LevelData lvl, int index, int count)
        {
            Exit();
            level = lvl;
            levelIndex = index;
            levelCount = count;
            submissions = 0;
            rejections = 0;
            shiftOnlyLevel = lvl != null && lvl.HasShiftOnlyOptions();

            form.Setup(lvl);
            form.SetSubmissions(0);
            caseLabel.text = "CASE " + (index + 1) + "/" + count + "  —  " + (lvl != null ? lvl.title : "");
            SetHudVisible(true);
            RebuildStage();
            phase = Phase.Editing;
            clues.SetActive(true);
            if (reaper != null) reaper.Show(true);
            AudioManager.PlayBgm("level");
            AudioManager.PlaySfx("page");
            tutorial.BeginLevel(lvl, index, shiftOnlyLevel);
        }

        /// <summary>Tears the level down (back to title, restart, next level).</summary>
        public void Exit()
        {
            if (beats != null) beats.Stop();
            StopAllCoroutines();
            if (peeking) ExitPeek();
            peeking = false;
            if (popup != null) popup.Close();
            if (clues != null) { clues.SetActive(false); clues.Bind(null, 0); }
            if (certificate != null) { Destroy(certificate.gameObject); certificate = null; }
            if (rejectCard != null) rejectCard.root.gameObject.SetActive(false);
            SetCaptionBar(false);
            if (skipButton != null) skipButton.gameObject.SetActive(false);
            if (debugLabel != null) debugLabel.SetActive(false);
            if (form != null) { form.SetSupervisor(false); form.ClearStamps(); }
            if (tutorial != null) tutorial.Hide();
            if (stage != null) { Destroy(stage.gameObject); stage = null; }
            SetHudVisible(false);
            phase = Phase.Idle;
        }

        public void RestartLevel()
        {
            if (level == null) return;
            AudioManager.PlaySfx("page");
            Begin(level, levelIndex, levelCount);
        }

        private void RebuildStage()
        {
            if (stage != null)
            {
                stage.gameObject.SetActive(false);
                Destroy(stage.gameObject);
                stage = null;
            }
            try
            {
                stage = StageBuilder.Build(level, root.World);
            }
            catch (Exception e)
            {
                Debug.LogError("[LevelController] Stage build failed: " + e);
                var go = new GameObject("Stage_empty");
                go.transform.SetParent(root.World, false);
                stage = go.AddComponent<Stage>();
                stage.level = level;
            }
            root.ApplyLighting(stage.palette, false);
            FrameStage();
            clues.Bind(stage, rejections);
        }

        /// <summary>Camera fits the diorama (the layout's pitch / yaw, auto distance); haze follows the distance.</summary>
        private void FrameStage()
        {
            if (stage == null) return;
            root.Rig.fullScreen = false;
            if (stage.hasFitBounds)
            {
                root.Rig.window = Rect.MinMaxRect(-0.92f, -0.86f, 0.92f, 0.8f);
                root.Rig.FitTo(stage.fitBounds, stage.camPitch, stage.camYaw, stage.camFill);
                root.SetFogRange(root.Rig.Distance, stage.fitBounds.min.y);
            }
            else
            {
                root.Rig.Frame(stage.camFocus, stage.camDistance, stage.camPitch, stage.camYaw);
                root.SetFogRange(stage.camDistance, -1f);
            }
        }

        // ---------------------------------------------------------------- update

        private void Update()
        {
            if (phase == Phase.Idle) return;
            bool modal = GameRoot.ModalOpen;

            if (hintLabel.gameObject.activeSelf != (phase == Phase.Editing)) hintLabel.SetActive(phase == Phase.Editing);
            tutorial.Tick(phase == Phase.Editing && !peeking && !modal, form);
            if (phase == Phase.Editing)
            {
                UpdateShift(modal);
                clues.SetActive(!peeking && !modal);
            }
            else if (peeking)
            {
                ExitPeek();
            }

            if (!modal && GameInput.GetKeyDown(KeyCode.Escape))
            {
                if (popup.IsOpen) popup.Close();
                else if (phase != Phase.Playing) flow.OpenPauseMenu();
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (GameInput.GetKeyDown(KeyCode.F1)) ToggleDebugSolution();
#endif
        }

        private void UpdateShift(bool modal)
        {
            bool held = GameRoot.ShiftHeld && !modal;
            if (shiftOnlyLevel)
            {
                if (held != form.SupervisorMode)
                {
                    form.SetSupervisor(held);
                    AudioManager.PlaySfx(held ? "shift_in" : "shift_out");
                }
                return;
            }
            if (held && !peeking) EnterPeek();
            else if (!held && peeking) ExitPeek();
            else if (peeking && form.Version != peekVersion) ApplyPeek();
        }

        private void EnterPeek()
        {
            if (stage == null) return;
            peeking = true;
            clues.SetActive(false);
            peekTint.color = stage.palette.peekTint;
            peekTint.gameObject.SetActive(true);
            peekLabel.SetActive(true);
            peekSub.SetActive(true);
            AudioManager.PlaySfx("shift_in");
            ApplyPeek();
        }

        private void ApplyPeek()
        {
            if (stage == null || level == null) return;
            peekVersion = form.Version;
            FieldData shiftField = level.FindField(Simulator.ShiftField);
            string sub = "";
            string title = "THE OTHER SIDE";
            if (shiftField != null && shiftField.options.Count > 0)
            {
                string shiftId = form.Get(Simulator.ShiftField);
                bool picked = shiftId != null;
                if (!picked) shiftId = shiftField.options[0].id;
                OptionData o = shiftField.FindOption(shiftId);
                title += " — " + (o != null ? o.label : shiftId);
                if (!picked) sub = "No shift chosen yet: showing the first one. Pick a Shift on the form.";
                stage.ApplyShow(shiftId);
            }
            else
            {
                stage.ResetShow();
            }

            string place = form.Get(Simulator.PlaceField);
            if (place != null) stage.ShowGhost(place);
            else
            {
                stage.HideGhost();
                sub = (sub.Length > 0 ? sub + "\n" : "") + "Pick a Place to see where you would be.";
            }
            peekLabel.text = title;
            peekSub.text = sub;
            root.ApplyLighting(stage.palette, true);
        }

        private void ExitPeek()
        {
            peeking = false;
            peekTint.gameObject.SetActive(false);
            peekLabel.SetActive(false);
            peekSub.SetActive(false);
            if (stage != null)
            {
                stage.ResetShow();
                stage.HideGhost();
                root.ApplyLighting(stage.palette, false);
            }
            if (phase == Phase.Editing) AudioManager.PlaySfx("shift_out");
        }

        // ---------------------------------------------------------------- submit & playback

        private void Submit()
        {
            if (phase != Phase.Editing || level == null || !form.IsComplete) return;
            if (peeking) ExitPeek();
            form.SetSupervisor(false);
            popup.Close();

            submissions++;
            form.SetSubmissions(submissions);
            tutorial.OnSubmitted();
            Dictionary<string, string> choices = form.GetChoices();

            SimResult result;
            try
            {
                result = Simulator.Simulate(level, choices);
            }
            catch (Exception e)
            {
                Debug.LogError("[LevelController] Simulate failed: " + e);
                result = new SimResult();
                result.approved = false;
                result.verdict = "The clerk spilled ink on the form. Please file it again.";
            }

            LastResult = result;
            phase = Phase.Playing;
            form.SetLocked(true);
            RebuildStage();
            clues.SetActive(false);
            string shift = form.Get(Simulator.ShiftField);
            if (!string.IsNullOrEmpty(shift)) stage.ApplyShow(shift);

            ShowCaption("");
            SetCaptionBar(true);
            skipButton.gameObject.SetActive(true);
            if (reaper != null) reaper.Show(false); // the Skip button lives in his corner during playback
            AudioManager.PlaySfx("submit");
            SimResult captured = result;
            beats.Play(stage, result, delegate { OnPlaybackDone(captured); });
        }

        private void SetCaptionBar(bool on)
        {
            if (captionBar == null) return;
            captionBar.gameObject.SetActive(on);
            float h = root.CanvasRect != null ? root.CanvasRect.rect.height : GameRoot.RefHeight;
            if (root.Rig != null) root.Rig.bottomInset = on && h > 1f ? CaptionHeight / h : 0f;
            if (reaper != null) reaper.SetBaseY(on ? CaptionHeight - 14f : -6f);
        }

        private void ShowCaption(string text)
        {
            if (captionText != null) captionText.text = text ?? "";
        }

        private void OnPlaybackDone(SimResult result)
        {
            skipButton.gameObject.SetActive(false);
            phase = Phase.Result;
            if (result.approved) StartCoroutine(ApproveRoutine(result));
            else StartCoroutine(RejectRoutine(result));
        }

        private IEnumerator StampRoutine(string word, Color color)
        {
            form.ClearStamps();
            // Death raises the stamp...
            if (reaper != null) { reaper.Show(true); reaper.Stamp(0.24f, 0.14f); }
            float t = 0f;
            while (t < 0.2f) { t += Time.deltaTime; yield return null; }

            // ...and brings it down: the stamp drops in, accelerating, then squashes on impact.
            CanvasGroup g;
            RectTransform s = ResultUI.Stamp(form.StampRoot, word, color, out g);
            Quaternion rest = s != null ? s.localRotation : Quaternion.identity;
            t = 0f;
            const float dur = 0.15f;
            while (t < dur)
            {
                float k = t / dur;
                float e = k * k;
                if (s != null)
                {
                    s.localScale = Vector3.one * Mathf.Lerp(2.6f, 1f, e);
                    s.localRotation = rest * Quaternion.Euler(0f, 0f, (1f - e) * 8f);
                }
                if (g != null) g.alpha = Mathf.Clamp01(k * 2.5f);
                t += Time.deltaTime;
                yield return null;
            }
            if (s != null) { s.localScale = Vector3.one; s.localRotation = rest; }
            if (g != null) g.alpha = 1f;
            AudioManager.PlaySfx("stamp");
            root.Rig.Shake(0.22f);
            UIShake.Kick(form.Root, 7f, 0.32f);
            t = 0f;
            while (t < 0.12f)
            {
                float k = t / 0.12f;
                float sq = 1f - Mathf.Sin(k * Mathf.PI) * 0.07f;
                if (s != null) s.localScale = new Vector3(2f - sq, sq, 1f);
                t += Time.deltaTime;
                yield return null;
            }
            if (s != null) s.localScale = Vector3.one;
        }

        private IEnumerator ApproveRoutine(SimResult result)
        {
            yield return StampRoutine("APPROVED", ResultUI.ApprovedColor);
            UIConfetti.Burst(form.StampRoot, Vector2.zero, 34); // under the stamp root: cleared with the stamps
            AudioManager.PlaySfx("approve");
            if (level != null) Progress.RecordApproved(levelIndex, level.id, submissions);
            yield return new WaitForSeconds(1.0f);
            bool last = levelIndex >= levelCount - 1;
            certificate = ResultUI.BuildCertificate(root.ModalLayer, level, form.GetChoices(), submissions, last, OnCertificateNext);
            AudioManager.PlaySfx("page");
        }

        private void OnCertificateNext()
        {
            if (certificate != null) { Destroy(certificate.gameObject); certificate = null; }
            int finished = levelIndex;
            Exit();
            flow.OnLevelApproved(finished);
        }

        private IEnumerator RejectRoutine(SimResult result)
        {
            rejections++;
            yield return StampRoutine("REJECTED", ResultUI.RejectedColor);
            AudioManager.PlaySfx("reject");
            yield return new WaitForSeconds(0.35f);

            string lastCaption = "";
            if (result.events != null && result.events.Count > 0 && result.events[result.events.Count - 1] != null)
            {
                lastCaption = result.events[result.events.Count - 1].caption ?? "";
            }
            string verdict = result.verdict ?? "";
            bool duplicate = verdict.Trim() == lastCaption.Trim();
            if (string.IsNullOrEmpty(lastCaption))
            {
                // Nothing on the caption bar: the card carries the reason.
                SetCaptionBar(false);
            }
            rejectCard.verdict.text = duplicate ? "" : verdict;
            float h = duplicate ? 120f : 190f;
            rejectCard.root.sizeDelta = new Vector2(440f, h);
            rejectCard.root.gameObject.SetActive(true);
        }

        private void Revise()
        {
            if (phase != Phase.Result) return;
            rejectCard.root.gameObject.SetActive(false);
            SetCaptionBar(false);
            form.ClearStamps();
            RebuildStage();
            form.SetLocked(false);
            phase = Phase.Editing;
            clues.SetActive(true);
            if (reaper != null) reaper.Show(true);
            AudioManager.PlaySfx("page");
        }

        // ---------------------------------------------------------------- debug

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void ToggleDebugSolution()
        {
            if (debugLabel == null || level == null) return;
            bool on = !debugLabel.gameObject.activeSelf;
            debugLabel.SetActive(on);
            if (!on) return;
            var sb = new System.Text.StringBuilder();
            sb.Append("[F1] Solutions for ").Append(level.id).Append(":\n");
            int shown = 0;
            for (int i = 0; i < level.solutions.Count && shown < 4; i++, shown++)
            {
                Dictionary<string, string> d = level.solutions[i].ToDictionary();
                var parts = new List<string>();
                for (int f = 0; f < level.fields.Count; f++)
                {
                    string v;
                    if (d.TryGetValue(level.fields[f].id, out v))
                    {
                        OptionData o = level.fields[f].FindOption(v);
                        parts.Add(o != null ? o.label : v);
                    }
                }
                sb.Append("  ").Append(string.Join(" + ", parts.ToArray())).Append('\n');
            }
            if (level.solutions.Count > shown) sb.Append("  (+").Append(level.solutions.Count - shown).Append(" more)");
            debugLabel.text = sb.ToString();
        }
#endif
    }
}
