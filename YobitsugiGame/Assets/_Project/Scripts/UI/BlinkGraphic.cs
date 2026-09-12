using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Yobitsugi.UI
{
    /// <summary>Pulses and bobs a graphic, used for the "click to continue" marker.</summary>
    [RequireComponent(typeof(Graphic))]
    public class BlinkGraphic : MonoBehaviour
    {
        [SerializeField] private float cycleSeconds = 0.9f;
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float bobDistance = 5f;

        private Sequence sequence;

        private void OnEnable()
        {
            var graphic = GetComponent<Graphic>();
            var rect = (RectTransform)transform;
            float baseY = rect.anchoredPosition.y;

            sequence = DOTween.Sequence()
                .Join(graphic.DOFade(minAlpha, cycleSeconds).SetEase(Ease.InOutSine))
                .Join(rect.DOAnchorPosY(baseY - bobDistance, cycleSeconds).SetEase(Ease.InOutSine))
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void OnDisable()
        {
            sequence?.Kill();
            sequence = null;
        }
    }
}
