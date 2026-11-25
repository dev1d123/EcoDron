using UnityEngine;

namespace EcoDron.Core
{
    [ExecuteAlways]
    public class Camera25D : MonoBehaviour
    {
        [Header("Target & Follow")]
        [SerializeField] private Transform target;
        [SerializeField] private bool followTarget = true;

        [Header("Zoom & Offset Settings (2.5D Closer View)")]
        [Tooltip("Camera offset relative to target. Z controls zoom distance (e.g. -8m).")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, -8f);

        [Tooltip("Camera tilt angle (degrees around X axis)")]
        [SerializeField] private float tiltAngle = 10f;

        [Header("Fluid Smooth Damp Settings")]
        [Tooltip("Camera follow smooth damp time in seconds (0.12 - 0.25 recommended).")]
        [Range(0.05f, 0.6f)]
        [SerializeField] private float smoothTime = 0.18f;

        [Tooltip("Lock camera Z axis to maintain consistent 2.5D depth")]
        [SerializeField] private bool lockZDepth = true;

        [Header("Look Ahead (Smooth Anticipation)")]
        [SerializeField] private bool enableLookAhead = true;
        [SerializeField] private float lookAheadDistance = 1.2f;
        [SerializeField] private float lookAheadSmoothTime = 0.35f;

        [Header("Static Mode Fallback")]
        [SerializeField] private Vector3 staticPosition = new Vector3(0f, 5.5f, -17f);

        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 currentLookAhead = Vector3.zero;
        private Vector3 lookAheadVelocity = Vector3.zero;

        private void Awake()
        {
            FindTargetIfMissing();
        }

        private void Start()
        {
            FindTargetIfMissing();
            if (followTarget && target != null)
            {
                transform.position = target.position + offset;
            }
            transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        }

        private void LateUpdate()
        {
            if (!followTarget || target == null)
            {
                FindTargetIfMissing();
                if (!followTarget || target == null)
                {
                    transform.position = staticPosition;
                    transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
                    return;
                }
            }

            // Smoothly calculate filtered look-ahead to eliminate physics tick micro-stutter
            Vector3 targetLookAhead = Vector3.zero;
            if (enableLookAhead)
            {
                Rigidbody rb = target.GetComponent<Rigidbody>();
                if (rb != null)
                {
#if UNITY_6000_0_OR_NEWER
                    Vector3 vel = rb.linearVelocity;
#else
                    Vector3 vel = rb.velocity;
#endif
                    // Normalize and scale look-ahead gently
                    float speed = vel.magnitude;
                    if (speed > 0.1f)
                    {
                        targetLookAhead = (vel / Mathf.Max(speed, 8f)) * lookAheadDistance;
                    }
                }
            }

            currentLookAhead = Vector3.SmoothDamp(
                currentLookAhead,
                targetLookAhead,
                ref lookAheadVelocity,
                lookAheadSmoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );

            // Compute ideal camera position based on interpolated target position
            Vector3 desiredPosition = target.position + offset + currentLookAhead;
            if (lockZDepth)
            {
                desiredPosition.z = offset.z;
            }

            // Ultra-smooth follow damping
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentVelocity,
                smoothTime,
                Mathf.Infinity,
                Time.deltaTime
            );

            transform.rotation = Quaternion.Euler(tiltAngle, 0f, 0f);
        }

        private void FindTargetIfMissing()
        {
            if (target == null)
            {
                var drone = GameObject.Find("EcoDron");
                if (drone != null) target = drone.transform;
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }

        public void SetFollow(bool follow)
        {
            followTarget = follow;
        }

        public void SetZoom(float distanceZ)
        {
            offset.z = -Mathf.Abs(distanceZ);
        }

        public void SetSmoothTime(float time)
        {
            smoothTime = Mathf.Max(0.01f, time);
        }
    }
}
