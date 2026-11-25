using System.Collections.Generic;
using System.IO;
using EcoDron.UI.Common;
using EcoDron.UI.MainMenu;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace EcoDron.EditorTools
{
    /// <summary>
    /// Configures UI sprites/fonts and builds the Main Menu scene matching the EcoDron reference design.
    /// </summary>
    public static class MainMenuBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/01_MainMenu.unity";
        private const string BoldFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Bold SDF.asset";
        private const string RegularFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Regular SDF.asset";

        private static readonly Color SlateTextColor = new Color32(22, 50, 59, 255);
        private static readonly Color MutedSlateColor = new Color32(58, 88, 96, 255);
        private static readonly Color EcoGreenColor = new Color32(47, 184, 74, 255);
        private static readonly Color WarmGoldColor = new Color32(248, 209, 114, 255);

        [MenuItem("EcoDron/1. Setup UI Assets & TMP Fonts")]
        public static void SetupAssets()
        {
            ImportTmpEssentialsIfNeeded();
            ConfigureSprites();
            CreateTmpFontAsset("Assets/_Project/Fonts/TrebuchetMS_Bold.ttf", BoldFontSdfPath);
            CreateTmpFontAsset("Assets/_Project/Fonts/TrebuchetMS_Regular.ttf", RegularFontSdfPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[EcoDron] UI assets and TMP fonts configured.");
        }

        [MenuItem("EcoDron/2. Build Main Menu Scene")]
        public static void BuildMainMenuScene()
        {
            SetupAssets();

            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontSdfPath);
            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontSdfPath);
            if (boldFont == null || regularFont == null)
            {
                Debug.LogError("[EcoDron] Missing TMP_FontAsset. Run 'EcoDron/1. Setup UI Assets & TMP Fonts' first.");
                return;
            }

            Sprite bgSprite = LoadSprite("Assets/_Project/Textures/UI/Backgrounds/BG_Colegio.jpg");
            Sprite logoSprite = LoadSprite("Assets/_Project/Textures/UI/Branding/EcoDron_Logo.png");
            Sprite sidebarSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Panel_FrostedDark.png");
            Sprite greenBtnSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Primary_Green.png");
            Sprite glassBtnSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Secondary_Glass.png");
            Sprite popupCardSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Popup_Card.png");
            Sprite yellowCircleSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Yellow_Circle.png");

            Sprite iconPlay = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Play.png");
            Sprite iconControls = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Controls.png");
            Sprite iconOptions = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Options.png");
            Sprite iconCredits = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Credits.png");
            Sprite iconExit = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Exit.png");
            Sprite iconClose = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Close.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(125, 198, 232, 255);
            cam.orthographic = false;
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1.5f, -10f);

            // EventSystem (Input System)
            GameObject eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // Canvas
            GameObject canvasGo = new GameObject("Canvas_MainMenu");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = false;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.7f;

            canvasGo.AddComponent<GraphicRaycaster>();
            MainMenuController menuController = canvasGo.AddComponent<MainMenuController>();

            // 1. Background Image
            GameObject bgGo = CreateUIObject("Background_Colegio", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bgImage = bgGo.AddComponent<Image>();
            bgImage.sprite = bgSprite;
            bgImage.raycastTarget = false;
            AspectRatioFitter bgFitter = bgGo.AddComponent<AspectRatioFitter>();
            bgFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bgFitter.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 16f / 9f;

            // 2. Drone 3D Anchor placeholder on the right side
            GameObject droneAnchor = CreateUIObject("Drone3D_SlotAnchor", canvasGo.transform);
            RectTransform droneRt = droneAnchor.GetComponent<RectTransform>();
            droneRt.anchorMin = new Vector2(0.70f, 0.50f);
            droneRt.anchorMax = new Vector2(0.70f, 0.50f);
            droneRt.sizeDelta = new Vector2(400f, 400f);

            // 3. Left Frosted Sidebar
            GameObject sidebarGo = CreateUIObject("Panel_LeftSidebar", canvasGo.transform);
            RectTransform sidebarRt = sidebarGo.GetComponent<RectTransform>();
            sidebarRt.anchorMin = new Vector2(0f, 0f);
            sidebarRt.anchorMax = new Vector2(0f, 1f);
            sidebarRt.pivot = new Vector2(0f, 0.5f);
            sidebarRt.anchoredPosition = Vector2.zero;
            sidebarRt.sizeDelta = new Vector2(675f, 0f);
            Image sidebarImg = sidebarGo.AddComponent<Image>();
            sidebarImg.sprite = sidebarSprite;
            sidebarImg.type = Image.Type.Simple;
            sidebarImg.color = Color.white;
            sidebarImg.raycastTarget = false;

            // 4. Logo
            GameObject logoGo = CreateUIObject("Img_LogoEcoDron", sidebarGo.transform);
            RectTransform logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 1f);
            logoRt.anchorMax = new Vector2(0.5f, 1f);
            logoRt.pivot = new Vector2(0.5f, 1f);
            logoRt.anchoredPosition = new Vector2(0f, -40f);
            logoRt.sizeDelta = new Vector2(520f, 210f);
            Image logoImg = logoGo.AddComponent<Image>();
            logoImg.sprite = logoSprite;
            logoImg.preserveAspect = true;
            logoImg.raycastTarget = false;

            // 5. Buttons Stack
            GameObject stackGo = CreateUIObject("MenuButtons_Container", sidebarGo.transform);
            RectTransform stackRt = stackGo.GetComponent<RectTransform>();
            stackRt.anchorMin = new Vector2(0.5f, 1f);
            stackRt.anchorMax = new Vector2(0.5f, 1f);
            stackRt.pivot = new Vector2(0.5f, 1f);
            stackRt.anchoredPosition = new Vector2(0f, -290f);
            stackRt.sizeDelta = new Vector2(520f, 520f);

            VerticalLayoutGroup vlg = stackGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Button btnCampaign = CreateMenuButton(
                "Btn_ModoCampana",
                stackGo.transform,
                "MODO CAMPAÑA",
                greenBtnSprite,
                iconPlay,
                boldFont,
                36f,
                Color.white,
                98f,
                true
            );

            Button btnControls = CreateMenuButton(
                "Btn_Controles",
                stackGo.transform,
                "CONTROLES",
                glassBtnSprite,
                iconControls,
                boldFont,
                32f,
                SlateTextColor,
                84f,
                false
            );

            Button btnOptions = CreateMenuButton(
                "Btn_Opciones",
                stackGo.transform,
                "OPCIONES",
                glassBtnSprite,
                iconOptions,
                boldFont,
                32f,
                SlateTextColor,
                84f,
                false
            );

            Button btnCredits = CreateMenuButton(
                "Btn_Creditos",
                stackGo.transform,
                "CRÉDITOS",
                glassBtnSprite,
                iconCredits,
                boldFont,
                32f,
                SlateTextColor,
                84f,
                false
            );

            Button btnExit = CreateMenuButton(
                "Btn_Salir",
                stackGo.transform,
                "SALIR",
                glassBtnSprite,
                iconExit,
                boldFont,
                32f,
                SlateTextColor,
                84f,
                false
            );

            // 6. Modal Popups Layer
            GameObject popupsLayer = CreateUIObject("Popups_Layer", canvasGo.transform);
            StretchFull(popupsLayer.GetComponent<RectTransform>());

            UIPopupWindow controlsPopup = CreateControlsPopup(
                popupsLayer.transform, menuController, popupCardSprite, yellowCircleSprite, iconClose, boldFont, regularFont);

            UIPopupWindow optionsPopup = CreateOptionsPopup(
                popupsLayer.transform, menuController, popupCardSprite, glassBtnSprite, greenBtnSprite, yellowCircleSprite, iconClose, boldFont, regularFont);

            UIPopupWindow creditsPopup = CreateCreditsPopup(
                popupsLayer.transform, menuController, popupCardSprite, yellowCircleSprite, iconClose, boldFont, regularFont);

            UIPopupWindow campaignPendingPopup = CreateCampaignPendingPopup(
                popupsLayer.transform, menuController, popupCardSprite, greenBtnSprite, yellowCircleSprite, iconClose, boldFont, regularFont);

            // Wire MainMenuController serialized fields
            SerializedObject menuSo = new SerializedObject(menuController);
            menuSo.FindProperty("scenarioSelectSceneName").stringValue = "02_ScenarioSelect";
            menuSo.FindProperty("campaignButton").objectReferenceValue = btnCampaign;
            menuSo.FindProperty("controlsButton").objectReferenceValue = btnControls;
            menuSo.FindProperty("optionsButton").objectReferenceValue = btnOptions;
            menuSo.FindProperty("creditsButton").objectReferenceValue = btnCredits;
            menuSo.FindProperty("exitButton").objectReferenceValue = btnExit;
            menuSo.FindProperty("controlsPopup").objectReferenceValue = controlsPopup;
            menuSo.FindProperty("optionsPopup").objectReferenceValue = optionsPopup;
            menuSo.FindProperty("creditsPopup").objectReferenceValue = creditsPopup;
            menuSo.FindProperty("campaignPendingPopup").objectReferenceValue = campaignPendingPopup;
            menuSo.ApplyModifiedPropertiesWithoutUndo();

            // Save Scene & Register in Build Settings
            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[EcoDron] Built and saved Main Menu scene at " + ScenePath);
        }

        private static Button CreateMenuButton(
            string name,
            Transform parent,
            string label,
            Sprite bgSprite,
            Sprite iconSprite,
            TMP_FontAsset font,
            float fontSize,
            Color textColor,
            float height,
            bool isPrimary)
        {
            GameObject btnGo = CreateUIObject(name, parent);
            LayoutElement le = btnGo.AddComponent<LayoutElement>();
            le.preferredHeight = height;

            Image bgImg = btnGo.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.04f, 1.04f, 1.04f, 1f);
            colors.pressedColor = new Color(0.88f, 0.92f, 0.90f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            btn.colors = colors;

            btnGo.AddComponent<UIButtonFX>();

            // Icon
            GameObject iconGo = CreateUIObject("Icon", btnGo.transform);
            RectTransform iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(44f, 2f);
            iconRt.sizeDelta = isPrimary ? new Vector2(48f, 48f) : new Vector2(44f, 44f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.sprite = iconSprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            // Label
            GameObject textGo = CreateUIObject("Text_Label", btnGo.transform);
            RectTransform textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0f, 0f);
            textRt.anchorMax = new Vector2(1f, 1f);
            textRt.offsetMin = new Vector2(114f, 6f);
            textRt.offsetMax = new Vector2(-28f, -2f);

            TextMeshProUGUI tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.raycastTarget = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = isPrimary ? 1.5f : 1.0f;

            if (isPrimary)
            {
                tmp.outlineWidth = 0.14f;
                tmp.outlineColor = new Color32(18, 92, 34, 200);
            }

            return btn;
        }

        private static UIPopupWindow CreateControlsPopup(
            Transform parent,
            MainMenuController controller,
            Sprite cardSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            var (popup, card) = CreatePopupShell("Popup_Controls", parent, controller, cardSprite, closeBtnSprite, closeIcon, boldFont, "CONTROLES DEL DRON", 820f, 560f);

            string[,] rows =
            {
                { "W A S D  /  Flechas", "Desplazar el dron horizontalmente" },
                { "Espacio  /  C", "Elevar o descender altitud" },
                { "Shift Izquierdo", "Impulso rápido de vuelo" },
                { "E", "Activar pinza para recoger o soltar residuos" },
                { "Ratón / Stick Der.", "Orientar cámara y apuntado" },
                { "Esc", "Pausar partida o cerrar ventanas" }
            };

            GameObject listGo = CreateUIObject("ControlsList", card.transform);
            RectTransform listRt = listGo.GetComponent<RectTransform>();
            listRt.anchorMin = new Vector2(0f, 0f);
            listRt.anchorMax = new Vector2(1f, 1f);
            listRt.offsetMin = new Vector2(56f, 44f);
            listRt.offsetMax = new Vector2(-56f, -120f);

            VerticalLayoutGroup vlg = listGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            for (int i = 0; i < rows.GetLength(0); i++)
            {
                GameObject row = CreateUIObject($"Row_{i}", listGo.transform);
                LayoutElement le = row.AddComponent<LayoutElement>();
                le.preferredHeight = 52f;

                Image rowBg = row.AddComponent<Image>();
                rowBg.color = i % 2 == 0 ? new Color32(232, 241, 242, 255) : new Color32(242, 247, 247, 255);

                CreateLabel("Key", row.transform, boldFont, rows[i, 0], 24f, EcoGreenColor, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0f, 0f), new Vector2(0.42f, 1f), new Vector2(22f, 0f), Vector2.zero);

                CreateLabel("Action", row.transform, regularFont, rows[i, 1], 24f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                    new Vector2(0.42f, 0f), new Vector2(1f, 1f), new Vector2(10f, 0f), new Vector2(-20f, 0f));
            }

            popup.gameObject.SetActive(false);
            return popup;
        }

        private static UIPopupWindow CreateOptionsPopup(
            Transform parent,
            MainMenuController controller,
            Sprite cardSprite,
            Sprite glassSprite,
            Sprite greenSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            var (popup, card) = CreatePopupShell("Popup_Options", parent, controller, cardSprite, closeBtnSprite, closeIcon, boldFont, "OPCIONES", 840f, 580f);
            OptionsPopupController optionsCtrl = popup.gameObject.AddComponent<OptionsPopupController>();

            GameObject body = CreateUIObject("OptionsBody", card.transform);
            RectTransform bodyRt = body.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(56f, 44f);
            bodyRt.offsetMax = new Vector2(-56f, -120f);

            VerticalLayoutGroup vlg = body.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 24f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            var (masterSlider, masterVal) = CreateSliderRow("Row_MasterVolume", body.transform, "Volumen General", boldFont, glassSprite, greenSprite, closeBtnSprite);
            var (musicSlider, musicVal) = CreateSliderRow("Row_MusicVolume", body.transform, "Música", boldFont, glassSprite, greenSprite, closeBtnSprite);
            var (sfxSlider, sfxVal) = CreateSliderRow("Row_SfxVolume", body.transform, "Efectos (SFX)", boldFont, glassSprite, greenSprite, closeBtnSprite);
            Toggle fullscreenToggle = CreateToggleRow("Row_Fullscreen", body.transform, "Pantalla Completa", boldFont, glassSprite, greenSprite);
            TMP_Dropdown qualityDropdown = CreateDropdownRow("Row_Quality", body.transform, "Calidad Gráfica", boldFont, regularFont, glassSprite, greenSprite);

            SerializedObject optSo = new SerializedObject(optionsCtrl);
            optSo.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
            optSo.FindProperty("masterVolumeLabel").objectReferenceValue = masterVal;
            optSo.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            optSo.FindProperty("musicVolumeLabel").objectReferenceValue = musicVal;
            optSo.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            optSo.FindProperty("sfxVolumeLabel").objectReferenceValue = sfxVal;
            optSo.FindProperty("fullscreenToggle").objectReferenceValue = fullscreenToggle;
            optSo.FindProperty("qualityDropdown").objectReferenceValue = qualityDropdown;
            optSo.ApplyModifiedPropertiesWithoutUndo();

            popup.gameObject.SetActive(false);
            return popup;
        }

        private static UIPopupWindow CreateCreditsPopup(
            Transform parent,
            MainMenuController controller,
            Sprite cardSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            var (popup, card) = CreatePopupShell("Popup_Credits", parent, controller, cardSprite, closeBtnSprite, closeIcon, boldFont, "CRÉDITOS", 800f, 560f);

            string creditsBody =
                "<b>EcoDron: Misión Limpieza</b>\n" +
                "Juego educativo de recolección y cuidado ambiental.\n\n" +
                "<b>Equipo de Desarrollo</b>\n" +
                "• Christopher Brad Del Castillo Montoya\n" +
                "• Julio Rubén Chura Acabaan\n" +
                "• Marco Ramiro Quispe Flores\n" +
                "• David Alfredo Huamani Ollachica\n\n" +
                "<b>Tecnología</b>\n" +
                "Desarrollado en Unity 6 (URP) con TextMeshPro e Input System.";

            CreateLabel("CreditsText", card.transform, regularFont, creditsBody, 24f, SlateTextColor, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(56f, 40f), new Vector2(-56f, -116f));

            popup.gameObject.SetActive(false);
            return popup;
        }

        private static UIPopupWindow CreateCampaignPendingPopup(
            Transform parent,
            MainMenuController controller,
            Sprite cardSprite,
            Sprite greenBtnSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            var (popup, card) = CreatePopupShell("Popup_CampaignPending", parent, controller, cardSprite, closeBtnSprite, closeIcon, boldFont, "MODO CAMPAÑA", 760f, 430f);

            string msg =
                "El selector de escenarios (<b>02_ScenarioSelect</b>) será el siguiente módulo a conectar.\n\n" +
                "Escenarios preparados:\n" +
                "• Escenario 1: El Colegio\n" +
                "• Escenario 2: Los Jardines\n" +
                "• Escenario 3: El Río";

            CreateLabel("MessageText", card.transform, regularFont, msg, 24f, SlateTextColor, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(56f, 120f), new Vector2(-56f, -116f));

            GameObject okGo = CreateUIObject("Btn_Entendido", card.transform);
            RectTransform okRt = okGo.GetComponent<RectTransform>();
            okRt.anchorMin = new Vector2(0.5f, 0f);
            okRt.anchorMax = new Vector2(0.5f, 0f);
            okRt.pivot = new Vector2(0.5f, 0f);
            okRt.anchoredPosition = new Vector2(0f, 34f);
            okRt.sizeDelta = new Vector2(280f, 74f);
            Image okImg = okGo.AddComponent<Image>();
            okImg.sprite = greenBtnSprite;
            okImg.type = Image.Type.Sliced;
            Button okBtn = okGo.AddComponent<Button>();
            okBtn.targetGraphic = okImg;
            okGo.AddComponent<UIButtonFX>();
            UnityEventTools.AddPersistentListener(okBtn.onClick, controller.CloseActivePopup);

            CreateLabel("Label", okGo.transform, boldFont, "ENTENDIDO", 28f, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            popup.gameObject.SetActive(false);
            return popup;
        }

        private static (UIPopupWindow popup, GameObject card) CreatePopupShell(
            string name,
            Transform parent,
            MainMenuController controller,
            Sprite cardSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            string title,
            float width,
            float height)
        {
            GameObject overlay = CreateUIObject(name, parent);
            StretchFull(overlay.GetComponent<RectTransform>());
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color32(10, 24, 32, 165);
            overlay.AddComponent<CanvasGroup>();
            UIPopupWindow popup = overlay.AddComponent<UIPopupWindow>();

            GameObject card = CreateUIObject("Card", overlay.transform);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(width, height);
            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = cardSprite;
            cardImg.type = Image.Type.Sliced;

            // Header Accent Strip
            GameObject header = CreateUIObject("Header", card.transform);
            RectTransform headerRt = header.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -24f);
            headerRt.sizeDelta = new Vector2(-80f, 70f);

            CreateLabel("Title", header.transform, boldFont, title, 36f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, new Vector2(16f, 0f), new Vector2(-80f, 0f));

            // Close Button
            GameObject closeGo = CreateUIObject("Btn_Close", card.transform);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-28f, -24f);
            closeRt.sizeDelta = new Vector2(64f, 64f);
            Image closeBg = closeGo.AddComponent<Image>();
            closeBg.sprite = closeBtnSprite;
            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeGo.AddComponent<UIButtonFX>();
            UnityEventTools.AddPersistentListener(closeBtn.onClick, controller.CloseActivePopup);

            GameObject closeIconGo = CreateUIObject("Icon_X", closeGo.transform);
            RectTransform closeIconRt = closeIconGo.GetComponent<RectTransform>();
            closeIconRt.anchorMin = new Vector2(0.24f, 0.24f);
            closeIconRt.anchorMax = new Vector2(0.76f, 0.76f);
            closeIconRt.offsetMin = Vector2.zero;
            closeIconRt.offsetMax = Vector2.zero;
            Image closeIconImg = closeIconGo.AddComponent<Image>();
            closeIconImg.sprite = closeIcon;
            closeIconImg.preserveAspect = true;
            closeIconImg.raycastTarget = false;

            SerializedObject popSo = new SerializedObject(popup);
            popSo.FindProperty("cardTransform").objectReferenceValue = cardRt;
            popSo.ApplyModifiedPropertiesWithoutUndo();

            return (popup, card);
        }

        private static (Slider slider, TextMeshProUGUI valueLabel) CreateSliderRow(
            string name,
            Transform parent,
            string title,
            TMP_FontAsset boldFont,
            Sprite trackSprite,
            Sprite fillSprite,
            Sprite handleSprite)
        {
            GameObject row = CreateUIObject(name, parent);
            row.AddComponent<LayoutElement>().preferredHeight = 56f;

            CreateLabel("Label", row.transform, boldFont, title, 25f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.38f, 1f), Vector2.zero, Vector2.zero);

            GameObject sliderGo = CreateUIObject("Slider", row.transform);
            RectTransform sliderRt = sliderGo.GetComponent<RectTransform>();
            sliderRt.anchorMin = new Vector2(0.40f, 0.5f);
            sliderRt.anchorMax = new Vector2(0.86f, 0.5f);
            sliderRt.sizeDelta = new Vector2(0f, 24f);

            // Track
            GameObject bgGo = CreateUIObject("Background", sliderGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = trackSprite;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = new Color32(208, 224, 226, 255);

            // Fill Area
            GameObject fillArea = CreateUIObject("Fill Area", sliderGo.transform);
            StretchFull(fillArea.GetComponent<RectTransform>());
            GameObject fill = CreateUIObject("Fill", fillArea.transform);
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            StretchFull(fillRt);
            Image fillImg = fill.AddComponent<Image>();
            fillImg.sprite = fillSprite;
            fillImg.type = Image.Type.Sliced;

            // Handle Slide Area
            GameObject handleArea = CreateUIObject("Handle Slide Area", sliderGo.transform);
            RectTransform handleAreaRt = handleArea.GetComponent<RectTransform>();
            StretchFull(handleAreaRt);
            handleAreaRt.offsetMin = new Vector2(16f, -7f);
            handleAreaRt.offsetMax = new Vector2(-16f, 7f);

            GameObject handle = CreateUIObject("Handle", handleArea.transform);
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(38f, 38f);
            Image handleImg = handle.AddComponent<Image>();
            handleImg.sprite = handleSprite;
            AspectRatioFitter handleAspect = handle.AddComponent<AspectRatioFitter>();
            handleAspect.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            handleAspect.aspectRatio = 1f;

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.targetGraphic = handleImg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.85f;

            TextMeshProUGUI valLabel = CreateLabel("ValueLabel", row.transform, boldFont, "85%", 24f, MutedSlateColor, TextAlignmentOptions.MidlineRight,
                new Vector2(0.87f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

            return (slider, valLabel);
        }

        private static Toggle CreateToggleRow(
            string name,
            Transform parent,
            string title,
            TMP_FontAsset boldFont,
            Sprite boxSprite,
            Sprite checkSprite)
        {
            GameObject row = CreateUIObject(name, parent);
            row.AddComponent<LayoutElement>().preferredHeight = 56f;

            CreateLabel("Label", row.transform, boldFont, title, 25f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.65f, 1f), Vector2.zero, Vector2.zero);

            GameObject toggleGo = CreateUIObject("Toggle", row.transform);
            RectTransform toggleRt = toggleGo.GetComponent<RectTransform>();
            toggleRt.anchorMin = new Vector2(1f, 0.5f);
            toggleRt.anchorMax = new Vector2(1f, 0.5f);
            toggleRt.pivot = new Vector2(1f, 0.5f);
            toggleRt.sizeDelta = new Vector2(54f, 54f);

            Image boxImg = toggleGo.AddComponent<Image>();
            boxImg.sprite = boxSprite;
            boxImg.type = Image.Type.Sliced;
            boxImg.color = new Color32(215, 229, 231, 255);

            GameObject checkGo = CreateUIObject("Checkmark", toggleGo.transform);
            RectTransform checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.18f, 0.18f);
            checkRt.anchorMax = new Vector2(0.82f, 0.82f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;
            Image checkImg = checkGo.AddComponent<Image>();
            checkImg.sprite = checkSprite;
            checkImg.type = Image.Type.Sliced;

            Toggle toggle = toggleGo.AddComponent<Toggle>();
            toggle.targetGraphic = boxImg;
            toggle.graphic = checkImg;
            toggle.isOn = Screen.fullScreen;

            return toggle;
        }

        private static TMP_Dropdown CreateDropdownRow(
            string name,
            Transform parent,
            string title,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont,
            Sprite boxSprite,
            Sprite checkSprite)
        {
            GameObject row = CreateUIObject(name, parent);
            row.AddComponent<LayoutElement>().preferredHeight = 58f;

            CreateLabel("Label", row.transform, boldFont, title, 25f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f), new Vector2(0.38f, 1f), Vector2.zero, Vector2.zero);

            GameObject ddGo = CreateUIObject("Dropdown", row.transform);
            RectTransform ddRt = ddGo.GetComponent<RectTransform>();
            ddRt.anchorMin = new Vector2(0.40f, 0f);
            ddRt.anchorMax = new Vector2(1f, 1f);
            ddRt.offsetMin = Vector2.zero;
            ddRt.offsetMax = Vector2.zero;

            Image ddBg = ddGo.AddComponent<Image>();
            ddBg.sprite = boxSprite;
            ddBg.type = Image.Type.Sliced;
            ddBg.color = new Color32(226, 238, 240, 255);

            TextMeshProUGUI caption = CreateLabel("Label", ddGo.transform, boldFont, "PC", 24f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-46f, 0f));

            CreateLabel("Arrow", ddGo.transform, boldFont, "v", 22f, SlateTextColor, TextAlignmentOptions.Center,
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-44f, 0f), new Vector2(-12f, 0f));

            // Template
            GameObject templateGo = CreateUIObject("Template", ddGo.transform);
            RectTransform templateRt = templateGo.GetComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0f, 0f);
            templateRt.anchorMax = new Vector2(1f, 0f);
            templateRt.pivot = new Vector2(0.5f, 1f);
            templateRt.anchoredPosition = new Vector2(0f, -4f);
            templateRt.sizeDelta = new Vector2(0f, 150f);
            Image templateBg = templateGo.AddComponent<Image>();
            templateBg.sprite = boxSprite;
            templateBg.type = Image.Type.Sliced;
            ScrollRect scrollRect = templateGo.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = CreateUIObject("Viewport", templateGo.transform);
            RectTransform viewportRt = viewport.GetComponent<RectTransform>();
            StretchFull(viewportRt);
            viewportRt.offsetMin = new Vector2(6f, 6f);
            viewportRt.offsetMax = new Vector2(-6f, -6f);
            viewport.AddComponent<RectMask2D>();

            GameObject content = CreateUIObject("Content", viewport.transform);
            RectTransform contentRt = content.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.sizeDelta = new Vector2(0f, 48f);

            GameObject item = CreateUIObject("Item", content.transform);
            RectTransform itemRt = item.GetComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0f, 0.5f);
            itemRt.anchorMax = new Vector2(1f, 0.5f);
            itemRt.sizeDelta = new Vector2(0f, 46f);
            Toggle itemToggle = item.AddComponent<Toggle>();

            GameObject itemBg = CreateUIObject("Item Background", item.transform);
            StretchFull(itemBg.GetComponent<RectTransform>());
            Image itemBgImg = itemBg.AddComponent<Image>();
            itemBgImg.color = new Color32(236, 245, 246, 255);

            GameObject itemCheck = CreateUIObject("Item Checkmark", item.transform);
            RectTransform itemCheckRt = itemCheck.GetComponent<RectTransform>();
            itemCheckRt.anchorMin = new Vector2(0f, 0.5f);
            itemCheckRt.anchorMax = new Vector2(0f, 0.5f);
            itemCheckRt.anchoredPosition = new Vector2(22f, 0f);
            itemCheckRt.sizeDelta = new Vector2(18f, 18f);
            Image itemCheckImg = itemCheck.AddComponent<Image>();
            itemCheckImg.sprite = checkSprite;

            TextMeshProUGUI itemLabel = CreateLabel("Item Label", item.transform, regularFont, "Option", 22f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                Vector2.zero, Vector2.one, new Vector2(44f, 0f), new Vector2(-12f, 0f));

            itemToggle.targetGraphic = itemBgImg;
            itemToggle.graphic = itemCheckImg;
            itemToggle.isOn = true;

            scrollRect.viewport = viewportRt;
            scrollRect.content = contentRt;

            TMP_Dropdown dropdown = ddGo.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = ddBg;
            dropdown.template = templateRt;
            dropdown.captionText = caption;
            dropdown.itemText = itemLabel;
            templateGo.SetActive(false);

            return dropdown;
        }

        private static TextMeshProUGUI CreateLabel(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string text,
            float size,
            Color color,
            TextAlignmentOptions align,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject go = CreateUIObject(name, parent);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Sprite LoadSprite(string path)
        {
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) Debug.LogWarning("[EcoDron] Sprite not found at " + path);
            return s;
        }

        private static void ImportTmpEssentialsIfNeeded()
        {
            if (AssetDatabase.IsValidFolder("Assets/TextMesh Pro")) return;

            string pkgPath = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
            if (File.Exists(pkgPath))
            {
                AssetDatabase.ImportPackage(pkgPath, false);
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("[EcoDron] TMP Essential Resources package not found at: " + pkgPath);
            }
        }

        private static void ConfigureSprites()
        {
            var borders = new Dictionary<string, Vector4>
            {
                { "Assets/_Project/Textures/UI/Shapes/Btn_Primary_Green.png", new Vector4(48f, 48f, 48f, 48f) },
                { "Assets/_Project/Textures/UI/Shapes/Btn_Secondary_Glass.png", new Vector4(44f, 44f, 44f, 44f) },
                { "Assets/_Project/Textures/UI/Shapes/Popup_Card.png", new Vector4(46f, 46f, 46f, 46f) }
            };

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project/Textures" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }
                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    dirty = true;
                }
                if (importer.maxTextureSize < 2048)
                {
                    importer.maxTextureSize = 2048;
                    dirty = true;
                }
                if (borders.TryGetValue(path, out Vector4 border) && importer.spriteBorder != border)
                {
                    importer.spriteBorder = border;
                    dirty = true;
                }

                if (dirty) importer.SaveAndReimport();
            }
        }

        private static void CreateTmpFontAsset(string ttfPath, string sdfPath)
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath) != null) return;

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                Debug.LogError("[EcoDron] Font not found at: " + ttfPath);
                return;
            }

            TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true
            );
            tmpFont.name = Path.GetFileNameWithoutExtension(sdfPath);

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,;:!?¡¿+-*/=()[]{}%°@#&_\"'•ÁÉÍÓÚÜÑáéíóúüñ";
            tmpFont.TryAddCharacters(chars, out _);

            AssetDatabase.CreateAsset(tmpFont, sdfPath);
            if (tmpFont.atlasTextures != null && tmpFont.atlasTextures.Length > 0 && tmpFont.atlasTextures[0] != null)
            {
                tmpFont.atlasTextures[0].name = tmpFont.name + " Atlas";
                AssetDatabase.AddObjectToAsset(tmpFont.atlasTextures[0], tmpFont);
            }
            if (tmpFont.material != null)
            {
                tmpFont.material.name = tmpFont.name + " Material";
                AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
            }
        }

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
