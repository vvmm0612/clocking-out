using DeathNotice.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// First-time tips: three steps on case 1 (clues, form, submit) and one on the first case with a
    /// Shift field (the peek). Each set is shown once (PlayerPrefs), steps advance on the matching action
    /// or on "Next", and "Skip tips" closes it for good. Non-blocking: it sits at the top of the stage.
    /// </summary>
    public class Tutorial
    {
        public const string KeyCase1 = "co_tut_case1";
        public const string KeyShift = "co_tut_shift";

        private static readonly string[] Case1Steps =
        {
            "Hover objects marked ? to read clues. Click one to copy it into your Notes.",
            "Fill in the form: pick a Cause and a Place from the dropdowns on the right.",
            "Submit — Death will check if it adds up."
        };

        private static readonly string[] ShiftSteps =
        {
            "New field: SHIFT. Hold the SHIFT key to peek at the other side: who is where, and how high the water is, on the shift you picked."
        };

        private readonly RectTransform root;
        private readonly Label stepLabel;
        private readonly Label body;
        private readonly UIButton next;
        private readonly UIButton skip;

        private string[] steps;
        private string prefKey;
        private int step;

        public bool Active { get { return steps != null; } }
        public int Step { get { return step; } }
        public bool Visible { get { return root != null && root.gameObject.activeSelf; } }
        public UIButton SkipButton { get { return skip; } }
        public UIButton NextButton { get { return next; } }

        public Tutorial(RectTransform hud)
        {
            Image bg = UIFactory.Panel(hud, "Tutorial", new Color(0.99f, 0.95f, 0.80f, 0.97f), true);
            root = bg.rectTransform;
            // Top-left under the HUD buttons: empty sky on cases 1 and 2 (the lighthouse top and the dock are clues).
            UIFactory.Box(root, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(12f, -56f), new Vector2(430f, 96f));
            Outline o = bg.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.15f, 0.12f, 0.1f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);

            stepLabel = UIFactory.Text(root, "Step", "", 12f, UIFactory.InkSoft, TextAnchor.MiddleLeft, true);
            UIFactory.TopRow(stepLabel.rect, 6f, 16f, 14f, 14f);
            body = UIFactory.Text(root, "Body", "", 16f, UIFactory.Ink, TextAnchor.UpperLeft, false);
            UIFactory.TopRow(body.rect, 22f, 44f, 14f, 14f);

            skip = UIFactory.Button(root, "Skip", "Skip tips", 13f, UIFactory.PaperDark, UIFactory.Ink, Finish);
            UIFactory.Box(skip.rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-96f, 6f), new Vector2(84f, 24f));
            next = UIFactory.Button(root, "Next", "Next", 13f, new Color(0.55f, 0.18f, 0.16f, 1f), new Color(1f, 0.96f, 0.9f, 1f), Advance);
            UIFactory.Box(next.rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 6f), new Vector2(80f, 24f));
            root.gameObject.SetActive(false);
        }

        public static bool Seen(string key)
        {
            return PlayerPrefs.GetInt(key, 0) != 0;
        }

        public static void ResetSeen()
        {
            PlayerPrefs.DeleteKey(KeyCase1);
            PlayerPrefs.DeleteKey(KeyShift);
        }

        /// <summary>Decides which tips (if any) this level gets.</summary>
        public void BeginLevel(LevelData level, int index, bool shiftOnlyLevel)
        {
            steps = null;
            prefKey = null;
            step = 0;
            if (level != null)
            {
                bool hasShift = level.FindField(Simulator.ShiftField) != null;
                if (index == 0 && !Seen(KeyCase1)) { steps = Case1Steps; prefKey = KeyCase1; }
                else if (hasShift && !shiftOnlyLevel && !Seen(KeyShift)) { steps = ShiftSteps; prefKey = KeyShift; }
            }
            Refresh();
        }

        /// <summary>Per-frame: visibility and auto-advance on the player's own actions.</summary>
        public void Tick(bool editing, NoticeForm form)
        {
            if (!Active) { if (Visible) root.gameObject.SetActive(false); return; }
            if (steps == Case1Steps && form != null)
            {
                if (step == 0 && form.NoteCount > 0) Advance();
                else if (step == 1 && form.IsComplete) Advance();
            }
            if (!Active) return;
            if (Visible != editing) root.gameObject.SetActive(editing);
        }

        public void OnSubmitted()
        {
            if (Active && steps == Case1Steps && step >= steps.Length - 1) Finish();
        }

        public void Hide()
        {
            if (root != null) root.gameObject.SetActive(false);
        }

        private void Advance()
        {
            if (!Active) return;
            step++;
            if (step >= steps.Length) { Finish(); return; }
            Refresh();
        }

        private void Finish()
        {
            if (prefKey != null)
            {
                PlayerPrefs.SetInt(prefKey, 1);
                PlayerPrefs.Save();
            }
            steps = null;
            prefKey = null;
            root.gameObject.SetActive(false);
        }

        private void Refresh()
        {
            if (!Active) { root.gameObject.SetActive(false); return; }
            stepLabel.text = steps.Length > 1 ? "TIP " + (step + 1) + " / " + steps.Length : "TIP";
            body.text = steps[step];
            bool last = step >= steps.Length - 1;
            next.label.text = last ? "Got it" : "Next";
            skip.gameObject.SetActive(!last || steps.Length > 1);
            root.gameObject.SetActive(true);
        }
    }
}
