using UnityEngine;

namespace EcoDron.Core
{
    /// <summary>
    /// Persists and applies player audio, display, and quality preferences.
    /// </summary>
    public static class GameSettings
    {
        private const string MasterVolumeKey = "EcoDron_MasterVolume";
        private const string MusicVolumeKey = "EcoDron_MusicVolume";
        private const string SfxVolumeKey = "EcoDron_SfxVolume";
        private const string FullscreenKey = "EcoDron_Fullscreen";
        private const string QualityKey = "EcoDron_Quality";

        public static float MasterVolume
        {
            get => PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
            set
            {
                float clamped = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MasterVolumeKey, clamped);
                AudioListener.volume = clamped;
            }
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.80f);
            set => PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 0.90f);
            set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public static bool IsFullscreen
        {
            get => PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
            set
            {
                PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
                Screen.fullScreen = value;
            }
        }

        public static int QualityIndex
        {
            get
            {
                int maxIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
                return Mathf.Clamp(PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()), 0, maxIndex);
            }
            set
            {
                int maxIndex = Mathf.Max(0, QualitySettings.names.Length - 1);
                int clamped = Mathf.Clamp(value, 0, maxIndex);
                PlayerPrefs.SetInt(QualityKey, clamped);
                QualitySettings.SetQualityLevel(clamped, true);
            }
        }

        public static void ApplySavedSettings()
        {
            AudioListener.volume = MasterVolume;
            Screen.fullScreen = IsFullscreen;
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(QualityIndex, true);
            }
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }
    }
}
