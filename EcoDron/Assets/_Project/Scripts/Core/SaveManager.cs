using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace EcoDron.Core
{
    [Serializable]
    public class LevelSaveData
    {
        public string levelId;
        public int stars;
    }

    [Serializable]
    public class CampaignSaveData
    {
        public string selectedScenarioId = "scenario_01";
        public string selectedLevelId = "1.1";
        public List<LevelSaveData> levels = new List<LevelSaveData>();
    }

    /// <summary>
    /// Persistent JSON-based SaveManager that survives scene loads and works in both Runtime and Editor.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        private const string SaveFileName = "ecodron_save.json";

        private static SaveManager _instance;
        private static CampaignSaveData _cachedData;
        private static bool _isLoaded;

        public static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static SaveManager Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying)
                {
                    Bootstrap();
                }
                return _instance;
            }
        }

        public static CampaignSaveData Data
        {
            get
            {
                if (!_isLoaded || _cachedData == null)
                {
                    Load();
                }
                return _cachedData;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (_instance != null) return;

            SaveManager existing = FindFirstObjectByType<SaveManager>();
            if (existing != null)
            {
                _instance = existing;
                DontDestroyOnLoad(_instance.gameObject);
            }
            else
            {
                GameObject go = new GameObject("[SaveManager]");
                _instance = go.AddComponent<SaveManager>();
                DontDestroyOnLoad(go);
            }

            Load();
            Debug.Log($"<b><color=#00E5FF>[SaveManager]</color></b> Inicializado (DontDestroyOnLoad). Archivo: {SaveFilePath}");
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (!_isLoaded)
            {
                Load();
            }
        }

        public static void Load()
        {
            string path = SaveFilePath;

            if (File.Exists(path))
            {
                try
                {
                    string json = File.ReadAllText(path);
                    CampaignSaveData loaded = JsonUtility.FromJson<CampaignSaveData>(json);
                    _cachedData = loaded ?? new CampaignSaveData();
                    if (_cachedData.levels == null)
                    {
                        _cachedData.levels = new List<LevelSaveData>();
                    }
                    _isLoaded = true;
                    Debug.Log($"<b><color=#00E5FF>[SaveManager]</color></b> Partida cargada desde disco ({_cachedData.levels.Count} niveles con registro). Escenario activo: '{_cachedData.selectedScenarioId}', Nivel activo: '{_cachedData.selectedLevelId}'.");
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[SaveManager] Error leyendo '{path}', se creará un guardado limpio. Detalle: {ex.Message}");
                }
            }

            _cachedData = new CampaignSaveData();
            _isLoaded = true;
            Save("Creación inicial de archivo de guardado");
        }

        public static void Save(string reason = null)
        {
            if (_cachedData == null)
            {
                _cachedData = new CampaignSaveData();
            }

            try
            {
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonUtility.ToJson(_cachedData, true);
                File.WriteAllText(SaveFilePath, json);

                string reasonTag = string.IsNullOrEmpty(reason) ? string.Empty : $" ({reason})";
                Debug.Log($"<b><color=#00FF7F>[SaveManager]</color></b> Guardado persistente actualizado{reasonTag} -> {SaveFilePath}\n{BuildCompactSummary()}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] No se pudo guardar en '{SaveFilePath}': {ex.Message}");
            }
        }

        public static void ResetSave()
        {
            _cachedData = new CampaignSaveData
            {
                selectedScenarioId = "scenario_01",
                selectedLevelId = "1.1",
                levels = new List<LevelSaveData>()
            };
            _isLoaded = true;

            ClearLegacyPlayerPrefs();

            if (File.Exists(SaveFilePath))
            {
                File.Delete(SaveFilePath);
            }

            Save("Reinicio completo de partida");
            Debug.Log("<b><color=#FFD166>[SaveManager]</color></b> Progreso vaciado y reiniciado a estado inicial.");
        }

        public static string GetSelectedScenarioId()
        {
            return string.IsNullOrEmpty(Data.selectedScenarioId) ? "scenario_01" : Data.selectedScenarioId;
        }

        public static void SetSelectedScenarioId(string scenarioId)
        {
            if (string.IsNullOrEmpty(scenarioId)) return;
            if (Data.selectedScenarioId == scenarioId) return;

            Data.selectedScenarioId = scenarioId;
            Save($"SelectedScenarioId -> {scenarioId}");
        }

        public static string GetSelectedLevelId()
        {
            return string.IsNullOrEmpty(Data.selectedLevelId) ? "1.1" : Data.selectedLevelId;
        }

        public static void SetSelectedLevelId(string levelId)
        {
            if (string.IsNullOrEmpty(levelId)) return;
            if (Data.selectedLevelId == levelId) return;

            Data.selectedLevelId = levelId;
            Save($"SelectedLevelId -> {levelId}");
        }

        public static int GetLevelStars(string levelId)
        {
            if (string.IsNullOrEmpty(levelId) || Data.levels == null) return 0;

            for (int i = 0; i < Data.levels.Count; i++)
            {
                if (Data.levels[i] != null && Data.levels[i].levelId == levelId)
                {
                    return Mathf.Clamp(Data.levels[i].stars, 0, 3);
                }
            }

            return 0;
        }

        public static bool SetLevelStars(string levelId, int stars)
        {
            if (string.IsNullOrEmpty(levelId)) return false;

            int clamped = Mathf.Clamp(stars, 0, 3);
            int current = GetLevelStars(levelId);
            if (clamped <= current)
            {
                Debug.Log($"[SaveManager] Nivel '{levelId}' ya tiene {current}★ (nuevo valor {clamped}★ no lo supera).");
                return false;
            }

            LevelSaveData entry = null;
            for (int i = 0; i < Data.levels.Count; i++)
            {
                if (Data.levels[i] != null && Data.levels[i].levelId == levelId)
                {
                    entry = Data.levels[i];
                    break;
                }
            }

            if (entry == null)
            {
                entry = new LevelSaveData { levelId = levelId, stars = clamped };
                Data.levels.Add(entry);
            }
            else
            {
                entry.stars = clamped;
            }

            Save($"Nivel {levelId} -> {clamped}★");
            return true;
        }

        public static string BuildCompactSummary()
        {
            CampaignSaveData d = Data;
            if (d.levels == null || d.levels.Count == 0)
            {
                return $"[Resumen Save] Escenario: '{d.selectedScenarioId}' | Nivel: '{d.selectedLevelId}' | Niveles completados: 0";
            }

            var parts = new List<string>(d.levels.Count);
            for (int i = 0; i < d.levels.Count; i++)
            {
                if (d.levels[i] != null)
                {
                    parts.Add($"{d.levels[i].levelId}:{d.levels[i].stars}★");
                }
            }

            return $"[Resumen Save] Escenario: '{d.selectedScenarioId}' | Nivel: '{d.selectedLevelId}' | Niveles: [{string.Join(", ", parts)}]";
        }

        private static void ClearLegacyPlayerPrefs()
        {
            PlayerPrefs.DeleteKey("EcoDron_SelectedScenario");
            PlayerPrefs.DeleteKey("EcoDron_SelectedLevel");

            for (int s = 1; s <= 5; s++)
            {
                for (int l = 1; l <= 5; l++)
                {
                    PlayerPrefs.DeleteKey($"EcoDron_LevelStars_{s}.{l}");
                }
            }

            PlayerPrefs.Save();
        }
    }
}
