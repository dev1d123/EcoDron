using EcoDron.Core;
using EcoDron.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoDron.Gameplay
{
    /// <summary>
    /// Handles active level session metadata, debug HUD, and quick completion with the O key.
    /// </summary>
    public class LevelSessionController : MonoBehaviour
    {
        [Header("Campaign Data")]
        [SerializeField] private CampaignDatabaseSO campaignDatabase;
        [SerializeField] private string scenarioId;
        [SerializeField] private string levelId;

        [Header("Navigation")]
        [SerializeField] private string scenarioSelectSceneName = "02_ScenarioSelect";
        [SerializeField] private string levelRouteSceneName = "03_LevelRoute";

        [Header("Debug")]
        [SerializeField] private bool showDebugOverlay = true;

        private ScenarioDataSO _scenario;
        private LevelDataSO _level;
        private LevelObjectiveController _objectiveController;
        private int _scenarioIndex = -1;
        private int _levelIndex = -1;
        private bool _isTransitioning;
        private GUIStyle _boxStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        public string CurrentLevelDisplayLabel =>
            _level != null ? $"Nivel {_level.levelId}: {_level.displayName}" : SceneManager.GetActiveScene().name;

        private void Awake()
        {
            ResolveLevelMetadata();
            _objectiveController = FindFirstObjectByType<LevelObjectiveController>();
        }

        private void Start()
        {
            int currentStars = _level != null ? CampaignProgress.GetLevelStars(_level.levelId) : 0;
            string activeScene = SceneManager.GetActiveScene().name;
            string title = _level != null ? $"{_level.levelId} - {_level.displayName}" : activeScene;
            string scenTitle = _scenario != null ? _scenario.headerTitle : scenarioId;

            Debug.Log(
                $"<b><color=#00E5FF>[LevelSession]</color></b> Escena iniciada: <b>{activeScene}</b> | " +
                $"Escenario: <b>{scenTitle}</b> | Nivel: <b>{title}</b> | Estrellas guardadas: <b>{currentStars}/3</b>\n" +
                $"Controles: [WASD] Mover Dron | [Mouse + Click/Espacio] Garra | [O] Completar con 3★ y avanzar | [ESC] Volver a '{levelRouteSceneName}'.\n" +
                SaveManager.BuildCompactSummary()
            );
        }

        private void Update()
        {
            if (_isTransitioning) return;

            bool completePressed = false;
            bool backPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                completePressed = Keyboard.current.oKey.wasPressedThisFrame;
                backPressed = Keyboard.current.escapeKey.wasPressedThisFrame;
            }
#else
            completePressed = Input.GetKeyDown(KeyCode.O);
            backPressed = Input.GetKeyDown(KeyCode.Escape);
#endif

            if (completePressed)
            {
                CompleteCurrentLevelWithThreeStars();
            }
            else if (backPressed)
            {
                ReturnToLevelRoute();
            }
        }

        public void MarkLevelCompletedWithoutTransition(int stars = 3)
        {
            ResolveLevelMetadata();
            if (_level == null) return;

            CampaignProgress.SetLevelStars(_level.levelId, stars);
            Debug.Log($"<b><color=#00FF7F>[LevelSession]</color></b> Nivel <b>{_level.levelId}: {_level.displayName}</b> guardado con <b>{stars}★</b>.");
        }

        public void CompleteCurrentLevelWithThreeStars()
        {
            if (_isTransitioning) return;
            ResolveLevelMetadata();

            if (_level == null)
            {
                Debug.LogWarning($"[LevelSession] No se pudo resolver LevelDataSO para la escena '{SceneManager.GetActiveScene().name}'.");
                return;
            }

            MarkLevelCompletedWithoutTransition(3);
            AdvanceAfterCompletion();
        }

        public void AdvanceAfterCompletion()
        {
            if (_isTransitioning) return;
            ResolveLevelMetadata();
            _isTransitioning = true;

            // Check if there is a next level in the current scenario
            if (_scenario != null && _scenario.levels != null && _levelIndex >= 0 && _levelIndex + 1 < _scenario.levels.Count)
            {
                LevelDataSO nextLevel = _scenario.levels[_levelIndex + 1];
                if (nextLevel != null)
                {
                    CampaignProgress.SelectedLevelId = nextLevel.levelId;
                    Debug.Log($"<b><color=#FFD166>[LevelSession]</color></b> Avanzando al siguiente nivel del escenario: <b>{nextLevel.levelId}: {nextLevel.displayName}</b> (Escena: '{nextLevel.sceneName}').");

                    if (!string.IsNullOrEmpty(nextLevel.sceneName) && Application.CanStreamedLevelBeLoaded(nextLevel.sceneName))
                    {
                        SceneManager.LoadScene(nextLevel.sceneName);
                        return;
                    }

                    Debug.LogWarning($"[LevelSession] La escena '{nextLevel.sceneName}' no está en Build Settings. Volviendo a '{levelRouteSceneName}'.");
                    LoadSceneIfAvailable(levelRouteSceneName);
                    return;
                }
            }

            // Current scenario finished -> advance to next scenario if available
            if (campaignDatabase != null && campaignDatabase.scenarios != null && _scenarioIndex >= 0 && _scenarioIndex + 1 < campaignDatabase.scenarios.Count)
            {
                ScenarioDataSO nextScenario = campaignDatabase.scenarios[_scenarioIndex + 1];
                if (nextScenario != null)
                {
                    CampaignProgress.SelectedScenarioId = nextScenario.scenarioId;
                    if (nextScenario.levels != null && nextScenario.levels.Count > 0 && nextScenario.levels[0] != null)
                    {
                        CampaignProgress.SelectedLevelId = nextScenario.levels[0].levelId;
                    }

                    Debug.Log(
                        $"<b><color=#00FF7F>[LevelSession]</color></b> ¡Escenario <b>{(_scenario != null ? _scenario.headerTitle : scenarioId)}</b> completado al 100%! " +
                        $"Siguiente escenario desbloqueado: <b>{nextScenario.headerTitle}</b> ('{nextScenario.scenarioId}'). Pasando a '{scenarioSelectSceneName}'."
                    );
                    LoadSceneIfAvailable(scenarioSelectSceneName);
                    return;
                }
            }

            Debug.Log($"<b><color=#00FF7F>[LevelSession]</color></b> ¡Todos los escenarios de la campaña han sido completados! Volviendo a '{scenarioSelectSceneName}'.");
            LoadSceneIfAvailable(scenarioSelectSceneName);
        }

        public void ReloadCurrentLevel()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            string activeScene = SceneManager.GetActiveScene().name;
            Debug.Log($"[LevelSession] Reiniciando nivel '{activeScene}'.");
            SceneManager.LoadScene(activeScene);
        }

        public void ReturnToLevelRoute()
        {
            if (_isTransitioning) return;
            _isTransitioning = true;
            Debug.Log($"[LevelSession] Saliendo del nivel hacia '{levelRouteSceneName}'.");
            LoadSceneIfAvailable(levelRouteSceneName);
        }

        private void LoadSceneIfAvailable(string targetScene)
        {
            if (!string.IsNullOrEmpty(targetScene) && Application.CanStreamedLevelBeLoaded(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                Debug.LogError($"[LevelSession] No se puede cargar la escena '{targetScene}'. Verifica Build Settings.");
                _isTransitioning = false;
            }
        }

        private void ResolveLevelMetadata()
        {
            if (campaignDatabase == null || campaignDatabase.scenarios == null) return;

            string activeSceneName = SceneManager.GetActiveScene().name;

            for (int s = 0; s < campaignDatabase.scenarios.Count; s++)
            {
                ScenarioDataSO scen = campaignDatabase.scenarios[s];
                if (scen == null || scen.levels == null) continue;

                for (int l = 0; l < scen.levels.Count; l++)
                {
                    LevelDataSO lvl = scen.levels[l];
                    if (lvl == null) continue;

                    bool matchesId = !string.IsNullOrEmpty(levelId) && lvl.levelId == levelId;
                    bool matchesScene = !string.IsNullOrEmpty(lvl.sceneName) && lvl.sceneName == activeSceneName;

                    if (matchesId || matchesScene)
                    {
                        _scenario = scen;
                        _level = lvl;
                        _scenarioIndex = s;
                        _levelIndex = l;
                        scenarioId = scen.scenarioId;
                        levelId = lvl.levelId;
                        return;
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showDebugOverlay) return;

            EnsureGuiStyles();

            int stars = _level != null ? CampaignProgress.GetLevelStars(_level.levelId) : 0;
            string scenName = _scenario != null ? _scenario.headerTitle : SceneManager.GetActiveScene().name;
            string lvlName = _level != null ? $"Nivel {_level.levelId}: {_level.displayName}" : SceneManager.GetActiveScene().name;
            string objectiveLine = _objectiveController != null
                ? $"Objetivo: Basura en el tacho ({_objectiveController.DepositedCount}/{_objectiveController.TotalTrashCount})   |   "
                : string.Empty;

            GUILayout.BeginArea(new Rect(16f, 16f, 700f, 118f), _boxStyle);
            GUILayout.Label($"{scenName}  •  {lvlName}", _titleStyle);
            GUILayout.Label($"{objectiveLine}Estrellas guardadas: {stars} / 3 ★   |   Save: {SaveManager.GetSelectedScenarioId()} / {SaveManager.GetSelectedLevelId()}", _bodyStyle);
            GUILayout.Label("[WASD] Volar  |  [Mouse + Click/Espacio] Garra  |  [O] Auto-Completar (3★)  |  [ESC] Ruta", _bodyStyle);
            GUILayout.EndArea();
        }

        private void EnsureGuiStyles()
        {
            if (_boxStyle == null)
            {
                Texture2D bgTex = new Texture2D(1, 1);
                bgTex.SetPixel(0, 0, new Color(0.06f, 0.12f, 0.16f, 0.82f));
                bgTex.Apply();

                _boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(14, 14, 10, 10)
                };
                _boxStyle.normal.background = bgTex;
            }

            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
                _titleStyle.normal.textColor = new Color(0.35f, 0.95f, 0.72f);
            }

            if (_bodyStyle == null)
            {
                _bodyStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13
                };
                _bodyStyle.normal.textColor = Color.white;
            }
        }
    }
}
