using System.Collections.Generic;
using DeathNotice.Core;
using NUnit.Framework;

namespace DeathNotice.Tests
{
    public class SimulatorTests
    {
        // ---------- helpers

        private static List<string> L(params string[] items)
        {
            return new List<string>(items);
        }

        private static RuleData Rule(string id, List<string> when)
        {
            var r = new RuleData();
            r.id = id;
            r.when = when;
            r.caption = id;
            return r;
        }

        private static FieldData Field(string id, params string[] options)
        {
            var f = new FieldData();
            f.id = id;
            f.label = id;
            for (int i = 0; i < options.Length; i++)
            {
                var o = new OptionData();
                o.id = options[i];
                o.label = options[i];
                f.options.Add(o);
            }
            return f;
        }

        private static LevelData Level()
        {
            var level = new LevelData();
            level.id = "test";
            level.title = "Test";
            level.fields.Add(Field("cause", "burn", "drown"));
            level.fields.Add(Field("place", "a", "b"));
            level.fields.Add(Field("shift", "day", "night"));
            level.defaultVerdict = "default";
            level.wish.all = L("goal");
            return level;
        }

        private static Dictionary<string, string> Choose(string cause, string place, string shift)
        {
            var d = new Dictionary<string, string>();
            d["cause"] = cause;
            d["place"] = place;
            if (shift != null) d["shift"] = shift;
            return d;
        }

        // ---------- facts

        [Test]
        public void BuildFacts_IncludesChoicesAlwaysAndChosenShiftOnly()
        {
            LevelData level = Level();
            level.always = L("x");
            var day = new StateData(); day.shift = "day"; day.facts = L("sunny");
            var night = new StateData(); night.shift = "night"; night.facts = L("dark");
            level.states.Add(day);
            level.states.Add(night);

            HashSet<string> facts = Simulator.BuildFacts(level, Choose("burn", "a", "night"));
            Assert.IsTrue(facts.Contains("cause:burn"));
            Assert.IsTrue(facts.Contains("place:a"));
            Assert.IsTrue(facts.Contains("shift:night"));
            Assert.IsTrue(facts.Contains("x"));
            Assert.IsTrue(facts.Contains("dark"));
            Assert.IsFalse(facts.Contains("sunny"));
        }

        [Test]
        public void BuildFacts_DerivesHereFactsForChosenPlaceOnly()
        {
            LevelData level = Level();
            level.always = L("fire@a", "water@b");
            var night = new StateData(); night.shift = "night"; night.facts = L("witness@a");
            level.states.Add(night);

            HashSet<string> atA = Simulator.BuildFacts(level, Choose("burn", "a", "night"));
            Assert.IsTrue(atA.Contains("fire@here"));
            Assert.IsTrue(atA.Contains("witness@here"), "facts from states must also derive @here");
            Assert.IsFalse(atA.Contains("water@here"));

            HashSet<string> atB = Simulator.BuildFacts(level, Choose("burn", "b", "night"));
            Assert.IsFalse(atB.Contains("fire@here"));
            Assert.IsTrue(atB.Contains("water@here"));
        }

        [Test]
        public void Simulate_LevelWithoutShiftField_UsesAlwaysOnly()
        {
            LevelData level = Level();
            level.fields.RemoveAt(2);
            level.always = L("fire@a");
            RuleData r = Rule("win", L("cause:burn", "fire@here"));
            r.add = L("goal");
            level.rules.Add(r);

            Assert.IsTrue(Simulator.Simulate(level, Choose("burn", "a", null)).approved);
            Assert.IsFalse(Simulator.Simulate(level, Choose("burn", "b", null)).approved);
        }

        // ---------- rule semantics

        [Test]
        public void Simulate_UnlessBlocksRule()
        {
            LevelData level = Level();
            level.always = L("blocker");
            RuleData r = Rule("win", L("cause:burn"));
            r.unless = L("blocker");
            r.add = L("goal");
            level.rules.Add(r);

            SimResult res = Simulator.Simulate(level, Choose("burn", "a", "day"));
            Assert.IsFalse(res.approved);
            Assert.AreEqual(0, res.events.Count);
            Assert.AreEqual("default", res.verdict, "no rule set a verdict, so defaultVerdict is used");
        }

