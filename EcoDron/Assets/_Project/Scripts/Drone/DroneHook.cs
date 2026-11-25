using System.Collections.Generic;
using UnityEngine;
using EcoDron.Interaction;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EcoDron.Drone
{
    /// <summary>
    /// Controls the drone's actual 3D modeled robotic arm and 3-finger claw.
    /// Completely removes legacy procedural primitives (rope/cylinder), and
    /// rigs the 3D model transforms to aim with the mouse and arrow keys,
    /// bend with 2-bone Inverse Kinematics, open/close the 3 claw fingers,
    /// and grab/fling cargo boxes.
    /// </summary>
    public class DroneHook : MonoBehaviour
    {
        [Header("Robotic Arm Kinematics")]
        [Tooltip("Maximum reach of the robotic arm when fully extended (meters)")]
        [SerializeField] private float maxReach = 1.3f;

        [Tooltip("Minimum reach of the robotic arm (meters)")]
        [SerializeField] private float minReach = 0.35f;

        [Tooltip("Speed of arm aiming towards mouse (smooth damp time)")]
        [SerializeField] private float armAimSmoothTime = 0.05f;

        [Tooltip("Allow telescopic reach scaling beyond natural 3D arm length (DISABLED: Arm is 100% rigid)")]
        [SerializeField] private bool allowTelescopicExtension = false;

        [Header("Mouse Wheel Winch & Nudge")]
        [Tooltip("Sensitivity of the mouse scroll wheel to extend/retract arm reach")]
        [SerializeField] private float mouseScrollSensitivity = 0.8f;

        [Tooltip("Speed of extending/retracting with Arrow Keys (m/s)")]
        [SerializeField] private float arrowNudgeSpeed = 2.5f;

        [Header("Claw & Fingers Configuration")]
        [Tooltip("Outward rotation angle of each claw finger when open (degrees)")]
        [SerializeField] private float fingerOpenAngle = 68f;

        [Tooltip("Speed of opening/closing fingers animation")]
        [SerializeField] private float fingerAnimationSpeed = 16f;

        [Tooltip("Detection radius around claw to grab cargo")]
        [SerializeField] private float grabRadius = 1.8f;

        [Header("Mouse Fling / Slingshot Mechanics")]
        [Tooltip("Multiplier for throwing cargo with mouse flick velocity upon release")]
        [SerializeField] private float flingForceMultiplier = 1.45f;

        [Tooltip("Maximum fling velocity boost allowed")]
        [SerializeField] private float maxFlingBoost = 15.0f;

        [Header("Visual Feedback & Aiming Guide")]
        [SerializeField] private bool showAimingLaser = true;
        [SerializeField] private Color laserIdleColor = new Color(0.0f, 0.85f, 1.0f, 0.35f);
        [SerializeField] private Color laserLockedColor = new Color(0.1f, 1.0f, 0.5f, 0.85f);
        [SerializeField] private Color ledOpenColor = new Color(0.1f, 1.0f, 0.4f);
        [SerializeField] private Color ledCarryingColor = new Color(1.0f, 0.75f, 0.1f);
        [SerializeField] private Color ledClosedColor = new Color(0.0f, 0.8f, 1.0f);

        [Header("Status (Read Only)")]
        [SerializeField] private bool isClawOpen = false;
        [SerializeField] private GrabbableObject attachedCargo = null;
        [SerializeField] private GrabbableObject hoveredCargo = null;

        // Auto-rigged pivots
        private Transform shoulderPivot;
        private Transform elbowPivot;
        private Transform wristPivot;
        private List<FingerRig> fingerRigs = new List<FingerRig>();

        // Rest orientations & vectors
        private Vector3 initialUpperArmVector;
        private Vector3 initialForearmVector;
        private Vector3 initialWristForwardVector;
        private Quaternion initialShoulderWorldRot;
        private Quaternion initialElbowWorldRot;
        private Quaternion initialWristWorldRot;

        private float naturalUpperArmLength = 0.55f;
        private float naturalForearmLength = 0.74f;
        private float naturalTotalReach = 1.29f;

        private float currentClawOpenAmount = 0f; // 0 = closed, 1 = open
        private float targetReachDistance = 2.4f;
        private float currentReachDistance = 2.4f;

        private Vector3 desiredArmTarget;
        private Vector3 currentArmTarget;
        private Vector3 armTargetVelocity;
        private Vector3 directionalOffset = Vector3.zero;

        private Vector3 mouseWorldPos;
        private Vector3 prevMouseWorldPos;
        private Vector3 mouseWorldVelocity;
        private float targetZ = 0f;

        private Camera mainCamera;
        private Rigidbody droneRb;
        private ConfigurableJoint currentCargoJoint;

        private LineRenderer aimingLaser;
        private Renderer ledRingRenderer;

        public bool IsClawOpen => isClawOpen;
        public bool IsHoldingCargo => attachedCargo != null;
        public float AttachedCargoMass => (attachedCargo != null && attachedCargo.Rigidbody != null) ? attachedCargo.Rigidbody.mass : 0f;
        public GrabbableObject AttachedCargo => attachedCargo;
        public Vector3 ClawPosition => wristPivot != null ? wristPivot.position : transform.position;

        private class FingerRig
        {
            public Transform pivot;
            public Quaternion initialWorldRot;
            public Vector3 hingeAxis;
        }

        private void Awake()
        {
            droneRb = GetComponent<Rigidbody>();
            mainCamera = Camera.main;
            targetZ = transform.position.z;

            CleanUpLegacyProceduralObjects();
            BuildRoboticArmHierarchy();
            SetupAimingVisuals();
        }

        private void Start()
        {
            if (shoulderPivot != null)
            {
                currentArmTarget = shoulderPivot.position + Vector3.down * currentReachDistance;
                desiredArmTarget = currentArmTarget;
                prevMouseWorldPos = currentArmTarget;
            }
        }

        private void FixedUpdate()
        {
            // Continuously update joint anchor to track the moving claw in physics
            if (currentCargoJoint != null && wristPivot != null)
            {
                currentCargoJoint.anchor = transform.InverseTransformPoint(wristPivot.position);
            }
        }

        /// <summary>
        /// Destroys legacy procedural primitives (the old grey rope and green cylinder).
        /// </summary>
        private void CleanUpLegacyProceduralObjects()
        {
            Transform oldClaw = transform.Find("HookClaw");
            if (oldClaw != null) Destroy(oldClaw.gameObject);

            Transform oldOrigin = transform.Find("CableOrigin");
            if (oldOrigin != null) Destroy(oldOrigin.gameObject);

            Transform oldReticle = transform.Find("MouseReticle");
            if (oldReticle != null) Destroy(oldReticle.gameObject);

            LineRenderer lr = GetComponent<LineRenderer>();
            if (lr != null) Destroy(lr);
        }

        /// <summary>
        /// Finds the actual 3D modeled arm nodes from EcoDron.fbx and builds
        /// an articulated kinematic hierarchy with shoulder, elbow, wrist, and 3-finger pivots.
        /// </summary>
        private void BuildRoboticArmHierarchy()
        {
            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            Dictionary<string, Transform> map = new Dictionary<string, Transform>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var t in allChildren)
            {
                if (!map.ContainsKey(t.name)) map.Add(t.name, t);
            }

            // 1. Locate key joint centers using bounds center for exact world positions
            Vector3 shoulderPos = GetWorldCenter(map, "Arm_BaseHinge", transform.position + new Vector3(-0.02f, -0.3f, 0f));
            Vector3 elbowPos = GetWorldCenter(map, "Arm_ElbowJoint", shoulderPos + new Vector3(-0.25f, -0.4f, 0f));
            Vector3 wristPos = GetWorldCenter(map, "Claw_Palm_Chassis", elbowPos + new Vector3(-0.35f, -0.5f, 0f));

            initialUpperArmVector = (elbowPos - shoulderPos);
            naturalUpperArmLength = Mathf.Max(0.1f, initialUpperArmVector.magnitude);
            initialForearmVector = (wristPos - elbowPos);
            naturalForearmLength = Mathf.Max(0.1f, initialForearmVector.magnitude);
            naturalTotalReach = naturalUpperArmLength + naturalForearmLength;
            maxReach = naturalTotalReach;
            minReach = Mathf.Min(minReach, naturalTotalReach * 0.35f);
            targetReachDistance = Mathf.Clamp(targetReachDistance, minReach, maxReach);
            currentReachDistance = targetReachDistance;

            // 2. Create Shoulder Pivot
            GameObject shoulderObj = new GameObject("Arm_ShoulderPivot");
            shoulderObj.transform.position = shoulderPos;
            shoulderObj.transform.rotation = transform.rotation;
            shoulderObj.transform.SetParent(transform, true);
            shoulderPivot = shoulderObj.transform;
            initialShoulderWorldRot = shoulderPivot.rotation;

            // 3. Create Elbow Pivot
            GameObject elbowObj = new GameObject("Arm_ElbowPivot");
            elbowObj.transform.position = elbowPos;
            elbowObj.transform.rotation = transform.rotation;
            elbowObj.transform.SetParent(shoulderPivot, true);
            elbowPivot = elbowObj.transform;
            initialElbowWorldRot = elbowPivot.rotation;

            // 4. Create Wrist / Palm Pivot
            GameObject wristObj = new GameObject("Arm_WristPivot");
            wristObj.transform.position = wristPos;
            wristObj.transform.rotation = transform.rotation;
            wristObj.transform.SetParent(elbowPivot, true);
            wristPivot = wristObj.transform;
            initialWristWorldRot = wristPivot.rotation;

            initialWristForwardVector = (wristPos - elbowPos).normalized;

            // 5. Parent Upper Arm meshes to Shoulder Pivot
            string[] shoulderParts = { "Arm_BaseHinge", "Arm_ShoulderPin", "Arm_Shoulder_Washer_-1", "Arm_Shoulder_Washer_1", "Arm_Segment1", "Arm_Seg1_Strip" };
            foreach (var part in shoulderParts)
            {
                if (map.TryGetValue(part, out Transform t)) t.SetParent(shoulderPivot, true);
            }

            // 6. Parent Forearm meshes to Elbow Pivot
            string[] elbowParts = { "Arm_ElbowJoint", "Arm_ElbowCap", "Arm_Segment2", "Cable_Arm" };
            foreach (var part in elbowParts)
            {
                if (map.TryGetValue(part, out Transform t)) t.SetParent(elbowPivot, true);
            }

            // 7. Parent Wrist & Palm chassis to Wrist Pivot
            string[] wristParts = {
                "Claw_Palm_Chassis", "Claw_Axis1_PitchCylinder", "Claw_Axis1_PitchServo",
                "Claw_Axis2_ForkArm_-1", "Claw_Axis2_ForkArm_1", "Claw_Axis2_YawBearing",
                "Claw_Axis3_LEDRing", "Claw_Axis3_RollCollar", "Claw_Axis3_ToolFlange", "Claw_CentralDriveRam"
            };
            foreach (var part in wristParts)
            {
                if (map.TryGetValue(part, out Transform t))
                {
                    t.SetParent(wristPivot, true);
                    if (part == "Claw_Axis3_LEDRing") ledRingRenderer = t.GetComponent<Renderer>();
                }
            }

            // 8. Build 3-Finger Rigs (Finger 0, 1, 2)
            fingerRigs.Clear();
            for (int i = 0; i < 3; i++)
            {
                string hingeName = $"Claw_BaseHinge_{i}";
                Vector3 fingerHingePos = GetWorldCenter(map, hingeName, wristPos);

                GameObject fingerObj = new GameObject($"Claw_Finger_{i}_Pivot");
                fingerObj.transform.position = fingerHingePos;
                fingerObj.transform.rotation = wristPivot.rotation;
                fingerObj.transform.SetParent(wristPivot, true);

                // Compute radial outward direction and hinge rotation axis
                Vector3 radialOut = (fingerHingePos - wristPos);
                if (radialOut.sqrMagnitude < 0.0001f)
                    radialOut = Quaternion.Euler(0, 0, i * 120f) * Vector3.right;
                Vector3 hingeAxis = Vector3.Cross(initialWristForwardVector, radialOut.normalized).normalized;
                if (hingeAxis.sqrMagnitude < 0.001f) hingeAxis = Vector3.forward;

                FingerRig rig = new FingerRig
                {
                    pivot = fingerObj.transform,
                    initialWorldRot = fingerObj.transform.rotation,
                    hingeAxis = hingeAxis
                };

                // Parent all meshes belonging to this finger
                string suffix = $"_{i}";
                foreach (var kvp in map)
                {
                    if (kvp.Key.StartsWith("Claw_") && kvp.Key.EndsWith(suffix))
                    {
                        kvp.Value.SetParent(fingerObj.transform, true);
                    }
                }

                fingerRigs.Add(rig);
            }

            Debug.Log($"<b><color=#00FF7F>[DroneHook]</color></b> Brazo robótico 3D auto-rigged con éxito: hombro, codo, muñeca y {fingerRigs.Count} dedos de garra.");
        }

        private Vector3 GetWorldCenter(Dictionary<string, Transform> map, string key, Vector3 fallback)
        {
            if (map.TryGetValue(key, out Transform t))
            {
                Renderer rend = t.GetComponent<Renderer>();
                if (rend != null) return rend.bounds.center;
                return t.position;
            }
            return fallback;
        }

        private void SetupAimingVisuals()
        {
            if (showAimingLaser && aimingLaser == null)
            {
                GameObject laserObj = new GameObject("ArmAimingLaser");
                laserObj.transform.SetParent(transform);
                aimingLaser = laserObj.AddComponent<LineRenderer>();
                aimingLaser.startWidth = 0.02f;
                aimingLaser.endWidth = 0.008f;
                aimingLaser.positionCount = 2;
                aimingLaser.material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
                aimingLaser.startColor = laserIdleColor;
                aimingLaser.endColor = laserIdleColor;
                aimingLaser.useWorldSpace = true;
            }
        }

        private void Update()
        {
            UpdateMouseCoordinates();
            HandlePlayerInputs();
            SolveArmInverseKinematics();
            AnimateClawFingers();
            UpdateVisualFeedback();
        }

        private void UpdateMouseCoordinates()
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector2 mouseScreenPos = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null) mouseScreenPos = Mouse.current.position.ReadValue();
#else
            mouseScreenPos = Input.mousePosition;
