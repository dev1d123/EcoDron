using System.Collections.Generic;
using EcoDron.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoDron.UI.MainMenu
{
    /// <summary>
    /// Controls audio, fullscreen, and quality preferences inside the Options popup.
    /// </summary>
    public class OptionsPopupController : MonoBehaviour
    {
        [Header("Audio Sliders")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI masterVolumeLabel;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TextMeshProUGUI musicVolumeLabel;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI sfxVolumeLabel;

        [Header("Display & Quality")]
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private TMP_Dropdown qualityDropdown;

        private bool _isInitializing;

        private void OnEnable()
        {
            SyncUIFromSettings();
        }

        private void Awake()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
        }

        private void OnDestroy()
        {
            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);
        }

        public void SyncUIFromSettings()
        {
            _isInitializing = true;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
                UpdatePercentLabel(masterVolumeLabel, GameSettings.MasterVolume);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
                UpdatePercentLabel(musicVolumeLabel, GameSettings.MusicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
                UpdatePercentLabel(sfxVolumeLabel, GameSettings.SfxVolume);
            }

            if (fullscreenToggle != null)
            {
                fullscreenToggle.SetIsOnWithoutNotify(GameSettings.IsFullscreen);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.ClearOptions();
                List<string> options = new List<string>(QualitySettings.names);
                qualityDropdown.AddOptions(options);
                qualityDropdown.SetValueWithoutNotify(GameSettings.QualityIndex);
                qualityDropdown.RefreshShownValue();
            }

            _isInitializing = false;
        }

        private void OnMasterVolumeChanged(float value)
        {
            if (_isInitializing) return;
            GameSettings.MasterVolume = value;
            UpdatePercentLabel(masterVolumeLabel, value);
            GameSettings.Save();
        }

        private void OnMusicVolumeChanged(float value)
        {
            if (_isInitializing) return;
            GameSettings.MusicVolume = value;
            UpdatePercentLabel(musicVolumeLabel, value);
            GameSettings.Save();
        }

        private void OnSfxVolumeChanged(float value)
        {
            if (_isInitializing) return;
            GameSettings.SfxVolume = value;
            UpdatePercentLabel(sfxVolumeLabel, value);
            GameSettings.Save();
        }

        private void OnFullscreenChanged(bool isFullscreen)
        {
            if (_isInitializing) return;
            GameSettings.IsFullscreen = isFullscreen;
            GameSettings.Save();
        }

        private void OnQualityChanged(int index)
        {
            if (_isInitializing) return;
            GameSettings.QualityIndex = index;
            GameSettings.Save();
        }

        private static void UpdatePercentLabel(TextMeshProUGUI label, float normalizedValue)
        {
            if (label != null)
            {
                label.text = $"{Mathf.RoundToInt(normalizedValue * 100f)}%";
            }
        }
    }
}
