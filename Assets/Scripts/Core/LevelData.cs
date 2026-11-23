using System;
using System.Collections.Generic;

namespace DeathNotice.Core
{
    // Data model for one level file (Assets/Resources/Levels/level_XX.json).
    // Every class is a plain [Serializable] class with public fields so that
    // UnityEngine.JsonUtility can fill it. No properties, no dictionaries,
    // no top-level arrays. See docs/level-schema.md for the JSON layout.

    [Serializable]
    public class OptionData
    {
        public string id = "";
        public string label = "";
        /// <summary>Only listed in the dropdown while the player holds Shift.</summary>
        public bool shiftOnly;
    }

    [Serializable]
    public class FieldData
    {
        public string id = "";
        public string label = "";
        public List<OptionData> options = new List<OptionData>();

        public OptionData FindOption(string optionId)
        {
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].id == optionId) return options[i];
            }
            return null;
        }
    }

    [Serializable]
    public class StateData
    {
        /// <summary>Option id of the "shift" field this state belongs to.</summary>
        public string shift = "";
        /// <summary>Facts true during this shift. Facts starting with "show:" are
        /// visual-only (read by the stage for the Shift preview) and never used by rules.</summary>
        public List<string> facts = new List<string>();
    }

    [Serializable]
    public class ClueData
    {
        public string anchor = "";
        public string text = "";
        /// <summary>The clue only becomes visible after this many rejected submissions (0 = always).</summary>
        public int minRejects;
    }

    [Serializable]
    public class BeatData
    {
        // "do" is a C# keyword; the verbatim identifier keeps the serialized name "do".
        public string @do = "";
        /// <summary>Anchor name. "here" means the place the player picked
        /// (the simulator substitutes it in the events it returns).</summary>
        public string target = "";
        public string arg = "";

        public BeatData() { }

        public BeatData(string op, string target, string arg)
        {
            this.@do = op;
            this.target = target;
            this.arg = arg;
        }
    }

    [Serializable]
    public class RuleData
    {
        public string id = "";
        public List<string> when = new List<string>();
        public List<string> unless = new List<string>();
        public List<string> add = new List<string>();
        public List<string> remove = new List<string>();
        public bool reject;
        public string verdict = "";
        public string caption = "";
        public List<BeatData> beats = new List<BeatData>();
        /// <summary>Authoring flag: this rule is known never to fire. Ignored by the simulator.</summary>
        public bool unreachable;
    }

    [Serializable]
    public class WishData
    {
        public List<string> all = new List<string>();
        public List<string> none = new List<string>();
    }

    [Serializable]
    public class ChoiceData
    {
        public string field = "";
        public string option = "";
    }

    [Serializable]
    public class SolutionData
    {
        public List<ChoiceData> choices = new List<ChoiceData>();

        public Dictionary<string, string> ToDictionary()
        {
            var d = new Dictionary<string, string>();
            for (int i = 0; i < choices.Count; i++)
            {
                d[choices[i].field] = choices[i].option;
            }
            return d;
        }
    }

    [Serializable]
    public class LevelData
    {
        public string id = "";
        public string title = "";
        public string deceased = "";
        public string wishText = "";
        /// <summary>Line Death says before the level starts (may be empty).</summary>
        public string intro = "";
        /// <summary>Line shown after the level is approved (may be empty).</summary>
        public string outro = "";
        public List<FieldData> fields = new List<FieldData>();
        public List<string> always = new List<string>();
        public List<StateData> states = new List<StateData>();
        /// <summary>Every anchor name the stage must provide for this level.</summary>
        public List<string> anchors = new List<string>();
        public List<ClueData> clues = new List<ClueData>();
        public List<RuleData> rules = new List<RuleData>();
        public WishData wish = new WishData();
        public string defaultVerdict = "";
        public List<SolutionData> solutions = new List<SolutionData>();

        public FieldData FindField(string fieldId)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].id == fieldId) return fields[i];
            }
            return null;
        }

        /// <summary>State for the given shift option id, or null (also for levels without a shift field).</summary>
        public StateData FindState(string shiftId)
        {
            if (shiftId == null) return null;
            for (int i = 0; i < states.Count; i++)
            {
                if (states[i].shift == shiftId) return states[i];
            }
            return null;
        }

        /// <summary>"show:" facts of a shift with the prefix stripped, e.g. "baker@oven", "water_level=high".</summary>
        public List<string> GetShowFacts(string shiftId)
        {
            var result = new List<string>();
            StateData st = FindState(shiftId);
            if (st == null) return result;
            for (int i = 0; i < st.facts.Count; i++)
            {
                string f = st.facts[i];
                if (f != null && f.StartsWith(Simulator.ShowPrefix, StringComparison.Ordinal))
                {
                    result.Add(f.Substring(Simulator.ShowPrefix.Length));
                }
            }
            return result;
        }

        public bool HasShiftOnlyOptions()
        {
            for (int i = 0; i < fields.Count; i++)
            {
                for (int j = 0; j < fields[i].options.Count; j++)
                {
                    if (fields[i].options[j].shiftOnly) return true;
                }
            }
            return false;
        }
    }
}
