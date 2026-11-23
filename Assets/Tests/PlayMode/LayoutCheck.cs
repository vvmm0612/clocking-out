using System.Collections.Generic;
using DeathNotice.Game;
using TMPro;
using UnityEngine;

namespace DeathNotice.Tests.PlayMode
{
    /// <summary>
    /// Geometric self-check of the in-level UI at the current canvas size: everything inside the canvas,
    /// the HUD never reaching into the paper, HUD / form elements not overlapping each other, and key
    /// single-purpose labels not overflowing their boxes.
    /// </summary>
    public static class LayoutCheck
    {
        public static Rect CanvasSpace(GameRoot root, RectTransform rt)
        {
            var c = new Vector3[4];
            rt.GetWorldCorners(c);
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = root.CanvasRect.InverseTransformPoint(c[i]);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        private static bool Overlap(Rect a, Rect b)
        {
            const float eps = 0.5f;
            return a.xMin < b.xMax - eps && b.xMin < a.xMax - eps && a.yMin < b.yMax - eps && b.yMin < a.yMax - eps;
        }

        public static List<string> InsideCanvas(GameRoot root, RectTransform rt, string label)
        {
            var problems = new List<string>();
            if (rt == null) { problems.Add(label + ": missing"); return problems; }
            Rect canvas = root.CanvasRect.rect;
            Rect r = CanvasSpace(root, rt);
            if (r.xMin < canvas.xMin - 1f || r.xMax > canvas.xMax + 1f || r.yMin < canvas.yMin - 1f || r.yMax > canvas.yMax + 1f)
                problems.Add(label + ": " + rt.name + " " + r + " leaves the canvas " + canvas);
            return problems;
        }

        private static RectTransform Child(Transform parent, string name)
        {
            Transform t = parent != null ? parent.Find(name) : null;
            return t != null && t.gameObject.activeInHierarchy ? t as RectTransform : null;
        }

        private static void CheckLabelFits(List<string> problems, RectTransform rt, string label)
        {
            if (rt == null) return;
            TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp == null || string.IsNullOrEmpty(tmp.text)) return;
            Vector2 pref = tmp.GetPreferredValues(tmp.text, rt.rect.width, 0f);
            if (pref.y > rt.rect.height + 2f) problems.Add(label + ": text of " + rt.name + " needs " + pref.y.ToString("0") + " px, box is " + rt.rect.height.ToString("0") + " ('" + tmp.text + "')");
        }

        public static List<string> Run(GameRoot root, LevelController lc, string label)
        {
            var problems = new List<string>();
            Rect paper = CanvasSpace(root, root.PaperArea);
            problems.AddRange(InsideCanvas(root, root.PaperArea, label));

            RectTransform hud = lc.Hud;
            string[] hudNames = { "Menu", "Reset", "Case", "Hint", "Tutorial", "RejectCard", "CaptionBar", "Skip" };
            var hudRects = new List<KeyValuePair<string, Rect>>();
            foreach (string n in hudNames)
            {
                RectTransform rt = Child(hud, n);
                if (rt == null) continue;
                Rect r = CanvasSpace(root, rt);
                problems.AddRange(InsideCanvas(root, rt, label));
                if (Overlap(r, paper)) problems.Add(label + ": HUD '" + n + "' " + r + " overlaps the paper " + paper);
                hudRects.Add(new KeyValuePair<string, Rect>(n, r));
            }
            string[,] mustNotTouch = { { "Menu", "Reset" }, { "Reset", "Case" }, { "Menu", "Case" }, { "Case", "Tutorial" }, { "Hint", "Tutorial" } };
            for (int i = 0; i < mustNotTouch.GetLength(0); i++)
            {
                Rect? a = null, b = null;
                foreach (var kv in hudRects)
                {
                    if (kv.Key == mustNotTouch[i, 0]) a = kv.Value;
                    if (kv.Key == mustNotTouch[i, 1]) b = kv.Value;
                }
                if (a.HasValue && b.HasValue && Overlap(a.Value, b.Value)) problems.Add(label + ": HUD '" + mustNotTouch[i, 0] + "' overlaps '" + mustNotTouch[i, 1] + "'");
            }
            CheckLabelFits(problems, Child(hud, "Case"), label);

            RectTransform form = lc.Form.Root;
            string[] formNames = { "Header", "Case", "Name", "Wish", "Rows", "Submit", "Clear", "Count", "NotesHeader", "Notes", "Footer" };
            var formRects = new Dictionary<string, Rect>();
            foreach (string n in formNames)
            {
                RectTransform rt = Child(form, n);
                if (rt == null) continue;
                Rect r = CanvasSpace(root, rt);
                formRects[n] = r;
                if (r.xMin < paper.xMin - 1f || r.xMax > paper.xMax + 1f || r.yMin < paper.yMin - 1f || r.yMax > paper.yMax + 1f)
                    problems.Add(label + ": form '" + n + "' " + r + " leaves the paper " + paper);
            }
            string[,] formPairs = { { "Submit", "Clear" }, { "Rows", "Submit" }, { "Rows", "Clear" }, { "Count", "Clear" }, { "Submit", "NotesHeader" }, { "Notes", "Footer" }, { "Wish", "Rows" } };
            for (int i = 0; i < formPairs.GetLength(0); i++)
            {
                Rect a, b;
                if (formRects.TryGetValue(formPairs[i, 0], out a) && formRects.TryGetValue(formPairs[i, 1], out b) && Overlap(a, b))
                    problems.Add(label + ": form '" + formPairs[i, 0] + "' overlaps '" + formPairs[i, 1] + "'");
            }
            if (formRects.ContainsKey("Notes") && formRects["Notes"].height < 40f) problems.Add(label + ": Notes box is only " + formRects["Notes"].height.ToString("0") + " px tall");
            CheckLabelFits(problems, Child(form, "Header"), label);
            CheckLabelFits(problems, Child(form, "Name"), label);
            CheckLabelFits(problems, Child(form, "Wish"), label);
            Debug.Log("[Layout] " + label + ": canvas " + root.CanvasRect.rect.size + ", paper " + paper + ", " + problems.Count + " problem(s)");
            return problems;
        }
    }
}
