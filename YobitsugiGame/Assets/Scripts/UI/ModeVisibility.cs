using DG.Tweening;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    /// <summary>
    /// Shows/hides a target object based on the current game mode.
    /// Lives on an always-active object (never on the target itself, or it could not switch itself back on).
    /// </summary>
    public class ModeVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject target;
        [SerializeField] private bool visibleInVN;
        [SerializeField] private bool visibleInExploration = true;
        [Tooltip("Short crossfade instead of an instant pop; cheap insurance in case the HUD ever outlives the screen fade's black.")]
        [SerializeField] private float fadeDuration = 0.2f;

        private CanvasGroup canvasGroup;
        private Tween tween;

        private void Awake()
        {
            if (target == null) return;

            canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = target.AddComponent<CanvasGroup>();
        }

        private void OnEnable() => GameEvents.OnModeChanged += Apply;
        private void OnDisable() => GameEvents.OnModeChanged -= Apply;

        private void Apply(bool isInVN)
        {
            if (target == null) return;

            bool visible = isInVN ? visibleInVN : visibleInExploration;
            tween?.Kill();

            if (canvasGroup == null)
            {
                target.SetActive(visible);
                return;
            }

            if (visible)
            {
                target.SetActive(true);
                canvasGroup.alpha = 0f;
                tween = canvasGroup.DOFade(1f, fadeDuration).SetLink(target);
                return;
            }

            tween = canvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => target.SetActive(false))
                .SetLink(target);
        }
    }
}
