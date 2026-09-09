using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace Yobitsugi.UI
{
    /// <summary>Full-screen fade used for mode transitions. Runs on unscaled time so it still plays while the game is paused.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour, IScreenFader
    {
        public static ScreenFader Instance { get; private set; }

        [SerializeField] private float defaultDuration = 0.35f;
        [Tooltip("Boot the game on a black screen so the first transition fades in instead of cutting.")]
        [SerializeField] private bool startBlack = true;

        private CanvasGroup canvasGroup;
        private Sequence running;

        private void Awake()
        {
            Instance = this;
            canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = startBlack ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnDestroy()
        {
            running?.Kill();
            if (Instance == this) Instance = null;
        }

        public void Transition(Action atBlack, float duration = -1f)
        {
            float d = Resolve(duration);

            // Complete rather than drop an in-flight transition: killing it outright would discard its
            // pending at-black callback (skipping a mode switch) and could strand the screen on black.
            FinishRunning();
            canvasGroup.blocksRaycasts = true;

            running = DOTween.Sequence()
                .Append(canvasGroup.DOFade(1f, d).SetEase(Ease.InOutQuad))
                .AppendCallback(() => atBlack?.Invoke())
                .Append(canvasGroup.DOFade(0f, d).SetEase(Ease.InOutQuad))
                .OnComplete(() =>
                {
                    canvasGroup.blocksRaycasts = false;
                    running = null;
                })
                .SetUpdate(true);
        }

        public UniTask FadeOutAsync(float duration = -1f, CancellationToken cancellationToken = default) =>
            FadeAsync(1f, duration, blocksDuring: true, blocksAtEnd: true, cancellationToken);

        /// <summary>
        /// Fading in reveals whatever is already sitting underneath (a title screen, the just-restored gameplay);
        /// there is nothing there worth protecting from a click, so — unlike FadeOutAsync — this never blocks
        /// input, letting an impatient click through instead of being silently eaten for the fade's duration.
        /// </summary>
        public UniTask FadeInAsync(float duration = -1f, CancellationToken cancellationToken = default) =>
            FadeAsync(0f, duration, blocksDuring: false, blocksAtEnd: false, cancellationToken);

        /// <summary>
        /// Sets blocksRaycasts from OnKill rather than after the awaited task, so a fade that gets cancelled
        /// mid-flight (e.g. a sequence step interrupted by scene teardown) still settles the raycast block
        /// instead of leaving an invisible full-screen click-eater behind.
        /// </summary>
        private UniTask FadeAsync(float target, float duration, bool blocksDuring, bool blocksAtEnd, CancellationToken cancellationToken)
        {
            FinishRunning();
            canvasGroup.blocksRaycasts = blocksDuring;

            var sequence = DOTween.Sequence()
                .Append(canvasGroup.DOFade(target, Resolve(duration)).SetEase(Ease.InOutQuad))
                .SetUpdate(true);

            sequence.OnKill(() =>
            {
                canvasGroup.blocksRaycasts = blocksAtEnd;
                if (running == sequence) running = null;
            });

            running = sequence;
            return sequence.ToUniTask(cancellationToken: cancellationToken);
        }

        /// <summary>Runs an in-flight transition to its end (callbacks included) so no mode switch is lost.</summary>
        private void FinishRunning()
        {
            if (running == null) return;

            var finishing = running;
            running = null;
            finishing.Kill(true);
        }

        private float Resolve(float duration) => duration >= 0f ? duration : defaultDuration;
    }
}