#endif

            Plane plane = new Plane(Vector3.forward, new Vector3(0f, 0f, targetZ));
            Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
            if (plane.Raycast(ray, out float enter))
            {
                mouseWorldPos = ray.GetPoint(enter);
                mouseWorldPos.z = targetZ;

                if (Time.deltaTime > 0.0001f)
                {
                    Vector3 rawVel = (mouseWorldPos - prevMouseWorldPos) / Time.deltaTime;
                    mouseWorldVelocity = Vector3.Lerp(mouseWorldVelocity, rawVel, Time.deltaTime * 15f);
                }
                prevMouseWorldPos = mouseWorldPos;
            }
        }

        private void HandlePlayerInputs()
        {
            // 1. Mouse Scroll Wheel: Winch reach extend/retract
            float scrollDelta = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                scrollDelta = Mouse.current.scroll.ReadValue().y;
                if (Mathf.Abs(scrollDelta) > 0.01f) scrollDelta = Mathf.Sign(scrollDelta) * 0.5f;
            }
#else
            scrollDelta = Input.GetAxis("Mouse ScrollWheel");
#endif
            if (Mathf.Abs(scrollDelta) > 0.001f)
            {
                targetReachDistance -= scrollDelta * mouseScrollSensitivity;
                targetReachDistance = Mathf.Clamp(targetReachDistance, minReach, maxReach);
            }
            currentReachDistance = Mathf.Lerp(currentReachDistance, targetReachDistance, Time.deltaTime * 10f);

            // 2. Arrow Keys: Fine-tune arm offset
            float arrowX = 0f;
            float arrowY = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var k = Keyboard.current;
                if (k.upArrowKey.isPressed) arrowY += 1f;
                if (k.downArrowKey.isPressed) arrowY -= 1f;
                if (k.leftArrowKey.isPressed) arrowX -= 1f;
                if (k.rightArrowKey.isPressed) arrowX += 1f;
            }
