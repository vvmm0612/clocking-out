using System.Collections.Generic;
using DeathNotice.Core;
using DeathNotice.Game;
using NUnit.Framework;
using UnityEngine;

namespace DeathNotice.Tests.EditMode
{
    /// <summary>Builds every level's diorama and checks that everything the JSON refers to exists on stage.</summary>
    public class StageTests
    {
        private static IEnumerable<string> LevelIds()
        {
            foreach (LevelData l in LevelLoader.LoadAllFromResources()) yield return l.id;
        }

        [TestCaseSource(nameof(LevelIds))]
        public void StageProvidesEverythingTheLevelNeeds(string levelId)
        {
            LevelData level = LevelLoader.LoadFromResources(levelId);
            var warnings = new List<string>();
            Application.LogCallback cb = delegate (string msg, string stack, LogType type)
            {
                if (type != LogType.Log) warnings.Add(type + ": " + msg);
            };
            Application.logMessageReceived += cb;
            Stage stage = null;
            try
            {
                stage = StageBuilder.Build(level, null);

                var problems = new List<string>();
                var wanted = new List<string>(level.anchors);
                foreach (ClueData c in level.clues) wanted.Add(c.anchor);
                foreach (RuleData r in level.rules)
                {
                    foreach (BeatData b in r.beats)
                    {
                        if (!string.IsNullOrEmpty(b.target) && b.target != Simulator.Here) wanted.Add(b.target);
                        if ((b.@do == "move" || b.@do == "npc_to") && b.arg != Simulator.Here) wanted.Add(b.arg);
                    }
                }
                foreach (string id in wanted)
                {
                    if (!stage.Has(id)) problems.Add("missing anchor " + id);
                }

                foreach (StateData st in level.states) stage.ApplyShow(st.shift);
                stage.ResetShow();

                foreach (string w in warnings) problems.Add(w);
                Assert.IsEmpty(problems, levelId + ":\n" + string.Join("\n", problems.ToArray()));
            }
            finally
            {
                Application.logMessageReceived -= cb;
                if (stage != null) Object.DestroyImmediate(stage.gameObject);
            }
        }
    }
}
