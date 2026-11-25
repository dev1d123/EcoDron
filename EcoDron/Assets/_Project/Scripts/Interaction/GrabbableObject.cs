using UnityEngine;

namespace EcoDron.Interaction
{
    [RequireComponent(typeof(Rigidbody))]
    public class GrabbableObject : MonoBehaviour
    {
        [Header("Cargo Attributes")]
        [SerializeField] private string cargoName = "Bio-Waste Container";
        [SerializeField] private float mass = 1.5f;

        [Header("Attach Point")]
        [Tooltip("Exact point where the hook or cable attaches. Defaults to top of object if null.")]
        [SerializeField] private Transform attachPoint;

        [Header("Visual Feedback")]
        [SerializeField] private Renderer mainRenderer;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.6f, 0.9f);
        [SerializeField] private Color targetedColor = new Color(0.1f, 1.0f, 0.6f);
        [SerializeField] private Color grabbedColor = new Color(1.0f, 0.8f, 0.1f);

        [Header("Respawn")]
        [SerializeField] private float fallThresholdY = -15f;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;

        private Rigidbody rb;
        private Material instanceMat;
        private bool isTargeted = false;
        private bool isGrabbed = false;
        private Transform currentHook;

        public bool IsGrabbed => isGrabbed;
        public Rigidbody Rigidbody => rb;
        public Transform AttachPoint => attachPoint != null ? attachPoint : transform;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.mass = mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            if (mainRenderer == null)
            {
                mainRenderer = GetComponent<Renderer>();
            }

            if (mainRenderer != null)
            {
                instanceMat = mainRenderer.material;
                SetColor(normalColor);
            }
        }

        private void Update()
        {
            // Safety respawn if falling into the void
            if (transform.position.y < fallThresholdY)
            {
                Respawn();
            }
        }

        public void SetTargeted(bool targeted)
        {
            if (isGrabbed) return;
            isTargeted = targeted;
            SetColor(isTargeted ? targetedColor : normalColor);
        }

        public void OnGrabbed(Transform hookTransform)
        {
            isGrabbed = true;
            isTargeted = false;
            currentHook = hookTransform;
            SetColor(grabbedColor);
        }

        public void OnReleased()
        {
            isGrabbed = false;
            currentHook = null;
            SetColor(normalColor);
        }

        private void SetColor(Color col)
        {
            if (instanceMat != null && instanceMat.HasProperty("_BaseColor"))
            {
                instanceMat.SetColor("_BaseColor", col);
            }
            else if (instanceMat != null && instanceMat.HasProperty("_Color"))
            {
                instanceMat.color = col;
            }
        }

        public void Respawn()
        {
            if (isGrabbed && currentHook != null)
            {
                OnReleased();
            }

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.position = spawnPosition;
            transform.rotation = spawnRotation;
        }
    }
}
