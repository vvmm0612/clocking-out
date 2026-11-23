using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeathNotice.Core
{
    public static class LevelLoader
    {
        public const string ResourcesFolder = "Levels";

        /// <summary>Parses one level file. Does not touch Resources, so tests can feed it any string.</summary>
        public static LevelData FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Level JSON is empty.");
            LevelData level = JsonUtility.FromJson<LevelData>(json);
            if (level == null) throw new ArgumentException("Level JSON could not be parsed.");
            Normalize(level);
            return level;
        }

        /// <summary>Loads every TextAsset in Resources/Levels, sorted by asset name (level_01, level_02, ...).</summary>
        public static List<LevelData> LoadAllFromResources()
        {
            TextAsset[] assets = Resources.LoadAll<TextAsset>(ResourcesFolder);
            var sorted = new List<TextAsset>(assets);
            sorted.Sort(delegate (TextAsset a, TextAsset b) { return string.CompareOrdinal(a.name, b.name); });

            var levels = new List<LevelData>();
            for (int i = 0; i < sorted.Count; i++)
            {
                levels.Add(FromJson(sorted[i].text));
            }
            return levels;
        }

        /// <summary>Loads Resources/Levels/{levelId}, or returns null if missing.</summary>
        public static LevelData LoadFromResources(string levelId)
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcesFolder + "/" + levelId);
            return asset == null ? null : FromJson(asset.text);
        }

        /// <summary>Replaces nulls left by JsonUtility with empty values so callers never null-check.</summary>
        public static void Normalize(LevelData level)
        {
            if (level.id == null) level.id = "";
            if (level.title == null) level.title = "";
            if (level.deceased == null) level.deceased = "";
            if (level.wishText == null) level.wishText = "";
            if (level.intro == null) level.intro = "";
            if (level.outro == null) level.outro = "";
            if (level.defaultVerdict == null) level.defaultVerdict = "";
            if (level.fields == null) level.fields = new List<FieldData>();
            if (level.always == null) level.always = new List<string>();
            if (level.states == null) level.states = new List<StateData>();
            if (level.anchors == null) level.anchors = new List<string>();
            if (level.clues == null) level.clues = new List<ClueData>();
            if (level.rules == null) level.rules = new List<RuleData>();
            if (level.wish == null) level.wish = new WishData();
            if (level.wish.all == null) level.wish.all = new List<string>();
            if (level.wish.none == null) level.wish.none = new List<string>();
            if (level.solutions == null) level.solutions = new List<SolutionData>();

            for (int i = 0; i < level.fields.Count; i++)
            {
                FieldData f = level.fields[i];
                if (f.options == null) f.options = new List<OptionData>();
            }
            for (int i = 0; i < level.states.Count; i++)
            {
                if (level.states[i].facts == null) level.states[i].facts = new List<string>();
            }
            for (int i = 0; i < level.rules.Count; i++)
            {
                RuleData r = level.rules[i];
                if (r.id == null) r.id = "";
                if (r.when == null) r.when = new List<string>();
                if (r.unless == null) r.unless = new List<string>();
                if (r.add == null) r.add = new List<string>();
                if (r.remove == null) r.remove = new List<string>();
                if (r.verdict == null) r.verdict = "";
                if (r.caption == null) r.caption = "";
                if (r.beats == null) r.beats = new List<BeatData>();
                for (int b = 0; b < r.beats.Count; b++)
                {
                    BeatData beat = r.beats[b];
                    if (beat.@do == null) beat.@do = "";
                    if (beat.target == null) beat.target = "";
                    if (beat.arg == null) beat.arg = "";
                }
            }
            for (int i = 0; i < level.solutions.Count; i++)
            {
                if (level.solutions[i].choices == null) level.solutions[i].choices = new List<ChoiceData>();
            }
        }
    }
}
