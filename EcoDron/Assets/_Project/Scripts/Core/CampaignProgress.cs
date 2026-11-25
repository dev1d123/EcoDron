using EcoDron.Data;
using UnityEngine;

namespace EcoDron.Core
{
    public enum ScenarioState
    {
        Locked = 0,
        InProgress = 1,
        Completed = 2
    }

    /// <summary>
    /// Tracks active selection and persisted campaign progress across scenarios and levels.
    /// </summary>
    public static class CampaignProgress
    {
        private const string SelectedScenarioKey = "EcoDron_SelectedScenario";
        private const string SelectedLevelKey = "EcoDron_SelectedLevel";
        private const string StarsPrefix = "EcoDron_LevelStars_";

        public static string SelectedScenarioId
        {
            get => PlayerPrefs.GetString(SelectedScenarioKey, "scenario_01");
            set => PlayerPrefs.SetString(SelectedScenarioKey, value);
        }

        public static string SelectedLevelId
        {
            get => PlayerPrefs.GetString(SelectedLevelKey, "1.1");
            set => PlayerPrefs.SetString(SelectedLevelKey, value);
        }

        public static int GetLevelStars(string levelId)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(StarsPrefix + levelId, 0), 0, 3);
        }

        public static void SetLevelStars(string levelId, int stars)
        {
            int current = GetLevelStars(levelId);
            int clamped = Mathf.Clamp(stars, 0, 3);
            if (clamped > current)
            {
                PlayerPrefs.SetInt(StarsPrefix + levelId, clamped);
                PlayerPrefs.Save();
            }
        }

        public static int GetScenarioCompletedCount(ScenarioDataSO scenario)
        {
            if (scenario == null || scenario.levels == null) return 0;
            int completed = 0;
            for (int i = 0; i < scenario.levels.Count; i++)
            {
                if (scenario.levels[i] != null && GetLevelStars(scenario.levels[i].levelId) > 0)
                {
                    completed++;
                }
            }
            return completed;
        }

        public static int GetScenarioTotalStars(ScenarioDataSO scenario)
        {
            if (scenario == null || scenario.levels == null) return 0;
            int total = 0;
            for (int i = 0; i < scenario.levels.Count; i++)
            {
                if (scenario.levels[i] != null)
                {
                    total += GetLevelStars(scenario.levels[i].levelId);
                }
            }
            return total;
        }

        public static bool IsScenarioUnlocked(CampaignDatabaseSO database, int scenarioIndex)
        {
            if (database == null || scenarioIndex < 0 || scenarioIndex >= database.scenarios.Count) return false;
            ScenarioDataSO scenario = database.scenarios[scenarioIndex];
            if (scenario == null) return false;
            if (scenarioIndex == 0 || scenario.unlockedByDefault) return true;

            ScenarioDataSO previous = database.scenarios[scenarioIndex - 1];
            return previous != null && GetScenarioCompletedCount(previous) >= previous.levels.Count && previous.levels.Count > 0;
        }

        public static ScenarioState GetScenarioState(CampaignDatabaseSO database, int scenarioIndex)
        {
            if (!IsScenarioUnlocked(database, scenarioIndex)) return ScenarioState.Locked;

            ScenarioDataSO scenario = database.scenarios[scenarioIndex];
            int totalLevels = scenario.levels != null ? scenario.levels.Count : 0;
            int completed = GetScenarioCompletedCount(scenario);

            if (totalLevels > 0 && completed >= totalLevels) return ScenarioState.Completed;
            return ScenarioState.InProgress;
        }
    }
}
