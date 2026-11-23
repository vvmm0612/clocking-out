using System.Collections.Generic;
using System.IO;
using DeathNotice.Core;
using NUnit.Framework;
using UnityEngine;

namespace DeathNotice.Tests
{
    /// <summary>
    /// EditMode equivalent of tools/validate_levels.py: reads the shipped level
    /// files from disk and runs the full-combination checks on each.
    /// </summary>
    public class LevelFileTests
    {
        private static string LevelPath(string levelId)
        {
            return Path.Combine(Path.Combine(Path.Combine(Application.dataPath, "Resources"), "Levels"), levelId + ".json");
        }

        private static LevelData Load(string levelId)
        {
            string path = LevelPath(levelId);
            Assert.IsTrue(File.Exists(path), "Missing level file: " + path);
            return LevelLoader.FromJson(File.ReadAllText(path));
        }

        [TestCase("level_01", 9, 1)]
        [TestCase("level_02", 27, 1)]
        [TestCase("level_03", 27, 1)]
        [TestCase("level_04", 27, 1)]
        [TestCase("level_05", 48, 1)]
        [TestCase("level_06", 36, 9)]
        public void Level_PassesFullCombinationCheck(string levelId, int expectedCombos, int expectedSolutions)
        {
            LevelData level = Load(levelId);
            Assert.AreEqual(levelId, level.id, "id inside the file does not match its name");
            Assert.AreEqual(expectedCombos, Simulator.EnumerateCombos(level).Count, "combination count");
            Assert.AreEqual(expectedSolutions, level.solutions.Count, "solution count");

            List<string> errors = LevelValidator.Validate(level);
            Assert.IsEmpty(errors, string.Join("\n", errors.ToArray()));
        }

        [TestCase("level_01")]
        [TestCase("level_02")]
        [TestCase("level_03")]
        [TestCase("level_04")]
        [TestCase("level_05")]
        [TestCase("level_06")]
        public void Level_EverySolutionIsApproved_AndEveryOtherComboHasAReason(string levelId)
        {
            // Redundant with the validator on purpose: a direct, readable statement of the core contract.
            LevelData level = Load(levelId);
            var solutionKeys = new HashSet<string>();
            for (int i = 0; i < level.solutions.Count; i++)
            {
                solutionKeys.Add(Simulator.ComboKey(level, level.solutions[i].ToDictionary()));
            }

            List<Dictionary<string, string>> combos = Simulator.EnumerateCombos(level);
            for (int i = 0; i < combos.Count; i++)
            {
                string key = Simulator.ComboKey(level, combos[i]);
                SimResult res = Simulator.Simulate(level, combos[i]);
                Assert.AreEqual(solutionKeys.Contains(key), res.approved, key);
                if (!res.approved)
                {
                    Assert.IsFalse(string.IsNullOrEmpty(res.verdict), key + " is rejected without a reason");
                }
            }
        }

        [Test]
        public void Level04_DivertedRule_IsMarkedUnreachable()
        {
            LevelData level = Load("level_04");
            RuleData diverted = null;
            for (int i = 0; i < level.rules.Count; i++)
            {
                if (level.rules[i].id == "diverted") diverted = level.rules[i];
            }
            Assert.IsNotNull(diverted);
            Assert.IsTrue(diverted.unreachable);
        }

        [Test]
        public void Level06_RetireOption_IsShiftOnly()
        {
            LevelData level = Load("level_06");
            OptionData retire = level.FindField("cause").FindOption("retire");
            Assert.IsNotNull(retire);
            Assert.IsTrue(retire.shiftOnly);
            Assert.IsFalse(level.FindField("cause").FindOption("burn").shiftOnly);
        }

        [Test]
        public void Level01_ParsesBeatOpField()
        {
            // Guards the "do" JSON key, which maps to the C# field @do.
            LevelData level = Load("level_01");
            Assert.AreEqual("ghost_to", level.rules[0].beats[0].@do);
            Assert.AreEqual("here", level.rules[0].beats[0].target);
        }

        [Test]
        public void Resources_LoadAll_FindsSixLevelsInOrder()
        {
            List<LevelData> levels = LevelLoader.LoadAllFromResources();
            Assert.AreEqual(6, levels.Count);
            for (int i = 0; i < levels.Count; i++)
            {
                Assert.AreEqual("level_0" + (i + 1), levels[i].id);
            }
        }
    }
}
