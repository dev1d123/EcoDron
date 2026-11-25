#if UNITY_EDITOR
using EcoDron.Core;
using EcoDron.Data;
using EcoDron.UI.LevelRoute;
using EcoDron.UI.ScenarioSelect;
using UnityEditor;
using UnityEngine;

namespace EcoDron.EditorTools
{
    /// <summary>
    /// Editor toolbar utilities for inspecting, clearing, and resetting persistent SaveManager data.
    /// </summary>
    public static class SaveManagerTools
    {
        private const string DatabasePath = "Assets/_Project/Data/CampaignDatabase.asset";

        [MenuItem("EcoDron/Save/Vaciar y Reiniciar Guardado", false, 50)]
        public static void ResetSaveData()
        {
            SaveManager.ResetSave();

            if (Application.isPlaying)
            {
                ScenarioSelectController scenarioCtrl = Object.FindFirstObjectByType<ScenarioSelectController>();
                if (scenarioCtrl != null)
                {
                    scenarioCtrl.RefreshCards();
                }

                LevelRouteController routeCtrl = Object.FindFirstObjectByType<LevelRouteController>();
                if (routeCtrl != null)
                {
                    routeCtrl.RefreshRoute();
                }
            }

            Debug.Log($"<b><color=#FFD166>[SaveManagerTools]</color></b> Guardado reiniciado desde la barra de herramientas. Archivo: {SaveManager.SaveFilePath}");
        }

        [MenuItem("EcoDron/Save/Ver Estado del Guardado (Console)", false, 51)]
        public static void LogSaveData()
        {
            SaveManager.Load();
            Debug.Log($"<b><color=#00E5FF>[SaveManagerTools]</color></b> Ruta JSON: {SaveManager.SaveFilePath}\n{SaveManager.BuildCompactSummary()}");

            CampaignDatabaseSO db = AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath);
            if (db == null || db.scenarios == null) return;

            for (int i = 0; i < db.scenarios.Count; i++)
            {
                ScenarioDataSO scenario = db.scenarios[i];
                if (scenario == null) continue;

                ScenarioState state = CampaignProgress.GetScenarioState(db, i);
                int completed = CampaignProgress.GetScenarioCompletedCount(scenario);
                int total = scenario.levels != null ? scenario.levels.Count : 0;
                int stars = CampaignProgress.GetScenarioTotalStars(scenario);

                Debug.Log($"  • [{scenario.scenarioId}] {scenario.headerTitle} -> Estado: <b>{state}</b> | Niveles: {completed}/{total} | Estrellas: {stars}/{total * 3}");

                if (scenario.levels != null)
                {
                    for (int j = 0; j < scenario.levels.Count; j++)
                    {
                        LevelDataSO lvl = scenario.levels[j];
                        if (lvl == null) continue;
                        int lvlStars = CampaignProgress.GetLevelStars(lvl.levelId);
                        Debug.Log($"      - Nivel {lvl.levelId} ({lvl.displayName}) [{lvl.sceneName}]: {lvlStars}/3★");
                    }
                }
            }
        }

        [MenuItem("EcoDron/Save/Abrir Carpeta de Guardado", false, 52)]
        public static void RevealSaveFolder()
        {
            SaveManager.Load();
            EditorUtility.RevealInFinder(SaveManager.SaveFilePath);
        }
    }
}
#endif
