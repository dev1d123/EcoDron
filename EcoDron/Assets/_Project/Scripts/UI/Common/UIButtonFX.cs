using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoDron.UI.Common
{
    /// <summary>
    /// Adds responsive hover and press scale/tint feedback to uGUI buttons.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class UIButtonFX : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        ISelectHandler,
        IDeselectHandler
    {
        [Header("Scale Feedback")]
        [SerializeField] private float hoverScale = 1.035f;
        [SerializeField] private float pressScale = 0.965f;
        [SerializeField] private float animationSpeed = 14f;

        [Header("Optional Tint Target")]
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalTint = Color.white;
        [SerializeField] private Color hoverTint = new Color(1f, 1f, 1f, 1f);
        [SerializeField] private Color pressTint = new Color(0.90f, 0.94f, 0.92f, 1f);

        [Header("Optional Audio")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip hoverClip;
        [SerializeField] private AudioClip clickClip;

        private RectTransform _rectTransform;
        private Selectable _selectable;
        private Vector3 _baseScale = Vector3.one;
        private bool _isHovered;
        private bool _isPressed;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _selectable = GetComponent<Selectable>();
            _baseScale = _rectTransform.localScale;
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }
        }

        private void OnEnable()
        {
            _isHovered = false;
            _isPressed = false;
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _baseScale;
            }
            ApplyTint(normalTint);
        }

        private void Update()
        {
            if (_selectable != null && !_selectable.interactable)
            {
                _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, _baseScale, Time.unscaledDeltaTime * animationSpeed);
                return;
            }

            float targetMultiplier = _isPressed ? pressScale : (_isHovered ? hoverScale : 1f);
            Vector3 desiredScale = _baseScale * targetMultiplier;
            _rectTransform.localScale = Vector3.Lerp(_rectTransform.localScale, desiredScale, Time.unscaledDeltaTime * animationSpeed);

            Color desiredTint = _isPressed ? pressTint : (_isHovered ? hoverTint : normalTint);
            if (targetGraphic != null)
            {
                targetGraphic.color = Color.Lerp(targetGraphic.color, desiredTint, Time.unscaledDeltaTime * animationSpeed);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            _isHovered = true;
            PlayClip(hoverClip);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            _isPressed = true;
            PlayClip(clickClip);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
        }

        public void OnSelect(BaseEventData eventData)
        {
            if (_selectable != null && !_selectable.interactable) return;
            _isHovered = true;
        }

        public void OnDeselect(BaseEventData eventData)
        {
            _isHovered = false;
            _isPressed = false;
        }

        private void ApplyTint(Color tint)
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = tint;
            }
        }

        private void PlayClip(AudioClip clip)
        {
            if (sfxSource != null && clip != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
