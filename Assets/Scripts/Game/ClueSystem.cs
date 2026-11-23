using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>
    /// Hover / click clues in the scene: floating "?" markers over clue anchors, highlight + tooltip on
    /// hover, click to copy the clue into the form's Notes. Clues with minRejects above the current
    /// rejection count stay hidden.
    /// </summary>
    public class ClueSystem : MonoBehaviour
    {
        public Camera cam;
        public RectTransform overlay;
        public NoticeForm form;

        private class Marker
        {
            public AnchorTag anchor;
            public string text;
            public RectTransform rect;
            public Label label;
            public Image bg;
            public Vector3 worldPoint;
        }

        private readonly List<Marker> markers = new List<Marker>();
        private Stage stage;
        private RectTransform markerLayer;
        private RectTransform tooltip;
        private Label tooltipText;
        private Marker hovered;
        private readonly List<Renderer> litRenderers = new List<Renderer>();
        private MaterialPropertyBlock block;
        private bool active;

        public void Init(RectTransform overlayRect, RectTransform markerParent, RectTransform tooltipParent)
        {
            overlay = overlayRect;
            markerLayer = markerParent;
            block = new MaterialPropertyBlock();

            Image tip = UIFactory.Panel(tooltipParent, "Tooltip", new Color(0.12f, 0.1f, 0.1f, 0.92f), false);
            tooltip = tip.rectTransform;
            tooltip.anchorMin = new Vector2(0.5f, 0.5f);
            tooltip.anchorMax = new Vector2(0.5f, 0.5f);
            tooltip.pivot = new Vector2(0f, 0f);
            tooltip.sizeDelta = new Vector2(300f, 60f);
            tooltipText = UIFactory.Text(tooltip, "Text", "", 16f, new Color(0.98f, 0.95f, 0.88f, 1f), TextAnchor.UpperLeft, false);
            UIFactory.Stretch(tooltipText.rect, 10f, 8f, 10f, 8f);
            tooltip.gameObject.SetActive(false);
        }

        /// <summary>Rebuilds markers for the given stage (call after every stage rebuild or rejection).</summary>
        public void Bind(Stage s, int rejects)
        {
            Unhover();
            ClearMarkers();
            stage = s;
            if (s == null || s.level == null) return;

            var texts = new Dictionary<string, string>();
            var order = new List<string>();
            for (int i = 0; i < s.level.clues.Count; i++)
            {
                ClueData c = s.level.clues[i];
                if (c == null || string.IsNullOrEmpty(c.anchor) || string.IsNullOrEmpty(c.text)) continue;
                if (c.minRejects > rejects) continue;
                string existing;
                if (texts.TryGetValue(c.anchor, out existing)) texts[c.anchor] = existing + "\n" + c.text;
                else { texts[c.anchor] = c.text; order.Add(c.anchor); }
            }
            for (int i = 0; i < order.Count; i++)
            {
                AnchorTag a = s.Find(order[i]);
                if (a == null) continue;
                var m = new Marker();
                m.anchor = a;
                m.text = texts[order[i]];
                m.worldPoint = a.MarkerPoint();
                m.bg = UIFactory.Panel(markerLayer, "Marker_" + a.id, new Color(0.98f, 0.92f, 0.62f, 0.95f), false);
                m.rect = m.bg.rectTransform;
                m.rect.anchorMin = new Vector2(0.5f, 0.5f);
                m.rect.anchorMax = new Vector2(0.5f, 0.5f);
                m.rect.pivot = new Vector2(0.5f, 0f);
                m.rect.sizeDelta = new Vector2(22f, 24f);
                Outline o = m.bg.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0.15f, 0.12f, 0.1f, 0.9f);
                o.effectDistance = new Vector2(1.5f, -1.5f);
                m.label = UIFactory.Text(m.rect, "Q", "?", 18f, UIFactory.Ink, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(m.label.rect, 0f, 0f, 0f, 0f);
                markers.Add(m);
            }
            RefreshNoted();
        }

        public void SetActive(bool on)
        {
            active = on;
            if (!on) Unhover();
            if (markerLayer != null) markerLayer.gameObject.SetActive(on);
        }

        private void ClearMarkers()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].rect != null) Destroy(markers[i].rect.gameObject);
            }
            markers.Clear();
        }

        private void RefreshNoted()
        {
            for (int i = 0; i < markers.Count; i++)
            {
                Marker m = markers[i];
                bool noted = form != null && form.HasNote(m.text);
                m.bg.color = noted ? new Color(0.78f, 0.76f, 0.70f, 0.8f) : new Color(0.98f, 0.92f, 0.62f, 0.95f);
                m.label.color = noted ? UIFactory.InkSoft : UIFactory.Ink;
            }
        }

        private void Update()
        {
            if (!active || stage == null || cam == null) { Unhover(); return; }

            Marker hit = PickUnderMouse();
            if (hit != hovered)
            {
                Unhover();
                if (hit != null) Hover(hit);
            }
            if (hovered != null)
            {
                PlaceTooltip();
                if (Input.GetMouseButtonDown(0) && form != null)
                {
                    if (form.AddNote(hovered.text))
                    {
                        AudioManager.PlaySfx("page");
                        RefreshNoted();
                    }
                    else
                    {
                        AudioManager.PlaySfx("click");
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (!active || cam == null || markerLayer == null) return;
            float bob = Mathf.Sin(Time.time * 3f) * 4f;
            for (int i = 0; i < markers.Count; i++)
            {
                Marker m = markers[i];
                if (m.anchor == null || m.rect == null) continue;
                bool visible = m.anchor.gameObject.activeInHierarchy;
                Vector3 sp = cam.WorldToScreenPoint(m.worldPoint);
                if (sp.z <= 0f || !cam.pixelRect.Contains(new Vector2(sp.x, sp.y))) visible = false;
                if (m.rect.gameObject.activeSelf != visible) m.rect.gameObject.SetActive(visible);
                if (!visible) continue;
                Vector2 local = UIFactory.ScreenToLocal(markerLayer, new Vector2(sp.x, sp.y));
                m.rect.anchoredPosition = local + new Vector2(0f, bob);
            }
        }

        private Marker PickUnderMouse()
        {
            Vector3 mouse = Input.mousePosition;
            if (!cam.pixelRect.Contains(new Vector2(mouse.x, mouse.y))) return null;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return null;
            Ray ray = cam.ScreenPointToRay(mouse);
            RaycastHit hitInfo;
            if (!Physics.Raycast(ray, out hitInfo, 200f)) return null;
            AnchorTag tag = hitInfo.collider.GetComponentInParent<AnchorTag>();
            if (tag == null) return null;
            for (int i = 0; i < markers.Count; i++)
            {
                if (markers[i].anchor == tag) return markers[i];
            }
            return null;
        }

        private void Hover(Marker m)
        {
            hovered = m;
            AudioManager.PlaySfx("hover");
            tooltipText.text = m.text;
            tooltip.gameObject.SetActive(true);
            float width = 320f;
            tooltip.sizeDelta = new Vector2(width, 60f);
            Canvas.ForceUpdateCanvases();
            float h = tooltipText.PreferredHeight + 18f;
            tooltip.sizeDelta = new Vector2(width, Mathf.Max(40f, h));
            PlaceTooltip();

            litRenderers.Clear();
            Renderer[] rs = m.anchor.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rs.Length; i++)
            {
                Renderer r = rs[i];
                if (r == null || r is ParticleSystemRenderer || r.sharedMaterial == null) continue;
                Color baseColor = Mats.GetColor(r.sharedMaterial);
                Color lit = Color.Lerp(baseColor, new Color(1f, 0.95f, 0.7f, baseColor.a), 0.45f);
                r.GetPropertyBlock(block);
                block.SetColor(Mats.BaseColorId, lit);
                block.SetColor(Mats.ColorId, lit);
                r.SetPropertyBlock(block);
                litRenderers.Add(r);
            }
        }

        private void Unhover()
        {
            if (hovered == null && litRenderers.Count == 0)
            {
                if (tooltip != null && tooltip.gameObject.activeSelf) tooltip.gameObject.SetActive(false);
                return;
            }
            hovered = null;
            for (int i = 0; i < litRenderers.Count; i++)
            {
                if (litRenderers[i] != null) litRenderers[i].SetPropertyBlock(null);
            }
            litRenderers.Clear();
            if (tooltip != null) tooltip.gameObject.SetActive(false);
        }

        private void PlaceTooltip()
        {
            if (tooltip == null || overlay == null) return;
            RectTransform parent = tooltip.parent as RectTransform;
            if (parent == null) return;
            Vector2 local = UIFactory.ScreenToLocal(parent, Input.mousePosition);
            Vector2 pos = local + new Vector2(18f, 12f);
            Rect pr = parent.rect;
            Vector2 size = tooltip.sizeDelta;
            if (pos.x + size.x > pr.xMax - 6f) pos.x = local.x - size.x - 12f;
            if (pos.y + size.y > pr.yMax - 6f) pos.y = pr.yMax - 6f - size.y;
            if (pos.x < pr.xMin + 6f) pos.x = pr.xMin + 6f;
            if (pos.y < pr.yMin + 6f) pos.y = pr.yMin + 6f;
            tooltip.anchoredPosition = pos;
        }

        private void OnDisable()
        {
            Unhover();
        }
    }
}