#else
            if (Input.GetKey(KeyCode.UpArrow)) arrowY += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) arrowY -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) arrowX -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) arrowX += 1f;
#endif
            directionalOffset.x += arrowX * arrowNudgeSpeed * Time.deltaTime;
            directionalOffset.y += arrowY * arrowNudgeSpeed * Time.deltaTime;
            directionalOffset.x = Mathf.Clamp(directionalOffset.x, -2.5f, 2.5f);
            directionalOffset.y = Mathf.Clamp(directionalOffset.y, -2.5f, 2.5f);

            if (Mathf.Abs(arrowX) < 0.01f) directionalOffset.x = Mathf.MoveTowards(directionalOffset.x, 0f, Time.deltaTime * 1.5f);
            if (Mathf.Abs(arrowY) < 0.01f) directionalOffset.y = Mathf.MoveTowards(directionalOffset.y, 0f, Time.deltaTime * 1.5f);

            // 3. Claw Actions: Space or Left-Click opens, Right-Click closes
            bool openInput = false;
            bool closeInput = false;

#if ENABLE_INPUT_SYSTEM
            var m = Mouse.current;
            var kb = Keyboard.current;
            if ((kb != null && kb.spaceKey.wasPressedThisFrame) || (m != null && m.leftButton.wasPressedThisFrame))
            {
                if (isClawOpen && attachedCargo == null) closeInput = true;
                else openInput = true;
            }
            if (m != null && m.rightButton.wasPressedThisFrame) closeInput = true;
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            {
                if (isClawOpen && attachedCargo == null) closeInput = true;
                else openInput = true;
            }
            if (Input.GetMouseButtonDown(1)) closeInput = true;
