using System.Collections.Generic;

namespace DeathNotice.Core
{
    /// <summary>One fired rule, in firing order. Beats have "here" already replaced by the chosen place.</summary>
    public class SimEvent
    {
        public string ruleId;
        public string caption;
        public List<BeatData> beats = new List<BeatData>();
        /// <summary>True when this event's rule rejected the form and ended the simulation.</summary>
        public bool isReject;
        /// <summary>Verdict set by this rule ("" if none).</summary>
        public string verdict;
    }

    public class SimResult
    {
        public bool approved;
        /// <summary>Rejection reason. Empty when approved.</summary>
        public string verdict = "";
        public List<SimEvent> events = new List<SimEvent>();
        /// <summary>Id of the reject rule that stopped the simulation, or null.</summary>
        public string rejectedBy;
        /// <summary>Facts at the end of the simulation (includes derived X@here facts).</summary>
        public HashSet<string> facts = new HashSet<string>();
    }
}
