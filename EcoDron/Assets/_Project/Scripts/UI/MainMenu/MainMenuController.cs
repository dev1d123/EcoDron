using EcoDron.Core;
using EcoDron.UI.Common;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoDron.UI.MainMenu
{
    /// <summary>
    /// Handles Main Menu button actions, modal popups, and campaign scene transition.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Navigation")]
        [SerializeField] private string scenarioSelectSceneName = "02_ScenarioSelect";

        [Header("Primary Menu Buttons")]
        [SerializeField] private Button campaignButton;
        [SerializeField] private Button controlsButton;
        [SerializeField] private Button optionsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button exitButton;

        [Header("Modal Popups")]
        [SerializeField] private UIPopupWindow controlsPopup;
        [SerializeField] private UIPopupWindow optionsPopup;
        [SerializeField] private UIPopupWindow creditsPopup;
        [SerializeField] private UIPopupWindow campaignPendingPopup;

        private UIPopupWindow _activePopup;

        private void Awake()
        {
            GameSettings.ApplySavedSettings();

            InitializePopup(controlsPopup);
            InitializePopup(optionsPopup);
            InitializePopup(creditsPopup);
            InitializePopup(campaignPendingPopup);

            if (campaignButton != null) campaignButton.onClick.AddListener(OnCampaignClicked);
            if (controlsButton != null) controlsButton.onClick.AddListener(() => OpenPopup(controlsPopup));
            if (optionsButton != null) optionsButton.onClick.AddListener(() => OpenPopup(optionsPopup));
            if (creditsButton != null) creditsButton.onClick.AddListener(() => OpenPopup(creditsPopup));
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && _activePopup != null)
            {
                CloseActivePopup();
            }
        }

        public void OnCampaignClicked()
        {
            if (!string.IsNullOrEmpty(scenarioSelectSceneName) && Application.CanStreamedLevelBeLoaded(scenarioSelectSceneName))
            {
                SceneManager.LoadScene(scenarioSelectSceneName);
                return;
            }

            if (campaignPendingPopup != null)
            {
                OpenPopup(campaignPendingPopup);
            }
            else
            {
                Debug.Log($"[MainMenu] Escena '{scenarioSelectSceneName}' aún no agregada a Build Settings.");
            }
        }

        public void OpenPopup(UIPopupWindow popup)
        {
            if (popup == null) return;

            if (_activePopup != null && _activePopup != popup)
            {
                _activePopup.Close();
            }

            _activePopup = popup;
            _activePopup.Open();
        }

        public void CloseActivePopup()
        {
            if (_activePopup == null) return;
            _activePopup.Close();
            _activePopup = null;
        }

        public void OnExitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void InitializePopup(UIPopupWindow popup)
        {
            if (popup != null)
            {
                popup.InitializeClosed();
            }
        }
    }
}
