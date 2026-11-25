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
    /// Tracks active selection and persisted campaign progress across scenarios and levels via SaveManager.
    /// </summary>
    public static class CampaignProgress
    {
        public static string SelectedScenarioId
        {
            get => SaveManager.GetSelectedScenarioId();
            set => SaveManager.SetSelectedScenarioId(value);
        }

        public static string SelectedLevelId
        {
            get => SaveManager.GetSelectedLevelId();
            set => SaveManager.SetSelectedLevelId(value);
        }

        public static int GetLevelStars(string levelId)
        {
            return SaveManager.GetLevelStars(levelId);
        }

        public static void SetLevelStars(string levelId, int stars)
        {
            SaveManager.SetLevelStars(levelId, stars);
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
