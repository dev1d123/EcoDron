using System;
using System.Collections.Generic;
using EcoDron.Interaction;
using UnityEngine;

namespace EcoDron.Gameplay
{
    /// <summary>
    /// Detects GrabbableObject trash items deposited inside the trash bin trigger volume.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrashBinZone : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [SerializeField] private Renderer indicatorRenderer;
        [SerializeField] private Color idleColor = new Color(0.95f, 0.72f, 0.15f);
        [SerializeField] private Color completedColor = new Color(0.15f, 0.92f, 0.42f);

        private readonly HashSet<GrabbableObject> _depositedItems = new HashSet<GrabbableObject>();
        private Material _indicatorMat;

        public event Action<GrabbableObject, int> OnTrashDeposited;
        public int DepositedCount => _depositedItems.Count;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            if (indicatorRenderer != null)
            {
                _indicatorMat = indicatorRenderer.material;
                SetIndicatorColor(idleColor);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            GrabbableObject grabbable = other.GetComponentInParent<GrabbableObject>();
            if (grabbable == null) return;

            // Only count once the player releases the trash inside the bin
            if (grabbable.IsGrabbed) return;

            if (_depositedItems.Add(grabbable))
            {
                Debug.Log($"<b><color=#00FF7F>[TrashBinZone]</color></b> ¡Basura depositada dentro del tacho: <b>{grabbable.name}</b>! Total en tacho: {_depositedItems.Count}");
                SetIndicatorColor(completedColor);
                OnTrashDeposited?.Invoke(grabbable, _depositedItems.Count);
            }
        }

        public void SetCompletedVisual(bool completed)
        {
            SetIndicatorColor(completed ? completedColor : idleColor);
        }

        private void SetIndicatorColor(Color color)
        {
            if (_indicatorMat == null) return;
            if (_indicatorMat.HasProperty("_BaseColor"))
            {
                _indicatorMat.SetColor("_BaseColor", color);
            }
            else if (_indicatorMat.HasProperty("_Color"))
            {
                _indicatorMat.color = color;
            }
        }
    }
}
