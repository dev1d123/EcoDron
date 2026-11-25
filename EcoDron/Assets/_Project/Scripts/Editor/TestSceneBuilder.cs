#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using EcoDron.Drone;
using EcoDron.Core;

namespace EcoDron.Editor
{
    public static class TestSceneBuilder
    {
        private const string SceneSavePath = "Assets/_Project/Scenes/TestDroneScene.unity";
        private const string MaterialsFolder = "Assets/_Project/Materials";

        [MenuItem("EcoDron/Build Test Drone Scene")]
        public static void BuildScene()
        {
            // 1. Ensure Materials Folder
            if (!AssetDatabase.IsValidFolder(MaterialsFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Materials");
            }

            // Create or load materials
            Material matFloor = GetOrCreateMaterial("M_TestFloor", new Color(0.12f, 0.14f, 0.18f), 0.2f);
            Material matPad = GetOrCreateMaterial("M_LandingPad", new Color(0.0f, 0.9f, 0.5f), 0.7f);
            Material matObstacle = GetOrCreateMaterial("M_Obstacle", new Color(1.0f, 0.45f, 0.05f), 0.4f);
            Material matPlatform = GetOrCreateMaterial("M_Platform", new Color(0.25f, 0.45f, 0.85f), 0.5f);
            Material matRing = GetOrCreateMaterial("M_FlightRing", new Color(1.0f, 0.85f, 0.1f), 0.8f);

            // 2. Setup EcoDron GameObject
            GameObject drone = GameObject.Find("EcoDron");
            if (drone != null)
            {
                Undo.RecordObject(drone.transform, "Setup EcoDron");
                drone.transform.position = new Vector3(0f, 1.0f, 0f);
                drone.transform.rotation = Quaternion.Euler(270f, 0f, 0f);

                // Rigidbody
                Rigidbody rb = drone.GetComponent<Rigidbody>();
                if (rb == null) rb = drone.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                rb.constraints = RigidbodyConstraints.FreezeRotation;

                // BoxCollider
                BoxCollider col = drone.GetComponent<BoxCollider>();
                if (col == null) col = drone.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0f, 0f);
                col.size = new Vector3(2.0f, 2.0f, 0.6f);

                // Controller
                DroneController controller = drone.GetComponent<DroneController>();
                if (controller == null) controller = drone.AddComponent<DroneController>();
                controller.SetLockZ(true, 0f);
                controller.SetupRotorHubs();

                // Hook System
                DroneHook hook = drone.GetComponent<DroneHook>();
                if (hook == null) hook = drone.AddComponent<DroneHook>();
            }

            // Spawn Cargo Boxes
            PrefabBuilder.SpawnCargoInScene();

            // 3. Setup Main Camera
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                GameObject camObj = mainCam.gameObject;
                Undo.RecordObject(camObj.transform, "Setup 2.5D Camera");
                camObj.transform.position = new Vector3(0f, 2.8f, -8f);
                camObj.transform.rotation = Quaternion.Euler(10f, 0f, 0f);

                Camera25D camScript = camObj.GetComponent<Camera25D>();
                if (camScript == null) camScript = camObj.AddComponent<Camera25D>();
                if (drone != null) camScript.SetTarget(drone.transform);
                camScript.SetFollow(true);
                camScript.SetSmoothTime(0.25f);
            }

            // 4. Clean up any existing [TestArena]
            GameObject existingArena = GameObject.Find("[TestArena]");
            if (existingArena != null)
            {
                Undo.DestroyObjectImmediate(existingArena);
            }

            // 5. Create new [TestArena]
            GameObject arenaRoot = new GameObject("[TestArena]");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Test Arena");

            // Ground Floor
            CreateCube("Floor_Main", new Vector3(0f, -0.5f, 0f), new Vector3(50f, 1f, 10f), matFloor, arenaRoot.transform);

