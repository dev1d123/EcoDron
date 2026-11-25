using System.Collections.Generic;
using EcoDron.Core;
using EcoDron.Interaction;
using EcoDron.UI.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoDron.Gameplay
{
    /// <summary>
    /// Tracks trash-to-bin objective progress and shows the Victory Alert when all trash is deposited.
    /// </summary>
    public class LevelObjectiveController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TrashBinZone trashBin;
        [SerializeField] private LevelSessionController sessionController;

        [Header("HUD Objective")]
        [SerializeField] private TextMeshProUGUI objectiveHudText;

        [Header("Victory Alert Popup")]
        [SerializeField] private UIPopupWindow victoryPopup;
        [SerializeField] private TextMeshProUGUI victoryTitleText;
        [SerializeField] private TextMeshProUGUI victoryMessageText;
        [SerializeField] private Button nextButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button routeButton;

        private int _totalTrashCount;
        private int _depositedCount;
        private bool _victoryTriggered;

        public int TotalTrashCount => _totalTrashCount;
        public int DepositedCount => _depositedCount;
        public bool IsVictoryTriggered => _victoryTriggered;

        private void Awake()
        {
            if (sessionController == null)
            {
                sessionController = FindFirstObjectByType<LevelSessionController>();
            }

            if (trashBin == null)
            {
                trashBin = FindFirstObjectByType<TrashBinZone>();
            }

            if (victoryPopup != null)
            {
                victoryPopup.InitializeClosed();
            }

            if (nextButton != null) nextButton.onClick.AddListener(HandleNextClicked);
            if (retryButton != null) retryButton.onClick.AddListener(HandleRetryClicked);
            if (routeButton != null) routeButton.onClick.AddListener(HandleRouteClicked);
        }

        private void OnEnable()
        {
            if (trashBin != null)
            {
                trashBin.OnTrashDeposited += HandleTrashDeposited;
            }
        }

        private void OnDisable()
        {
            if (trashBin != null)
            {
                trashBin.OnTrashDeposited -= HandleTrashDeposited;
            }
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(HandleNextClicked);
            if (retryButton != null) retryButton.onClick.RemoveListener(HandleRetryClicked);
            if (routeButton != null) routeButton.onClick.RemoveListener(HandleRouteClicked);
        }

        private void Start()
        {
            GrabbableObject[] items = FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
            _totalTrashCount = Mathf.Max(1, items.Length);
            _depositedCount = trashBin != null ? trashBin.DepositedCount : 0;
            UpdateObjectiveHud();

            Debug.Log($"<b><color=#00E5FF>[LevelObjective]</color></b> Objetivo iniciado: depositar <b>{_totalTrashCount}</b> basura(s) en el tacho.");
        }

        private void HandleTrashDeposited(GrabbableObject item, int currentBinCount)
        {
            _depositedCount = currentBinCount;
            UpdateObjectiveHud();

            if (!_victoryTriggered && _depositedCount >= _totalTrashCount)
            {
                TriggerVictory();
            }
        }

        public void TriggerVictory()
        {
            if (_victoryTriggered) return;
            _victoryTriggered = true;

            if (trashBin != null)
            {
                trashBin.SetCompletedVisual(true);
            }

            if (sessionController != null)
            {
                sessionController.MarkLevelCompletedWithoutTransition(3);
            }
            else
            {
                CampaignProgress.SetLevelStars(CampaignProgress.SelectedLevelId, 3);
            }

            UpdateObjectiveHud();

            if (victoryTitleText != null)
            {
                victoryTitleText.text = "¡VICTORIA!";
            }

            if (victoryMessageText != null)
            {
                string levelLabel = sessionController != null ? sessionController.CurrentLevelDisplayLabel : "Nivel 1.1";
                victoryMessageText.text =
                    $"¡Has depositado toda la basura en el tacho!\n\n" +
                    $"<b>{levelLabel}</b> completado con <b>3 / 3 estrellas</b>.\n" +
                    $"Progreso guardado en SaveManager.";
            }

            if (victoryPopup != null)
            {
                victoryPopup.Open();
            }

            Debug.Log($"<b><color=#00FF7F>[VICTORIA]</color></b> ¡Nivel completado! Basura en el tacho: {_depositedCount}/{_totalTrashCount}. Alert de victoria mostrado.");
        }

        private void UpdateObjectiveHud()
        {
            if (objectiveHudText != null)
            {
                objectiveHudText.text = _victoryTriggered
                    ? $"OBJETIVO COMPLETADO: {_depositedCount} / {_totalTrashCount} EN EL TACHO"
                    : $"OBJETIVO: MUEVE LA BASURA AL TACHO ({_depositedCount} / {_totalTrashCount})";
            }
        }

        private void HandleNextClicked()
        {
            if (sessionController != null)
            {
                sessionController.AdvanceAfterCompletion();
            }
        }

        private void HandleRetryClicked()
        {
            if (sessionController != null)
            {
                sessionController.ReloadCurrentLevel();
            }
        }

        private void HandleRouteClicked()
        {
            if (sessionController != null)
            {
                sessionController.ReturnToLevelRoute();
            }
        }
    }
}
