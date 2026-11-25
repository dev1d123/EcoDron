#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using EcoDron.Interaction;

namespace EcoDron.Editor
{
    public static class PrefabBuilder
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs";
        private const string PrefabPath = "Assets/_Project/Prefabs/CargoBox.prefab";
        private const string MaterialPath = "Assets/_Project/Materials/M_CargoBox.mat";

        [InitializeOnLoadMethod]
        private static void AutoEnsurePrefab()
        {
            if (!System.IO.File.Exists(PrefabPath))
            {
                CreateCargoPrefab();
                SpawnCargoInScene();
            }
        }

        [MenuItem("EcoDron/Create Grabbable Cargo Prefab")]
        public static GameObject CreateCargoPrefab()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
            }

            // Material
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                mat = new Material(shader);
                mat.name = "M_CargoBox";
                Color baseColor = new Color(0.15f, 0.65f, 0.95f); // Tech Cyan
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
                else mat.color = baseColor;
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0.4f);
                AssetDatabase.CreateAsset(mat, MaterialPath);
            }

            // Handle material (Golden hook loop)
            string handleMatPath = "Assets/_Project/Materials/M_CargoHandle.mat";
            Material handleMat = AssetDatabase.LoadAssetAtPath<Material>(handleMatPath);
            if (handleMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                handleMat = new Material(shader);
                handleMat.name = "M_CargoHandle";
                Color handleColor = new Color(1.0f, 0.8f, 0.1f); // Gold / Yellow
                if (handleMat.HasProperty("_BaseColor")) handleMat.SetColor("_BaseColor", handleColor);
                else handleMat.color = handleColor;
                if (handleMat.HasProperty("_Metallic")) handleMat.SetFloat("_Metallic", 0.85f);
                if (handleMat.HasProperty("_Smoothness")) handleMat.SetFloat("_Smoothness", 0.8f);
                AssetDatabase.CreateAsset(handleMat, handleMatPath);
            }

            // Root container
            GameObject root = new GameObject("CargoBox");

            // Main Box Visual
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(root.transform);
            body.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            body.transform.localScale = new Vector3(1.1f, 0.9f, 0.9f);
            body.GetComponent<MeshRenderer>().sharedMaterial = mat;
            // Remove primitive collider from child, attach BoxCollider to root
            Object.DestroyImmediate(body.GetComponent<Collider>());

            // Top Attach Ring / Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "AttachHandle";
            handle.transform.SetParent(root.transform);
            handle.transform.localPosition = new Vector3(0f, 0.95f, 0f);
            handle.transform.localScale = new Vector3(0.35f, 0.08f, 0.35f);
            handle.GetComponent<MeshRenderer>().sharedMaterial = handleMat;
            Object.DestroyImmediate(handle.GetComponent<Collider>());

            // Attach Point Transform
            GameObject attachPoint = new GameObject("AttachPoint");
            attachPoint.transform.SetParent(root.transform);
            attachPoint.transform.localPosition = new Vector3(0f, 1.0f, 0f);

            // Root Physics & Collider
            BoxCollider col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, 0f);
            col.size = new Vector3(1.1f, 1.0f, 0.9f);

            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 2.0f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 1.0f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            // In 2.5D, prevent cargo from flying out of the Z plane
            rb.constraints = RigidbodyConstraints.FreezePositionZ;

            // Script
            GrabbableObject grabbable = root.AddComponent<GrabbableObject>();
            var serialized = new SerializedObject(grabbable);
            serialized.FindProperty("attachPoint").objectReferenceValue = attachPoint.transform;
            serialized.FindProperty("mainRenderer").objectReferenceValue = body.GetComponent<Renderer>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // Save Prefab
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<b><color=#00FF7F>[PrefabBuilder]</color></b> Grabbable Cargo Box prefab created at: {PrefabPath}");
            return prefabAsset;
        }

        [MenuItem("EcoDron/Spawn Cargo in Test Scene")]
        public static void SpawnCargoInScene()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = CreateCargoPrefab();
            }

            // Remove previous spawned boxes
            foreach (var existing in GameObject.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None))
            {
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            // Spawn 3 boxes across the test arena
            Vector3[] spawnPositions = new Vector3[]
            {
                new Vector3(-5f, 0.5f, 0f),    // Near ground center
                new Vector3(-13f, 3.8f, 0f),   // On Left elevated platform
                new Vector3(13f, 5.8f, 0f)     // On Right high platform
            };

            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = $"CargoBox_{i + 1}";
                instance.transform.position = spawnPositions[i];
                Undo.RegisterCreatedObjectUndo(instance, "Spawn Cargo");
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log($"<b><color=#00E5FF>[PrefabBuilder]</color></b> Spawned 3 cargo boxes in TestDroneScene.");
        }
    }
}
#endif
