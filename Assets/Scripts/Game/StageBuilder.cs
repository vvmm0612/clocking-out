using System;
using System.Collections.Generic;
using DeathNotice.Core;
using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>
    /// Builds the diorama for a level: layout from <see cref="StageLayouts"/>, a placeholder node for any
    /// anchor the layout forgot, optional model replacement from Resources/Models/&lt;levelId&gt;/&lt;anchor&gt;,
    /// then the protagonist and the Shift ghost.
    /// </summary>
    public static class StageBuilder
    {
        public const string ModelsFolder = "Models";

        public static Stage Build(LevelData level, Transform parent)
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
                ApplyModelOverrides(level, stage);
            }

            stage.player = Figure.Create(go.transform, "Player", stage.palette.player, 0.7f, false, null);
            stage.player.gameObject.SetActive(false);
            stage.ghost = Figure.Create(go.transform, "Ghost", new Color(0.82f, 0.86f, 1f, 0.45f), 0.7f, true, null);
            stage.ghost.gameObject.SetActive(false);
            stage.InitShowables();
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

        /// <summary>Replacement hook: Resources/Models/&lt;levelId&gt;/&lt;anchor&gt; (prefab or model) replaces the greybox visuals.</summary>
        private static void ApplyModelOverrides(LevelData level, Stage stage)
        {
            foreach (AnchorTag t in new List<AnchorTag>(stage.AllAnchors))
            {
                GameObject prefab = null;
                try { prefab = Resources.Load<GameObject>(ModelsFolder + "/" + level.id + "/" + t.id); }
                catch (Exception) { prefab = null; }
                if (prefab == null) continue;

                for (int i = t.transform.childCount - 1; i >= 0; i--)
                {
                    GameObject child = t.transform.GetChild(i).gameObject;
                    child.SetActive(false);
                    UnityEngine.Object.Destroy(child);
                }
                Collider own = t.GetComponent<Collider>();
                if (own != null) UnityEngine.Object.Destroy(own);
                t.spins.Clear();
                t.onToggle = null;
                t.onLight = null;

                GameObject inst = UnityEngine.Object.Instantiate(prefab, t.transform);
                inst.name = "model";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                if (inst.GetComponentInChildren<Collider>() == null)
                {
                    MeshRenderer[] rs = inst.GetComponentsInChildren<MeshRenderer>();
                    for (int i = 0; i < rs.Length; i++) rs[i].gameObject.AddComponent<BoxCollider>();
                }
                Debug.Log("[StageBuilder] Replaced greybox '" + t.id + "' with model " + prefab.name);
            }
        }
    }
}
