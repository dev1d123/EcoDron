#if UNITY_EDITOR
using EcoDron.Core;
using EcoDron.Data;
using EcoDron.Drone;
using EcoDron.Gameplay;
using EcoDron.Interaction;
using EcoDron.UI.Common;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoDron.EditorTools
{
    /// <summary>
    /// Builds the Level 1.1 greybox scene: Tacho (left) - EcoDron (center) - Basura (right)
    /// with objective tracking and a Victory Alert popup.
    /// </summary>
    public static class Level1GreyboxBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Scenarios/Scenario_01_Colegio/Level_1_1.unity";
        private const string DatabasePath = "Assets/_Project/Data/CampaignDatabase.asset";
        private const string DroneModelPath = "Assets/_Project/Models/Dron/EcoDron.fbx";
        private const string MaterialsFolder = "Assets/_Project/Materials";
        private const string BoldFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Bold SDF.asset";
        private const string RegularFontSdfPath = "Assets/_Project/Fonts/TrebuchetMS_Regular SDF.asset";

        private static readonly Color SlateTextColor = new Color32(22, 50, 59, 255);

        [MenuItem("EcoDron/6. Build Level 1-1 Greybox", false, 21)]
        public static void BuildLevel1GreyboxScene()
        {
            CampaignDatabaseSO database = AssetDatabase.LoadAssetAtPath<CampaignDatabaseSO>(DatabasePath);
            GameObject droneModel = AssetDatabase.LoadAssetAtPath<GameObject>(DroneModelPath);
            TMP_FontAsset boldFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontSdfPath);
            TMP_FontAsset regularFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(RegularFontSdfPath);

            if (database == null || droneModel == null)
            {
                Debug.LogError("[Level1GreyboxBuilder] Faltan CampaignDatabase.asset o EcoDron.fbx.");
                return;
            }

            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            Material matFloor = GetOrCreateMaterial("M_Greybox_Floor", new Color(0.16f, 0.19f, 0.24f), 0.2f);
            Material matWall = GetOrCreateMaterial("M_Greybox_BackWall", new Color(0.22f, 0.26f, 0.32f), 0.15f);
            Material matBin = GetOrCreateMaterial("M_Greybox_Bin", new Color(0.12f, 0.56f, 0.36f), 0.45f);
            Material matBinIndicator = GetOrCreateMaterial("M_Greybox_BinIndicator", new Color(0.95f, 0.72f, 0.15f), 0.7f);
            Material matTrash = GetOrCreateMaterial("M_Greybox_Trash", new Color(0.76f, 0.48f, 0.18f), 0.35f);
            Material matHandle = GetOrCreateMaterial("M_Greybox_TrashHandle", new Color(1.0f, 0.85f, 0.15f), 0.8f);
            Material matPad = GetOrCreateMaterial("M_Greybox_ZonePad", new Color(0.28f, 0.34f, 0.42f), 0.3f);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 1. Greybox Environment
            GameObject envRoot = new GameObject("[Greybox_Environment]");
            CreateCube("Floor_Main", new Vector3(0f, -0.5f, 0f), new Vector3(32f, 1f, 8f), matFloor, envRoot.transform);
            CreateCube("Wall_Back", new Vector3(0f, 5.5f, 4.5f), new Vector3(32f, 12f, 1f), matWall, envRoot.transform);
            CreateCube("Wall_Left", new Vector3(-15.5f, 3.5f, 0f), new Vector3(1f, 8f, 8f), matWall, envRoot.transform);
            CreateCube("Wall_Right", new Vector3(15.5f, 3.5f, 0f), new Vector3(1f, 8f, 8f), matWall, envRoot.transform);
            CreateCube("Pad_Tacho", new Vector3(-6f, 0.02f, 0f), new Vector3(3.6f, 0.04f, 3.2f), matPad, envRoot.transform);
            CreateCube("Pad_Basura", new Vector3(6f, 0.02f, 0f), new Vector3(2.4f, 0.04f, 2.4f), matPad, envRoot.transform);

            // 2. Tacho (Left: x = -6)
            TrashBinZone binZone = BuildTrashBin(new Vector3(-6f, 0f, 0f), matBin, matBinIndicator);

            // 3. EcoDron (Center: x = 0)
            GameObject drone = (GameObject)PrefabUtility.InstantiatePrefab(droneModel, scene);
            drone.name = "EcoDron";
            drone.transform.position = new Vector3(0f, 2.0f, 0f);
            drone.transform.rotation = Quaternion.Euler(270f, 0f, 0f);

            Rigidbody droneRb = drone.GetComponent<Rigidbody>();
            if (droneRb == null) droneRb = drone.AddComponent<Rigidbody>();
            droneRb.useGravity = false;
            droneRb.interpolation = RigidbodyInterpolation.Interpolate;
            droneRb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            droneRb.constraints = RigidbodyConstraints.FreezeRotation;

            BoxCollider droneCol = drone.GetComponent<BoxCollider>();
            if (droneCol == null) droneCol = drone.AddComponent<BoxCollider>();
            droneCol.center = Vector3.zero;
            droneCol.size = new Vector3(2.0f, 2.0f, 0.6f);

            DroneController droneCtrl = drone.GetComponent<DroneController>();
            if (droneCtrl == null) droneCtrl = drone.AddComponent<DroneController>();
            droneCtrl.SetLockZ(true, 0f);

            if (drone.GetComponent<DroneHook>() == null)
            {
                drone.AddComponent<DroneHook>();
            }

            // 4. Basura (Right: x = +6)
            BuildTrashObject("Basura_1", new Vector3(6f, 0.05f, 0f), matTrash, matHandle);

            // 5. Camera 2.5D
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                GameObject camObj = mainCam.gameObject;
                camObj.transform.position = new Vector3(0f, 3.2f, -10.5f);
                camObj.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

                Camera25D cam25D = camObj.GetComponent<Camera25D>();
                if (cam25D == null) cam25D = camObj.AddComponent<Camera25D>();
                cam25D.SetTarget(drone.transform);
                cam25D.SetFollow(true);
                cam25D.SetZoom(10.5f);
                cam25D.SetSmoothTime(0.22f);
            }

            // 6. EventSystem for Victory Alert UI
            GameObject eventGo = new GameObject("EventSystem");
            eventGo.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventGo.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            // 7. LevelSessionController
            GameObject sessionGo = new GameObject("[LevelSession]");
            LevelSessionController sessionCtrl = sessionGo.AddComponent<LevelSessionController>();
            SerializedObject sessionSo = new SerializedObject(sessionCtrl);
            sessionSo.FindProperty("campaignDatabase").objectReferenceValue = database;
            sessionSo.FindProperty("scenarioId").stringValue = "scenario_01";
            sessionSo.FindProperty("levelId").stringValue = "1.1";
            sessionSo.FindProperty("scenarioSelectSceneName").stringValue = "02_ScenarioSelect";
            sessionSo.FindProperty("levelRouteSceneName").stringValue = "03_LevelRoute";
            sessionSo.FindProperty("showDebugOverlay").boolValue = true;
            sessionSo.ApplyModifiedPropertiesWithoutUndo();

            // 8. HUD + Victory Alert Popup
            BuildLevelHudAndVictoryPopup(binZone, sessionCtrl, boldFont, regularFont);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<b><color=#00FF7F>[Level1GreyboxBuilder]</color></b> Greybox de Nivel 1.1 guardado en: {ScenePath}");
        }

        private static TrashBinZone BuildTrashBin(Vector3 position, Material matBin, Material matIndicator)
        {
            GameObject binRoot = new GameObject("Tacho_Basura");
            binRoot.transform.position = position;

            // Hollow open-top bin: Bottom + Left + Right + Back + Low Front lip
            CreateCube("Bin_Bottom", new Vector3(0f, 0.1f, 0f), new Vector3(2.8f, 0.2f, 2.4f), matBin, binRoot.transform);
            CreateCube("Bin_Wall_Left", new Vector3(-1.3f, 0.9f, 0f), new Vector3(0.2f, 1.6f, 2.4f), matBin, binRoot.transform);
            CreateCube("Bin_Wall_Right", new Vector3(1.3f, 0.9f, 0f), new Vector3(0.2f, 1.6f, 2.4f), matBin, binRoot.transform);
            CreateCube("Bin_Wall_Back", new Vector3(0f, 0.9f, 1.1f), new Vector3(2.4f, 1.6f, 0.2f), matBin, binRoot.transform);
            CreateCube("Bin_Wall_FrontLip", new Vector3(0f, 0.45f, -1.1f), new Vector3(2.4f, 0.7f, 0.2f), matBin, binRoot.transform);

            // Indicator bar along top back rim
            GameObject indicatorGo = CreateCube("Bin_Indicator", new Vector3(0f, 1.78f, 1.1f), new Vector3(2.4f, 0.16f, 0.24f), matIndicator, binRoot.transform);

            // Trigger zone inside the bin cavity
            GameObject zoneGo = new GameObject("DepositZone");
            zoneGo.transform.SetParent(binRoot.transform, false);
            zoneGo.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            BoxCollider triggerCol = zoneGo.AddComponent<BoxCollider>();
            triggerCol.isTrigger = true;
            triggerCol.size = new Vector3(2.2f, 1.3f, 1.9f);

            TrashBinZone binZone = zoneGo.AddComponent<TrashBinZone>();
            SerializedObject zoneSo = new SerializedObject(binZone);
            zoneSo.FindProperty("indicatorRenderer").objectReferenceValue = indicatorGo.GetComponent<Renderer>();
            zoneSo.ApplyModifiedPropertiesWithoutUndo();

            return binZone;
        }

        private static GrabbableObject BuildTrashObject(string name, Vector3 position, Material matTrash, Material matHandle)
        {
            GameObject root = new GameObject(name);
            root.transform.position = position;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.42f, 0f);
            body.transform.localScale = new Vector3(1.0f, 0.84f, 0.84f);
            body.GetComponent<MeshRenderer>().sharedMaterial = matTrash;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "GrabHandle";
            handle.transform.SetParent(root.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            handle.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);
            handle.GetComponent<MeshRenderer>().sharedMaterial = matHandle;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            GameObject attachPoint = new GameObject("AttachPoint");
            attachPoint.transform.SetParent(root.transform, false);
            attachPoint.transform.localPosition = new Vector3(0f, 0.95f, 0f);

            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.48f, 0f);
            col.size = new Vector3(1.0f, 0.96f, 0.84f);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 1.5f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            rb.constraints = RigidbodyConstraints.FreezePositionZ;

            GrabbableObject grabbable = root.AddComponent<GrabbableObject>();
            SerializedObject gSo = new SerializedObject(grabbable);
            gSo.FindProperty("cargoName").stringValue = "Basura";
            gSo.FindProperty("mass").floatValue = 1.5f;
            gSo.FindProperty("attachPoint").objectReferenceValue = attachPoint.transform;
            gSo.FindProperty("mainRenderer").objectReferenceValue = body.GetComponent<Renderer>();
            gSo.FindProperty("normalColor").colorValue = new Color(0.76f, 0.48f, 0.18f);
            gSo.ApplyModifiedPropertiesWithoutUndo();

            return grabbable;
        }

        private static void BuildLevelHudAndVictoryPopup(
            TrashBinZone binZone,
            LevelSessionController sessionCtrl,
            TMP_FontAsset boldFont,
            TMP_FontAsset regularFont)
        {
            Sprite popupCardSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/Shapes/Popup_Card.png");
            Sprite greenBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/Shapes/Btn_Primary_Green.png");
            Sprite blueBtnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/Shapes/Btn_Blue_Repeat.png");
            Sprite whitePillSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/Shapes/Btn_White_Pill.png");
            Sprite starFilledSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Textures/UI/Icons/Star_Filled.png");

            GameObject canvasGo = new GameObject("Canvas_LevelHUD");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            LevelObjectiveController objectiveCtrl = canvasGo.AddComponent<LevelObjectiveController>();

            // Top-Right Objective Banner
            GameObject objPill = CreateUIObject("Objective_Banner", canvasGo.transform);
            RectTransform objRt = objPill.GetComponent<RectTransform>();
            objRt.anchorMin = new Vector2(1f, 1f);
            objRt.anchorMax = new Vector2(1f, 1f);
            objRt.pivot = new Vector2(1f, 1f);
            objRt.anchoredPosition = new Vector2(-28f, -24f);
            objRt.sizeDelta = new Vector2(650f, 64f);
            Image objBg = objPill.AddComponent<Image>();
            objBg.sprite = whitePillSprite;
            objBg.type = Image.Type.Sliced;

            TextMeshProUGUI objTmp = CreateLabel(
                "Text_Objective",
                objPill.transform,
                boldFont,
                "OBJETIVO: MUEVE LA BASURA AL TACHO (0 / 1)",
                22f,
                SlateTextColor,
                TextAlignmentOptions.Center,
                Vector2.zero,
                Vector2.one,
                new Vector2(20f, 4f),
                new Vector2(-20f, -4f)
            );

            // Victory Alert Popup
            GameObject overlay = CreateUIObject("Popup_Victory", canvasGo.transform);
            StretchFull(overlay.GetComponent<RectTransform>());
            Image dim = overlay.AddComponent<Image>();
            dim.color = new Color32(10, 24, 32, 175);
            overlay.AddComponent<CanvasGroup>();
            UIPopupWindow victoryPopup = overlay.AddComponent<UIPopupWindow>();

            GameObject card = CreateUIObject("Card", overlay.transform);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardRt.sizeDelta = new Vector2(780f, 480f);
            Image cardImg = card.AddComponent<Image>();
            cardImg.sprite = popupCardSprite;
            cardImg.type = Image.Type.Sliced;

            TextMeshProUGUI titleTmp = CreateLabel(
                "Text_Title",
                card.transform,
                boldFont,
                "¡VICTORIA!",
                44f,
                new Color32(28, 146, 62, 255),
                TextAlignmentOptions.Center,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(40f, -96f),
                new Vector2(-40f, -24f)
            );

            // 3 Stars Row
            GameObject starsRow = CreateUIObject("Stars_Row", card.transform);
            RectTransform starsRt = starsRow.GetComponent<RectTransform>();
            starsRt.anchorMin = new Vector2(0.5f, 1f);
            starsRt.anchorMax = new Vector2(0.5f, 1f);
            starsRt.pivot = new Vector2(0.5f, 1f);
            starsRt.anchoredPosition = new Vector2(0f, -104f);
            starsRt.sizeDelta = new Vector2(240f, 64f);
            HorizontalLayoutGroup starsHlg = starsRow.AddComponent<HorizontalLayoutGroup>();
            starsHlg.spacing = 16f;
            starsHlg.childAlignment = TextAnchor.MiddleCenter;
            starsHlg.childControlWidth = false;
            starsHlg.childControlHeight = false;

            for (int i = 0; i < 3; i++)
            {
                GameObject sGo = CreateUIObject($"Star_{i + 1}", starsRow.transform);
                sGo.GetComponent<RectTransform>().sizeDelta = new Vector2(56f, 56f);
                Image sImg = sGo.AddComponent<Image>();
                sImg.sprite = starFilledSprite;
                sImg.preserveAspect = true;
                sImg.raycastTarget = false;
            }

            TextMeshProUGUI msgTmp = CreateLabel(
                "Text_Message",
                card.transform,
                regularFont,
                "¡Has depositado toda la basura en el tacho!",
                24f,
                SlateTextColor,
                TextAlignmentOptions.Center,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(52f, 132f),
                new Vector2(-52f, -176f)
            );

            // Buttons Row
            GameObject btnRow = CreateUIObject("Buttons_Row", card.transform);
            RectTransform btnRowRt = btnRow.GetComponent<RectTransform>();
            btnRowRt.anchorMin = new Vector2(0.5f, 0f);
            btnRowRt.anchorMax = new Vector2(0.5f, 0f);
            btnRowRt.pivot = new Vector2(0.5f, 0f);
            btnRowRt.anchoredPosition = new Vector2(0f, 34f);
            btnRowRt.sizeDelta = new Vector2(700f, 76f);
            HorizontalLayoutGroup btnHlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 18f;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            btnHlg.childControlHeight = false;

            Button routeBtn = CreatePopupButton("Btn_Ruta", btnRow.transform, whitePillSprite, SlateTextColor, boldFont, "RUTA", 180f);
            Button retryBtn = CreatePopupButton("Btn_Repetir", btnRow.transform, blueBtnSprite, Color.white, boldFont, "REPETIR", 200f);
            Button nextBtn = CreatePopupButton("Btn_Siguiente", btnRow.transform, greenBtnSprite, Color.white, boldFont, "SIGUIENTE", 250f);

            SerializedObject popSo = new SerializedObject(victoryPopup);
            popSo.FindProperty("cardTransform").objectReferenceValue = cardRt;
            popSo.ApplyModifiedPropertiesWithoutUndo();
            overlay.SetActive(false);

            SerializedObject objSo = new SerializedObject(objectiveCtrl);
            objSo.FindProperty("trashBin").objectReferenceValue = binZone;
            objSo.FindProperty("sessionController").objectReferenceValue = sessionCtrl;
            objSo.FindProperty("objectiveHudText").objectReferenceValue = objTmp;
            objSo.FindProperty("victoryPopup").objectReferenceValue = victoryPopup;
            objSo.FindProperty("victoryTitleText").objectReferenceValue = titleTmp;
            objSo.FindProperty("victoryMessageText").objectReferenceValue = msgTmp;
            objSo.FindProperty("nextButton").objectReferenceValue = nextBtn;
            objSo.FindProperty("retryButton").objectReferenceValue = retryBtn;
            objSo.FindProperty("routeButton").objectReferenceValue = routeBtn;
            objSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Button CreatePopupButton(
            string name,
            Transform parent,
            Sprite bgSprite,
            Color textColor,
            TMP_FontAsset font,
            string label,
            float width)
        {
            GameObject go = CreateUIObject(name, parent);
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(width, 68f);
            Image img = go.AddComponent<Image>();
            img.sprite = bgSprite;
            img.type = Image.Type.Sliced;
            Button btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            go.AddComponent<UIButtonFX>();

            CreateLabel("Label", go.transform, font, label, 23f, textColor, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(10f, 4f), new Vector2(-10f, 0f));

            return btn;
        }

        private static GameObject CreateCube(string name, Vector3 localPos, Vector3 scale, Material mat, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
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
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = name;
                AssetDatabase.CreateAsset(mat, path);
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