#endif

            if (openInput) OpenClaw();
            else if (closeInput) CloseClaw();
        }

        /// <summary>
        /// Solves 2-bone Inverse Kinematics on the 2.5D XY plane so the 3D robotic arm
        /// points and bends smoothly towards the target or cargo.
        /// </summary>
        private void SolveArmInverseKinematics()
        {
            if (shoulderPivot == null || elbowPivot == null || wristPivot == null) return;

            Vector3 shoulderPos = shoulderPivot.position;

            // Direction from shoulder to mouse cursor on the 2.5D plane
            Vector3 aimDir = mouseWorldPos - shoulderPos;
            aimDir.z = 0f;

            float dist = aimDir.magnitude;
            if (dist > maxReach) aimDir = aimDir.normalized * maxReach;
            if (dist < minReach) aimDir = aimDir.normalized * minReach;

            // Restrict upper elevation so it stays beneath the rotor disk
            if (aimDir.y > -0.05f) aimDir.y = -0.05f;

            // Direct player mouse aim (NO MAGNET EFFECT - 100% free manual control)
            desiredArmTarget = shoulderPos + aimDir + directionalOffset;
            desiredArmTarget.z = targetZ;

            // Check hovered cargo within grab radius for visual laser feedback only (NO PULL / NO MAGNET)
            hoveredCargo = FindNearestGrabbable(wristPivot.position, grabRadius);

            // Smooth target position
            currentArmTarget = Vector3.SmoothDamp(currentArmTarget, desiredArmTarget, ref armTargetVelocity, armAimSmoothTime);
            currentArmTarget.z = targetZ;

            // 2D Planar IK calculation (Fixed natural rigid length - NO STRETCHING)
            Vector3 targetDelta = currentArmTarget - shoulderPos;
            targetDelta.z = 0f;
            float targetDist = targetDelta.magnitude;
            if (targetDist < 0.01f) return;

            float l1 = naturalUpperArmLength;
            float l2 = naturalForearmLength;

            // Strict clamp to physical reach of the 3D model
            float d = Mathf.Clamp(targetDist, Mathf.Abs(l1 - l2) + 0.02f, (l1 + l2) - 0.002f);

            // Law of Cosines:
            float cosAlpha = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
            float cosBeta = Mathf.Clamp((l1 * l1 + l2 * l2 - d * d) / (2f * l1 * l2), -1f, 1f);

            float alpha = Mathf.Acos(cosAlpha) * Mathf.Rad2Deg;
            float beta = Mathf.Acos(cosBeta) * Mathf.Rad2Deg;

            // 1. Shoulder alignment using FromToRotation (Locked at 1.0 scale)
            Vector3 targetUpperArmDir = Quaternion.Euler(0, 0, -alpha) * targetDelta.normalized;
            shoulderPivot.rotation = Quaternion.FromToRotation(initialUpperArmVector.normalized, targetUpperArmDir) * initialShoulderWorldRot;
            shoulderPivot.localScale = Vector3.one;

            // 2. Elbow alignment bending towards wrist target (Locked at 1.0 scale)
            Vector3 currentElbowPos = shoulderPos + targetUpperArmDir * l1;
            Vector3 targetForearmDir = (currentArmTarget - currentElbowPos).normalized;
            elbowPivot.rotation = Quaternion.FromToRotation(initialForearmVector.normalized, targetForearmDir) * initialElbowWorldRot;
            elbowPivot.localScale = Vector3.one;

            // 3. Wrist alignment pointing towards target
            wristPivot.rotation = Quaternion.FromToRotation(initialWristForwardVector, targetForearmDir) * initialWristWorldRot;
        }

        /// <summary>
        /// Smoothly animates the 3 claw fingers rotating outwards (wide open) or inwards (closed).
        /// </summary>
        private void AnimateClawFingers()
        {
            float targetOpen = isClawOpen ? 1.0f : 0.0f;
            currentClawOpenAmount = Mathf.MoveTowards(currentClawOpenAmount, targetOpen, Time.deltaTime * fingerAnimationSpeed);

            Quaternion wristDelta = (wristPivot != null) ? (wristPivot.rotation * Quaternion.Inverse(initialWristWorldRot)) : Quaternion.identity;

            for (int i = 0; i < fingerRigs.Count; i++)
            {
                var rig = fingerRigs[i];
                if (rig == null || rig.pivot == null) continue;

                float angle = currentClawOpenAmount * fingerOpenAngle;
                Vector3 currentHingeAxis = wristDelta * rig.hingeAxis;
                rig.pivot.rotation = Quaternion.AngleAxis(angle, currentHingeAxis) * (wristDelta * rig.initialWorldRot);
            }
        }

        public void OpenClaw()
        {
            isClawOpen = true;
            ReleaseAttachedCargo();
            Debug.Log("<b><color=#00FF7F>[DroneHook]</color></b> Garras mecánicas ABIERTAS (Claw Opened).");
        }

        public void CloseClaw()
        {
            isClawOpen = false;

            // Grab nearest cargo if within reach
            Vector3 searchPos = wristPivot != null ? wristPivot.position : transform.position;
            GrabbableObject candidate = hoveredCargo ?? FindNearestGrabbable(searchPos, grabRadius);
            if (candidate != null)
            {
                AttachCargo(candidate);
            }
            Debug.Log("<b><color=#FFD700>[DroneHook]</color></b> Garras mecánicas CERRADAS (Claw Closed).");
        }

        private GrabbableObject FindNearestGrabbable(Vector3 searchCenter, float radius)
        {
            Collider[] hits = Physics.OverlapSphere(searchCenter, radius);
            GrabbableObject best = null;
            float closest = float.MaxValue;

            foreach (var hit in hits)
            {
                GrabbableObject g = hit.GetComponentInParent<GrabbableObject>();
                if (g != null && !g.IsGrabbed)
                {
                    float d = Vector3.Distance(searchCenter, g.AttachPoint.position);
                    if (d < closest)
                    {
                        closest = d;
                        best = g;
                    }
                }
            }
            return best;
        }

        private void AttachCargo(GrabbableObject cargo)
        {
            attachedCargo = cargo;
            cargo.OnGrabbed(wristPivot);

            currentCargoJoint = gameObject.AddComponent<ConfigurableJoint>();
            currentCargoJoint.connectedBody = cargo.Rigidbody;
            currentCargoJoint.autoConfigureConnectedAnchor = false;
            currentCargoJoint.anchor = transform.InverseTransformPoint(wristPivot.position);
            currentCargoJoint.connectedAnchor = cargo.transform.InverseTransformPoint(cargo.AttachPoint.position);

            currentCargoJoint.xMotion = ConfigurableJointMotion.Limited;
            currentCargoJoint.yMotion = ConfigurableJointMotion.Limited;
            currentCargoJoint.zMotion = ConfigurableJointMotion.Locked;
            currentCargoJoint.angularXMotion = ConfigurableJointMotion.Limited;
            currentCargoJoint.angularYMotion = ConfigurableJointMotion.Locked;
            currentCargoJoint.angularZMotion = ConfigurableJointMotion.Limited;

            SoftJointLimit linearLimit = new SoftJointLimit { limit = 0.05f };
            currentCargoJoint.linearLimit = linearLimit;
            SoftJointLimitSpring linearSpring = new SoftJointLimitSpring { spring = 500f, damper = 30f };
            currentCargoJoint.linearLimitSpring = linearSpring;

            SoftJointLimit angularLimit = new SoftJointLimit { limit = 25f };
            currentCargoJoint.angularYLimit = angularLimit;
            currentCargoJoint.angularZLimit = angularLimit;

            Debug.Log($"<b><color=#00E5FF>[DroneHook]</color></b> Objeto sujetado con garras mecánicas: [{cargo.name}]");
        }

        private void ReleaseAttachedCargo()
        {
            if (attachedCargo != null)
            {
                attachedCargo.OnReleased();

                if (attachedCargo.Rigidbody != null)
                {
                    Vector3 releaseVelocity = Vector3.zero;
                    if (droneRb != null)
                    {
#if UNITY_6000_0_OR_NEWER
                        releaseVelocity = droneRb.linearVelocity;
#else
                        releaseVelocity = droneRb.velocity;
#endif
                    }

                    // Mouse slingshot fling
                    Vector3 mouseFlick = Vector3.ClampMagnitude(mouseWorldVelocity * flingForceMultiplier, maxFlingBoost);
                    mouseFlick.z = 0f;
                    releaseVelocity += mouseFlick;

                    attachedCargo.Rigidbody.linearVelocity = releaseVelocity;
                }

                attachedCargo = null;
            }

            if (currentCargoJoint != null)
            {
                Destroy(currentCargoJoint);
                currentCargoJoint = null;
            }
        }

        private void UpdateVisualFeedback()
        {
            // Aiming Laser from wrist/claw to mouse target
            if (aimingLaser != null && showAimingLaser)
            {
                aimingLaser.enabled = (attachedCargo == null);
                if (aimingLaser.enabled && wristPivot != null)
                {
                    aimingLaser.SetPosition(0, wristPivot.position);
                    aimingLaser.SetPosition(1, currentArmTarget);

                    Color targetColor = (hoveredCargo != null) ? laserLockedColor : laserIdleColor;
                    aimingLaser.startColor = targetColor;
                    aimingLaser.endColor = targetColor;
                }
            }

            // LED Ring Color Feedback
            if (ledRingRenderer != null)
            {
                Color ledColor = isClawOpen ? ledOpenColor :
                                (attachedCargo != null ? ledCarryingColor : ledClosedColor);

                if (ledRingRenderer.material.HasProperty("_EmissionColor"))
                {
                    ledRingRenderer.material.SetColor("_EmissionColor", ledColor * 1.5f);
                    ledRingRenderer.material.EnableKeyword("_EMISSION");
                }
                else if (ledRingRenderer.material.HasProperty("_BaseColor"))
                {
                    ledRingRenderer.material.SetColor("_BaseColor", ledColor);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (wristPivot != null)
            {
                Gizmos.color = isClawOpen ? Color.green : Color.yellow;
                Gizmos.DrawWireSphere(wristPivot.position, grabRadius);
            }
        }
    }
}
