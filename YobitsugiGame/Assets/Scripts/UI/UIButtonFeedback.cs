using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Yobitsugi.UI
{
    /// <summary>Small hover/press scale feedback so buttons don't feel like dead static art.</summary>
    public class UIButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private float hoverScale = 1.06f;
        [SerializeField] private float pressScale = 0.94f;
        [SerializeField] private float duration = 0.12f;

        private Vector3 baseScale;

        private void Awake()
        {
            baseScale = transform.localScale;
        }

        public void OnPointerEnter(PointerEventData eventData) => Animate(hoverScale);
        public void OnPointerExit(PointerEventData eventData) => Animate(1f);
        public void OnPointerDown(PointerEventData eventData) => Animate(pressScale);
        public void OnPointerUp(PointerEventData eventData) => Animate(hoverScale);

        private void Animate(float scale)
        {
            transform.DOKill();
            transform.DOScale(baseScale * scale, duration).SetEase(Ease.OutQuad).SetLink(gameObject);
        }
    }
}
