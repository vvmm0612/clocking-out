using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Builds the diorama for a level: layout from <see cref="StageLayouts"/>, a placeholder node for any
    /// anchor the layout forgot, then optional authored models (see <see cref="ModelSwap"/> and
    /// docs/art-brief.md):
    ///   Resources/Models/&lt;levelId&gt;/&lt;anchor&gt;  one anchor's look (NPCs are anchors too)
    ///   Resources/Models/&lt;levelId&gt;/_set      all scenery that is not an anchor (tower, walls, plinth, trees)
    ///   Resources/Models/common/player       the protagonist (the Shift ghost uses it half-transparent)
    ///   Resources/Models/common/death        the protagonist when the deceased is DEATH (case 6)
    /// </summary>
    public static class StageBuilder
    {
        public const string ModelsFolder = "Models";
        public const string SetName = "_set";
        public const string CommonFolder = "common";
        public const string PlayerModel = "player";
        public const string DeathModel = "death";

        /// <summary>Where models come from. Tests swap in fakes; null result = keep the greybox.</summary>
        public static Func<string, GameObject> ModelLoader = DefaultLoader;

        public static GameObject DefaultLoader(string resourcePath)
        {
            try { return Resources.Load<GameObject>(resourcePath); }
            catch (Exception) { return null; }
        }

        private static GameObject LoadModel(string path)
        {
            Func<string, GameObject> loader = ModelLoader ?? DefaultLoader;
            try { return loader(ModelsFolder + "/" + path); }
            catch (Exception e) { Debug.LogWarning("[StageBuilder] Model loader failed for " + path + ": " + e.Message); return null; }
        }

        public static Stage Build(LevelData level, Transform parent)
        {
            return Build(level, parent, true);
        }

        /// <param name="useModels">False builds the pure greybox (GreyboxExport).</param>
        public static Stage Build(LevelData level, Transform parent, bool useModels)
        {
            var go = new GameObject("Stage_" + (level != null ? level.id : "none"));
            go.transform.SetParent(parent, false);
            Stage stage = go.AddComponent<Stage>();
            stage.level = level;
            stage.palette = StageLayouts.PaletteFor(level != null ? level.id : "");
            var kit = new StageKit(stage);

            bool known = false;
            try
            {
                known = StageLayouts.Build(level != null ? level.id : "", kit);
            }
            catch (Exception e)
            {
                Debug.LogError("[StageBuilder] Layout for " + (level != null ? level.id : "?") + " failed: " + e);
            }
            if (!known)
            {
                try { StageLayouts.Generic(kit, level != null ? level.anchors : new List<string>()); }
                catch (Exception e) { Debug.LogError("[StageBuilder] Generic layout failed: " + e); }
            }

            if (level != null)
            {
                EnsureAnchors(level, kit);
                if (useModels) ApplyModelOverrides(level, stage);
            }

            GameObject playerModel = null;
            bool isDeath = level != null && string.Equals(level.deceased, "DEATH", StringComparison.OrdinalIgnoreCase);
            if (useModels)
            {
                if (isDeath) playerModel = LoadModel(CommonFolder + "/" + DeathModel);
                if (playerModel == null) playerModel = LoadModel(CommonFolder + "/" + PlayerModel);
            }
            stage.player = Figure.Create(go.transform, "Player", stage.palette.player, 0.7f, false, null, playerModel, isDeath ? FigureStyle.Reaper : FigureStyle.Player);
            stage.player.gameObject.SetActive(false);
            stage.ghost = Figure.Create(go.transform, "Ghost", new Color(0.82f, 0.86f, 1f, 0.42f), 0.7f, true, null, playerModel, FigureStyle.Ghost);
            stage.ghost.gameObject.SetActive(false);
            if (!Mathf.Approximately(stage.figureScale, 1f))
            {
                stage.player.transform.localScale = Vector3.one * stage.figureScale;
                stage.ghost.transform.localScale = Vector3.one * stage.figureScale;
            }
            stage.InitShowables();
            stage.ComputeFitBounds();
            return stage;
        }

        /// <summary>Every anchor named in the JSON gets a node, even if the layout does not know it.</summary>
        private static void EnsureAnchors(LevelData level, StageKit kit)
        {
            var missing = new List<string>();
            var wanted = new List<string>(level.anchors);
            // Places and clue anchors must exist too, even if the author forgot to list them.
            FieldData place = level.FindField(Simulator.PlaceField);
            if (place != null)
            {
                for (int i = 0; i < place.options.Count; i++) wanted.Add(place.options[i].id);
            }
            for (int i = 0; i < level.clues.Count; i++) wanted.Add(level.clues[i].anchor);

            for (int i = 0; i < wanted.Count; i++)
            {
                string id = wanted[i];
                if (string.IsNullOrEmpty(id) || kit.stage.Has(id) || missing.Contains(id)) continue;
                missing.Add(id);
            }
            for (int i = 0; i < missing.Count; i++)
            {
                float ang = (i / (float)Mathf.Max(1, missing.Count)) * Mathf.PI * 2f;
                Vector3 pos = new Vector3(Mathf.Cos(ang) * 3f, 0f, 0.5f + Mathf.Sin(ang) * 2f);
                AnchorTag t = kit.Anchor(missing[i], pos);
                kit.Box(t.transform, pos + Vector3.up * 0.3f, new Vector3(0.6f, 0.6f, 0.6f), kit.pal.accent);
                Debug.LogWarning("[StageBuilder] " + level.id + ": anchor '" + missing[i] + "' has no layout; placed a placeholder block.");
            }
        }

        /// <summary>Which behaviours each anchor has, from the layout and from the level's beats.</summary>
        public static ModelSwap.Uses UsesOf(LevelData level, AnchorTag t)
        {
            var u = new ModelSwap.Uses();
            u.spin = t.spins.Count > 0;
            u.toggle = t.onToggle != null || t.toggleMotion != null;
            u.light = t.onLight != null;
            if (level == null) return u;
            for (int r = 0; r < level.rules.Count; r++)
            {
                List<BeatData> beats = level.rules[r].beats;
                for (int b = 0; b < beats.Count; b++)
                {
                    if (beats[b] == null || beats[b].target != t.id) continue;
                    if (beats[b].@do == "rotate") u.spin = true;
                    else if (beats[b].@do == "toggle") u.toggle = true;
                    else if (beats[b].@do == "light") u.light = true;
                }
            }
            return u;
        }

        private static void ApplyModelOverrides(LevelData level, Stage stage)
        {
            GameObject set = LoadModel(level.id + "/" + SetName);
            if (set != null) ModelSwap.ApplySet(stage, set);
            foreach (AnchorTag t in new List<AnchorTag>(stage.AllAnchors))
            {
                GameObject prefab = LoadModel(level.id + "/" + t.id);
                if (prefab == null) continue;
                try { ModelSwap.ApplyAnchor(t, prefab, UsesOf(level, t)); }
                catch (Exception e) { Debug.LogError("[StageBuilder] Model for " + level.id + "/" + t.id + " failed: " + e); }
            }
        }
    }
}