        [Test]
        public void Simulate_AddedFactsEnableLaterRules_InFileOrder()
        {
            LevelData level = Level();
            RuleData second = Rule("second", L("step1"));
            second.add = L("goal");
            RuleData first = Rule("first", L("cause:burn"));
            first.add = L("step1");

            // "second" before "first": single pass, so it must NOT fire.
            level.rules.Add(second);
            level.rules.Add(first);
            Assert.IsFalse(Simulator.Simulate(level, Choose("burn", "a", "day")).approved);

            level.rules.Clear();
            level.rules.Add(first);
            level.rules.Add(second);
            SimResult res = Simulator.Simulate(level, Choose("burn", "a", "day"));
            Assert.IsTrue(res.approved);
            Assert.AreEqual(2, res.events.Count);
            Assert.AreEqual("first", res.events[0].ruleId);
            Assert.AreEqual("second", res.events[1].ruleId);
            Assert.AreEqual("", res.verdict);
        }

        [Test]
        public void Simulate_RemoveTakesFactAway_AndBlocksLaterRules()
        {
            LevelData level = Level();
            RuleData put = Rule("put", L("cause:burn"));
            put.add = L("body");
            RuleData pull = Rule("pull", L("body", "rescuer"));
            pull.remove = L("body");
            pull.verdict = "pulled out";
            RuleData jam = Rule("jam", L("body"));
            jam.add = L("goal");
            level.rules.Add(put);
            level.rules.Add(pull);
            level.rules.Add(jam);

            level.always = L("rescuer");
            SimResult res = Simulator.Simulate(level, Choose("burn", "a", "day"));
            Assert.IsFalse(res.approved);
            Assert.IsFalse(res.facts.Contains("body"));
            Assert.AreEqual("pulled out", res.verdict);
            Assert.AreEqual(2, res.events.Count);

            level.always = L();
            Assert.IsTrue(Simulator.Simulate(level, Choose("burn", "a", "day")).approved);
        }

        [Test]
        public void Simulate_RejectStopsImmediately_EvenIfWishWouldBeMet()
        {
            LevelData level = Level();
            RuleData win = Rule("win", L("cause:burn"));
            win.add = L("goal");
            RuleData no = Rule("no", L("cause:burn"));
            no.reject = true;
            no.verdict = "rejected!";
            RuleData after = Rule("after", L("cause:burn"));
            after.verdict = "never seen";
            level.rules.Add(win);
            level.rules.Add(no);
            level.rules.Add(after);

            SimResult res = Simulator.Simulate(level, Choose("burn", "a", "day"));
            Assert.IsFalse(res.approved);
            Assert.AreEqual("rejected!", res.verdict);
            Assert.AreEqual("no", res.rejectedBy);
            Assert.AreEqual(2, res.events.Count);
            Assert.IsTrue(res.events[1].isReject);
        }

        [Test]
        public void Simulate_VerdictIsTheLastOneRecorded()
        {
            LevelData level = Level();
            RuleData a = Rule("a", L("cause:drown"));
            a.verdict = "first";
            RuleData b = Rule("b", L("cause:drown"));
            b.verdict = "second";
            RuleData c = Rule("c", L("cause:drown"));   // no verdict: must not clear the previous one
            level.rules.Add(a);
            level.rules.Add(b);
            level.rules.Add(c);

            SimResult res = Simulator.Simulate(level, Choose("drown", "a", "day"));
            Assert.IsFalse(res.approved);
            Assert.AreEqual("second", res.verdict);
            Assert.AreEqual(3, res.events.Count);
        }

        [Test]
        public void Simulate_WishNoneBlocksApproval()
        {
            LevelData level = Level();
            level.wish.none = L("bad");
            RuleData r = Rule("win", L("cause:burn"));
            r.add = L("goal", "bad");
            r.verdict = "too much";
            level.rules.Add(r);

            SimResult res = Simulator.Simulate(level, Choose("burn", "a", "day"));
            Assert.IsFalse(res.approved);
            Assert.AreEqual("too much", res.verdict);
        }

