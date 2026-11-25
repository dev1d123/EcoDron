#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using EcoDron.Core;
using EcoDron.Data;
using EcoDron.Drone;
using EcoDron.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoDron.EditorTools
{
    /// <summary>
    /// Builds all campaign level scenes with the EcoDron, 2.5D camera, and LevelSessionController,
    /// and registers them in Build Settings.
    /// </summary>
    public static class LevelScenesBuilder
    {
        private const string DatabasePath = "Assets/_Project/Data/CampaignDatabase.asset";
        private const string DroneModelPath = "Assets/_Project/Models/Dron/EcoDron.fbx";
        private const string ScenariosRoot = "Assets/_Project/Scenes/Scenarios";

        private static readonly Dictionary<string, string> ScenarioFolderNames = new Dictionary<string, string>
        {
            { "scenario_01", "Scenario_01_Colegio" },
            { "scenario_02", "Scenario_02_Jardines" },
            { "scenario_03", "Scenario_03_Rio" }
        };

        [MenuItem("EcoDron/5. Build All Campaign Level Scenes", false, 20)]
        public static void BuildAllLevelScenes()
        {
            CampaignDatabaseSO database = AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath);
            if (database == null || database.scenarios == null || database.scenarios.Count == 0)
            {
                Debug.LogError($"[LevelScenesBuilder] No se encontró CampaignDatabase en '{DatabasePath}'.");
                return;
            }

            GameObject droneModel = AssetDatabase.LoadAssetAtPath<GameObject>(DroneModelPath);
            if (droneModel == null)
            {
                Debug.LogError($"[LevelScenesBuilder] No se encontró el modelo del dron en '{DroneModelPath}'.");
                return;
            }

            EnsureFolder(ScenariosRoot);

            var levelSpecs = new List<(string scenePath, string scenarioId, string levelId)>();

            for (int s = 0; s < database.scenarios.Count; s++)
            {
                ScenarioDataSO scenario = database.scenarios[s];
                if (scenario == null || scenario.levels == null) continue;

                string folderName = ScenarioFolderNames.TryGetValue(scenario.scenarioId, out string mapped)
                    ? mapped
                    : $"Scenario_{scenario.scenarioNumber:00}";

                string scenarioFolder = $"{ScenariosRoot}/{folderName}";
                EnsureFolder(scenarioFolder);

                for (int l = 0; l < scenario.levels.Count; l++)
                {
                    LevelDataSO level = scenario.levels[l];
                    if (level == null || string.IsNullOrEmpty(level.sceneName)) continue;

                    string scenePath = $"{scenarioFolder}/{level.sceneName}.unity";
                    levelSpecs.Add((scenePath, scenario.scenarioId, level.levelId));
                }
            }

            var builtPaths = new List<string>(levelSpecs.Count);
            for (int i = 0; i < levelSpecs.Count; i++)
            {
                var spec = levelSpecs[i];
                if (spec.levelId == "1.1")
                {
                    Level1GreyboxBuilder.BuildLevel1GreyboxScene();
                }
                else
                {
                    BuildSingleLevelScene(spec.scenarioId, spec.levelId, spec.scenePath);
                }
                builtPaths.Add(spec.scenePath);
            }

            RegisterScenesInBuildSettings(builtPaths);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<b><color=#00FF7F>[LevelScenesBuilder]</color></b> Se crearon y registraron {builtPaths.Count} escenas de nivel:\n - {string.Join("\n - ", builtPaths)}");
        }

        private static void BuildSingleLevelScene(
            string scenarioId,
            string levelId,
            string scenePath)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            CampaignDatabaseSO database = AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath);
            GameObject droneModel = AssetDatabase.LoadAssetAtPath<GameObject>(DroneModelPath);

            // Spawn EcoDron matching TestSceneBuilder setup
            GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(droneModel, scene);
            drone.name = "EcoDron";
            drone.transform.position = new Vector3(0f, 1.0f, 0f);
            drone.transform.rotation = Quaternion.Euler(270f, 0f, 0f);

            Rigidbody rb = drone.GetComponent<Rigidbody>();
            if (rb == null) rb = drone.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            BoxCollider col = drone.GetComponent<BoxCollider>();
            if (col == null) col = drone.AddComponent<BoxCollider>();
            col.center = Vector3.zero;
            col.size = new Vector3(2.0f, 2.0f, 0.6f);

            DroneController droneController = drone.GetComponent<DroneController>();
            if (droneController == null) droneController = drone.AddComponent<DroneController>();
            droneController.SetLockZ(true, 0f);

            DroneHook droneHook = drone.GetComponent<DroneHook>();
            if (droneHook == null) droneHook = drone.AddComponent<DroneHook>();

            // Configure Main Camera with Camera25D
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                GameObject camObj = mainCam.gameObject;
                camObj.transform.position = new Vector3(0f, 2.8f, -8f);
                camObj.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

                Camera25D cam25D = camObj.GetComponent<Camera25D>();
                if (cam25D == null) cam25D = camObj.AddComponent<Camera25D>();
                cam25D.SetTarget(drone.transform);
                cam25D.SetFollow(true);
                cam25D.SetSmoothTime(0.25f);
            }

            // Configure LevelSessionController
            GameObject sessionGo = new GameObject("[LevelSession]");
            LevelSessionController sessionCtrl = sessionGo.AddComponent<LevelSessionController>();

            SerializedObject sessionSo = new SerializedObject(sessionCtrl);
            sessionSo.FindProperty("campaignDatabase").objectReferenceValue = database;
            sessionSo.FindProperty("scenarioId").stringValue = scenarioId;
            sessionSo.FindProperty("levelId").stringValue = levelId;
            sessionSo.FindProperty("scenarioSelectSceneName").stringValue = "02_ScenarioSelect";
            sessionSo.FindProperty("levelRouteSceneName").stringValue = "03_LevelRoute";
            sessionSo.FindProperty("showDebugOverlay").boolValue = true;
            sessionSo.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, scenePath);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
            {
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }

        private static void RegisterScenesInBuildSettings(List<string> newScenePaths)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            for (int i = 0; i < newScenePaths.Count; i++)
            {
                string path = newScenePaths[i];
                int existingIdx = scenes.FindIndex(s => s.path == path);
                if (existingIdx >= 0)
                {
                    scenes[existingIdx] = new EditorBuildSettingsScene(path, true);
                }
                else
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