            // Back Wall (grid background reference)
            CreateCube("Wall_Background", new Vector3(0f, 8f, 5f), new Vector3(50f, 18f, 1f), matFloor, arenaRoot.transform);

            // Starting Landing Pad
            CreateCylinder("Pad_Start", new Vector3(0f, 0.05f, 0f), new Vector3(4f, 0.1f, 4f), matPad, arenaRoot.transform);

            // Elevated Platform Left
            CreateCube("Platform_Left_Base", new Vector3(-13f, 1.5f, 0f), new Vector3(2f, 3f, 3f), matPlatform, arenaRoot.transform);
            CreateCube("Platform_Left_Top", new Vector3(-13f, 3.1f, 0f), new Vector3(5f, 0.2f, 4f), matPad, arenaRoot.transform);

            // Elevated Platform Right
            CreateCube("Platform_Right_Base", new Vector3(13f, 2.5f, 0f), new Vector3(2f, 5f, 3f), matPlatform, arenaRoot.transform);
            CreateCube("Platform_Right_Top", new Vector3(13f, 5.1f, 0f), new Vector3(5f, 0.2f, 4f), matPad, arenaRoot.transform);

            // Slanted Ramp
            GameObject ramp = CreateCube("Ramp_Test", new Vector3(-6.5f, 1.2f, 0f), new Vector3(5f, 0.3f, 3f), matPlatform, arenaRoot.transform);
            ramp.transform.rotation = Quaternion.Euler(0f, 0f, -22f);

            // Obstacle Pillars
            CreateCube("Obstacle_Pillar_1", new Vector3(-3f, 2.5f, 0f), new Vector3(1f, 5f, 1f), matObstacle, arenaRoot.transform);
            CreateCube("Obstacle_Floating_Bar", new Vector3(3.5f, 4.5f, 0f), new Vector3(4f, 0.8f, 1.5f), matObstacle, arenaRoot.transform);
            CreateCube("Obstacle_Pillar_2", new Vector3(8f, 2f, 0f), new Vector3(1f, 4f, 1f), matObstacle, arenaRoot.transform);

            // High Floating Landing Pad
            CreateCube("Pad_High_Tower", new Vector3(0f, 8f, 0f), new Vector3(3f, 0.3f, 3f), matPad, arenaRoot.transform);

            // Flight Rings (Hoops) to fly through
            // 6. Save scene
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), SceneSavePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<b><color=#00FF7F>[EcoDron]</color></b> Test scene built and saved successfully to: {SceneSavePath}");
        }

        private static GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.SetParent(parent);
            if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreateCylinder(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obj.name = name;
            obj.transform.position = pos;
            obj.transform.localScale = scale;
            obj.transform.SetParent(parent);
            if (mat != null) obj.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return obj;
        }

        private static GameObject CreateFlightRing(string name, Vector3 center, float innerSize, Material mat, Transform parent)
        {
            GameObject ringRoot = new GameObject(name);
            ringRoot.transform.position = center;
            ringRoot.transform.SetParent(parent);

            float half = innerSize * 0.5f;
            float thickness = 0.25f;
            float depth = 0.5f;

            // Top
            CreateCube("Top", center + new Vector3(0, half, 0), new Vector3(innerSize + thickness, thickness, depth), mat, ringRoot.transform);
            // Bottom
            CreateCube("Bottom", center + new Vector3(0, -half, 0), new Vector3(innerSize + thickness, thickness, depth), mat, ringRoot.transform);
            // Left
            CreateCube("Left", center + new Vector3(-half, 0, 0), new Vector3(thickness, innerSize, depth), mat, ringRoot.transform);
            // Right
            CreateCube("Right", center + new Vector3(half, 0, 0), new Vector3(thickness, innerSize, depth), mat, ringRoot.transform);

            return ringRoot;
        }

        private static Material GetOrCreateMaterial(string name, Color color, float smoothness)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.color = color;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }
    }
}
#endif
