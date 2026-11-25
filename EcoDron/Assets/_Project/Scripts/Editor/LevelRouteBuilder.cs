using System.Collections.Generic;
using EcoDron.Data;
using EcoDron.UI.Common;
using EcoDron.UI.LevelRoute;
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
    /// Builds the Level Route scene (03_LevelRoute.unity) matching 03 ruta niveles (2).jpg.
    /// </summary>
    public static class LevelRouteBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/03_LevelRoute.unity";
        private const string DatabasePath = "Assets/_Project/Data/CampaignDatabase.asset";
        private const string BoldFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Bold SDF.asset";
        private const string RegularFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Regular SDF.asset";

        private static readonly Color SlateTextColor = new Color32(22, 50, 59, 255);
        private static readonly Color MutedSlateColor = new Color32(58, 88, 96, 255);

        [MenuItem("EcoDron/4. Build Level Route Scene")]
        public static void BuildLevelRouteScene()
        {
            ConfigureRouteSprites();

            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontSdfPath);
            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontSdfPath);
            if (boldFont == null || regularFont == null)
            {
                Debug.LogError("[EcoDron] Missing TMP_FontAsset. Run 'EcoDron/1. Setup UI Assets & TMP Fonts' first.");
                return;
            }

            CampaignDatabaseSO database = AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("[EcoDron] Missing CampaignDatabase.asset. Run 'EcoDron/3. Build Scenario Select Scene' first.");
                return;
            }

            Sprite bgDefault = LoadSprite("Assets/_Project/Textures/UI/Backgrounds/BG_Jardines.png");
            Sprite pillDarkGold = LoadSprite("Assets/_Project/Textures/UI/Shapes/Pill_DarkGold.png");
            Sprite whitePillBtn = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_White_Pill.png");
            Sprite dashedLine = LoadSprite("Assets/_Project/Textures/UI/Shapes/Route_DashedLine.png");
            Sprite nodeGreen = LoadSprite("Assets/_Project/Textures/UI/Shapes/Node_Green.png");
            Sprite nodeYellow = LoadSprite("Assets/_Project/Textures/UI/Shapes/Node_Yellow.png");
            Sprite nodeGray = LoadSprite("Assets/_Project/Textures/UI/Shapes/Node_Gray.png");
            Sprite nodeGlow = LoadSprite("Assets/_Project/Textures/UI/Shapes/Node_GlowRing.png");
            Sprite popupCard = LoadSprite("Assets/_Project/Textures/UI/Shapes/Popup_Card.png");
            Sprite pointerDown = LoadSprite("Assets/_Project/Textures/UI/Shapes/Pointer_Down_White.png");
            Sprite roundedMask = LoadSprite("Assets/_Project/Textures/UI/Shapes/Rounded_Mask.png");
            Sprite greenBtn = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Primary_Green.png");
            Sprite yellowCircle = LoadSprite("Assets/_Project/Textures/UI/Shapes/Btn_Yellow_Circle.png");

            Sprite starFilled = LoadSprite("Assets/_Project/Textures/UI/Icons/Star_Filled.png");
            Sprite starEmpty = LoadSprite("Assets/_Project/Textures/UI/Icons/Star_Empty.png");
            Sprite lockIcon = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Lock.png");
            Sprite closeIcon = LoadSprite("Assets/_Project/Textures/UI/Icons/Icon_Close.png");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            Camera cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color32(72, 112, 68, 255);
            camGo.AddComponent<AudioListener>();
            camGo.transform.position = new Vector3(0f, 1.5f, -10f);

            // EventSystem
            GameObject eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // Canvas
            GameObject canvasGo = new GameObject("Canvas_LevelRoute");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            LevelRouteController controller = canvasGo.AddComponent<LevelRouteController>();

            // 1. Background + contrast overlay
            GameObject bgGo = CreateUIObject("Background_Route", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = bgDefault;
            bgImg.raycastTarget = false;
            AspectRatioFitter bgFitter = bgGo.AddComponent<AspectRatioFitter>();
            bgFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bgFitter.aspectRatio = bgDefault != null ? bgDefault.rect.width / bgDefault.rect.height : 16f / 9f;

            GameObject dimGo = CreateUIObject("Background_Overlay", canvasGo.transform);
            StretchFull(dimGo.GetComponent<RectTransform>());
            Image dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color32(14, 28, 20, 65);
            dimImg.raycastTarget = false;

            // 2. Top Header Bar
            GameObject topBar = CreateUIObject("TopBar", canvasGo.transform);
            RectTransform topRt = topBar.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.anchoredPosition = new Vector2(0f, -24f);
            topRt.sizeDelta = new Vector2(-88f, 78f);

            TextMeshProUGUI scenarioTitleTmp = CreateLabel(
                "Text_ScenarioTitle",
                topBar.transform,
                boldFont,
                "ESCENARIO 2: LOS JARDINES",
                36f,
                Color.white,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0f),
                new Vector2(0.41f, 1f),
                Vector2.zero,
                Vector2.zero
            );
            scenarioTitleTmp.enableAutoSizing = true;
            scenarioTitleTmp.fontSizeMin = 20f;
            scenarioTitleTmp.fontSizeMax = 36f;
            scenarioTitleTmp.outlineWidth = 0.24f;
            scenarioTitleTmp.outlineColor = new Color32(14, 30, 38, 250);

            // Center Stars Pill
            GameObject starsPill = CreateUIObject("Pill_StarsTotal", topBar.transform);
            RectTransform pillRt = starsPill.GetComponent<RectTransform>();
            pillRt.anchorMin = new Vector2(0.5f, 0.5f);
            pillRt.anchorMax = new Vector2(0.5f, 0.5f);
            pillRt.sizeDelta = new Vector2(178f, 60f);
            Image pillImg = starsPill.AddComponent<Image>();
            pillImg.sprite = pillDarkGold;
            pillImg.type = Image.Type.Sliced;

            GameObject pillStarGo = CreateUIObject("Icon_Star", starsPill.transform);
            RectTransform pillStarRt = pillStarGo.GetComponent<RectTransform>();
            pillStarRt.anchorMin = new Vector2(0f, 0.5f);
            pillStarRt.anchorMax = new Vector2(0f, 0.5f);
            pillStarRt.pivot = new Vector2(0f, 0.5f);
            pillStarRt.anchoredPosition = new Vector2(20f, 0f);
            pillStarRt.sizeDelta = new Vector2(36f, 36f);
            Image pillStarImg = pillStarGo.AddComponent<Image>();
            pillStarImg.sprite = starFilled;
            pillStarImg.preserveAspect = true;
            pillStarImg.raycastTarget = false;

            TextMeshProUGUI starsTotalTmp = CreateLabel(
                "Text_StarsTotal",
                starsPill.transform,
                boldFont,
                "3 / 9",
                28f,
                Color.white,
                TextAlignmentOptions.Center,
                new Vector2(0.32f, 0f),
                new Vector2(1f, 1f),
                Vector2.zero,
                new Vector2(-12f, 0f)
            );

            // Right Back Button
            GameObject backGo = CreateUIObject("Btn_VolverEscenarios", topBar.transform);
            RectTransform backRt = backGo.GetComponent<RectTransform>();
            backRt.anchorMin = new Vector2(1f, 0.5f);
            backRt.anchorMax = new Vector2(1f, 0.5f);
            backRt.pivot = new Vector2(1f, 0.5f);
            backRt.sizeDelta = new Vector2(330f, 62f);
            Image backImg = backGo.AddComponent<Image>();
            backImg.sprite = whitePillBtn;
            backImg.type = Image.Type.Sliced;
            Button backBtn = backGo.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            backGo.AddComponent<UIButtonFX>();

            CreateLabel(
                "Label",
                backGo.transform,
                boldFont,
                "VOLVER A ESCENARIOS",
                21f,
                SlateTextColor,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(14f, 4f),
                new Vector2(-14f, 0f)
            );

            // 3. Route Map Container
            GameObject routeGo = CreateUIObject("Route_Container", canvasGo.transform);
            RectTransform routeRt = routeGo.GetComponent<RectTransform>();
            routeRt.anchorMin = new Vector2(0.5f, 0.43f);
            routeRt.anchorMax = new Vector2(0.5f, 0.43f);
            routeRt.sizeDelta = new Vector2(1380f, 480f);

            // Dashed Line behind nodes
            GameObject lineGo = CreateUIObject("Route_DashedLine", routeGo.transform);
            RectTransform lineRt = lineGo.GetComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0.5f, 0.5f);
            lineRt.anchorMax = new Vector2(0.5f, 0.5f);
            lineRt.anchoredPosition = new Vector2(0f, -108f);
            lineRt.sizeDelta = new Vector2(960f, 16f);
            Image lineImg = lineGo.AddComponent<Image>();
            lineImg.sprite = dashedLine;
            lineImg.type = Image.Type.Tiled;
            lineImg.pixelsPerUnitMultiplier = 1f;
            lineImg.raycastTarget = false;

            // 3 Level Nodes
            float[] nodeX = { -450f, 0f, 450f };
            float nodeY = -108f;
            var nodeList = new List<LevelNodeUI>();

            for (int i = 0; i < 3; i++)
            {
                LevelNodeUI node = CreateLevelNode(
                    $"LevelNode_{i + 1}",
                    routeGo.transform,
                    new Vector2(nodeX[i], nodeY),
                    nodeGreen,
                    nodeYellow,
                    nodeGray,
                    nodeGlow,
                    whitePillBtn,
                    pointerDown,
                    lockIcon,
                    starFilled,
                    starEmpty,
                    boldFont
                );
                nodeList.Add(node);
            }

            // 4. Floating Level Popover
            LevelPopoverUI popover = CreateLevelPopover(
                routeGo.transform,
                popupCard,
                pointerDown,
                roundedMask,
                greenBtn,
                boldFont,
                regularFont
            );

            // 5. Pending Level Popup
            var (pendingPopup, pendingMsg) = CreatePendingLevelPopup(
                canvasGo.transform, controller, popupCard, greenBtn, yellowCircle, closeIcon, boldFont, regularFont);

            // Wire LevelRouteController
            SerializedObject ctrlSo = new SerializedObject(controller);
            ctrlSo.FindProperty("campaignDatabase").objectReferenceValue = database;
            ctrlSo.FindProperty("scenarioSelectSceneName").stringValue = "02_ScenarioSelect";
            ctrlSo.FindProperty("backButton").objectReferenceValue = backBtn;
            ctrlSo.FindProperty("backgroundImage").objectReferenceValue = bgImg;
            ctrlSo.FindProperty("backgroundAspectFitter").objectReferenceValue = bgFitter;
            ctrlSo.FindProperty("scenarioTitleText").objectReferenceValue = scenarioTitleTmp;
            ctrlSo.FindProperty("starsTotalText").objectReferenceValue = starsTotalTmp;
            ctrlSo.FindProperty("levelPopover").objectReferenceValue = popover;
            ctrlSo.FindProperty("previewReferenceStates").boolValue = false;
            ctrlSo.FindProperty("defaultPreviewScenarioId").stringValue = "scenario_02";
            ctrlSo.FindProperty("levelPendingPopup").objectReferenceValue = pendingPopup;
            ctrlSo.FindProperty("levelPendingMessageText").objectReferenceValue = pendingMsg;

            SerializedProperty nodesProp = ctrlSo.FindProperty("levelNodes");
            nodesProp.ClearArray();
            for (int i = 0; i < nodeList.Count; i++)
            {
                nodesProp.InsertArrayElementAtIndex(i);
                nodesProp.GetArrayElementAtIndex(i).objectReferenceValue = nodeList[i];
            }
            ctrlSo.ApplyModifiedPropertiesWithoutUndo();

            // Pre-populate Edit Mode preview
            controller.RefreshRoute();
            EditorUtility.SetDirty(controller);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[EcoDron] Built and saved Level Route scene at " + ScenePath);
        }

        private static LevelNodeUI CreateLevelNode(
            string name,
            Transform parent,
            Vector2 anchoredPos,
            Sprite greenDisc,
            Sprite yellowDisc,
            Sprite grayDisc,
            Sprite glowRingSprite,
            Sprite tooltipBubble,
            Sprite pointerDown,
            Sprite lockIcon,
            Sprite starFilled,
            Sprite starEmpty,
            TMP_FontAsset boldFont)
        {
            GameObject nodeGo = CreateUIObject(name, parent);
            RectTransform nodeRt = nodeGo.GetComponent<RectTransform>();
            nodeRt.anchorMin = new Vector2(0.5f, 0.5f);
            nodeRt.anchorMax = new Vector2(0.5f, 0.5f);
            nodeRt.anchoredPosition = anchoredPos;
            nodeRt.sizeDelta = new Vector2(192f, 124f);

            LevelNodeUI nodeUi = nodeGo.AddComponent<LevelNodeUI>();

            // Glow Ring
            GameObject glowGo = CreateUIObject("GlowRing", nodeGo.transform);
            RectTransform glowRt = glowGo.GetComponent<RectTransform>();
            glowRt.anchorMin = new Vector2(0.5f, 0.5f);
            glowRt.anchorMax = new Vector2(0.5f, 0.5f);
            glowRt.sizeDelta = new Vector2(272f, 176f);
            Image glowImg = glowGo.AddComponent<Image>();
            glowImg.sprite = glowRingSprite;
            glowImg.raycastTarget = false;

            // Main Disc Button
            GameObject discGo = CreateUIObject("Btn_Disc", nodeGo.transform);
            StretchFull(discGo.GetComponent<RectTransform>());
            Image discImg = discGo.AddComponent<Image>();
            discImg.sprite = greenDisc;
            discImg.preserveAspect = true;
            Button btn = discGo.AddComponent<Button>();
            btn.targetGraphic = discImg;
            discGo.AddComponent<UIButtonFX>();

            TextMeshProUGUI idTmp = CreateLabel(
                "Text_LevelId",
                discGo.transform,
                boldFont,
                "2.1",
                40f,
                Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 12f),
                Vector2.zero
            );

            // Lock Icon (for locked state)
            GameObject lockGo = CreateUIObject("Icon_Lock", nodeGo.transform);
            RectTransform lockRt = lockGo.GetComponent<RectTransform>();
            lockRt.anchorMin = new Vector2(0.5f, 0.5f);
            lockRt.anchorMax = new Vector2(0.5f, 0.5f);
            lockRt.anchoredPosition = new Vector2(0f, 56f);
            lockRt.sizeDelta = new Vector2(68f, 68f);
            Image lockImg = lockGo.AddComponent<Image>();
            lockImg.sprite = lockIcon;
            lockImg.preserveAspect = true;
            lockImg.raycastTarget = false;

            // Completed small white Tooltip above node
            GameObject tooltipGo = CreateUIObject("Tooltip_Bubble", nodeGo.transform);
            RectTransform tooltipRt = tooltipGo.GetComponent<RectTransform>();
            tooltipRt.anchorMin = new Vector2(0.5f, 0.5f);
            tooltipRt.anchorMax = new Vector2(0.5f, 0.5f);
            tooltipRt.anchoredPosition = new Vector2(0f, 106f);
            tooltipRt.sizeDelta = new Vector2(240f, 70f);
            Image tooltipImg = tooltipGo.AddComponent<Image>();
            tooltipImg.sprite = tooltipBubble;
            tooltipImg.type = Image.Type.Sliced;
            tooltipImg.raycastTarget = false;

            GameObject tipPtrGo = CreateUIObject("Pointer", tooltipGo.transform);
            RectTransform tipPtrRt = tipPtrGo.GetComponent<RectTransform>();
            tipPtrRt.anchorMin = new Vector2(0.5f, 0f);
            tipPtrRt.anchorMax = new Vector2(0.5f, 0f);
            tipPtrRt.pivot = new Vector2(0.5f, 1f);
            tipPtrRt.anchoredPosition = new Vector2(0f, 8f);
            tipPtrRt.sizeDelta = new Vector2(26f, 15f);
            Image tipPtrImg = tipPtrGo.AddComponent<Image>();
            tipPtrImg.sprite = pointerDown;
            tipPtrImg.raycastTarget = false;

            TextMeshProUGUI tipTmp = CreateLabel(
                "Text_LevelName",
                tooltipGo.transform,
                boldFont,
                "Nombre Nivel",
                18f,
                SlateTextColor,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(16f, 8f),
                new Vector2(-16f, -6f)
            );
            tipTmp.textWrappingMode = TextWrappingModes.Normal;

            // Stars below completed node
            GameObject starsGo = CreateUIObject("Stars_Container", nodeGo.transform);
            RectTransform starsRt = starsGo.GetComponent<RectTransform>();
            starsRt.anchorMin = new Vector2(0.5f, 0.5f);
            starsRt.anchorMax = new Vector2(0.5f, 0.5f);
            starsRt.anchoredPosition = new Vector2(0f, -90f);
            starsRt.sizeDelta = new Vector2(150f, 44f);

            HorizontalLayoutGroup starsHlg = starsGo.AddComponent<HorizontalLayoutGroup>();
            starsHlg.spacing = 8f;
            starsHlg.childAlignment = TextAnchor.MiddleCenter;
            starsHlg.childControlWidth = false;
            starsHlg.childControlHeight = false;

            var starImgs = new List<Image>();
            for (int s = 0; s < 3; s++)
            {
                GameObject sGo = CreateUIObject($"Star_{s + 1}", starsGo.transform);
                sGo.GetComponent<RectTransform>().sizeDelta = new Vector2(38f, 38f);
                Image sImg = sGo.AddComponent<Image>();
                sImg.sprite = starFilled;
                sImg.preserveAspect = true;
                sImg.raycastTarget = false;
                starImgs.Add(sImg);
            }

            // Wire LevelNodeUI
            SerializedObject nodeSo = new SerializedObject(nodeUi);
            nodeSo.FindProperty("discImage").objectReferenceValue = discImg;
            nodeSo.FindProperty("nodeButton").objectReferenceValue = btn;
            nodeSo.FindProperty("levelIdText").objectReferenceValue = idTmp;
            nodeSo.FindProperty("greenDiscSprite").objectReferenceValue = greenDisc;
            nodeSo.FindProperty("yellowDiscSprite").objectReferenceValue = yellowDisc;
            nodeSo.FindProperty("grayDiscSprite").objectReferenceValue = grayDisc;
            nodeSo.FindProperty("glowRing").objectReferenceValue = glowRt;
            nodeSo.FindProperty("lockIconObject").objectReferenceValue = lockGo;
            nodeSo.FindProperty("completedTooltip").objectReferenceValue = tooltipGo;
            nodeSo.FindProperty("completedTooltipText").objectReferenceValue = tipTmp;
            nodeSo.FindProperty("starsContainer").objectReferenceValue = starsGo;
            nodeSo.FindProperty("starFilledSprite").objectReferenceValue = starFilled;
            nodeSo.FindProperty("starEmptySprite").objectReferenceValue = starEmpty;

            SerializedProperty starsProp = nodeSo.FindProperty("starImages");
            starsProp.ClearArray();
            for (int s = 0; s < starImgs.Count; s++)
            {
                starsProp.InsertArrayElementAtIndex(s);
                starsProp.GetArrayElementAtIndex(s).objectReferenceValue = starImgs[s];
            }
            nodeSo.ApplyModifiedPropertiesWithoutUndo();

            return nodeUi;
        }

        private static LevelPopoverUI CreateLevelPopover(
            Transform parent,
            Sprite cardSprite,
            Sprite pointerDown,
            Sprite maskSprite,
            Sprite greenBtnSprite,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            GameObject popGo = CreateUIObject("Level_Popover", parent);
            RectTransform popRt = popGo.GetComponent<RectTransform>();
            popRt.anchorMin = new Vector2(0.5f, 0.5f);
            popRt.anchorMax = new Vector2(0.5f, 0.5f);
            popRt.pivot = new Vector2(0.5f, 0f);
            popRt.anchoredPosition = new Vector2(0f, -14f);
            popRt.sizeDelta = new Vector2(530f, 218f);

            Image cardImg = popGo.AddComponent<Image>();
            cardImg.sprite = cardSprite;
            cardImg.type = Image.Type.Sliced;

            LevelPopoverUI popover = popGo.AddComponent<LevelPopoverUI>();

            // Bottom pointer triangle
            GameObject ptrGo = CreateUIObject("Pointer_Down", popGo.transform);
            RectTransform ptrRt = ptrGo.GetComponent<RectTransform>();
            ptrRt.anchorMin = new Vector2(0.5f, 0f);
            ptrRt.anchorMax = new Vector2(0.5f, 0f);
            ptrRt.pivot = new Vector2(0.5f, 1f);
            ptrRt.anchoredPosition = new Vector2(0f, 12f);
            ptrRt.sizeDelta = new Vector2(40f, 22f);
            Image ptrImg = ptrGo.AddComponent<Image>();
            ptrImg.sprite = pointerDown;
            ptrImg.raycastTarget = false;

            // Left Thumbnail with Rounded Mask
            GameObject maskGo = CreateUIObject("Thumbnail_Mask", popGo.transform);
            RectTransform maskRt = maskGo.GetComponent<RectTransform>();
            maskRt.anchorMin = new Vector2(0f, 0.5f);
            maskRt.anchorMax = new Vector2(0f, 0.5f);
            maskRt.pivot = new Vector2(0f, 0.5f);
            maskRt.anchoredPosition = new Vector2(24f, 3f);
            maskRt.sizeDelta = new Vector2(170f, 164f);
            Image maskImg = maskGo.AddComponent<Image>();
            maskImg.sprite = maskSprite;
            maskImg.type = Image.Type.Sliced;
            Mask mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject thumbGo = CreateUIObject("Img_Thumbnail", maskGo.transform);
            StretchFull(thumbGo.GetComponent<RectTransform>());
            Image thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.preserveAspect = false;
            thumbImg.raycastTarget = false;

            // Right Info + Launch Button
            TextMeshProUGUI titleTmp = CreateLabel(
                "Text_LevelTitle",
                popGo.transform,
                boldFont,
                "2.2: La Arboleda Frondosa",
                25f,
                SlateTextColor,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, 0.54f),
                new Vector2(1f, 1f),
                new Vector2(212f, 0f),
                new Vector2(-24f, -22f)
            );
            titleTmp.textWrappingMode = TextWrappingModes.Normal;
            titleTmp.enableAutoSizing = true;
            titleTmp.fontSizeMin = 18f;
            titleTmp.fontSizeMax = 25f;

            TextMeshProUGUI metaTmp = CreateLabel(
                "Text_LevelMeta",
                popGo.transform,
                regularFont,
                "10 Residuos | 110s",
                21f,
                MutedSlateColor,
                TextAlignmentOptions.MidlineLeft,
                new Vector2(0f, 0.39f),
                new Vector2(1f, 0.55f),
                new Vector2(212f, 0f),
                new Vector2(-24f, 0f)
            );

            GameObject launchGo = CreateUIObject("Btn_Despegar", popGo.transform);
            RectTransform launchRt = launchGo.GetComponent<RectTransform>();
            launchRt.anchorMin = new Vector2(0f, 0f);
            launchRt.anchorMax = new Vector2(1f, 0f);
            launchRt.pivot = new Vector2(0.5f, 0f);
            launchRt.offsetMin = new Vector2(210f, 22f);
            launchRt.offsetMax = new Vector2(-24f, 88f);
            Image launchImg = launchGo.AddComponent<Image>();
            launchImg.sprite = greenBtnSprite;
            launchImg.type = Image.Type.Sliced;
            Button launchBtn = launchGo.AddComponent<Button>();
            launchBtn.targetGraphic = launchImg;
            launchGo.AddComponent<UIButtonFX>();

            TextMeshProUGUI launchTmp = CreateLabel(
                "Label",
                launchGo.transform,
                boldFont,
                "¡DESPEGAR!",
                25f,
                Color.white,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(10f, 4f),
                new Vector2(-10f, 0f)
            );
            launchTmp.outlineWidth = 0.14f;
            launchTmp.outlineColor = new Color32(18, 92, 34, 210);

            SerializedObject popSo = new SerializedObject(popover);
            popSo.FindProperty("popoverRect").objectReferenceValue = popRt;
            popSo.FindProperty("thumbnailImage").objectReferenceValue = thumbImg;
            popSo.FindProperty("titleText").objectReferenceValue = titleTmp;
            popSo.FindProperty("metadataText").objectReferenceValue = metaTmp;
            popSo.FindProperty("launchButton").objectReferenceValue = launchBtn;
            popSo.FindProperty("verticalOffsetAboveNode").floatValue = 94f;
            popSo.FindProperty("maxHorizontalClamp").floatValue = 390f;
            popSo.ApplyModifiedPropertiesWithoutUndo();

            return popover;
        }

        private static (UIPopupWindow popup, TextMeshProUGUI msgText) CreatePendingLevelPopup(
            Transform parent,
            LevelRouteController controller,
            Sprite cardSprite,
            Sprite greenBtnSprite,
            Sprite closeBtnSprite,
            Sprite closeIcon,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            GameObject overlay = CreateUIObject("Popup_LevelPending", parent);
            StretchFull(overlay.GetComponent<RectTransform>());
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color32(10, 24, 32, 165);
            overlay.AddComponent<CanvasGroup>();
            UIPopupWindow popup = overlay.AddComponent<UIPopupWindow>();

            GameObject card = CreateUIObject("Card", overlay.transform);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(780f, 420f);
            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = cardSprite;
            cardImg.type = Image.Type.Sliced;

            CreateLabel("Title", card.transform, boldFont, "PREPARAR DESPEGUE", 34f, SlateTextColor, TextAlignmentOptions.MidlineLeft,
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

        private static void ConfigureRouteSprites()
        {
            var borders = new Dictionary<string, Vector4>
            {
                { "Assets/_Project/Textures/UI/Shapes/Pill_DarkGold.png", new Vector4(48f, 48f, 48f, 48f) },
                { "Assets/_Project/Textures/UI/Shapes/Btn_White_Pill.png", new Vector4(44f, 44f, 44f, 44f) }
            };

            string dashedPath = "Assets/_Project/Textures/UI/Shapes/Route_DashedLine.png";

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

                if (path == dashedPath)
                {
                    if (importer.wrapMode != TextureWrapMode.Repeat)
                    {
                        importer.wrapMode = TextureWrapMode.Repeat;
                        dirty = true;
                    }
                    TextureImporterSettings settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    if (settings.spriteMeshType != SpriteMeshType.FullRect)
                    {
                        settings.spriteMeshType = SpriteMeshType.FullRect;
                        importer.SetTextureSettings(settings);
                        dirty = true;
                    }
                }
                else if (borders.TryGetValue(path, out Vector4 border) && importer.spriteBorder != border)
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
            int insertIndex = Mathf.Min(2, scenes.Count);
            scenes.Insert(insertIndex, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
