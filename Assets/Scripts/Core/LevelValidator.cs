using System;
using System.Collections.Generic;

namespace DeathNotice.Core
{
    /// <summary>
    /// Same checks as tools/validate_levels.py. Returns a list of human-readable
    /// errors; an empty list means the level is valid.
    ///  (a) approved combinations == declared solutions
    ///  (b) every rejected combination has a non-empty verdict
    ///  (c) every rule fires for some combination unless marked unreachable (and unreachable rules never fire)
    ///  (d) every anchor referenced by beats, clues and show: facts is listed in anchors
    ///  (e) every combination that ends in a death plays ghost_to + die_&lt;cause&gt;
    /// </summary>
    public static class LevelValidator
    {
        private static readonly string[] BeatOps =
        {
            "ghost_to", "die_burn", "die_drown", "die_fall", "fx", "move", "rotate",
            "toggle", "light", "npc_to", "sfx", "wait", "shake"
        };
        private static readonly string[] TargetOps =
        {
            "ghost_to", "die_burn", "die_drown", "die_fall", "fx", "move", "rotate",
            "toggle", "light", "npc_to"
        };
        private static readonly string[] ArgAnchorOps = { "move", "npc_to" };
        private static readonly string[] FxKinds = { "fire", "water", "smoke", "sparks", "splash" };
        private static readonly string[] DeathCauses = { "burn", "drown", "fall" };

