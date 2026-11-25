using System.Collections.Generic;
using EcoDron.Data;
using EcoDron.UI.Common;
using EcoDron.UI.ScenarioSelect;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoDron.EditorTools
{
    /// <summary>
    /// Creates campaign ScriptableObjects and builds the Scenario Select scene matching 02 escenarios.jpg.
    /// </summary>
    public static class ScenarioSelectBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/02_ScenarioSelect.unity";
        private const string DatabasePath = "Assets/_Project/Data/CampaignDatabase.asset";
        private const string BoldFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Bold SDF.asset";
        private const string RegularFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Regular SDF.asset";

        private static readonly Color SlateTextColor = new Color32(22, 50, 59, 255);

        [MenuItem("EcoDron/3. Build Scenario Select Scene")]
        public static void BuildScenarioSelectScene()
        {
            ConfigureScenarioSprites();

            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontSdfPath);
            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontSdfPath);
            if (boldFont == null || regularFont == null)
            {
                Debug.LogError("[EcoDron] Missing TMP_FontAsset. Run 'EcoDron/1. Setup UI Assets & TMP Fonts' first.");
                return;
            }

            CampaignDatabaseSO database = CreateOrLoadCampaignData();

            Sprite bgSprite = LoadSprite("Assets/_Project/Textures/UI/Backgrounds/BG_Colegio.jpg");
            Sprite backBtnSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Yellow_Circle.png");
            Sprite backArrowIcon = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_BackArrow.png");
            Sprite cardWhiteSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Card_White.png");
            Sprite cardYellowSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Card_ActiveYellow.png");
            Sprite cardLockedSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Card_LockedSlate.png");
            Sprite roundedMaskSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Rounded_Mask.png");
            Sprite lockIcon = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Lock.png");
            Sprite starFilled = LoadSprite("Assets/_Project/Textures/UI/Icons/Star_Filled.png");
            Sprite blueBtnSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Blue_Repeat.png");
            Sprite greenBtnSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Primary_Green.png");
            Sprite popupCardSprite = LoadSprite("Assets/_Project/Textures/UI/Shapes/Popup_Card.png");
            Sprite closeIcon = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Close.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(105, 175, 212, 255);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1.5f, -10f);

            // EventSystem
            GameObject eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // Canvas
            GameObject canvasGo = new GameObject("Canvas_ScenarioSelect");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.7f;
            canvasGo.AddComponent<GraphicRaycaster>();

            ScenarioSelectController controller = canvasGo.AddComponent<ScenarioSelectController>();

            // 1. Background + subtle contrast tint
            GameObject bgGo = CreateUIObject("Background_Escenarios", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bgSprite;
            bgImg.raycastTarget = false;
            AspectRatioFitter bgFitter = bgGo.AddComponent<AspectRatioFitter>();
            bgFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bgFitter.aspectRatio = bgSprite != null ? bgSprite.rect.width / bgSprite.rect.height : 16f / 9f;

            GameObject dimBg = CreateUIObject("Background_Overlay", canvasGo.transform);
            StretchFull(dimBg.GetComponent<RectTransform>());
            Image dimImg = dimBg.AddComponent<Image>();
            dimImg.color = new Color32(16, 36, 46, 75);
            dimImg.raycastTarget = false;

            // 2. Top Header (Back Button + Title)
            GameObject backGo = CreateUIObject("Btn_VolverMenu", canvasGo.transform);
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 1f);
            backRt.anchorMax = new Vector2(0f, 1f);
            backRt.pivot = new Vector2(0f, 1f);
            backRt.anchoredPosition = new Vector2(48f, -42f);
            backRt.sizeDelta = new Vector2(92f, 92f);
            Image backBg = backGo.AddComponent<Image>();
            backBg.sprite = backBtnSprite;
            Button backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backBg;
            backGo.AddComponent<UIButtonFX>();

            GameObject backArrowGo = CreateUIObject("Icon_BackArrow", backGo.transform);
            RectTransform backArrowRt = backArrowGo.GetComponent<RectTransform>();
            backArrowRt.anchorMin = new Vector2(0.18f, 0.18f);
            backArrowRt.anchorMax = new Vector2(0.82f, 0.82f);
            backArrowRt.offsetMin = Vector2.zero;
            backArrowRt.offsetMax = Vector2.zero;
            Image backArrowImg = backArrowGo.AddComponent<Image>();
            backArrowImg.sprite = backArrowIcon;
            backArrowImg.preserveAspect = true;
            backArrowImg.raycastTarget = false;

            TextMeshProUGUI headerTmp = CreateLabel(
                "Title_Header",
                canvasGo.transform,
                boldFont,
                "MODO CAMPAÑA - ELIGE UN ESCENARIO",
                52f,
                Color.white,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                Vector2.zero
            );
            RectTransform headerRt = headerTmp.GetComponent<RectTransform>();
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -54f);
            headerRt.sizeDelta = new Vector2(1380f, 80f);
            headerTmp.characterSpacing = 1.5f;
            headerTmp.outlineWidth = 0.26f;
            headerTmp.outlineColor = new Color32(14, 30, 38, 255);

            // 3. Scenarios Cards Row
            GameObject cardsRow = CreateUIObject("Scenarios_Container", canvasGo.transform);
            RectTransform rowRt = cardsRow.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.47f);
            rowRt.anchorMax = new Vector2(0.5f, 0.47f);
            rowRt.sizeDelta = new Vector2(1740f, 670f);

            HorizontalLayoutGroup hlg = cardsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 42f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            var cardList = new List<ScenarioCardUI>();
            for (int i = 0; i < 3; i++)
            {
                ScenarioCardUI card = CreateScenarioCard(
                    $"ScenarioCard_{i + 1}",
                    cardsRow.transform,
                    cardWhiteSprite,
                    cardYellowSprite,
                    cardLockedSprite,
                    roundedMaskSprite,
                    lockIcon,
                    starFilled,
                    blueBtnSprite,
                    greenBtnSprite,
                    boldFont
                );
                cardList.Add(card);
            }

            // 4. Pending Level Route Popup
            var (pendingPopup, pendingMsg) = CreatePendingRoutePopup(
                canvasGo.transform, controller, popupCardSprite, greenBtnSprite, backBtnSprite, closeIcon, boldFont, regularFont);

            // Wire ScenarioSelectController
            SerializedObject ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("campaignDatabase").objectReferenceValue = database;
            ctrlSo.FindProperty("mainMenuSceneName").stringValue = "01_MainMenu";
            ctrlSo.FindProperty("levelRouteSceneName").stringValue = "03_LevelRoute";
            ctrlSo.FindProperty("backButton").objectReferenceValue = backBtn;
            ctrlSo.FindProperty("previewReferenceStates").boolValue = true;
            ctrlSo.FindProperty("routePendingPopup").objectReferenceValue = pendingPopup;
            ctrlSo.FindProperty("routePendingMessageText").objectReferenceValue = pendingMsg;

            SerializedProperty cardsProp = ctrlSo.FindProperty("scenarioCards");
            cardsProp.ClearArray();
            for (int i = 0; i < cardList.Count; i++)
            {
                cardsProp.InsertArrayElementAtIndex(i);
                cardsProp.GetArrayElementAtIndex(i).objectReferenceValue = cardList[i];
            }
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            // Pre-populate cards in Edit Mode so the scene previews all 3 states immediately
            controller.RefreshCards();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[EcoDron] Built and saved Scenario Select scene at " + ScenePath);
        }

        private static ScenarioCardUI CreateScenarioCard(
            string name,
            Transform parent,
            Sprite whiteCardSprite,
            Sprite yellowCardSprite,
            Sprite lockedCardSprite,
            Sprite maskSprite,
            Sprite lockIcon,
            Sprite starFilled,
            Sprite repeatBtnSprite,
            Sprite enterBtnSprite,
            TMP_FontAsset boldFont)
        {
            GameObject cardGo = CreateUIObject(name, parent);
            RectTransform cardRt = cardGo.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(536f, 646f);

            Image cardBg = cardGo.AddComponent<Image>();
            cardBg.sprite = whiteCardSprite;
            cardBg.type = Image.Type.Sliced;

            ScenarioCardUI cardUi = cardGo.AddComponent<ScenarioCardUI>();

            // Card Header Title
            TextMeshProUGUI titleTmp = CreateLabel(
                "Text_Title",
                cardGo.transform,
                boldFont,
                "ESCENARIO",
                31f,
                SlateTextColor,
                TextAlignmentOptions.Center,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(28f, -94f),
                new Vector2(-28f, -26f)
            );

            // Thumbnail with Rounded Mask
            GameObject thumbMaskGo = CreateUIObject("Thumbnail_Mask", cardGo.transform);
            RectTransform maskRt = thumbMaskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = new Vector2(0.5f, 1f);
            maskRt.anchorMax = new Vector2(0.5f, 1f);
            maskRt.pivot = new Vector2(0.5f, 1f);
            maskRt.anchoredPosition = new Vector2(0f, -104f);
            maskRt.sizeDelta = new Vector2(464f, 326f);

            Image maskImg = thumbMaskGo.AddComponent<Image>();
            maskImg.sprite = maskSprite;
            maskImg.type = Image.Type.Sliced;
            Mask mask = thumbMaskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject thumbGo = CreateUIObject("Img_Thumbnail", thumbMaskGo.transform);
            StretchFull(thumbGo.GetComponent<RectTransform>());
            Image thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.preserveAspect = false;
            thumbImg.raycastTarget = false;

            // Locked Overlay inside Mask
            GameObject lockedOverlay = CreateUIObject("Locked_Overlay", thumbMaskGo.transform);
            StretchFull(lockedOverlay.GetComponent<RectTransform>());
            Image lockDim = lockedOverlay.AddComponent<Image>();
            lockDim.color = new Color32(44, 62, 74, 155);
            lockDim.raycastTarget = false;

            GameObject lockIconGo = CreateUIObject("Icon_Lock", lockedOverlay.transform);
            RectTransform lockIconRt = lockIconGo.GetComponent<RectTransform>();
            lockIconRt.anchorMin = new Vector2(0.5f, 0.58f);
            lockIconRt.anchorMax = new Vector2(0.5f, 0.58f);
            lockIconRt.sizeDelta = new Vector2(138f, 138f);
            Image lockIconImg = lockIconGo.AddComponent<Image>();
            lockIconImg.sprite = lockIcon;
            lockIconImg.preserveAspect = true;
            lockIconImg.raycastTarget = false;

            TextMeshProUGUI lockedLabel = CreateLabel(
                "Text_Bloqueado",
                lockedOverlay.transform,
                boldFont,
                "BLOQUEADO",
                36f,
                Color.white,
                TextAlignmentOptions.Center,
                new Vector2(0f, 0.08f),
                new Vector2(1f, 0.34f),
                Vector2.zero,
                Vector2.zero
            );
            lockedLabel.characterSpacing = 1.5f;
            lockedLabel.outlineWidth = 0.2f;
            lockedLabel.outlineColor = new Color32(20, 36, 44, 235);

            // Progress Row
            GameObject progressRow = CreateUIObject("Progress_Row", cardGo.transform);
            RectTransform progRt = progressRow.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(0f, 0f);
            progRt.anchorMax = new Vector2(1f, 0f);
            progRt.pivot = new Vector2(0.5f, 0f);
            progRt.anchoredPosition = new Vector2(0f, 146f);
            progRt.sizeDelta = new Vector2(-64f, 54f);

            HorizontalLayoutGroup progHlg = progressRow.AddComponent<HorizontalLayoutGroup>();
            progHlg.spacing = 14f;
            progHlg.childAlignment = TextAnchor.MiddleCenter;
            progHlg.childControlWidth = false;
            progHlg.childControlHeight = false;
            progHlg.childForceExpandWidth = false;
            progHlg.childForceExpandHeight = false;

            GameObject progTextGo = CreateUIObject("Text_Progress", progressRow.transform);
            progTextGo.GetComponent<RectTransform>().sizeDelta = new Vector2(276f, 46f);
            TextMeshProUGUI progTmp = progTextGo.AddComponent<TextMeshProUGUI>();
            progTmp.font = boldFont;
            progTmp.text = "3 / 3 COMPLETADO";
            progTmp.fontSize = 27f;
            progTmp.color = SlateTextColor;
            progTmp.alignment = TextAlignmentOptions.Center;
            progTmp.raycastTarget = false;

            GameObject starsGo = CreateUIObject("Stars_Container", progressRow.transform);
            starsGo.GetComponent<RectTransform>().sizeDelta = new Vector2(126f, 42f);
            HorizontalLayoutGroup starsHlg = starsGo.AddComponent<HorizontalLayoutGroup>();
            starsHlg.spacing = 6f;
            starsHlg.childAlignment = TextAnchor.MiddleCenter;
            starsHlg.childControlWidth = false;
            starsHlg.childControlHeight = false;

            for (int s = 0; s < 3; s++)
            {
                GameObject starGo = CreateUIObject($"Star_{s + 1}", starsGo.transform);
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(36f, 36f);
                Image starImg = starGo.AddComponent<Image>();
                starImg.sprite = starFilled;
                starImg.preserveAspect = true;
                starImg.raycastTarget = false;
            }

            // Action Button
            GameObject btnGo = CreateUIObject("Btn_Action", cardGo.transform);
            RectTransform btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 44f);
            btnRt.sizeDelta = new Vector2(276f, 82f);

            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = enterBtnSprite;
            btnImg.type = Image.Type.Sliced;

            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btnGo.AddComponent<UIButtonFX>();

            TextMeshProUGUI btnTmp = CreateLabel(
                "Text_Action",
                btnGo.transform,
                boldFont,
                "ENTRAR",
                32f,
                Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(12f, 4f),
                new Vector2(-12f, 0f)
            );
            btnTmp.characterSpacing = 1.5f;

            // Wire ScenarioCardUI serialized fields
            SerializedObject cardSo = new SerializedObject(cardUi);
            cardSo.FindProperty("cardBackground").objectReferenceValue = cardBg;
            cardSo.FindProperty("completedCardSprite").objectReferenceValue = whiteCardSprite;
            cardSo.FindProperty("activeCardSprite").objectReferenceValue = yellowCardSprite;
            cardSo.FindProperty("lockedCardSprite").objectReferenceValue = lockedCardSprite;
            cardSo.FindProperty("titleText").objectReferenceValue = titleTmp;
            cardSo.FindProperty("thumbnailImage").objectReferenceValue = thumbImg;
            cardSo.FindProperty("lockedOverlay").objectReferenceValue = lockedOverlay;
            cardSo.FindProperty("progressRow").objectReferenceValue = progressRow;
            cardSo.FindProperty("progressText").objectReferenceValue = progTmp;
            cardSo.FindProperty("starsContainer").objectReferenceValue = starsGo;
            cardSo.FindProperty("actionButton").objectReferenceValue = btn;
            cardSo.FindProperty("actionButtonImage").objectReferenceValue = btnImg;
            cardSo.FindProperty("actionButtonText").objectReferenceValue = btnTmp;
            cardSo.FindProperty("repeatButtonSprite").objectReferenceValue = repeatBtnSprite;
            cardSo.FindProperty("enterButtonSprite").objectReferenceValue = enterBtnSprite;
            cardSo.ApplyModifiedPropertiesWithoutUndo();

            return cardUi;
        }

        private static (UIPopupWindow popup, TextMeshProUGUI msgText) CreatePendingRoutePopup(
            Transform parent,
            ScenarioSelectController controller,
            Sprite cardSprite,
            Sprite greenBtnSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            GameObject overlay = CreateUIObject("Popup_RoutePending", parent);
            StretchFull(overlay.GetComponent<RectTransform>());
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color32(10, 24, 32, 165);
            overlay.AddComponent<CanvasGroup>();
            UIPopupWindow popup = overlay.AddComponent<UIPopupWindow>();

            GameObject card = CreateUIObject("Card", overlay.transform);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(760f, 410f);
            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = cardSprite;
            cardImg.type = Image.Type.Sliced;

            CreateLabel("Title", card.transform, boldFont, "RUTA DE NIVELES", 34f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -92f), new Vector2(-100f, -24f));

            GameObject closeGo = CreateUIObject("Btn_Close", card.transform);
            RectTransform closeRt = closeGo.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-26f, -22f);
            closeRt.sizeDelta = new Vector2(60f, 60f);
            Image closeBg = closeGo.AddComponent<Image>();
            closeBg.sprite = closeBtnSprite;
            Button closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeBg;
            closeGo.AddComponent<UIButtonFX>();
            UnityEventTools.AddPersistentListener(closeBtn.onClick, controller.ClosePendingPopup);

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

            TextMeshProUGUI msgTmp = CreateLabel("MessageText", card.transform, regularFont, "", 24f, SlateTextColor, TextAlignmentOptions.Center,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(54f, 118f), new Vector2(-54f, -106f));

            GameObject okGo = CreateUIObject("Btn_Entendido", card.transform);
            RectTransform okRt = okGo.GetComponent<RectTransform>();
            okRt.anchorMin = new Vector2(0.5f, 0f);
            okRt.anchorMax = new Vector2(0.5f, 0f);
            okRt.pivot = new Vector2(0.5f, 0f);
            okRt.anchoredPosition = new Vector2(0f, 34f);
            okRt.sizeDelta = new Vector2(270f, 72f);
            Image okImg = okGo.AddComponent<Image>();
            okImg.sprite = greenBtnSprite;
            okImg.type = Image.Type.Sliced;
            Button okBtn = okGo.AddComponent<Button>();
            okBtn.targetGraphic = okImg;
            okGo.AddComponent<UIButtonFX>();
            UnityEventTools.AddPersistentListener(okBtn.onClick, controller.ClosePendingPopup);

            CreateLabel("Label", okGo.transform, boldFont, "ENTENDIDO", 28f, Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            SerializedObject popSo = new SerializedObject(popup);
            popSo.FindProperty("cardTransform").objectReferenceValue = cardRt;
            popSo.ApplyModifiedPropertiesWithoutUndo();

            overlay.SetActive(false);
            return (popup, msgTmp);
        }

        private static CampaignDatabaseSO CreateOrLoadCampaignData()
        {
            Sprite thumbColegio = LoadSprite("Assets/_Project/Textures/Thumbnails/Scenarios/Thumb_Colegio.jpg");
            Sprite thumbJardines = LoadSprite("Assets/_Project/Textures/Thumbnails/Scenarios/Thumb_Jardines.png");
            Sprite thumbRio = LoadSprite("Assets/_Project/Textures/Thumbnails/Scenarios/Thumb_Rio.png");

            Sprite bgColegio = LoadSprite("Assets/_Project/Textures/UI/Backgrounds/BG_Colegio.jpg");
            Sprite bgJardines = LoadSprite("Assets/_Project/Textures/UI/Backgrounds/BG_Jardines.png");
            Sprite bgRio = LoadSprite("Assets/_Project/Textures/UI/Backgrounds/BG_Rio.png");

            ScenarioDataSO s1 = CreateOrUpdateScenario(
                "Assets/_Project/Data/Scenarios/Scenario_01_Colegio.asset",
                "scenario_01", 1, "ESCENARIO 1: EL COLEGIO", "El Colegio", true, thumbColegio, bgColegio,
                new[]
                {
                    ("1.1", 1, "El Patio Principal", 6, 90, "Level_1_1", thumbColegio),
                    ("1.2", 2, "Las Canchas", 8, 100, "Level_1_2", thumbColegio),
                    ("1.3", 3, "La Entrada Escolar", 10, 110, "Level_1_3", thumbColegio)
                });

            ScenarioDataSO s2 = CreateOrUpdateScenario(
                "Assets/_Project/Data/Scenarios/Scenario_02_Jardines.asset",
                "scenario_02", 2, "ESCENARIO 2: LOS JARDINES", "Los Jardines", false, thumbJardines, bgJardines,
                new[]
                {
                    ("2.1", 1, "El Sendero de los Arbustos", 8, 95, "Level_2_1", thumbJardines),
                    ("2.2", 2, "La Arboleda Frondosa", 10, 110, "Level_2_2", thumbJardines),
                    ("2.3", 3, "El Invernadero", 12, 120, "Level_2_3", thumbJardines)
                });

            ScenarioDataSO s3 = CreateOrUpdateScenario(
                "Assets/_Project/Data/Scenarios/Scenario_03_Rio.asset",
                "scenario_03", 3, "ESCENARIO 3: EL RÍO", "El Río", false, thumbRio, bgRio,
                new[]
                {
                    ("3.1", 1, "La Orilla Norte", 10, 110, "Level_3_1", thumbRio),
                    ("3.2", 2, "El Puente de Piedra", 12, 120, "Level_3_2", thumbRio),
                    ("3.3", 3, "La Desembocadura", 15, 140, "Level_3_3", thumbRio)
                });

            CampaignDatabaseSO db = AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CampaignDatabaseSO>();
                AssetDatabase.CreateAsset(db, DatabasePath);
            }
            db.scenarios = new List<ScenarioDataSO> { s1, s2, s3 };
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath) ?? db;
        }

        private static ScenarioDataSO CreateOrUpdateScenario(
            string scenarioPath,
            string id,
            int number,
            string headerTitle,
            string shortName,
            bool unlockedByDefault,
            Sprite cardThumb,
            Sprite routeBg,
            (string id, int num, string title, int waste, int time, string scene, Sprite thumb)[] levelSpecs)
        {
            var levels = new List<LevelDataSO>();
            foreach (var spec in levelSpecs)
            {
                string levelPath = $"Assets/_Project/Data/Levels/Level_{spec.id.Replace('.', '_')}.asset";
                LevelDataSO lvl = AssetDatabase.LoadAssetAtPath<LevelDataSO>(levelPath);
                if (lvl == null)
                {
                    lvl = ScriptableObject.CreateInstance<LevelDataSO>();
                    AssetDatabase.CreateAsset(lvl, levelPath);
                }
                lvl.levelId = spec.id;
                lvl.levelNumber = spec.num;
                lvl.displayName = spec.title;
                lvl.wasteCount = spec.waste;
                lvl.timeLimitSeconds = spec.time;
                lvl.sceneName = spec.scene;
                lvl.thumbnail = spec.thumb;
                EditorUtility.SetDirty(lvl);
                levels.Add(lvl);
            }

            ScenarioDataSO scenario = AssetDatabase.LoadAssetAtPath<ScenarioDataSO>(scenarioPath);
            if (scenario == null)
            {
                scenario = ScriptableObject.CreateInstance<ScenarioDataSO>();
                AssetDatabase.CreateAsset(scenario, scenarioPath);
            }
            scenario.scenarioId = id;
            scenario.scenarioNumber = number;
            scenario.headerTitle = headerTitle;
            scenario.shortName = shortName;
            scenario.unlockedByDefault = unlockedByDefault;
            scenario.cardThumbnail = cardThumb;
            scenario.routeBackground = routeBg;
            scenario.levels = levels;
            EditorUtility.SetDirty(scenario);
            return scenario;
        }

        private static void ConfigureScenarioSprites()
        {
            var borders = new Dictionary<string, Vector4>
            {
                { "Assets/_Project/Textures/UI/Shapes/Card_White.png", new Vector4(56f, 56f, 56f, 56f) },
                { "Assets/_Project/Textures/UI/Shapes/Card_ActiveYellow.png", new Vector4(56f, 56f, 56f, 56f) },
                { "Assets/_Project/Textures/UI/Shapes/Card_LockedSlate.png", new Vector4(56f, 56f, 56f, 56f) },
                { "Assets/_Project/Textures/UI/Shapes/Rounded_Mask.png", new Vector4(36f, 36f, 36f, 36f) },
                { "Assets/_Project/Textures/UI/Shapes/Btn_Blue_Repeat.png", new Vector4(44f, 44f, 44f, 44f) }
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

        private static void RegisterSceneInBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == scenePath);
            int insertIndex = Mathf.Min(1, scenes.Count);
            scenes.Insert(insertIndex, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
