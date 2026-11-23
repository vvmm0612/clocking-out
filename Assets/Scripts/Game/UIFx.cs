using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DeathNotice.Game
{
    /// <summary>Button feel: grows a little on hover, dips on press, springs back (unscaled time).</summary>
    public class ButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private bool over, down;
        private float scale = 1f;
        private Selectable sel;

        private void Awake() { sel = GetComponent<Selectable>(); }

        public void OnPointerEnter(PointerEventData e) { over = true; }
        public void OnPointerExit(PointerEventData e) { over = false; down = false; }
        public void OnPointerDown(PointerEventData e) { down = true; }
        public void OnPointerUp(PointerEventData e) { down = false; }

        private void OnDisable()
        {
            over = down = false;
            scale = 1f;
            transform.localScale = Vector3.one;
        }

        private void Update()
        {
            bool live = sel == null || sel.IsInteractable();
            float target = !live ? 1f : down ? 0.95f : over ? 1.04f : 1f;
            if (Mathf.Abs(scale - target) < 0.0005f && Mathf.Approximately(transform.localScale.x, target)) return;
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 18f));
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Drop-down opening: unfolds from the anchor edge and fades in over ~0.12 s.</summary>
    public class PopupAnim : MonoBehaviour
    {
        private CanvasGroup group;
        private float t;
        private const float Duration = 0.12f;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            Step();
        }

        private void Update()
        {
            t += Time.unscaledDeltaTime;
            Step();
            if (t >= Duration) enabled = false;
        }

        private void Step()
        {
            float k = Mathf.Clamp01(t / Duration);
            float e = 1f - (1f - k) * (1f - k);
            transform.localScale = new Vector3(1f, Mathf.Lerp(0.55f, 1f, e), 1f);
            group.alpha = Mathf.Lerp(0f, 1f, e);
        }
    }

    /// <summary>Shakes a RectTransform around its resting position for a moment (the paper when stamped).</summary>
    public class UIShake : MonoBehaviour
    {
        private RectTransform rt;
        private Vector2 rest;
        private float left, total, amp;

        public static void Kick(RectTransform target, float amplitude, float seconds)
        {
            if (target == null) return;
            UIShake s = target.GetComponent<UIShake>();
            if (s == null) s = target.gameObject.AddComponent<UIShake>();
            s.Begin(amplitude, seconds);
        }

        private void Begin(float amplitude, float seconds)
        {
            if (rt == null) { rt = (RectTransform)transform; }
            if (left <= 0f) rest = rt.anchoredPosition;
            amp = amplitude;
            total = left = Mathf.Max(0.01f, seconds);
            enabled = true;
        }

        private void Update()
        {
            if (rt == null || left <= 0f) { enabled = false; return; }
            left -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(left / total);
            if (left <= 0f) { rt.anchoredPosition = rest; enabled = false; return; }
            rt.anchoredPosition = rest + new Vector2(Mathf.Sin(Time.unscaledTime * 90f), Mathf.Cos(Time.unscaledTime * 73f)) * amp * k * k;
        }

        private void OnDisable()
        {
            if (rt != null && left > 0f) rt.anchoredPosition = rest;
            left = 0f;
        }
    }

    /// <summary>A small burst of paper confetti and stars over a rect (APPROVED). Self-destructs.</summary>
    public class UIConfetti : MonoBehaviour
    {
        private class Bit
        {
            public RectTransform rt;
            public Image img;
            public Vector2 v;
            public float spin, life;
        }

        private readonly List<Bit> bits = new List<Bit>();
        private float age;
        private const float Life = 1.6f;

        public static void Burst(RectTransform parent, Vector2 at, int count)
        {
            RectTransform root = UIFactory.Rect(parent, "Confetti");
            UIFactory.Stretch(root, 0f, 0f, 0f, 0f);
            UIConfetti c = root.gameObject.AddComponent<UIConfetti>();
            c.Spawn(at, count);
        }

        private void Spawn(Vector2 at, int count)
        {
            Color[] colors =
            {
                new Color(0.86f, 0.25f, 0.2f), new Color(0.98f, 0.82f, 0.36f), new Color(0.3f, 0.6f, 0.42f),
                new Color(0.97f, 0.95f, 0.88f), new Color(0.36f, 0.48f, 0.78f)
            };
            for (int i = 0; i < count; i++)
            {
                var b = new Bit();
                bool star = i % 5 == 0;
                b.img = UIFactory.Panel(transform, star ? "Star" : "Bit", colors[i % colors.Length], false);
                if (star) { b.img.sprite = UITextures.Diamond(); b.img.color = new Color(1f, 0.92f, 0.55f); }
                b.rt = b.img.rectTransform;
                b.rt.anchorMin = b.rt.anchorMax = new Vector2(0.5f, 0.5f);
                b.rt.sizeDelta = star ? new Vector2(14f, 14f) : new Vector2(Random.Range(6f, 10f), Random.Range(10f, 16f));
                b.rt.anchoredPosition = at + Random.insideUnitCircle * 20f;
                float ang = Random.Range(20f, 160f) * Mathf.Deg2Rad;
                float sp = Random.Range(260f, 520f);
                b.v = new Vector2(Mathf.Cos(ang) * sp, Mathf.Sin(ang) * sp);
                b.spin = Random.Range(-540f, 540f);
                b.rt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
                bits.Add(b);
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            age += dt;
            for (int i = 0; i < bits.Count; i++)
            {
                Bit b = bits[i];
                if (b.rt == null) continue;
                b.v.y -= 900f * dt;
                b.v *= 1f - 1.4f * dt;
                b.rt.anchoredPosition += b.v * dt;
                b.rt.localRotation = Quaternion.Euler(0f, 0f, b.rt.localEulerAngles.z + b.spin * dt);
                // paper flutter: squash the width as it turns
                float f = Mathf.Abs(Mathf.Sin(age * 9f + i));
                b.rt.localScale = new Vector3(0.35f + 0.65f * f, 1f, 1f);
                Color c = b.img.color;
                c.a = Mathf.Clamp01((Life - age) / 0.5f);
                b.img.color = c;
            }
            if (age >= Life) Destroy(gameObject);
        }
    }

    /// <summary>
    /// The Grim Reaper as a flat cut-out standing at the bottom-right of the 3D view, beside the paper:
    /// breathes slowly, and raises his arm and brings the rubber stamp down when a form is stamped
    /// (the arm drops straight back once the stamp lands).
    /// </summary>
    public class ReaperUI : MonoBehaviour
    {
        private RectTransform body;
        private RectTransform arm;
        private float idleT;
        private float armAngle = -12f;
        private Coroutine gesture;
        public float baseHeight = 230f;

        private CanvasGroup group;
        private float shown = 1f;
        private float showTarget = 1f;
        private RectTransform rootRt;
        private float yTarget;

        /// <summary>
        /// A cut-out of the given height whose bottom-right corner sits at anchor (normalized, in parent) plus
        /// offset. faceRight mirrors him (the sprites face left).
        /// </summary>
        public static ReaperUI Create(RectTransform parent, float height, Vector2 anchor, Vector2 pivot, Vector2 offset, bool faceRight)
        {
            RectTransform root = UIFactory.Rect(parent, "Reaper");
            root.anchorMin = root.anchorMax = anchor;
            root.pivot = pivot;
            root.sizeDelta = new Vector2(height * 0.5f, height);
            root.anchoredPosition = offset;
            ReaperUI r = root.gameObject.AddComponent<ReaperUI>();
            r.baseHeight = height;
            r.rootRt = root;
            r.yTarget = offset.y;
            r.group = root.gameObject.AddComponent<CanvasGroup>();
            r.group.blocksRaycasts = false;
            r.group.interactable = false;

            RectTransform flip = UIFactory.Rect(root, "Flip");
            UIFactory.Stretch(flip, 0f, 0f, 0f, 0f);
            flip.pivot = new Vector2(0.5f, 0f);
            flip.localScale = new Vector3(faceRight ? -1f : 1f, 1f, 1f);

            Image armImg = UIFactory.Panel(flip, "Arm", Color.white, false);
            armImg.sprite = UITextures.ReaperArm();
            r.arm = armImg.rectTransform;
            r.arm.anchorMin = r.arm.anchorMax = new Vector2(0.32f, 0.63f);
            r.arm.pivot = new Vector2(0.5f, 0.94f);
            r.arm.sizeDelta = new Vector2(height * 0.25f, height * 0.5f);
            r.arm.anchoredPosition = Vector2.zero;

            Image bodyImg = UIFactory.Panel(flip, "Body", Color.white, false);
            bodyImg.sprite = UITextures.ReaperBody();
            r.body = bodyImg.rectTransform;
            UIFactory.Stretch(r.body, 0f, 0f, 0f, 0f);
            r.body.pivot = new Vector2(0.5f, 0f);
            // the arm hangs in front of the robe
            r.arm.SetAsLastSibling();
            r.SetArm(r.armAngle);
            return r;
        }

        private void SetArm(float angle)
        {
            armAngle = angle;
            if (arm != null) arm.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDisable()
        {
            // Coroutines die with the object (leaving a case hides the paper); never stay frozen mid-stamp.
            gesture = null;
            SetArm(-12f);
        }

        /// <summary>Fade in / out (and sink a little) instead of popping.</summary>
        public void Show(bool on)
        {
            showTarget = on ? 1f : 0f;
        }

        public void SetBaseY(float y)
        {
            yTarget = y;
        }

        private void Update()
        {
            idleT += Time.unscaledDeltaTime;
            shown = Mathf.MoveTowards(shown, showTarget, Time.unscaledDeltaTime * 4f);
            if (group != null) group.alpha = shown;
            if (rootRt != null)
            {
                Vector2 p = rootRt.anchoredPosition;
                float y = yTarget - (1f - shown) * 30f;
                if (Mathf.Abs(p.y - y) > 0.01f) rootRt.anchoredPosition = new Vector2(p.x, Mathf.Lerp(p.y, y, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 12f)));
            }
            if (body != null)
            {
                float s = 1f + Mathf.Sin(idleT * 1.4f) * 0.012f;
                body.localScale = new Vector3(1f, s, 1f);
            }
            if (gesture == null) SetArm(Mathf.Lerp(armAngle, -12f + Mathf.Sin(idleT * 1.1f) * 3f, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 4f)));
        }

        /// <summary>Raise (raiseTime), then slam down at the moment the stamp lands; returns the raise duration.</summary>
        public void Stamp(float raiseTime, float slamTime)
        {
            if (!isActiveAndEnabled) return;
            if (gesture != null) StopCoroutine(gesture);
            gesture = StartCoroutine(StampRoutine(raiseTime, slamTime));
        }

        private IEnumerator StampRoutine(float raise, float slam)
        {
            float start = armAngle;
            float t = 0f;
            while (t < raise)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / raise);
                SetArm(Mathf.Lerp(start, -150f, 1f - (1f - k) * (1f - k)));
                yield return null;
            }
            t = 0f;
            while (t < slam)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / slam);
                SetArm(Mathf.Lerp(-150f, -78f, k * k * k));
                yield return null;
            }
            // the stamp has landed: pull the arm straight back down to the idle pose (no hold)
            t = 0f;
            const float back = 0.16f;
            while (t < back)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / back);
                SetArm(Mathf.Lerp(-78f, -12f, 1f - (1f - k) * (1f - k)));
                yield return null;
            }
            SetArm(-12f);
            gesture = null;
        }
    }
}
