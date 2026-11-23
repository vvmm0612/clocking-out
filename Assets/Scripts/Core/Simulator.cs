using System;
using System.Collections.Generic;

namespace DeathNotice.Core
{
    /// <summary>
    /// Deterministic single-pass rule simulator (design.md 5.1).
    /// </summary>
    public static class Simulator
    {
        public const string Here = "here";
        public const string ShowPrefix = "show:";
        public const string PlaceField = "place";
        public const string ShiftField = "shift";

        /// <summary>
        /// Step 1: choice facts ("cause:burn") + always + states[chosen shift] + derived X@here.
        /// </summary>
        public static HashSet<string> BuildFacts(LevelData level, IDictionary<string, string> choices)
        {
            var facts = new HashSet<string>(StringComparer.Ordinal);
            if (choices != null)
            {
                foreach (KeyValuePair<string, string> kv in choices)
                {
                    facts.Add(kv.Key + ":" + kv.Value);
                }
            }

            if (level.always != null)
            {
                for (int i = 0; i < level.always.Count; i++) facts.Add(level.always[i]);
            }

            string shift = Get(choices, ShiftField);
            StateData state = level.FindState(shift);
            if (state != null && state.facts != null)
            {
                for (int i = 0; i < state.facts.Count; i++) facts.Add(state.facts[i]);
            }

            string place = Get(choices, PlaceField);
            if (!string.IsNullOrEmpty(place))
            {
                string suffix = "@" + place;
                var derived = new List<string>();
                foreach (string f in facts)
                {
                    if (f.Length > suffix.Length && f.EndsWith(suffix, StringComparison.Ordinal))
                    {
                        derived.Add(f.Substring(0, f.Length - suffix.Length) + "@" + Here);
                    }
                }
                for (int i = 0; i < derived.Count; i++) facts.Add(derived[i]);
            }
            return facts;
        }

        public static SimResult Simulate(LevelData level, IDictionary<string, string> choices)
        {
            if (level == null) throw new ArgumentNullException("level");

            var result = new SimResult();
            HashSet<string> facts = BuildFacts(level, choices);
            string place = Get(choices, PlaceField);
            string verdict = "";
            bool rejected = false;

            List<RuleData> rules = level.rules ?? new List<RuleData>();
            for (int r = 0; r < rules.Count; r++)
            {
                RuleData rule = rules[r];
                if (!Fires(rule, facts)) continue;

                var ev = new SimEvent();
                ev.ruleId = rule.id;
                ev.caption = rule.caption ?? "";
                ev.verdict = rule.verdict ?? "";
                ev.isReject = rule.reject;
                if (rule.beats != null)
                {
                    for (int b = 0; b < rule.beats.Count; b++)
                    {
                        ev.beats.Add(ResolveBeat(rule.beats[b], place));
                    }
                }
                result.events.Add(ev);

                if (rule.add != null)
                {
                    for (int i = 0; i < rule.add.Count; i++) facts.Add(rule.add[i]);
                }
                if (rule.remove != null)
                {
                    for (int i = 0; i < rule.remove.Count; i++) facts.Remove(rule.remove[i]);
                }
                if (!string.IsNullOrEmpty(rule.verdict))
                {
                    verdict = rule.verdict;
                }
                if (rule.reject)
                {
                    rejected = true;
                    result.rejectedBy = rule.id;
                    break;
                }
            }

            result.facts = facts;
            if (!rejected && WishMet(level.wish, facts))
            {
                result.approved = true;
                result.verdict = "";
                return result;
            }

            result.approved = false;
            result.verdict = string.IsNullOrEmpty(verdict) ? (level.defaultVerdict ?? "") : verdict;
            return result;
        }

        public static bool Fires(RuleData rule, HashSet<string> facts)
        {
            if (rule.when != null)
            {
                for (int i = 0; i < rule.when.Count; i++)
                {
                    if (!facts.Contains(rule.when[i])) return false;
                }
            }
            if (rule.unless != null)
            {
                for (int i = 0; i < rule.unless.Count; i++)
                {
                    if (facts.Contains(rule.unless[i])) return false;
                }
            }
            return true;
        }

        public static bool WishMet(WishData wish, HashSet<string> facts)
        {
            if (wish == null) return false;
            if (wish.all != null)
            {
                for (int i = 0; i < wish.all.Count; i++)
                {
                    if (!facts.Contains(wish.all[i])) return false;
                }
            }
            if (wish.none != null)
            {
                for (int i = 0; i < wish.none.Count; i++)
                {
                    if (facts.Contains(wish.none[i])) return false;
                }
            }
            return true;
        }

        /// <summary>Every combination of options (shiftOnly options included), fields in file order.</summary>
        public static List<Dictionary<string, string>> EnumerateCombos(LevelData level)
        {
            var combos = new List<Dictionary<string, string>>();
            combos.Add(new Dictionary<string, string>());
            for (int f = 0; f < level.fields.Count; f++)
            {
                FieldData field = level.fields[f];
                var next = new List<Dictionary<string, string>>();
                for (int c = 0; c < combos.Count; c++)
                {
                    for (int o = 0; o < field.options.Count; o++)
                    {
                        var d = new Dictionary<string, string>(combos[c]);
                        d[field.id] = field.options[o].id;
                        next.Add(d);
                    }
                }
                combos = next;
            }
            return combos;
        }

        /// <summary>"burn + lamp + noon" style key, fields in file order.</summary>
        public static string ComboKey(LevelData level, IDictionary<string, string> choices)
        {
            var parts = new List<string>();
            for (int f = 0; f < level.fields.Count; f++)
            {
                string v = Get(choices, level.fields[f].id);
                parts.Add(v ?? "");
            }
            return string.Join(" + ", parts.ToArray());
        }

        private static BeatData ResolveBeat(BeatData src, string place)
        {
            var b = new BeatData(src.@do ?? "", src.target ?? "", src.arg ?? "");
            if (!string.IsNullOrEmpty(place))
            {
                if (b.target == Here) b.target = place;
                if (b.arg == Here) b.arg = place;
            }
            return b;
        }

        private static string Get(IDictionary<string, string> choices, string key)
        {
            if (choices == null) return null;
            string v;
            return choices.TryGetValue(key, out v) ? v : null;
        }
    }
}
