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

            Image sheet = UIFactory.Panel(root, "Sheet", new Color(0.97f, 0.94f, 0.86f, 1f), true);
            RectTransform s = sheet.rectTransform;
            UIFactory.Box(s, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 620f));
            Border(s, 4f, new Color(0.35f, 0.28f, 0.22f, 1f));

            float y = 22f;
            Label title = UIFactory.Text(s, "Title", "CERTIFICATE OF DEATH", 32f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(title.rect, y, 40f, 20f, 20f);
            y += 44f;
            Label sub = UIFactory.Text(s, "Sub", "Department of Final Arrangements — Form 1-D, as filed", 14f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.TopRow(sub.rect, y, 20f, 20f, 20f);
            y += 34f;

            Label certifies = UIFactory.Text(s, "Certifies", "This certifies that", 15f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
            UIFactory.TopRow(certifies.rect, y, 20f, 20f, 20f);
            y += 22f;
            Label name = UIFactory.Text(s, "Name", level != null ? level.deceased : "", 26f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.TopRow(name.rect, y, 34f, 20f, 20f);
            y += 44f;

            if (level != null)
            {
                for (int i = 0; i < level.fields.Count; i++)
                {
                    FieldData f = level.fields[i];
                    string v;
                    OptionData o = (choices != null && choices.TryGetValue(f.id, out v)) ? f.FindOption(v) : null;
                    Label row = UIFactory.Text(s, "Field_" + f.id, f.label + ":  " + (o != null ? o.label : "-"), 19f, UIFactory.Ink, TextAnchor.MiddleCenter, false);
                    UIFactory.TopRow(row.rect, y, 26f, 30f, 30f);
                    y += 28f;
                }
            }
            if (lastLevel)
            {
                Label succ = UIFactory.Text(s, "Successor", "Successor:  YOU", 22f, RejectedColor, TextAnchor.MiddleCenter, true);
                UIFactory.TopRow(succ.rect, y, 28f, 30f, 30f);
                y += 32f;
            }
            y += 8f;
            Label wish = UIFactory.Text(s, "Wish", "Last wish: “" + (level != null ? level.wishText : "") + "”  —  GRANTED", 17f, ApprovedColor, TextAnchor.UpperCenter, true);
            UIFactory.TopRow(wish.rect, y, 48f, 30f, 30f);

            // Seal + submissions
            Color sealColor = Progress.SealColor(submissions);
            Image seal = UIFactory.Panel(s, "Seal", sealColor, false);
            UIFactory.Box(seal.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(110f, 120f), new Vector2(92f, 92f));
            seal.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Image sealInner = UIFactory.Panel(seal.rectTransform, "Inner", Color.Lerp(sealColor, Color.white, 0.25f), false);
            UIFactory.Stretch(sealInner.rectTransform, 8f, 8f, 8f, 8f);
            Label sealWord = UIFactory.Text(s, "SealWord", Progress.SealName(submissions), 20f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
            UIFactory.Box(sealWord.rect, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(110f, 120f), new Vector2(120f, 30f));

            Label motto = UIFactory.Text(s, "Motto", Progress.SealMotto(submissions), 18f, UIFactory.Ink, TextAnchor.MiddleLeft, true);
            UIFactory.Box(motto.rect, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(190f, 134f), new Vector2(400f, 26f));
            Label count = UIFactory.Text(s, "Count", "Submissions: " + submissions, 16f, UIFactory.InkSoft, TextAnchor.MiddleLeft, false);
            UIFactory.Box(count.rect, new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(190f, 108f), new Vector2(400f, 22f));

            UIButton next = UIFactory.Button(s, "Next", lastLevel ? "Clock in" : "Next case", 20f, ApprovedColor, new Color(1f, 0.97f, 0.9f), onNext);
            UIFactory.Box(next.rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 22f), new Vector2(180f, 46f));
            return root;
        }
    }
}
