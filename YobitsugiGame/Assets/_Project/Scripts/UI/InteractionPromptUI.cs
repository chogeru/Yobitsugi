using DG.Tweening;
using UnityEngine;
using TMPro;
using Yobitsugi.Core;
using Yobitsugi.Player;

namespace Yobitsugi.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private float fadeDuration = 0.15f;
        [SerializeField] private float enterScale = 0.9f;
        [Tooltip("Text color while the focused target can't actually be interacted with right now (e.g. a locked door).")]
        [SerializeField] private Color blockedColor = new Color(1f, 0.55f, 0.5f, 1f);

        private CanvasGroup canvasGroup;
        private Tween tween;
        private Color defaultTextColor;
        private bool lastCanInteract = true;

        private void Awake()
        {
            if (panel == null) return;

            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = panel.AddComponent<CanvasGroup>();

            if (promptText != null) defaultTextColor = promptText.color;
        }

        private void OnEnable()
        {
            if (interactor != null)
                interactor.OnFocusChanged += HandleFocusChanged;
            SetVisible(false, instant: true);
        }

        private void OnDisable()
        {
            if (interactor != null)
                interactor.OnFocusChanged -= HandleFocusChanged;
            tween?.Kill();
        }

        private void HandleFocusChanged(IInteractable target)
        {
            if (target == null)
            {
                SetVisible(false);
                return;
            }

            if (promptText != null)
                promptText.text = target.InteractionPrompt;
            SetVisible(true);
        }

        /// <summary>
        /// Same target can go from locked to unlocked without ever losing focus (e.g. grabbing the last
        /// clue while standing at the door), so keep the text live instead of only refreshing on focus change.
        /// </summary>
        private void Update()
        {
            if (interactor == null || interactor.Current == null || promptText == null) return;

            var target = interactor.Current;
            string current = target.InteractionPrompt;
            if (promptText.text != current) promptText.text = current;

            bool canInteract = target.CanInteract(interactor.gameObject);
            if (canInteract != lastCanInteract)
            {
                lastCanInteract = canInteract;
                promptText.color = canInteract ? defaultTextColor : blockedColor;
            }
        }

        /// <summary>Fades and scales the prompt in/out instead of an instant SetActive pop.</summary>
        private void SetVisible(bool visible, bool instant = false)
        {
            if (panel == null) return;

            tween?.Kill();

            if (canvasGroup == null)
            {
                panel.SetActive(visible);
                return;
            }

            var rect = (RectTransform)panel.transform;

            if (visible)
            {
                panel.SetActive(true);

                if (instant)
                {
                    canvasGroup.alpha = 1f;
                    rect.localScale = Vector3.one;
                    return;
                }

                canvasGroup.alpha = 0f;
                rect.localScale = Vector3.one * enterScale;
                tween = DOTween.Sequence()
                    .Join(canvasGroup.DOFade(1f, fadeDuration))
                    .Join(rect.DOScale(1f, fadeDuration).SetEase(Ease.OutBack))
                    .SetLink(gameObject);
                return;
            }

            if (instant)
            {
                canvasGroup.alpha = 0f;
                panel.SetActive(false);
                return;
            }

            tween = canvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => panel.SetActive(false))
                .SetLink(gameObject);
        }
    }
}
