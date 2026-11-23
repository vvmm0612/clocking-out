using UnityEngine;

namespace DeathNotice.Game
{
    /// <summary>Save data in PlayerPrefs: how many levels are cleared and the best submission count per level.</summary>
    public static class Progress
    {
        private const string KeyCleared = "co_cleared";
        private const string KeyBestPrefix = "co_best_";

        /// <summary>Number of levels cleared in order (0 = none). Level index i is unlocked when i &lt;= Cleared.</summary>
        public static int Cleared
        {
            get { return Mathf.Max(0, PlayerPrefs.GetInt(KeyCleared, 0)); }
        }

        public static bool HasSave
        {
            get { return Cleared > 0; }
        }

        /// <summary>Best (lowest) submission count for a level, or 0 if never approved.</summary>
        public static int GetBest(string levelId)
        {
            return PlayerPrefs.GetInt(KeyBestPrefix + levelId, 0);
        }

        public static void RecordApproved(int levelIndex, string levelId, int submissions)
        {
            if (levelIndex + 1 > Cleared) PlayerPrefs.SetInt(KeyCleared, levelIndex + 1);
            int best = GetBest(levelId);
            if (best <= 0 || submissions < best) PlayerPrefs.SetInt(KeyBestPrefix + levelId, submissions);
            PlayerPrefs.Save();
        }

        public static void ResetAll(System.Collections.Generic.IList<string> levelIds)
        {
            PlayerPrefs.DeleteKey(KeyCleared);
            if (levelIds != null)
            {
                for (int i = 0; i < levelIds.Count; i++) PlayerPrefs.DeleteKey(KeyBestPrefix + levelIds[i]);
            }
            PlayerPrefs.Save();
        }

        /// <summary>Seal for a submission count: 1 = Gold, 2-3 = Silver, more = Paper.</summary>
        public static string SealName(int submissions)
        {
            if (submissions <= 1) return "GOLD";
            if (submissions <= 3) return "SILVER";
            return "PAPER";
        }

        public static string SealMotto(int submissions)
        {
            if (submissions <= 1) return "Filed without correction";
            if (submissions <= 3) return "Filed with minor corrections";
            return "Filed, eventually";
        }

        public static Color SealColor(int submissions)
        {
            if (submissions <= 1) return new Color(0.86f, 0.68f, 0.22f, 1f);
            if (submissions <= 3) return new Color(0.72f, 0.74f, 0.78f, 1f);
            return new Color(0.80f, 0.74f, 0.62f, 1f);
        }
    }
}
