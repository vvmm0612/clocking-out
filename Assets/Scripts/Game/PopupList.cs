using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// Minimal dropdown: a list of option buttons under (or above) an anchor rect, closed by clicking
    /// anywhere else. Lives on its own top-most layer so nothing can cover it.
    /// </summary>
    public class PopupList
    {
        private readonly RectTransform layer;
        private RectTransform root;
        private RectTransform anchorRect;
        private List<OptionData> options;
        private string current;
        private Action<OptionData> onPick;
        private bool animateNext;

        public bool IsOpen { get { return root != null; } }
        public object Owner { get; private set; }

        public PopupList(RectTransform layer)
        {
            this.layer = layer;
        }

        public void Show(object owner, RectTransform anchor, List<OptionData> opts, string currentId, Action<OptionData> pick)
        {
            Close();
            Owner = owner;
            anchorRect = anchor;
            options = opts ?? new List<OptionData>();
            current = currentId;
            onPick = pick;
            animateNext = true;
            Rebuild();
            AudioManager.PlaySfx("open");
        }

        /// <summary>Re-show with a different option list (used when Shift reveals hidden options).</summary>
        public void Refresh(List<OptionData> opts)
        {
            if (!IsOpen) return;
            options = opts ?? new List<OptionData>();
            Rebuild();
        }

        public void Close()
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            root = null;
            Owner = null;
        }

        private void Rebuild()
        {
            if (root != null) UnityEngine.Object.Destroy(root.gameObject);
            root = UIFactory.Rect(layer, "Popup");
            UIFactory.Stretch(root, 0f, 0f, 0f, 0f);
            UIFactory.Blocker(root, "Blocker", UIFactory.Clear, Close);

            const float rowH = 36f;
            const float pad = 4f;
            float h = pad * 2f + Mathf.Max(1, options.Count) * rowH;

            Vector3[] corners = new Vector3[4];
            float width = 240f;
            Vector2 topLeft = new Vector2(0f, 0f);
            Vector2 bottomLeftAbove = new Vector2(0f, 0f);
            if (anchorRect != null)
            {
                anchorRect.GetWorldCorners(corners);
                Vector2 bl = UIFactory.ScreenToLocal(layer, UIFactory.WorldToScreen(corners[0]));
                Vector2 tl = UIFactory.ScreenToLocal(layer, UIFactory.WorldToScreen(corners[1]));
                Vector2 tr = UIFactory.ScreenToLocal(layer, UIFactory.WorldToScreen(corners[2]));
                width = Mathf.Max(160f, tr.x - tl.x);
                topLeft = bl;
                bottomLeftAbove = tl;
            }

            Image panel = UIFactory.Panel(root, "List", new Color(0.99f, 0.97f, 0.91f, 1f), true);
            RectTransform prt = panel.rectTransform;
            prt.anchorMin = new Vector2(0.5f, 0.5f);
            prt.anchorMax = new Vector2(0.5f, 0.5f);
            prt.sizeDelta = new Vector2(width, h);
            Outline o = panel.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.2f, 0.17f, 0.15f, 0.9f);
            o.effectDistance = new Vector2(2f, -2f);
            Shadow drop = panel.gameObject.AddComponent<Shadow>();
            drop.effectColor = new Color(0f, 0f, 0f, 0.25f);
            drop.effectDistance = new Vector2(4f, -6f);

            // Below the anchor unless it would leave the screen, then above.
            float layerBottom = layer.rect.yMin;
            if (topLeft.y - h < layerBottom + 4f)
            {
                prt.pivot = new Vector2(0f, 0f);
                prt.anchoredPosition = bottomLeftAbove + new Vector2(0f, 2f);
            }
            else
            {
                prt.pivot = new Vector2(0f, 1f);
                prt.anchoredPosition = topLeft - new Vector2(0f, 2f);
            }

            // Unfold from the edge it hangs from (only when it first opens, not on a Shift refresh).
            if (animateNext) panel.gameObject.AddComponent<PopupAnim>();
            animateNext = false;

            if (options.Count == 0)
            {
                Label none = UIFactory.Text(prt, "Empty", "(nothing to choose)", 16f, UIFactory.InkSoft, TextAnchor.MiddleCenter, false);
                UIFactory.Stretch(none.rect, 4f, 4f, 4f, 4f);
                return;
            }

            for (int i = 0; i < options.Count; i++)
            {
                OptionData opt = options[i];
                bool selected = opt.id == current;
                Color bg = selected ? new Color(0.86f, 0.80f, 0.66f, 1f) : new Color(0.99f, 0.97f, 0.91f, 1f);
                if (opt.shiftOnly) bg = new Color(1f, 0.90f, 0.55f, 1f);
                string text = opt.label + (opt.shiftOnly ? "  *" : "");
                UIButton b = UIFactory.Button(prt, "Option_" + opt.id, text, 18f, bg, UIFactory.Ink, delegate
                {
                    OptionData chosen = opt;
                    Action<OptionData> cb = onPick;
                    Close();
                    if (cb != null) cb(chosen);
                });
                UIFactory.TopRow(b.rect, pad + i * rowH, rowH - 2f, pad, pad);
            }
        }
    }
}