        [Test]
        public void Simulate_ResolvesHereInBeats_WithoutTouchingTheLevel()
        {
            LevelData level = Level();
            RuleData r = Rule("r", L("cause:burn"));
            r.beats.Add(new BeatData("ghost_to", "here", ""));
            r.beats.Add(new BeatData("npc_to", "baker", "here"));
            r.beats.Add(new BeatData("fx", "lamp", "fire"));
            level.rules.Add(r);

            SimResult res = Simulator.Simulate(level, Choose("burn", "b", "day"));
            List<BeatData> beats = res.events[0].beats;
            Assert.AreEqual("b", beats[0].target);
            Assert.AreEqual("baker", beats[1].target);
            Assert.AreEqual("b", beats[1].arg);
            Assert.AreEqual("lamp", beats[2].target);
            Assert.AreEqual("here", r.beats[0].target, "level data must not be mutated");
        }

        [Test]
        public void Simulate_IsDeterministic()
        {
            LevelData level = Level();
            level.always = L("fire@a");
            RuleData r = Rule("win", L("fire@here"));
            r.add = L("goal");
            level.rules.Add(r);

            List<Dictionary<string, string>> combos = Simulator.EnumerateCombos(level);
            Assert.AreEqual(8, combos.Count);
            for (int i = 0; i < combos.Count; i++)
            {
                SimResult x = Simulator.Simulate(level, combos[i]);
                SimResult y = Simulator.Simulate(level, combos[i]);
                Assert.AreEqual(x.approved, y.approved);
                Assert.AreEqual(x.verdict, y.verdict);
                Assert.AreEqual(x.events.Count, y.events.Count);
            }
        }

        // ---------- JSON

        [Test]
        public void FromJson_ParsesArraysBeatsAndShiftOnly_AndFillsMissingLists()
        {
            string json =
                "{\"id\":\"j\",\"title\":\"J\",\"deceased\":\"D\",\"wishText\":\"W\"," +
                "\"fields\":[{\"id\":\"cause\",\"label\":\"Cause\",\"options\":[{\"id\":\"retire\",\"label\":\"End\",\"shiftOnly\":true}]}," +
                "{\"id\":\"place\",\"label\":\"Place\",\"options\":[{\"id\":\"desk\",\"label\":\"Desk\"}]}]," +
                "\"states\":[{\"shift\":\"day\",\"facts\":[\"show:baker@desk\"]}]," +
                "\"rules\":[{\"id\":\"r\",\"when\":[\"cause:retire\"],\"add\":[\"goal\"],\"caption\":\"c\"," +
                "\"beats\":[{\"do\":\"ghost_to\",\"target\":\"here\"}]}]," +
                "\"wish\":{\"all\":[\"goal\"]}," +
                "\"solutions\":[{\"choices\":[{\"field\":\"cause\",\"option\":\"retire\"},{\"field\":\"place\",\"option\":\"desk\"}]}]}";

            LevelData level = LevelLoader.FromJson(json);
            Assert.AreEqual("j", level.id);
            Assert.IsTrue(level.fields[0].options[0].shiftOnly);
            Assert.IsFalse(level.fields[1].options[0].shiftOnly);
            Assert.AreEqual("ghost_to", level.rules[0].beats[0].@do);
            Assert.AreEqual("", level.rules[0].beats[0].arg);
            Assert.IsNotNull(level.rules[0].unless);
            Assert.IsNotNull(level.rules[0].remove);
            Assert.IsNotNull(level.always);
            Assert.IsNotNull(level.wish.none);
            Assert.AreEqual(1, level.GetShowFacts("day").Count);
            Assert.AreEqual("baker@desk", level.GetShowFacts("day")[0]);
            Assert.AreEqual("desk", level.solutions[0].ToDictionary()["place"]);

            SimResult res = Simulator.Simulate(level, level.solutions[0].ToDictionary());
            Assert.IsTrue(res.approved);
            Assert.AreEqual("desk", res.events[0].beats[0].target);
        }
    }
}
