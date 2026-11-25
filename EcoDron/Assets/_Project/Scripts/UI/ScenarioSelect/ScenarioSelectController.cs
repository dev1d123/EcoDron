using System.Collections.Generic;
using EcoDron.Core;
using EcoDron.Data;
using EcoDron.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoDron.UI.ScenarioSelect
{
    /// <summary>
    /// Populates campaign scenario cards and handles navigation back to Main Menu or forward to Level Route.
    /// </summary>
    public class ScenarioSelectController : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CampaignDatabaseSO campaignDatabase;

        [Header("Navigation")]
        [SerializeField] private string mainMenuSceneName = "01_MainMenu";
        [SerializeField] private string levelRouteSceneName = "03_LevelRoute";
        [SerializeField] private Button backButton;

        [Header("Cards")]
        [SerializeField] private List<ScenarioCardUI> scenarioCards = new List<ScenarioCardUI>();

        [Header("Reference Preview Mode")]
        [Tooltip("Si está activo, muestra el Escenario 1 completado, el 2 activo y el 3 bloqueado como en la referencia visual hasta conectar los niveles jugables.")]
        [SerializeField] private bool previewReferenceStates = false;

        [Header("Pending Route Popup")]
        [SerializeField] private UIPopupWindow routePendingPopup;
        [SerializeField] private TextMeshProUGUI routePendingMessageText;

        private void Awake()
        {
            if (backButton != null)
            {
                backButton.onClick.AddListener(OnBackClicked);
            }

            if (routePendingPopup != null)
            {
                routePendingPopup.InitializeClosed();
            }

            RefreshCards();
        }

        private void Start()
        {
            RefreshCards();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (routePendingPopup != null && routePendingPopup.IsOpen)
                {
                    routePendingPopup.Close();
                }
                else
                {
                    OnBackClicked();
                }
            }
        }

        public void RefreshCards()
        {
            if (campaignDatabase == null || campaignDatabase.scenarios == null) return;

            int count = Mathf.Min(scenarioCards.Count, campaignDatabase.scenarios.Count);
            for (int i = 0; i < count; i++)
            {
                ScenarioDataSO scenario = campaignDatabase.scenarios[i];
                ScenarioCardUI card = scenarioCards[i];
                if (scenario == null || card == null) continue;

                int totalLevels = scenario.levels != null && scenario.levels.Count > 0 ? scenario.levels.Count : 3;
                ScenarioState state;
                int completedLevels;

                if (previewReferenceStates)
                {
                    if (i == 0)
                    {
                        state = ScenarioState.Completed;
                        completedLevels = totalLevels;
                    }
                    else if (i == 1)
                    {
                        state = ScenarioState.InProgress;
                        completedLevels = 1;
                    }
                    else
                    {
                        state = ScenarioState.Locked;
                        completedLevels = 0;
                    }
                }
                else
                {
                    state = CampaignProgress.GetScenarioState(campaignDatabase, i);
                    completedLevels = CampaignProgress.GetScenarioCompletedCount(scenario);
                }

                Debug.Log($"[ScenarioSelect] Tarjeta '{scenario.scenarioId}' ({scenario.headerTitle}) -> Estado: {state}, Progreso: {completedLevels}/{totalLevels}, Preview: {previewReferenceStates}");
                card.Bind(scenario, state, completedLevels, totalLevels, OnScenarioSelected);
            }
        }

        public void OnBackClicked()
        {
            Debug.Log($"[ScenarioSelect] Volviendo al Menú Principal ('{mainMenuSceneName}').");
            if (!string.IsNullOrEmpty(mainMenuSceneName) && Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }

        public void ClosePendingPopup()
        {
            if (routePendingPopup != null)
            {
                routePendingPopup.Close();
            }
        }

        private void OnScenarioSelected(ScenarioDataSO scenario)
        {
            CampaignProgress.SelectedScenarioId = scenario.scenarioId;
            Debug.Log($"<b><color=#00E5FF>[ScenarioSelect]</color></b> Escenario seleccionado: '{scenario.scenarioId}' ({scenario.headerTitle}). Cargando '{levelRouteSceneName}'...");

            if (!string.IsNullOrEmpty(levelRouteSceneName) && Application.CanStreamedLevelBeLoaded(levelRouteSceneName))
            {
                SceneManager.LoadScene(levelRouteSceneName);
                return;
            }

            if (routePendingPopup != null)
            {
                if (routePendingMessageText != null)
                {
                    routePendingMessageText.text =
                        $"Seleccionaste <b>{scenario.headerTitle}</b>.\n\n" +
                        "La siguiente pantalla a construir es la <b>Ruta de Niveles (03_LevelRoute)</b> con los nodos 1, 2 y 3 de este escenario.";
                }
                routePendingPopup.Open();
            }
        }
    }
}
