using System.Collections.Generic;
using EcoDron.Core;
using EcoDron.Data;
using EcoDron.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoDron.UI.LevelRoute
{
    /// <summary>
    /// Populates the Level Route screen for the chosen scenario and handles launching levels.
    /// </summary>
    public class LevelRouteController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CampaignDatabaseSO campaignDatabase;

        [Header("Scene Navigation")]
        [SerializeField] private string scenarioSelectSceneName = "02_ScenarioSelect";
        [SerializeField] private Button backButton;

        [Header("Header & Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private AspectRatioFitter backgroundAspectFitter;
        [SerializeField] private TextMeshProUGUI scenarioTitleText;
        [SerializeField] private TextMeshProUGUI starsTotalText;

        [Header("Route Nodes & Popover")]
        [SerializeField] private List<LevelNodeUI> levelNodes = new List<LevelNodeUI>();
        [SerializeField] private LevelPopoverUI levelPopover;

        [Header("Reference Preview Mode")]
        [Tooltip("Muestra estados de ejemplo coherentes con la referencia visual mientras se crean las escenas jugables.")]
        [SerializeField] private bool previewReferenceStates = true;
        [SerializeField] private string defaultPreviewScenarioId = "scenario_02";

        [Header("Pending Level Popup")]
        [SerializeField] private UIPopupWindow levelPendingPopup;
        [SerializeField] private TextMeshProUGUI levelPendingMessageText;

        private ScenarioDataSO _activeScenario;
        private LevelNodeUI _selectedNode;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (levelPendingPopup != null)
            {
                levelPendingPopup.InitializeClosed();
            }

            RefreshRoute();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (levelPendingPopup != null && levelPendingPopup.IsOpen)
                {
                    levelPendingPopup.Close();
                }
                else
                {
                    OnBackClicked();
                }
            }
        }

        public void RefreshRoute()
        {
            if (campaignDatabase == null || campaignDatabase.scenarios == null || campaignDatabase.scenarios.Count == 0)
            {
                return;
            }

            string targetId = CampaignProgress.SelectedScenarioId;
            _activeScenario = campaignDatabase.GetScenarioById(targetId);
            if (_activeScenario == null)
            {
                _activeScenario = campaignDatabase.GetScenarioById(defaultPreviewScenarioId) ?? campaignDatabase.scenarios[0];
            }

            if (scenarioTitleText != null)
            {
                scenarioTitleText.text = _activeScenario.headerTitle;
            }

            if (backgroundImage != null && _activeScenario.routeBackground != null)
            {
                backgroundImage.sprite = _activeScenario.routeBackground;
                if (backgroundAspectFitter != null)
                {
                    Rect r = _activeScenario.routeBackground.rect;
                    if (r.height > 0f)
                    {
                        backgroundAspectFitter.aspectRatio = r.width / r.height;
                    }
                }
            }

            int levelCount = _activeScenario.levels != null ? _activeScenario.levels.Count : 0;
            int nodeCount = Mathf.Min(levelNodes.Count, levelCount);
            int totalEarnedStars = 0;
            int maxPossibleStars = nodeCount * 3;
            int defaultSelectedIdx = 0;

            var states = new LevelNodeState[nodeCount];
            var stars = new int[nodeCount];

            for (int i = 0; i < nodeCount; i++)
            {
                LevelDataSO lvl = _activeScenario.levels[i];
                if (previewReferenceStates)
                {
                    if (_activeScenario.scenarioNumber == 1)
                    {
                        states[i] = LevelNodeState.Completed;
                        stars[i] = 3;
                        defaultSelectedIdx = nodeCount - 1;
                    }
                    else if (_activeScenario.scenarioNumber == 2)
                    {
                        if (i == 0) { states[i] = LevelNodeState.Completed; stars[i] = 3; }
                        else if (i == 1) { states[i] = LevelNodeState.UnlockedCurrent; stars[i] = 0; defaultSelectedIdx = 1; }
                        else { states[i] = LevelNodeState.Locked; stars[i] = 0; }
                    }
                    else
                    {
                        if (i == 0) { states[i] = LevelNodeState.UnlockedCurrent; stars[i] = 0; defaultSelectedIdx = 0; }
                        else { states[i] = LevelNodeState.Locked; stars[i] = 0; }
                    }
                }
                else
                {
                    int earned = lvl != null ? CampaignProgress.GetLevelStars(lvl.levelId) : 0;
                    stars[i] = earned;
                    if (earned > 0)
                    {
                        states[i] = LevelNodeState.Completed;
                        defaultSelectedIdx = Mathf.Min(i + 1, nodeCount - 1);
                    }
                    else if (i == 0 || (i > 0 && states[i - 1] == LevelNodeState.Completed))
                    {
                        states[i] = LevelNodeState.UnlockedCurrent;
                        defaultSelectedIdx = i;
                    }
                    else
                    {
                        states[i] = LevelNodeState.Locked;
                    }
                }

                totalEarnedStars += stars[i];
            }

            if (starsTotalText != null)
            {
                starsTotalText.text = $"{totalEarnedStars} / {maxPossibleStars}";
            }

            for (int i = 0; i < levelNodes.Count; i++)
            {
                LevelNodeUI node = levelNodes[i];
                if (node == null) continue;

                if (i >= nodeCount)
                {
                    node.gameObject.SetActive(false);
                    continue;
                }

                node.gameObject.SetActive(true);
                bool isSelected = i == defaultSelectedIdx;
                node.Bind(_activeScenario.levels[i], states[i], stars[i], isSelected, OnNodeSelected);
                if (isSelected)
                {
                    _selectedNode = node;
                }
            }

            if (levelPopover != null && _selectedNode != null && defaultSelectedIdx < nodeCount)
            {
                levelPopover.ShowAtNode(
                    _activeScenario.levels[defaultSelectedIdx],
                    _selectedNode.GetComponent<RectTransform>(),
                    OnLaunchLevel
                );
            }
        }

        public void OnBackClicked()
        {
            if (!string.IsNullOrEmpty(scenarioSelectSceneName) && Application.CanStreamedLevelBeLoaded(scenarioSelectSceneName))
            {
                SceneManager.LoadScene(scenarioSelectSceneName);
            }
        }

        public void ClosePendingPopup()
        {
            if (levelPendingPopup != null)
            {
                levelPendingPopup.Close();
            }
        }

        private void OnNodeSelected(LevelDataSO level, LevelNodeUI node)
        {
            if (_selectedNode != null && _selectedNode != node)
            {
                _selectedNode.SetSelected(false);
            }

            _selectedNode = node;
            _selectedNode.SetSelected(true);

            if (levelPopover != null)
            {
                levelPopover.ShowAtNode(level, node.GetComponent<RectTransform>(), OnLaunchLevel);
            }
        }

        private void OnLaunchLevel(LevelDataSO level)
        {
            if (level == null) return;

            CampaignProgress.SelectedLevelId = level.levelId;
            PlayerPrefs.Save();

            if (!string.IsNullOrEmpty(level.sceneName) && Application.CanStreamedLevelBeLoaded(level.sceneName))
            {
                SceneManager.LoadScene(level.sceneName);
                return;
            }

            if (levelPendingPopup != null)
            {
                if (levelPendingMessageText != null)
                {
                    levelPendingMessageText.text =
                        $"Misión seleccionada: <b>{level.levelId}: {level.displayName}</b>\n\n" +
                        $"Objetivo: <b>{level.wasteCount} Residuos</b>  •  Tiempo límite: <b>{level.timeLimitSeconds}s</b>\n\n" +
                        $"La escena jugable (<b>{level.sceneName}</b>) se conectará al desarrollar los niveles.";
                }
                levelPendingPopup.Open();
            }
        }
    }
}
