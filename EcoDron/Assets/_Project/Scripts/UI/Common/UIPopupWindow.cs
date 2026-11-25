using UnityEngine;

namespace EcoDron.UI.Common
{
    /// <summary>
    /// Animates modal popups cleanly using unscaled time.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIPopupWindow : MonoBehaviour
    {
        [SerializeField] private RectTransform cardTransform;
        [SerializeField] private float transitionSpeed = 12f;
        [SerializeField] private float closedScale = 0.92f;

        private CanvasGroup _canvasGroup;
        private bool _isOpen;
        private float _progress;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (cardTransform == null && transform.childCount > 0)
            {
                cardTransform = transform.GetChild(0) as RectTransform;
            }
        }

        public void InitializeClosed()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            _isOpen = false;
            _progress = 0f;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }

        public void Open()
        {
            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
            gameObject.SetActive(true);
            _isOpen = true;
            _progress = 0f;
            _canvasGroup.alpha = 0f;
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            if (cardTransform != null)
            {
                cardTransform.localScale = Vector3.one * closedScale;
            }
        }

        public void Close()
        {
            _isOpen = false;
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            float target = _isOpen ? 1f : 0f;
            _progress = Mathf.MoveTowards(_progress, target, Time.unscaledDeltaTime * transitionSpeed);
            float eased = 1f - Mathf.Pow(1f - _progress, 3f);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = eased;
            }

            if (cardTransform != null)
            {
                float scale = Mathf.Lerp(closedScale, 1f, eased);
                cardTransform.localScale = new Vector3(scale, scale, 1f);
            }

            if (!_isOpen && _progress <= 0.001f && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
