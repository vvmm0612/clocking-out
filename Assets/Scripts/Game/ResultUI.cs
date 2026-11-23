using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>UI pieces for the end of a submission: rubber stamps, the rejection card, the death certificate.</summary>
    public static class ResultUI
    {
        public static readonly Color ApprovedColor = new Color(0.18f, 0.48f, 0.26f, 1f);
        public static readonly Color RejectedColor = new Color(0.72f, 0.14f, 0.12f, 1f);

        /// <summary>Four thin images forming a rectangular border inside rt.</summary>
        public static void Border(RectTransform rt, float thickness, Color c)
        {
            Image top = UIFactory.Panel(rt, "BorderT", c, false);
            UIFactory.TopRow(top.rectTransform, 0f, thickness, 0f, 0f);
            Image bottom = UIFactory.Panel(rt, "BorderB", c, false);
            bottom.rectTransform.anchorMin = new Vector2(0f, 0f);
            bottom.rectTransform.anchorMax = new Vector2(1f, 0f);
            bottom.rectTransform.pivot = new Vector2(0.5f, 0f);
            bottom.rectTransform.offsetMin = new Vector2(0f, 0f);
            bottom.rectTransform.offsetMax = new Vector2(0f, thickness);
            Image left = UIFactory.Panel(rt, "BorderL", c, false);
            left.rectTransform.anchorMin = new Vector2(0f, 0f);
            left.rectTransform.anchorMax = new Vector2(0f, 1f);
            left.rectTransform.pivot = new Vector2(0f, 0.5f);
            left.rectTransform.offsetMin = new Vector2(0f, 0f);
            left.rectTransform.offsetMax = new Vector2(thickness, 0f);
            Image right = UIFactory.Panel(rt, "BorderR", c, false);
            right.rectTransform.anchorMin = new Vector2(1f, 0f);
            right.rectTransform.anchorMax = new Vector2(1f, 1f);
            right.rectTransform.pivot = new Vector2(1f, 0.5f);
            right.rectTransform.offsetMin = new Vector2(-thickness, 0f);
            right.rectTransform.offsetMax = new Vector2(0f, 0f);
        }

        /// <summary>A rubber stamp (border + big word), rotated a little. Returns its root (scale/alpha via CanvasGroup).</summary>
        public static RectTransform Stamp(RectTransform parent, string word, Color color, out CanvasGroup group)
        {
            RectTransform rt = UIFactory.Rect(parent, "Stamp_" + word);
            UIFactory.Stretch(rt, 0f, 0f, 0f, 0f);
            rt.localRotation = Quaternion.Euler(0f, 0f, 11f);
            group = rt.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Image fill = UIFactory.Panel(rt, "Fill", new Color(color.r, color.g, color.b, 0.07f), false);
            UIFactory.Stretch(fill.rectTransform, 0f, 0f, 0f, 0f);
            Border(rt, 6f, color);
            Label l = UIFactory.Text(rt, "Word", word, 44f, color, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(l.rect, 8f, 4f, 8f, 4f);
            return rt;
        }

        public class RejectCard
        {
            public RectTransform root;
            public Label title;
            public Label verdict;
            public UIButton revise;
        }

        public static RejectCard BuildRejectCard(RectTransform parent, UnityAction onRevise)
        {
            var card = new RejectCard();
            Image bg = UIFactory.Panel(parent, "RejectCard", new Color(0.97f, 0.93f, 0.85f, 0.97f), true);
            card.root = bg.rectTransform;
            UIFactory.Box(card.root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(440f, 190f));
            Border(card.root, 3f, RejectedColor);
            card.title = UIFactory.Text(card.root, "Title", "RETURNED FOR CORRECTION", 22f, RejectedColor, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(card.title.rect, 14f, 30f, 14f, 14f);
            card.verdict = UIFactory.Text(card.root, "Verdict", "", 17f, UIFactory.Ink, TextAnchor.UpperCenter, false);
            UIFactory.TopRow(card.verdict.rect, 50f, 80f, 20f, 20f);
            card.revise = UIFactory.Button(card.root, "Revise", "Revise the form", 18f, RejectedColor, new Color(1f, 0.96f, 0.9f), onRevise);
            UIFactory.Box(card.revise.rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(220f, 40f));
            card.root.gameObject.SetActive(false);
            return card;
        }

        /// <summary>Death certificate modal. Returns the root (child of the modal layer); destroy it to close.</summary>
        public static RectTransform BuildCertificate(RectTransform modalLayer, LevelData level, Dictionary<string, string> choices,
                                                     int submissions, bool lastLevel, UnityAction onNext)
        {
            Image dim = UIFactory.Blocker(modalLayer, "Certificate", new Color(0f, 0f, 0f, 0.6f), null);
            RectTransform root = dim.rectTransform;

            Color sheetColor = new Color(0.97f, 0.94f, 0.86f, 1f);
            Color frame = new Color(0.35f, 0.28f, 0.22f, 1f);
            Image sheet = UIFactory.Panel(root, "Sheet", sheetColor, true);
            RectTransform s = sheet.rectTransform;
            const float width = 620f;
            UIFactory.Box(s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, 600f));
            Border(s, 4f, frame);
            RectTransform inner = UIFactory.Rect(s, "InnerFrame");
            UIFactory.Stretch(inner, 10f, 10f, 10f, 10f);
            Border(inner, 1f, new Color(frame.r, frame.g, frame.b, 0.55f));

            float y = 28f;
            Label title = UIFactory.Text(s, "Title", "CERTIFICATE OF DEATH", 32f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(title.rect, y, 40f, 30f, 30f);
            y += 42f;
            Label sub = UIFactory.Text(s, "Sub", "Department of Final Arrangements — Form 1-D, as filed", 14f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.TopRow(sub.rect, y, 20f, 30f, 30f);
            y += 30f;
            Rule(s, "Rule1", y, 90f, frame);
            y += 16f;

            Label certifies = UIFactory.Text(s, "Certifies", "This certifies that", 15f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.TopRow(certifies.rect, y, 20f, 30f, 30f);
            y += 22f;
            Label name = UIFactory.Text(s, "Name", level != null ? level.deceased : "", 27f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(name.rect, y, 36f, 30f, 30f);
            y += 44f;

            if (level != null)
            {
                // Two columns: field label right-aligned, value left-aligned, meeting at the centre.
                for (int i = 0; i < level.fields.Count; i++)
                {
                    FieldData f = level.fields[i];
                    string v;
                    OptionData o = (choices != null && choices.TryGetValue(f.id, out v)) ? f.FindOption(v) : null;
                    Label key = UIFactory.Text(s, "Key_" + f.id, f.label.ToUpperInvariant(), 13f, UIFactory.InkSoft, TextAnchor.MiddleRight, true);
                    UIFactory.TopRow(key.rect, y, 26f, 40f, width * 0.5f + 8f);
                    Label val = UIFactory.Text(s, "Field_" + f.id, o != null ? o.label : "-", 19f, UIFactory.Ink, TextAnchor.MiddleLeft, false);
                    UIFactory.TopRow(val.rect, y, 26f, width * 0.5f + 8f, 40f);
                    y += 28f;
                }
            }
            if (lastLevel)
            {
                Label key = UIFactory.Text(s, "Key_successor", "SUCCESSOR", 13f, RejectedColor, TextAnchor.MiddleRight, true);
                UIFactory.TopRow(key.rect, y + 2f, 28f, 40f, width * 0.5f + 8f);
                Label succ = UIFactory.Text(s, "Successor", "YOU", 24f, RejectedColor, TextAnchor.MiddleLeft, true);
                UIFactory.TopRow(succ.rect, y + 2f, 28f, width * 0.5f + 8f, 40f);
                y += 34f;
            }
            y += 10f;
            Label wish = UIFactory.Text(s, "Wish", "Last wish: “" + (level != null ? level.wishText : "") + "”  —  GRANTED", 17f, ApprovedColor, TextAnchor.UpperCenter, true);
            UIFactory.TopRow(wish.rect, y, 24f, 40f, 40f);
            Canvas.ForceUpdateCanvases();
            float wishH = Mathf.Max(24f, wish.PreferredHeight + 2f);
            UIFactory.TopRow(wish.rect, y, wishH, 40f, 40f);
            y += wishH + 16f;
            Rule(s, "Rule2", y, 90f, frame);
            y += 18f;

            // Seal row: seal on the left, verdict text beside it, Next under the text.
            const float sealSize = 112f;
            RectTransform sealRoot = UIFactory.Rect(s, "Seal");
            UIFactory.Box(sealRoot, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(40f + sealSize * 0.5f, -(y + sealSize * 0.5f + 4f)), new Vector2(sealSize, sealSize));
            BuildSeal(sealRoot, submissions, sealSize);

            float textX = 40f + sealSize + 28f;
            Label motto = UIFactory.Text(s, "Motto", Progress.SealMotto(submissions), 19f, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.TopRow(motto.rect, y + 6f, 26f, textX, 30f);
            Label count = UIFactory.Text(s, "Count", "Submissions: " + submissions, 15f, UIFactory.InkSoft, TextAnchor.MiddleLeft, false);
            UIFactory.TopRow(count.rect, y + 34f, 22f, textX, 30f);

            UIButton next = UIFactory.Button(s, "Next", lastLevel ? "Clock in" : "Next case", 20f, ApprovedColor, new Color(1f, 0.97f, 0.9f), onNext);
            UIFactory.Box(next.rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-36f, -(y + 70f)), new Vector2(180f, 46f));
            y += Mathf.Max(sealSize + 8f, 70f + 46f) + 30f;

            s.sizeDelta = new Vector2(width, y);
            return root;
        }

        private static void Rule(RectTransform parent, string name, float top, float inset, Color c)
        {
            Image r = UIFactory.Panel(parent, name, new Color(c.r, c.g, c.b, 0.45f), false);
            UIFactory.TopRow(r.rectTransform, top, 1.5f, inset, inset);
        }

        /// <summary>
        /// The rating seal, built from plain shapes so the three read differently at a glance:
        /// GOLD = scalloped rosette with ribbon tails, SILVER = round medal with a double rim,
        /// PAPER = a squarish paper tag stuck on slightly crooked.
        /// </summary>
        public static void BuildSeal(RectTransform root, int submissions, float size)
        {
            string word = Progress.SealName(submissions);
            Color c = Progress.SealColor(submissions);
            Color dark = Color.Lerp(c, Color.black, 0.35f);
            Color light = Color.Lerp(c, Color.white, 0.35f);
            if (word == "GOLD")
            {
                for (int i = 0; i < 2; i++)
                {
                    Image tail = UIFactory.Panel(root, "Ribbon" + i, i == 0 ? new Color(0.62f, 0.2f, 0.17f, 1f) : new Color(0.52f, 0.16f, 0.14f, 1f), false);
                    UIFactory.Box(tail.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(i == 0 ? -size * 0.16f : size * 0.16f, -size * 0.05f), new Vector2(size * 0.2f, size * 0.55f));
                    tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, i == 0 ? -18f : 18f);
                }
                for (int i = 0; i < 16; i++)
                {
                    float a = i * 22.5f * Mathf.Deg2Rad;
                    UIFactory.Disc(root, "Scallop" + i, dark, new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * size * 0.4f, size * 0.24f);
                }
                UIFactory.Disc(root, "Face", c, Vector2.zero, size * 0.84f);
                UIFactory.Disc(root, "Ring", dark, Vector2.zero, size * 0.66f);
                UIFactory.Disc(root, "Inner", light, Vector2.zero, size * 0.62f);
            }
            else if (word == "SILVER")
            {
                UIFactory.Disc(root, "Rim", dark, Vector2.zero, size * 0.86f);
                UIFactory.Disc(root, "Face", c, Vector2.zero, size * 0.8f);
                UIFactory.Disc(root, "Ring", dark, Vector2.zero, size * 0.64f);
                UIFactory.Disc(root, "Inner", light, Vector2.zero, size * 0.6f);
            }
            else
            {
                RectTransform tag = UIFactory.Rect(root, "Tag");
                UIFactory.Box(tag, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.86f, size * 0.66f));
                tag.localRotation = Quaternion.Euler(0f, 0f, -7f);
                Image shadow = UIFactory.Panel(tag, "Shadow", new Color(0f, 0f, 0f, 0.12f), false);
                UIFactory.Stretch(shadow.rectTransform, 3f, 3f, -3f, -3f);
                Image face = UIFactory.Panel(tag, "Face", new Color(0.93f, 0.89f, 0.80f, 1f), false);
                UIFactory.Stretch(face.rectTransform, 0f, 0f, 0f, 0f);
                // Dashed "perforated" edge
                RectTransform dashes = UIFactory.Rect(tag, "Dashes");
                UIFactory.Stretch(dashes, 5f, 5f, 5f, 5f);
                Color ink = new Color(0.55f, 0.48f, 0.38f, 1f);
                for (int i = 0; i < 9; i++)
                {
                    float k = (i + 0.5f) / 9f;
                    Image top = UIFactory.Panel(dashes, "DT" + i, ink, false);
                    UIFactory.Box(top.rectTransform, new Vector2(k, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(5f, 1.5f));
                    Image bottom = UIFactory.Panel(dashes, "DB" + i, ink, false);
                    UIFactory.Box(bottom.rectTransform, new Vector2(k, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(5f, 1.5f));
                }
                for (int i = 0; i < 6; i++)
                {
                    float k = (i + 0.5f) / 6f;
                    Image left = UIFactory.Panel(dashes, "DL" + i, ink, false);
                    UIFactory.Box(left.rectTransform, new Vector2(0f, k), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1.5f, 5f));
                    Image right = UIFactory.Panel(dashes, "DR" + i, ink, false);
                    UIFactory.Box(right.rectTransform, new Vector2(1f, k), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(1.5f, 5f));
                }
                UIFactory.Disc(tag, "Hole", new Color(0.35f, 0.3f, 0.26f, 1f), new Vector2(-size * 0.3f, size * 0.17f), 7f);
                Label pw = UIFactory.Text(tag, "Word", word, 20f, ink, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(pw.rect, 4f, 4f, 4f, 4f);
                return;
            }
            Label l = UIFactory.Text(root, "Word", word, word == "GOLD" ? 18f : 17f, Color.Lerp(dark, Color.black, 0.45f), TextAnchor.MiddleCenter, true);
            UIFactory.Box(l.rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, 30f));
        }
    }
}