        public static List<string> Validate(LevelData level)
        {
            var errors = new List<string>();
            string lid = level.id;

            // ---------- structure
            if (string.IsNullOrEmpty(level.id)) errors.Add("(level without id): missing 'id'");
            if (string.IsNullOrEmpty(level.title)) errors.Add(lid + ": missing 'title'");
            if (string.IsNullOrEmpty(level.deceased)) errors.Add(lid + ": missing 'deceased'");
            if (string.IsNullOrEmpty(level.wishText)) errors.Add(lid + ": missing 'wishText'");
            if (level.wish.all.Count == 0 && level.wish.none.Count == 0) errors.Add(lid + ": wish has no conditions");

            var fieldIds = new HashSet<string>();
            for (int i = 0; i < level.fields.Count; i++)
            {
                FieldData f = level.fields[i];
                if (!fieldIds.Add(f.id)) errors.Add(lid + ": duplicate field id " + f.id);
                if (f.options.Count == 0) errors.Add(lid + ": field " + f.id + " has no options");
                var optIds = new HashSet<string>();
                for (int j = 0; j < f.options.Count; j++)
                {
                    if (!optIds.Add(f.options[j].id)) errors.Add(lid + ": duplicate option " + f.id + "." + f.options[j].id);
                }
            }
            FieldData placeField = level.FindField(Simulator.PlaceField);
            if (placeField == null) errors.Add(lid + ": no 'place' field");

            var anchors = new HashSet<string>();
            for (int i = 0; i < level.anchors.Count; i++)
            {
                if (!anchors.Add(level.anchors[i])) errors.Add(lid + ": duplicate anchor " + level.anchors[i]);
            }
            if (placeField != null)
            {
                for (int i = 0; i < placeField.options.Count; i++)
                {
                    if (!anchors.Contains(placeField.options[i].id))
                        errors.Add(lid + ": place '" + placeField.options[i].id + "' is not listed in anchors");
                }
            }

            FieldData shiftField = level.FindField(Simulator.ShiftField);
            var seenShift = new HashSet<string>();
            for (int i = 0; i < level.states.Count; i++)
            {
                StateData st = level.states[i];
                if (shiftField == null) errors.Add(lid + ": states given but level has no shift field");
                else if (shiftField.FindOption(st.shift) == null) errors.Add(lid + ": state for unknown shift '" + st.shift + "'");
                if (!seenShift.Add(st.shift)) errors.Add(lid + ": duplicate state for shift '" + st.shift + "'");
                for (int j = 0; j < st.facts.Count; j++)
                {
                    string fact = st.facts[j];
                    if (!fact.StartsWith(Simulator.ShowPrefix, StringComparison.Ordinal)) continue;
                    string body = fact.Substring(Simulator.ShowPrefix.Length);
                    int at = body.IndexOf('@');
                    if (at >= 0)
                    {
                        string who = body.Substring(0, at);
                        string where = body.Substring(at + 1);
                        if (!anchors.Contains(who)) errors.Add(lid + ": (d) show fact '" + fact + "' references unknown anchor '" + who + "'");
                        if (!anchors.Contains(where)) errors.Add(lid + ": (d) show fact '" + fact + "' references unknown anchor '" + where + "'");
                    }
                    else if (body.IndexOf('=') < 0)
                    {
                        errors.Add(lid + ": show fact '" + fact + "' is neither who@where nor key=value");
                    }
                }
            }

            for (int i = 0; i < level.clues.Count; i++)
            {
                ClueData c = level.clues[i];
                if (!anchors.Contains(c.anchor)) errors.Add(lid + ": (d) clue anchor '" + c.anchor + "' not in anchors");
                if (string.IsNullOrEmpty(c.text)) errors.Add(lid + ": clue '" + c.anchor + "' has empty text");
            }

            var ruleIds = new HashSet<string>();
            for (int i = 0; i < level.rules.Count; i++)
            {
                RuleData r = level.rules[i];
                if (!ruleIds.Add(r.id)) errors.Add(lid + ": duplicate rule id " + r.id);
                if (r.when.Count == 0) errors.Add(lid + ": rule " + r.id + " has empty 'when'");
                if (string.IsNullOrEmpty(r.caption)) errors.Add(lid + ": rule " + r.id + " has empty caption");
                if (r.reject && string.IsNullOrEmpty(r.verdict)) errors.Add(lid + ": reject rule " + r.id + " has no verdict");
                if (r.beats.Count < 2 || r.beats.Count > 5) errors.Add(lid + ": rule " + r.id + " has " + r.beats.Count + " beats (expected 2-5)");
                CheckShowUse(lid, r, r.when, errors);
                CheckShowUse(lid, r, r.unless, errors);
                CheckShowUse(lid, r, r.add, errors);
                CheckShowUse(lid, r, r.remove, errors);
                for (int b = 0; b < r.beats.Count; b++)
                {
                    CheckBeat(lid, r.id, r.beats[b], anchors, errors);
                }
            }

            var solutionKeys = new HashSet<string>();
            for (int i = 0; i < level.solutions.Count; i++)
            {
                Dictionary<string, string> ch = level.solutions[i].ToDictionary();
                foreach (KeyValuePair<string, string> kv in ch)
                {
                    FieldData f = level.FindField(kv.Key);
                    if (f == null) errors.Add(lid + ": solution references unknown field '" + kv.Key + "'");
                    else if (f.FindOption(kv.Value) == null) errors.Add(lid + ": solution references unknown option '" + kv.Key + "." + kv.Value + "'");
                }
                if (ch.Count != level.fields.Count || level.solutions[i].choices.Count != level.fields.Count)
                    errors.Add(lid + ": solution #" + i + " does not set exactly the level's fields");
                string key = Simulator.ComboKey(level, ch);
                if (!solutionKeys.Add(key)) errors.Add(lid + ": duplicate solution " + key);
            }

            if (errors.Count > 0) return errors;

            // ---------- simulate every combination
            var fired = new HashSet<string>();
            var approved = new HashSet<string>();
            List<Dictionary<string, string>> combos = Simulator.EnumerateCombos(level);
            for (int i = 0; i < combos.Count; i++)
            {
                Dictionary<string, string> ch = combos[i];
                SimResult res = Simulator.Simulate(level, ch);
                string key = Simulator.ComboKey(level, ch);
                for (int e = 0; e < res.events.Count; e++) fired.Add(res.events[e].ruleId);

                if (res.approved) approved.Add(key);
                else if (string.IsNullOrEmpty(res.verdict)) errors.Add(lid + ": (b) rejected combo " + key + " has empty verdict");

                string cause;
                if (res.rejectedBy == null && ch.TryGetValue("cause", out cause) && Contains(DeathCauses, cause))
                {
                    bool ghost = false, die = false;
                    for (int e = 0; e < res.events.Count; e++)
                    {
                        List<BeatData> beats = res.events[e].beats;
                        for (int b = 0; b < beats.Count; b++)
                        {
                            if (beats[b].@do == "ghost_to") ghost = true;
                            if (beats[b].@do == "die_" + cause) die = true;
                        }
                    }
                    if (!ghost || !die)
                        errors.Add(lid + ": (e) combo " + key + " dies by " + cause + " but plays no ghost_to + die_" + cause + " beat");
                }
            }

            foreach (string k in approved)
            {
                if (!solutionKeys.Contains(k)) errors.Add(lid + ": (a) approved but not declared as solution: " + k);
            }
            foreach (string k in solutionKeys)
            {
                if (!approved.Contains(k)) errors.Add(lid + ": (a) declared solution not approved: " + k);
            }

            for (int i = 0; i < level.rules.Count; i++)
            {
                RuleData r = level.rules[i];
                if (r.unreachable)
                {
                    if (fired.Contains(r.id)) errors.Add(lid + ": (c) rule " + r.id + " is marked unreachable but fires");
                }
                else if (!fired.Contains(r.id))
                {
                    errors.Add(lid + ": (c) rule " + r.id + " never fires (mark it unreachable if intended)");
                }
            }
            return errors;
        }

