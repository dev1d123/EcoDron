using System;
using EcoDron.Core;
using EcoDron.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoDron.UI.ScenarioSelect
{
    /// <summary>
    /// Renders a single scenario card in Completed, InProgress, or Locked state.
    /// </summary>
    public class ScenarioCardUI : MonoBehaviour
    {
        [Header("Card Frame")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Sprite completedCardSprite;
        [SerializeField] private Sprite activeCardSprite;
        [SerializeField] private Sprite lockedCardSprite;

        [Header("Title & Preview")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private GameObject lockedOverlay;

        [Header("Progress Row")]
        [SerializeField] private GameObject progressRow;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private GameObject starsContainer;

        [Header("Action Button")]
        [SerializeField] private Button actionButton;
        [SerializeField] private Image actionButtonImage;
        [SerializeField] private TextMeshProUGUI actionButtonText;
        [SerializeField] private Sprite repeatButtonSprite;
        [SerializeField] private Sprite enterButtonSprite;

        [Header("State Colors")]
        [SerializeField] private Color activeTextColor = new Color32(22, 50, 59, 255);
        [SerializeField] private Color lockedTextColor = new Color32(34, 52, 62, 210);
        [SerializeField] private Color lockedThumbnailTint = new Color32(115, 134, 146, 215);

        private ScenarioDataSO _scenario;
        private Action<ScenarioDataSO> _onSelected;

        private void Awake()
        {
            if (actionButton != null)
            {
                actionButton.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (actionButton != null)
            {
                actionButton.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Bind(
            ScenarioDataSO scenario,
            ScenarioState state,
            int completedLevels,
            int totalLevels,
            Action<ScenarioDataSO> onSelected)
        {
            _scenario = scenario;
            _onSelected = onSelected;

            if (titleText != null)
            {
                titleText.text = scenario != null ? scenario.headerTitle : string.Empty;
                titleText.color = state == ScenarioState.Locked ? lockedTextColor : activeTextColor;
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = scenario != null ? scenario.cardThumbnail : null;
                thumbnailImage.color = state == ScenarioState.Locked ? lockedThumbnailTint : Color.white;
            }

            if (cardBackground != null)
            {
                cardBackground.sprite = state switch
                {
                    ScenarioState.Completed => completedCardSprite,
                    ScenarioState.InProgress => activeCardSprite,
                    _ => lockedCardSprite
                };
            }

            bool isLocked = state == ScenarioState.Locked;
            if (lockedOverlay != null) lockedOverlay.SetActive(isLocked);
            if (progressRow != null) progressRow.SetActive(!isLocked);
            if (actionButton != null) actionButton.gameObject.SetActive(!isLocked);

            if (!isLocked)
            {
                bool isCompleted = state == ScenarioState.Completed;
                if (starsContainer != null) starsContainer.SetActive(isCompleted);

                if (progressText != null)
                {
                    progressText.text = isCompleted
                        ? $"{completedLevels} / {totalLevels} COMPLETADO"
                        : $"PROGRESO: {completedLevels} / {totalLevels}";
                    progressText.alignment = isCompleted ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.Center;
                }

                if (actionButtonImage != null)
                {
                    actionButtonImage.sprite = isCompleted ? repeatButtonSprite : enterButtonSprite;
                }

                if (actionButtonText != null)
                {
                    actionButtonText.text = isCompleted ? "REPETIR" : "ENTRAR";
                    actionButtonText.color = Color.white;
                    actionButtonText.outlineWidth = 0.14f;
                    actionButtonText.outlineColor = isCompleted
                        ? new Color32(42, 92, 120, 210)
                        : new Color32(18, 92, 34, 210);
                }
            }
        }

        private void HandleClicked()
        {
            if (_scenario != null)
            {
                _onSelected?.Invoke(_scenario);
            }
        }
    }
}
