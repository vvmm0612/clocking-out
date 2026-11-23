using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using DeathNotice.Core;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Plays a SimResult: for each event shows its caption and runs its beats in order (design.md 5.3).
    /// Click / Space jumps to the next event, holding speeds up, SkipAll() snaps to the end.
    /// Every beat runs inside <see cref="Guard"/>, so a broken beat is logged and skipped, never fatal.
    /// </summary>
    public class BeatPlayer : MonoBehaviour
    {
        public CameraRig rig;
        public Action<string> showCaption;

        public bool Playing { get; private set; }
        /// <summary>Index of the event being played (-1 before the first), and how many there are.</summary>
        public int EventIndex { get; private set; }
        public int EventCount { get; private set; }

        private Stage stage;
        private bool advance;
        private bool skipAll;
        private float ignoreInputUntil;
        private Action finished;
        private bool finishedCalled;

        private bool Instant { get { return advance || skipAll; } }

        private float Speed
        {
            get
            {
                if (skipAll) return 100f;
                bool held = GameInput.GetKey(KeyCode.Space) || GameInput.GetMouseButton(0);
                return held ? 3f : 1f;
            }
        }

        public void Play(Stage s, SimResult result, Action onDone)
        {
            StopAllCoroutines();
            stage = s;
            advance = false;
            skipAll = false;
            finished = onDone;
            finishedCalled = false;
            Playing = true;
            EventIndex = -1;
            EventCount = result != null && result.events != null ? result.events.Count : 0;
            ignoreInputUntil = Time.unscaledTime + 0.25f;
            StartCoroutine(Run(result));
        }

        public void SkipAll()
        {
            if (Playing) skipAll = true;
        }

        /// <summary>Abort without calling the completion callback (leaving the level).</summary>
        public void Stop()
        {
            StopAllCoroutines();
            Playing = false;
            finished = null;
        }

        private void Update()
        {
            if (!Playing || GameRoot.ModalOpen) return;
            if (Time.unscaledTime < ignoreInputUntil) return;
            if (GameInput.GetKeyDown(KeyCode.Space) || GameInput.GetMouseButtonDown(0) || GameInput.GetKeyDown(KeyCode.Return))
            {
                advance = true;
            }
            if (GameInput.GetKeyDown(KeyCode.Escape)) skipAll = true;
        }

        private IEnumerator Run(SimResult result)
        {
            yield return Guard(Sequence(result), "sequence");
            Playing = false;
            if (!finishedCalled && finished != null)
            {
                finishedCalled = true;
                try { finished(); }
                catch (Exception e) { Debug.LogError("[BeatPlayer] completion callback failed: " + e); }
            }
        }

        private IEnumerator Sequence(SimResult result)
        {
            if (result == null || result.events == null) yield break;
            for (int i = 0; i < result.events.Count; i++)
            {
                SimEvent ev = result.events[i];
                if (ev == null) continue;
                EventIndex = i;
                advance = false;
                ignoreInputUntil = Time.unscaledTime + 0.15f;
                string caption = ev.caption ?? "";
                if (showCaption != null) showCaption(caption);
                if (ev.beats != null)
                {
                    for (int b = 0; b < ev.beats.Count; b++)
                    {
                        yield return Guard(RunBeat(ev.beats[b]), ev.ruleId + "#" + b);
                    }
                }
                float hold = Mathf.Clamp(0.9f + caption.Length * 0.03f, 1.4f, 4.5f);
                yield return Wait(hold);
            }
            advance = false;
            yield return Wait(0.25f);
        }

        /// <summary>
        /// Runs a coroutine (and anything it yields) by hand so exceptions anywhere inside are caught.
        /// </summary>
        private IEnumerator Guard(IEnumerator root, string label)
        {
            var stack = new Stack<IEnumerator>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                bool moved;
                bool failed = false;
                object current = null;
                try
                {
                    moved = top.MoveNext();
                    if (moved) current = top.Current;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[BeatPlayer] '" + label + "' failed and was skipped: " + e.Message);
                    moved = false;
                    failed = true;
                }
                if (failed) break;
                if (!moved) { stack.Pop(); continue; }
                IEnumerator nested = current as IEnumerator;
                if (nested != null) { stack.Push(nested); continue; }
                yield return current;
            }
        }

        // ---------------------------------------------------------------- timing

        private IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds && !Instant)
            {
                yield return null;
                t += Time.deltaTime * Speed;
            }
        }

        private IEnumerator Tween(float duration, Action<float> step)
        {
            float t = 0f;
            while (t < duration && !Instant)
            {
                step(Mathf.Clamp01(t / duration));
                yield return null;
                t += Time.deltaTime * Speed;
            }
            step(1f);
        }

        private static float Smooth(float k) { return k * k * (3f - 2f * k); }
        private static float EaseIn(float k) { return k * k; }
        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }

        /// <summary>Lands a lying figure: flattened on impact, springs back to the resting squash.</summary>
        private IEnumerator LandSquash(Transform v, Vector3 rest, float duration)
        {
            if (v == null) yield break;
            Vector3 hit = new Vector3(rest.x * 1.3f, rest.y * 1.0f, rest.z * 0.45f);
            yield return Tween(duration, delegate (float k)
            {
                if (v == null) return;
                float e = EaseOutBack(k);
                v.localScale = Vector3.LerpUnclamped(hit, rest, e);
            });
        }

        private static float ParseFloat(string s, float fallback)
        {
            float v;
            if (!string.IsNullOrEmpty(s) && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) return v;
            return fallback;
        }

        // ---------------------------------------------------------------- dispatch

        private IEnumerator RunBeat(BeatData b)
        {
            if (b == null || stage == null) yield break;
            string op = b.@do ?? "";
            IEnumerator inner = null;
            switch (op)
            {
                case "ghost_to": inner = GhostTo(b.target); break;
                case "die_burn": inner = DieBurn(b.target); break;
                case "die_fall": inner = DieFall(b.target); break;
                case "die_drown": inner = DieDrown(b.target); break;
                case "fx": inner = Fx(b.target, b.arg); break;
                case "move": inner = Move(b.target, b.arg); break;
                case "npc_to": inner = NpcTo(b.target, b.arg); break;
                case "rotate":
                {
                    AnchorTag a = stage.Find(b.target);
                    if (a != null) a.SetSpin(ParseFloat(b.arg, 1f));
                    break;
                }
                case "toggle":
                {
                    AnchorTag a = stage.Find(b.target);
                    if (a != null) a.Toggle(b.arg != "off");
                    inner = Wait(0.2f);
                    break;
                }
                case "light":
                {
                    AnchorTag a = stage.Find(b.target);
                    if (a != null) a.LightUp();
                    inner = Wait(0.3f);
                    break;
                }
                case "sfx": AudioManager.PlaySfx(b.arg); break;
                case "wait": inner = Wait(Mathf.Clamp(ParseFloat(b.arg, 0.5f), 0f, 5f)); break;
                case "shake":
                    if (rig != null) rig.Shake(ParseFloat(b.arg, 0.3f));
                    inner = Wait(0.15f);
                    break;
                default:
                    stage.Warn("beat:" + op, "[BeatPlayer] Unknown beat '" + op + "' (ignored).");
                    break;
            }
            if (inner != null) yield return inner;
        }

        // ---------------------------------------------------------------- helpers

        private Figure Player { get { return stage != null ? stage.player : null; } }

        private Vector3 CameraPos
        {
            get { return rig != null && rig.cam != null ? rig.cam.transform.position : new Vector3(0f, 5f, -10f); }
        }

        /// <summary>Horizontal direction from the stage toward the camera.</summary>
        private Vector3 TowardCamera
        {
            get
            {
                if (rig == null || rig.cam == null) return Vector3.back;
                Vector3 f = -rig.cam.transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.back;
            }
        }

        private void Appear(Figure p, Vector3 at)
        {
            p.gameObject.SetActive(true);
            p.ResetPose();
            p.transform.position = at;
            p.Face(CameraPos);
        }

        private IEnumerator Hop(Figure p, Vector3 to, float duration)
        {
            Vector3 from = p.transform.position;
            float dist = Vector3.Distance(from, to);
            if (dist < 0.03f) yield break;
            float height = 0.25f + Mathf.Min(1.2f, dist * 0.12f);
            p.Face(to);
            Transform v = p.visual;
            yield return Tween(duration, delegate (float k)
            {
                if (p == null) return;
                float e = Smooth(k);
                p.transform.position = Vector3.Lerp(from, to, e) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * height);
                // stretch in the air, back to normal on landing
                float s = Mathf.Sin(k * Mathf.PI);
                if (v != null) v.localScale = new Vector3(1f - 0.12f * s, 1f + 0.18f * s, 1f - 0.12f * s);
            });
            if (v != null) v.localScale = Vector3.one;
            if (p != null) p.Face(CameraPos);
        }

        private IEnumerator PrepareAt(AnchorTag a)
        {
            Figure p = Player;
            if (p == null) yield break;
            if (!p.gameObject.activeSelf)
            {
                Appear(p, a != null ? a.StandPoint : p.transform.position);
                yield break;
            }
            if (a != null && Vector3.Distance(p.transform.position, a.StandPoint) > 0.2f)
            {
                yield return Hop(p, a.StandPoint, 0.45f);
            }
        }

        private Vector3 GroundBelow(Vector3 start)
        {
            RaycastHit hit;
            if (Physics.Raycast(start + Vector3.up * 0.1f, Vector3.down, out hit, 60f)) return hit.point;
            return start + Vector3.down * 3f;
        }

        // ---------------------------------------------------------------- beats

        private IEnumerator GhostTo(string target)
        {
            AnchorTag a = stage.Find(target);
            Figure p = Player;
            if (a == null || p == null) yield break;
            if (!p.gameObject.activeSelf)
            {
                Appear(p, a.StandPoint);
                Transform v = p.visual;
                yield return Tween(0.3f, delegate (float k)
                {
                    if (v != null) v.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, Smooth(k));
                });
                yield break;
            }
            yield return Hop(p, a.StandPoint, 0.55f);
        }

        private IEnumerator DieBurn(string target)
        {
            AnchorTag a = stage.Find(target);
            yield return PrepareAt(a);
            Figure p = Player;
            if (p == null) yield break;
            AudioManager.PlaySfx("die_burn");
            ParticleSystem fire = FxLibrary.Spawn("fire", p.transform.position + Vector3.up * 0.25f, stage.transform, Vector3.zero);
            Color hot = new Color(0.95f, 0.28f, 0.15f);
            Color ash = new Color(0.24f, 0.23f, 0.23f);
            Transform v = p.visual;
            // Heat up with a nervous jitter, then slump into a squat pile of ash that settles with a little bounce.
            yield return Tween(0.7f, delegate (float k)
            {
                if (p != null) p.Blend(hot, EaseIn(k) * 0.85f, ash, 0f);
                if (v != null) v.localPosition = new Vector3(Mathf.Sin(k * 60f) * 0.015f * k, 0f, 0f);
            });
            yield return Wait(0.4f);
            yield return Tween(0.55f, delegate (float k)
            {
                float e = EaseOutBack(k);
                if (p != null) p.Blend(hot, 0.85f, ash, k);
                if (v != null)
                {
                    v.localPosition = Vector3.zero;
                    v.localScale = new Vector3(1f + 0.4f * e, Mathf.LerpUnclamped(1f, 0.18f, e), 1f + 0.4f * e);
                }
            });
            FxLibrary.StopEmitting(fire);
            if (p != null) FxLibrary.Spawn("dust", p.transform.position + Vector3.up * 0.1f, stage.transform, Vector3.zero);
            yield return Wait(0.3f);
        }

        private IEnumerator DieFall(string target)
        {
            AnchorTag a = stage.Find(target);
            Figure p = Player;
            if (p == null) yield break;
            if (!p.gameObject.activeSelf) Appear(p, a != null ? a.StandPoint : p.transform.position);

            Vector3 from = p.transform.position;
            Vector3 to;
            if (a != null && a.hasFallLanding) to = a.fallLanding;
            else if (a != null) to = a.StandPoint;
            else to = from + Vector3.down * 2f;

            Vector3 flat = to - from;
            flat.y = 0f;
            if (flat.magnitude < 0.35f && Mathf.Abs(to.y - from.y) < 0.5f)
            {
                // Falling "from" where we stand: step off toward the camera and drop to whatever is below.
                to = GroundBelow(from + TowardCamera * 0.9f + Vector3.down * 0.3f);
            }

            float drop = Mathf.Max(0f, from.y - to.y);
            float duration = Mathf.Clamp(0.45f + Mathf.Sqrt(drop) * 0.22f, 0.5f, 1.3f);
            float hop = drop > 0.5f ? 0.35f : 0.6f;
            AudioManager.PlaySfx("die_fall");
            Transform v = p.visual;
            yield return Tween(duration, delegate (float k)
            {
                if (p == null) return;
                Vector3 pos = Vector3.Lerp(from, to, k);
                pos.y = Mathf.Lerp(from.y, to.y, k * k) + 4f * hop * k * (1f - k);
                p.transform.position = pos;
                if (v != null) v.localRotation = Quaternion.Euler(k * 280f, 0f, k * 35f);
            });
            if (p == null) yield break;
            p.transform.position = to;
            if (v != null)
            {
                v.localRotation = Quaternion.Euler(90f, 0f, 0f);
                v.localPosition = new Vector3(0f, 0.14f, 0f);
            }
            AudioManager.PlaySfx("impact");
            if (rig != null) rig.Shake(Mathf.Clamp01(0.25f + drop * 0.06f));
            FxLibrary.Spawn("dust", to + Vector3.up * 0.05f, stage.transform, Vector3.zero);
            yield return LandSquash(v, new Vector3(1.15f, 1f, 0.75f), 0.35f);
            yield return Wait(0.25f);
        }

        private IEnumerator DieDrown(string target)
        {
            AnchorTag a = stage.Find(target);
            yield return PrepareAt(a);
            Figure p = Player;
            if (p == null) yield break;
            if (a != null && a.drownHop.sqrMagnitude > 0.0001f)
            {
                yield return Hop(p, p.transform.position + a.drownHop, 0.4f);
            }
            if (p == null) yield break;
            AudioManager.PlaySfx("die_drown");
            Vector3 from = p.transform.position;
            Vector3 to = from + Vector3.down * 1.1f;
            FxLibrary.Spawn("splash", from + Vector3.up * 0.3f, stage.transform, Vector3.zero);
            ParticleSystem bubbles = FxLibrary.Spawn("bubbles", from + Vector3.up * 0.2f, stage.transform, Vector3.zero);
            Transform v = p.visual;
            yield return Tween(1.5f, delegate (float k)
            {
                if (p == null) return;
                // two struggling bobs, then a slow sink
                float bob = Mathf.Sin(k * Mathf.PI * 4f) * 0.08f * (1f - k);
                p.transform.position = Vector3.Lerp(from, to, EaseIn(k)) + Vector3.up * bob;
                if (v != null)
                {
                    v.localRotation = Quaternion.Euler(Mathf.Sin(k * 18f) * 8f, k * 120f, 0f);
                    float sq = 1f + Mathf.Sin(k * Mathf.PI * 4f) * 0.06f * (1f - k);
                    v.localScale = new Vector3(sq, 2f - sq, sq);
                }
            });
            if (p != null) p.gameObject.SetActive(false);
            yield return Wait(0.6f);
            FxLibrary.StopEmitting(bubbles);
        }

        private IEnumerator Fx(string target, string kind)
        {
            AnchorTag a = stage.Find(target);
            if (a == null) yield break;
            FxLibrary.Spawn(kind ?? "", a.FxPoint, stage.transform, a.waterDir);
            yield return Wait(0.35f);
        }

        private IEnumerator Move(string target, string destination)
        {
            AnchorTag mover = stage.Find(target);
            AnchorTag dest = stage.Find(destination);
            if (mover == null || dest == null) yield break;
            if (!mover.gameObject.activeSelf) mover.gameObject.SetActive(true);
            Transform tr = mover.transform;
            Vector3 from = tr.position;
            Vector3 to = mover.isFigure ? stage.NpcPoint(dest) : dest.ParkPoint;
            float duration = Mathf.Clamp(Vector3.Distance(from, to) * 0.22f, 0.6f, 2.2f);
            yield return Tween(duration, delegate (float k)
            {
                if (tr != null) tr.position = Vector3.Lerp(from, to, Smooth(k));
            });
        }

        private IEnumerator NpcTo(string target, string destination)
        {
            AnchorTag npc = stage.Find(target);
            AnchorTag dest = stage.Find(destination);
            if (npc == null || dest == null) yield break;
            Vector3 to = stage.NpcPoint(dest);
            Transform tr = npc.transform;
            if (!npc.gameObject.activeSelf)
            {
                npc.gameObject.SetActive(true);
                tr.position = to;
                yield return Wait(0.3f);
                yield break;
            }
            Vector3 from = tr.position;
            Vector3 d = to - from;
            d.y = 0f;
            if (d.sqrMagnitude > 0.0001f) tr.rotation = Quaternion.LookRotation(-d.normalized, Vector3.up);
            float duration = Mathf.Clamp(Vector3.Distance(from, to) * 0.35f, 0.4f, 1.4f);
            yield return Tween(duration, delegate (float k)
            {
                if (tr == null) return;
                tr.position = Vector3.Lerp(from, to, Smooth(k)) + Vector3.up * (Mathf.Abs(Mathf.Sin(k * Mathf.PI * 4f)) * 0.08f);
            });
            if (tr != null) tr.rotation = Quaternion.identity;
        }
    }
}
