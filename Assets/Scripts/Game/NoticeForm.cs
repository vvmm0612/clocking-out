using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// The paper "NOTICE OF DEATH — Form 1-D" on the right: name, last wish, one dropdown per field,
    /// SUBMIT / Clear, submission counter and the Notes list of collected clues.
    /// </summary>
    public class NoticeForm
    {
        private class Row
        {
            public FieldData field;
            public Label name;
            public UIButton value;
        }

        public Action onSubmit;
        public Action onChanged;

        public RectTransform Root { get; private set; }
        public RectTransform StampRoot { get; private set; }
        public int Version { get; private set; }
        public bool SupervisorMode { get; private set; }

        private readonly PopupList popup;
        private Image paper;
        private Outline glow;
        private CanvasGroup group;
        private Label header;
        private Label caseLine;
        private Label supervisorLine;
        private Label nameValue;
        private Label wishValue;
        private Label nameCaption;
        private Label wishCaption;
        private RectTransform rowsRoot;
        private UIButton submit;
        private UIButton clear;
        private Label countLabel;
        private Label hintLabel;
        private Label notesHeader;
        private RectTransform notesBox;
        private RectTransform notesContent;
        private Label notesEmpty;
        private Label footer;

        private LevelData level;
        private readonly List<Row> rows = new List<Row>();
        private readonly Dictionary<string, string> chosen = new Dictionary<string, string>();
        private readonly HashSet<string> notes = new HashSet<string>();
        private bool locked;

        private static readonly Color PaperNormal = UIFactory.Paper;
        private static readonly Color PaperShift = new Color(1f, 0.94f, 0.70f, 1f);

        public NoticeForm(RectTransform parent, PopupList popup)
        {
            this.popup = popup;
            Build(parent);
        }

        private void Build(RectTransform parent)
        {
            Image desk = UIFactory.Panel(parent, "Desk", new Color(0.24f, 0.19f, 0.17f, 1f), true);
            UIFactory.Stretch(desk.rectTransform, 0f, 0f, 0f, 0f);

            Image shadow = UIFactory.Panel(parent, "PaperShadow", new Color(0f, 0f, 0f, 0.35f), false);
            UIFactory.Stretch(shadow.rectTransform, 18f, 18f, 8f, 8f);

            paper = UIFactory.Panel(parent, "Paper", PaperNormal, true);
            Root = paper.rectTransform;
            UIFactory.Stretch(Root, 12f, 12f, 14f, 14f);
            UIFactory.MakePaper(paper, 0.32f);
            group = paper.gameObject.AddComponent<CanvasGroup>();
            glow = paper.gameObject.AddComponent<Outline>();
            glow.effectColor = new Color(1f, 0.8f, 0.3f, 0.9f);
            glow.effectDistance = new Vector2(5f, -5f);
            glow.enabled = false;

            Image band = UIFactory.Panel(Root, "Band", new Color(0.55f, 0.18f, 0.16f, 1f), false);
            UIFactory.TopRow(band.rectTransform, 0f, 8f, 0f, 0f);

            header = UIFactory.Text(Root, "Header", "NOTICE OF DEATH — Form 1-D", 25f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(header.rect, 16f, 34f, 16f, 16f);
            UIFactory.OrnateRule(Root, 52f, 22f, UIFactory.Ink);
            caseLine = UIFactory.Text(Root, "Case", "", 14f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.TopRow(caseLine.rect, 59f, 20f, 16f, 16f);
            // Level 6: its own line on the right of the name caption, well clear of the "Case file" line.
            supervisorLine = UIFactory.Text(Root, "Supervisor", "SUPERVISOR COPY", 13f, new Color(0.65f, 0.42f, 0.05f, 1f), TextAnchor.MiddleRight, true);
            UIFactory.TopRow(supervisorLine.rect, 84f, 16f, 200f, 22f);
            supervisorLine.SetActive(false);

            nameCaption = UIFactory.Text(Root, "NameCaption", "NAME OF THE DECEASED", 12f, UIFactory.InkSoft, TextAnchor.MiddleLeft, true);
            UIFactory.TopRow(nameCaption.rect, 84f, 16f, 22f, 22f);
            nameValue = UIFactory.Text(Root, "Name", "", 20f, UIFactory.Ink, TextAnchor.MiddleLeft, false);
            UIFactory.TopRow(nameValue.rect, 100f, 28f, 22f, 22f);
            Image nameLine = UIFactory.Panel(Root, "NameLine", new Color(0f, 0f, 0f, 0.25f), false);
            UIFactory.TopRow(nameLine.rectTransform, 128f, 1f, 22f, 22f);

            wishCaption = UIFactory.Text(Root, "WishCaption", "LAST WISH", 12f, UIFactory.InkSoft, TextAnchor.MiddleLeft, true);
            UIFactory.TopRow(wishCaption.rect, 136f, 16f, 22f, 22f);
            wishValue = UIFactory.Text(Root, "Wish", "", 18f, UIFactory.Ink, TextAnchor.UpperLeft, false);
            UIFactory.TopRow(wishValue.rect, 154f, 48f, 22f, 22f);

            rowsRoot = UIFactory.Rect(Root, "Rows");
            UIFactory.TopRow(rowsRoot, 208f, 160f, 22f, 22f);

            clear = UIFactory.Button(Root, "Clear", "Clear", 17f, UIFactory.PaperDark, UIFactory.Ink, OnClear);
            submit = UIFactory.Button(Root, "Submit", "SUBMIT", 22f, new Color(0.55f, 0.18f, 0.16f, 1f), new Color(1f, 0.96f, 0.9f, 1f), OnSubmitClicked);
            countLabel = UIFactory.Text(Root, "Count", "", 15f, UIFactory.InkSoft, TextAnchor.MiddleLeft, false);
            hintLabel = UIFactory.Text(Root, "Hint", "", 14f, UIFactory.InkSoft, TextAnchor.MiddleLeft, false);

            notesHeader = UIFactory.Text(Root, "NotesHeader", "NOTES", 14f, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            ScrollRect scroll;
            notesContent = UIFactory.ScrollList(Root, "Notes", out scroll);
            notesBox = scroll.GetComponent<RectTransform>();
            notesEmpty = UIFactory.Text(notesBox, "Empty", "Click the marked things in the scene to take notes.", 14f, UIFactory.InkSoft, TextAnchor.UpperLeft, false);
            UIFactory.Stretch(notesEmpty.rect, 10f, 8f, 10f, 8f);

            footer = UIFactory.Text(Root, "Footer", "Form 1-D  ·  Department of Final Arrangements", 11f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
            footer.rect.anchorMin = new Vector2(0f, 0f);
            footer.rect.anchorMax = new Vector2(1f, 0f);
            footer.rect.pivot = new Vector2(0.5f, 0f);
            footer.rect.offsetMin = new Vector2(10f, 6f);
            footer.rect.offsetMax = new Vector2(-10f, 22f);

            StampRoot = UIFactory.Rect(Root, "Stamp");
            UIFactory.Box(StampRoot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(360f, 96f));
        }

        // ---------------------------------------------------------------- per level

        public void Setup(LevelData lvl)
        {
            level = lvl;
            popup.Close();
            chosen.Clear();
            notes.Clear();
            UIFactory.DestroyChildren(notesContent);
            notesEmpty.SetActive(true);
            UIFactory.DestroyChildren(rowsRoot);
            rows.Clear();
            SetSupervisor(false);
            ClearStamps();
            locked = false;
            group.interactable = true;

            caseLine.text = "Case file: " + (lvl != null ? lvl.title : "");
            nameValue.text = lvl != null ? lvl.deceased : "";
            wishValue.text = lvl != null ? "“" + lvl.wishText + "”" : "";

            const float rowH = 46f;
            int n = lvl != null ? lvl.fields.Count : 0;
            for (int i = 0; i < n; i++)
            {
                FieldData f = lvl.fields[i];
                var row = new Row();
                row.field = f;
                row.name = UIFactory.Text(rowsRoot, "Field_" + f.id, f.label.ToUpperInvariant(), 16f, UIFactory.Ink, TextAnchor.MiddleLeft, true);
                UIFactory.TopRow(row.name.rect, i * rowH, rowH - 6f, 0f, 0f);
                row.name.rect.anchorMax = new Vector2(0f, 1f);
                row.name.rect.offsetMax = new Vector2(96f, row.name.rect.offsetMax.y);
                row.value = UIFactory.Button(rowsRoot, "Value_" + f.id, "", 18f, new Color(1f, 0.99f, 0.95f, 1f), UIFactory.Ink, null);
                UIFactory.TopRow(row.value.rect, i * rowH, rowH - 6f, 100f, 0f);
                Outline o = row.value.image.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0f, 0f, 0f, 0.35f);
                o.effectDistance = new Vector2(1f, -1f);
                Row captured = row;
                row.value.button.onClick.AddListener(delegate { OpenPopup(captured); });
                rows.Add(row);
            }
            float rowsHeight = Mathf.Max(1, n) * rowH;
            UIFactory.TopRow(rowsRoot, 208f, rowsHeight, 22f, 22f);

            float y = 208f + rowsHeight + 6f;
            UIFactory.TopRow(submit.rect, y, 48f, 0f, 22f);
            submit.rect.anchorMin = new Vector2(1f, 1f);
            submit.rect.anchorMax = new Vector2(1f, 1f);
            submit.rect.pivot = new Vector2(1f, 1f);
            submit.rect.anchoredPosition = new Vector2(-22f, -y);
            submit.rect.sizeDelta = new Vector2(170f, 48f);

            clear.rect.anchorMin = new Vector2(1f, 1f);
            clear.rect.anchorMax = new Vector2(1f, 1f);
            clear.rect.pivot = new Vector2(1f, 1f);
            clear.rect.anchoredPosition = new Vector2(-202f, -y - 6f);
            clear.rect.sizeDelta = new Vector2(90f, 36f);

            UIFactory.TopRow(countLabel.rect, y + 14f, 20f, 22f, 300f);
            UIFactory.TopRow(hintLabel.rect, y + 54f, 18f, 22f, 22f);

            float notesTop = y + 76f;
            UIFactory.TopRow(notesHeader.rect, notesTop, 20f, 22f, 22f);
            notesBox.anchorMin = new Vector2(0f, 0f);
            notesBox.anchorMax = new Vector2(1f, 1f);
            notesBox.pivot = new Vector2(0.5f, 0.5f);
            notesBox.offsetMin = new Vector2(22f, 28f);
            notesBox.offsetMax = new Vector2(-22f, -(notesTop + 24f));

            Refresh();
        }

        // ---------------------------------------------------------------- state

        public Dictionary<string, string> GetChoices()
        {
            return new Dictionary<string, string>(chosen);
        }

        public string Get(string fieldId)
        {
            string v;
            return chosen.TryGetValue(fieldId, out v) ? v : null;
        }

        public bool IsComplete
        {
            get
            {
                if (level == null || level.fields.Count == 0) return false;
                for (int i = 0; i < level.fields.Count; i++)
                {
                    if (!chosen.ContainsKey(level.fields[i].id)) return false;
                }
                return true;
            }
        }

        public bool PopupOpen { get { return popup.IsOpen; } }
        public int NoteCount { get { return notes.Count; } }
        public UIButton SubmitButton { get { return submit; } }
        public UIButton ClearButton { get { return clear; } }

        /// <summary>The dropdown button of a field row (null if the level has no such field).</summary>
        public UIButton RowButton(string fieldId)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i].field.id == fieldId) return rows[i].value;
            }
            return null;
        }

        public void Set(string fieldId, string optionId)
        {
            if (string.IsNullOrEmpty(optionId)) chosen.Remove(fieldId);
            else chosen[fieldId] = optionId;
            Version++;
            Refresh();
            if (onChanged != null) onChanged();
        }

        public void ClearAll()
        {
            chosen.Clear();
            Version++;
            Refresh();
            if (onChanged != null) onChanged();
        }

        public void SetSubmissions(int count)
        {
            countLabel.text = "Submitted: " + count;
        }

        public void SetLocked(bool value)
        {
            locked = value;
            group.interactable = !value;
            if (value) popup.Close();
            Refresh();
        }

        /// <summary>Level 6: while Shift is held the form "shifts" and reveals shiftOnly options.</summary>
        public void SetSupervisor(bool on)
        {
            if (SupervisorMode == on && paper.color == (on ? PaperShift : PaperNormal)) return;
            SupervisorMode = on;
            paper.color = on ? PaperShift : PaperNormal;
            glow.enabled = on;
            supervisorLine.SetActive(on);
            if (popup.IsOpen)
            {
                Row r = popup.Owner as Row;
                if (r != null) popup.Refresh(VisibleOptions(r.field));
            }
        }

        private List<OptionData> VisibleOptions(FieldData f)
        {
            var list = new List<OptionData>();
            for (int i = 0; i < f.options.Count; i++)
            {
                if (!f.options[i].shiftOnly || SupervisorMode) list.Add(f.options[i]);
            }
            return list;
        }

        private void OpenPopup(Row row)
        {
            if (locked || row == null) return;
            string cur = Get(row.field.id);
            Row captured = row;
            popup.Show(row, row.value.rect, VisibleOptions(row.field), cur, delegate (OptionData opt)
            {
                Set(captured.field.id, opt.id);
            });
        }

        private void Refresh()
        {
            for (int i = 0; i < rows.Count; i++)
            {
                Row r = rows[i];
                string v = Get(r.field.id);
                OptionData opt = v != null ? r.field.FindOption(v) : null;
                if (opt != null)
                {
                    r.value.label.text = opt.label + "   v";
                    r.value.label.color = opt.shiftOnly ? new Color(0.6f, 0.35f, 0.02f, 1f) : UIFactory.Ink;
                }
                else
                {
                    r.value.label.text = "(choose)   v";
                    r.value.label.color = new Color(0.55f, 0.5f, 0.46f, 1f);
                }
            }
            bool complete = IsComplete;
            submit.interactable = complete && !locked;
            clear.interactable = !locked && chosen.Count > 0;
            hintLabel.text = complete ? "" : "Fill in every field to submit.";
            hintLabel.color = new Color(0.55f, 0.18f, 0.16f, 0.85f);
        }

        private void OnClear()
        {
            if (locked) return;
            AudioManager.PlaySfx("page");
            ClearAll();
        }

        private void OnSubmitClicked()
        {
            if (locked || !IsComplete) return;
            if (onSubmit != null) onSubmit();
        }

        // ---------------------------------------------------------------- notes

        /// <summary>Adds a clue to the Notes list. Returns false if it was already noted.</summary>
        public bool AddNote(string text)
        {
            if (string.IsNullOrEmpty(text) || notes.Contains(text)) return false;
            notes.Add(text);
            notesEmpty.SetActive(false);
            Label l = UIFactory.Text(notesContent, "Note", "- " + text, 15f, UIFactory.Ink, TextAnchor.UpperLeft, false);
            LayoutElement le = l.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 18f;
            return true;
        }

        public bool HasNote(string text)
        {
            return notes.Contains(text);
        }

        /// <summary>Clears all paper stamps (child objects of StampRoot).</summary>
        public void ClearStamps()
        {
            UIFactory.DestroyChildren(StampRoot);
        }
    }
}
