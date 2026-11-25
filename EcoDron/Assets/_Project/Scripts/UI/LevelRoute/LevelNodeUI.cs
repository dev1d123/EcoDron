using System;
using System.Collections.Generic;
using EcoDron.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoDron.UI.LevelRoute
{
    public enum LevelNodeState
    {
        Locked = 0,
        UnlockedCurrent = 1,
        Completed = 2
    }

    /// <summary>
    /// Interactive node representing a single level along a scenario's route.
    /// </summary>
    public class LevelNodeUI : MonoBehaviour
    {
        [Header("Node Visuals")]
        [SerializeField] private Image discImage;
        [SerializeField] private Button nodeButton;
        [SerializeField] private TextMeshProUGUI levelIdText;
        [SerializeField] private Sprite greenDiscSprite;
        [SerializeField] private Sprite yellowDiscSprite;
        [SerializeField] private Sprite grayDiscSprite;

        [Header("State Attachments")]
        [SerializeField] private RectTransform glowRing;
        [SerializeField] private GameObject lockIconObject;
        [SerializeField] private GameObject completedTooltip;
        [SerializeField] private TextMeshProUGUI completedTooltipText;
        [SerializeField] private GameObject starsContainer;
        [SerializeField] private List<Image> starImages = new List<Image>();
        [SerializeField] private Sprite starFilledSprite;
        [SerializeField] private Sprite starEmptySprite;

        private LevelDataSO _level;
        private LevelNodeState _state;
        private bool _isSelected;
        private Action<LevelDataSO, LevelNodeUI> _onClicked;

        private void Awake()
        {
            if (nodeButton != null)
            {
                nodeButton.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (nodeButton != null)
            {
                nodeButton.onClick.RemoveListener(HandleClicked);
            }
        }

        private void Update()
        {
            if (glowRing != null && glowRing.gameObject.activeSelf)
            {
                float pulse = 1f + 0.06f * Mathf.Sin(Time.unscaledTime * 3.5f);
                glowRing.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        public void Bind(
            LevelDataSO level,
            LevelNodeState state,
            int starsEarned,
            bool isSelected,
            Action<LevelDataSO, LevelNodeUI> onClicked)
        {
            _level = level;
            _state = state;
            _onClicked = onClicked;

            if (levelIdText != null)
            {
                levelIdText.text = level != null ? level.levelId : string.Empty;
                levelIdText.color = state == LevelNodeState.Locked
                    ? new Color32(72, 82, 88, 235)
                    : Color.white;
                levelIdText.outlineWidth = 0.20f;
                levelIdText.outlineColor = state switch
                {
                    LevelNodeState.Completed => new Color32(18, 92, 34, 230),
                    LevelNodeState.UnlockedCurrent => new Color32(150, 102, 10, 230),
                    _ => new Color32(50, 58, 64, 180)
                };
            }

            if (discImage != null)
            {
                discImage.sprite = state switch
                {
                    LevelNodeState.Completed => greenDiscSprite,
                    LevelNodeState.UnlockedCurrent => yellowDiscSprite,
                    _ => grayDiscSprite
                };
            }

            bool isLocked = state == LevelNodeState.Locked;
            if (nodeButton != null)
            {
                nodeButton.interactable = !isLocked;
            }

            if (lockIconObject != null)
            {
                lockIconObject.SetActive(isLocked);
            }

            if (completedTooltipText != null && level != null)
            {
                completedTooltipText.text = level.displayName;
            }

            bool showStars = state == LevelNodeState.Completed;
            if (starsContainer != null)
            {
                starsContainer.SetActive(showStars);
            }

            if (showStars)
            {
                for (int i = 0; i < starImages.Count; i++)
                {
                    if (starImages[i] != null)
                    {
                        starImages[i].sprite = i < starsEarned ? starFilledSprite : starEmptySprite;
                    }
                }
            }

            SetSelected(isSelected);
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;

            if (glowRing != null)
            {
                glowRing.gameObject.SetActive(_isSelected && _state != LevelNodeState.Locked);
            }

            if (completedTooltip != null)
            {
                completedTooltip.SetActive(_state == LevelNodeState.Completed && !_isSelected);
            }
        }

        private void HandleClicked()
        {
            if (_state != LevelNodeState.Locked && _level != null)
            {
                _onClicked?.Invoke(_level, this);
            }
        }
    }
}