        private static void CheckShowUse(string lid, RuleData r, List<string> facts, List<string> errors)
        {
            for (int i = 0; i < facts.Count; i++)
            {
                if (facts[i].StartsWith(Simulator.ShowPrefix, StringComparison.Ordinal))
                    errors.Add(lid + ": rule " + r.id + " uses show: fact '" + facts[i] + "'");
            }
        }

        private static void CheckBeat(string lid, string rid, BeatData b, HashSet<string> anchors, List<string> errors)
        {
            string op = b.@do;
            if (!Contains(BeatOps, op))
            {
                errors.Add(lid + ": rule " + rid + ": unknown beat op '" + op + "'");
                return;
            }
            string tgt = b.target;
            bool tgtOk = tgt == Simulator.Here || anchors.Contains(tgt);
            if (Contains(TargetOps, op) ? !tgtOk : (!string.IsNullOrEmpty(tgt) && !tgtOk))
                errors.Add(lid + ": (d) rule " + rid + ": beat " + op + " target '" + tgt + "' not in anchors");

            string arg = b.arg;
            if (Contains(ArgAnchorOps, op) && arg != Simulator.Here && !anchors.Contains(arg))
                errors.Add(lid + ": (d) rule " + rid + ": beat " + op + " arg '" + arg + "' not in anchors");
            if (op == "fx" && !Contains(FxKinds, arg))
                errors.Add(lid + ": rule " + rid + ": fx kind '" + arg + "' unknown");
            if (op == "wait" || op == "shake" || op == "rotate")
            {
                float tmp;
                if (!float.TryParse(arg, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out tmp))
                    errors.Add(lid + ": rule " + rid + ": beat " + op + " arg '" + arg + "' is not a number");
            }
            if (op == "toggle" && arg != "on" && arg != "off")
                errors.Add(lid + ": rule " + rid + ": toggle arg '" + arg + "' must be on/off");
            if (op == "sfx" && string.IsNullOrEmpty(arg))
                errors.Add(lid + ": rule " + rid + ": sfx without arg");
        }

        private static bool Contains(string[] arr, string v)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == v) return true;
            }
            return false;
        }
    }
}
