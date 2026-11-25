using System;
using EcoDron.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoDron.UI.LevelRoute
{
    /// <summary>
    /// Floating preview card displayed above the currently selected level node.
    /// </summary>
    public class LevelPopoverUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform popoverRect;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI metadataText;
        [SerializeField] private Button launchButton;

        [Header("Positioning")]
        [SerializeField] private float verticalOffsetAboveNode = 112f;
        [SerializeField] private float maxHorizontalClamp = 560f;

        private LevelDataSO _currentLevel;
        private Action<LevelDataSO> _onLaunch;

        private void Awake()
        {
            if (popoverRect == null)
            {
                popoverRect = GetComponent<RectTransform>();
            }

            if (launchButton != null)
            {
                launchButton.onClick.AddListener(HandleLaunchClicked);
            }
        }

        private void OnDestroy()
        {
            if (launchButton != null)
            {
                launchButton.onClick.RemoveListener(HandleLaunchClicked);
            }
        }

        public void ShowAtNode(LevelDataSO level, RectTransform nodeRect, Action<LevelDataSO> onLaunch)
        {
            _currentLevel = level;
            _onLaunch = onLaunch;

            if (level == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = level.thumbnail;
            }

            if (titleText != null)
            {
                titleText.text = $"{level.levelId}: {level.displayName}";
            }

            if (metadataText != null)
            {
                metadataText.text = $"{level.wasteCount} Residuos | {level.timeLimitSeconds}s";
            }

            if (popoverRect != null && nodeRect != null)
            {
                float targetX = Mathf.Clamp(nodeRect.anchoredPosition.x, -maxHorizontalClamp, maxHorizontalClamp);
                float targetY = nodeRect.anchoredPosition.y + verticalOffsetAboveNode;
                popoverRect.anchoredPosition = new Vector2(targetX, targetY);
            }
        }

        private void HandleLaunchClicked()
        {
            if (_currentLevel != null)
            {
                _onLaunch?.Invoke(_currentLevel);
            }
        }
    }
}
