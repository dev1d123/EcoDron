using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoDron.Drone
{
    [RequireComponent(typeof(Rigidbody))]
    public class DroneController : MonoBehaviour
    {
        [Header("Flight Dynamics")]
        [Tooltip("Maximum horizontal speed (A/D)")]
        [SerializeField] private float maxHorizontalSpeed = 10f;

        [Tooltip("Maximum vertical climb/descent speed (W/S)")]
        [SerializeField] private float maxVerticalSpeed = 8f;

        [Tooltip("Time in seconds to reach full speed (lower = more responsive)")]
        [SerializeField] private float accelerationTime = 0.12f;

        [Tooltip("Time in seconds to come to a full stop when releasing keys")]
        [SerializeField] private float decelerationTime = 0.18f;

        [Header("2.5D Constraint")]
        [Tooltip("Lock drone strictly to the 2.5D plane")]
        [SerializeField] private bool lockZPosition = true;
        [SerializeField] private float targetZ = 0f;

        [Header("Tilt & Banking Aesthetics")]
        [Tooltip("Maximum roll tilt angle when moving horizontally (degrees)")]
        [SerializeField] private float maxRollAngle = 22f;

        [Tooltip("Maximum pitch tilt angle when climbing/descending (degrees)")]
        [SerializeField] private float maxPitchAngle = 10f;

        [Tooltip("Smooth time for tilt rotation (lower = quicker response)")]
        [SerializeField] private float tiltSmoothTime = 0.1f;

        [Header("Propellers & Rotors")]
        [Tooltip("Rotor spin speed at idle (degrees/sec)")]
        [SerializeField] private float idleRotorSpeed = 1800f;

        [Tooltip("Rotor spin speed under full throttle")]
        [SerializeField] private float activeRotorSpeed = 4000f;

        [Header("Cargo Weight & Inertia Dynamics")]
        [Tooltip("Additional inertia/drag applied to acceleration and braking when carrying cargo")]
        [SerializeField] private float cargoInertiaMultiplier = 0.6f;

        [Tooltip("Downward gravity pull speed when carrying cargo without upward throttle (W)")]
        [SerializeField] private float cargoGravitySagSpeed = 2.8f;

        [Tooltip("Extra roll tilt induced by the offset weight of the cargo")]
        [SerializeField] private float cargoWeightTiltAngle = 12f;

        private Rigidbody rb;
        private DroneHook hook;
        private Vector2 moveInput;
        private Vector3 currentVelocity = Vector3.zero;
        private Vector3 desiredVelocity = Vector3.zero;
        private Vector3 velocitySmoothing = Vector3.zero;

        private Quaternion baseRotation;
        private float currentRoll = 0f;
        private float currentPitch = 0f;
        private float rollVelocity = 0f;
        private float pitchVelocity = 0f;

        // Structured Rotors
        private class RotorHub
        {
            public Transform hubTransform;
            public float spinDirection; // +1 or -1
        }
        private readonly List<RotorHub> rotorHubs = new List<RotorHub>();

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            hook = GetComponent<DroneHook>();
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Essential for silky smooth camera tracking
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Use PhysX constraints so Z and rotation are natively locked without resetting interpolation buffer
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            if (lockZPosition)
            {
                rb.constraints |= RigidbodyConstraints.FreezePositionZ;
            }

            baseRotation = transform.localRotation;
            if (lockZPosition)
            {
                targetZ = transform.position.z;
            }

            SetupRotorHubs();
        }

        public void SetupRotorHubs()
        {
            rotorHubs.Clear();

            var bladeDict = new Dictionary<string, List<Transform>>();
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith("Blade_"))
                {
                    string[] parts = t.name.Split('_');
                    if (parts.Length >= 2)
                    {
                        string rotorKey = $"{parts[0]}_{parts[1]}";
                        if (!bladeDict.ContainsKey(rotorKey))
                            bladeDict[rotorKey] = new List<Transform>();
                        bladeDict[rotorKey].Add(t);
                    }
                }
            }

            var spinDirections = new Dictionary<string, float>
            {
                { "Blade_FL",  1f },
                { "Blade_FR", -1f },
                { "Blade_BL", -1f },
                { "Blade_BR",  1f }
            };

            foreach (var kvp in bladeDict)
            {
                string key = kvp.Key;
                List<Transform> blades = kvp.Value;
                if (blades.Count == 0) continue;

                string hubName = $"RotorHub_{key}";
                Transform existingHub = transform.Find(hubName);
                if (existingHub != null)
                {
                    float dir = spinDirections.ContainsKey(key) ? spinDirections[key] : 1f;
                    rotorHubs.Add(new RotorHub { hubTransform = existingHub, spinDirection = dir });
                    continue;
                }

                Vector3 center = Vector3.zero;
                int validCount = 0;
                foreach (var b in blades)
                {
                    Renderer r = b.GetComponent<Renderer>();
                    if (r != null)
                    {
                        center += r.bounds.center;
                        validCount++;
                    }
                    else
                    {
                        center += b.position;
                        validCount++;
                    }
                }
                if (validCount > 0) center /= validCount;

                GameObject hubObj = new GameObject(hubName);
                hubObj.transform.position = center;
                hubObj.transform.rotation = transform.rotation;
                hubObj.transform.SetParent(transform, true);

                foreach (var b in blades)
                {
                    b.SetParent(hubObj.transform, true);
                }

                float spinDir = spinDirections.ContainsKey(key) ? spinDirections[key] : 1f;
                rotorHubs.Add(new RotorHub { hubTransform = hubObj.transform, spinDirection = spinDir });
            }
        }

        private void Update()
        {
            ReadInput();
            SpinRotors();
        }

        private void FixedUpdate()
        {
            ApplyFlightVelocity();
            ApplyAestheticRotation();
        }

        private void ReadInput()
        {
            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var k = Keyboard.current;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
                if (k.wKey.isPressed || k.upArrowKey.isPressed)   v += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            }
            else
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }
#else
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
#endif

            moveInput = new Vector2(h, v);
        }

        /// <summary>
        /// Smooth velocity interpolation using continuous SmoothDamp.
        /// Naturally incorporates payload weight, downward gravity sag, and heavier inertia when carrying cargo.
        /// </summary>
        private void ApplyFlightVelocity()
        {
            float cargoMass = (hook != null) ? hook.AttachedCargoMass : 0f;
            bool isCarrying = cargoMass > 0.01f;

            // When carrying cargo: max speeds are heavier
            float speedFactor = isCarrying ? (1f / (1f + cargoMass * 0.15f)) : 1f;
            float currentMaxH = maxHorizontalSpeed * speedFactor;
            float currentMaxV = maxVerticalSpeed * speedFactor;

            desiredVelocity.x = moveInput.x * currentMaxH;
            desiredVelocity.y = moveInput.y * currentMaxV;
            desiredVelocity.z = 0f;

            // When carrying cargo and not thrusting upwards, gravity visibly sags/sinks the drone down
            if (isCarrying && moveInput.y <= 0.05f)
            {
                float sagSpeed = cargoGravitySagSpeed * (1f + cargoMass * 0.35f);
                desiredVelocity.y -= sagSpeed;
            }

            // Inertia multiplier: takes longer to accelerate and longer to brake with heavy payload
            float inertiaFactor = isCarrying ? (1f + cargoMass * cargoInertiaMultiplier) : 1f;
            float xTime = ((Mathf.Abs(moveInput.x) > 0.01f) ? accelerationTime : decelerationTime) * inertiaFactor;
            float yTime = ((Mathf.Abs(moveInput.y) > 0.01f) ? accelerationTime : decelerationTime) * inertiaFactor;

            currentVelocity.x = Mathf.SmoothDamp(currentVelocity.x, desiredVelocity.x, ref velocitySmoothing.x, xTime, Mathf.Infinity, Time.fixedDeltaTime);
            currentVelocity.y = Mathf.SmoothDamp(currentVelocity.y, desiredVelocity.y, ref velocitySmoothing.y, yTime, Mathf.Infinity, Time.fixedDeltaTime);
            currentVelocity.z = 0f;

#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = currentVelocity;
#else
            rb.velocity = currentVelocity;
#endif
        }

        /// <summary>
        /// Applies smooth quadcopter banking tilt via Rigidbody.MoveRotation in FixedUpdate.
        /// Also tilts towards the cargo weight displacement when holding payload.
        /// </summary>
        private void ApplyAestheticRotation()
        {
            float targetRoll = -(currentVelocity.x / maxHorizontalSpeed) * maxRollAngle;
            float targetPitch = (currentVelocity.y / maxVerticalSpeed) * maxPitchAngle;

            // Add asymmetric roll tilt from cargo displacement relative to drone center
            if (hook != null && hook.IsHoldingCargo)
            {
                Vector3 clawOffset = hook.ClawPosition - transform.position;
                float cargoRollOffset = -Mathf.Clamp(clawOffset.x * 1.5f, -1f, 1f) * cargoWeightTiltAngle;
                targetRoll += cargoRollOffset;
            }

            currentRoll = Mathf.SmoothDamp(currentRoll, targetRoll, ref rollVelocity, tiltSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);
            currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVelocity, tiltSmoothTime, Mathf.Infinity, Time.fixedDeltaTime);

            Quaternion worldTilt = Quaternion.Euler(currentPitch, 0f, currentRoll);
            Quaternion targetRotation = worldTilt * baseRotation;

            rb.MoveRotation(targetRotation);
        }

        /// <summary>
        /// Rotates the rotor blades every frame in Update for maximum visual fluidity (60/120/144Hz).
        /// Spins faster under heavy cargo load.
        /// </summary>
        private void SpinRotors()
        {
            if (rotorHubs == null || rotorHubs.Count == 0) return;

            float cargoBoost = (hook != null && hook.IsHoldingCargo) ? 0.35f : 0f;
            float throttleNorm = Mathf.Clamp01(Mathf.Max(Mathf.Abs(moveInput.x), Mathf.Abs(moveInput.y)) + cargoBoost);
            float currentSpeed = Mathf.Lerp(idleRotorSpeed, activeRotorSpeed, throttleNorm);
            float step = currentSpeed * Time.deltaTime;

            for (int i = 0; i < rotorHubs.Count; i++)
            {
                var hub = rotorHubs[i];
                if (hub != null && hub.hubTransform != null)
                {
                    hub.hubTransform.Rotate(Vector3.forward, step * hub.spinDirection, Space.Self);
                }
            }
        }

        public void SetLockZ(bool lockZ, float z = 0f)
        {
            lockZPosition = lockZ;
            targetZ = z;
            if (rb != null)
            {
                if (lockZ) rb.constraints |= RigidbodyConstraints.FreezePositionZ;
                else rb.constraints &= ~RigidbodyConstraints.FreezePositionZ;
            }
        }
    }
}
