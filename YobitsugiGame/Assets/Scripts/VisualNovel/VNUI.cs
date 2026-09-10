using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yobitsugi.Core;
using Yobitsugi.UI;

namespace Yobitsugi.VisualNovel
{
    /// <summary>MVP view: renders dialogue and reports clicks. Holds no scenario logic.</summary>
    public class VNUI : MonoBehaviour, IVNView
    {
        [Header("Refs")]
        [SerializeField] private GameObject root;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image backgroundFadeImage;
        [SerializeField] private CanvasGroup textPanelGroup;
        [SerializeField] private TMP_Text speakerText;
        [Tooltip("Nameplate background chip, hidden entirely for narration lines with no speaker.")]
        [SerializeField] private GameObject speakerNameplate;
        [Tooltip("Optional colour accent on the nameplate that matches the speaking character.")]
        [SerializeField] private Image speakerAccent;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private GameObject nextIndicator;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonTemplate;
        [Tooltip("Full-screen white image used for the screen-flash effect. Alpha should start at 0.")]
        [SerializeField] private Image flashOverlay;

        [Header("Input")]
        [Tooltip("Project input actions; the UI/Submit action advances dialogue from keyboard and gamepad.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Presentation")]
        [SerializeField] private float panelFadeIn = 0.3f;
        [SerializeField] private float panelRise = 40f;
        [SerializeField] private float backgroundCrossfade = 0.5f;
        [SerializeField] private float choiceFadeIn = 0.25f;
        [SerializeField] private float choiceStagger = 0.07f;
        [SerializeField] private float choiceEnterScale = 0.85f;
        [Tooltip("Time the clicked choice gets to visibly confirm before the scenario actually advances.")]
        [SerializeField] private float choiceSelectDuration = 0.15f;
        [Tooltip("Slow zoom applied to the background for as long as it's on screen, so scenes never look static.")]
        [SerializeField] private float backgroundZoom = 1.06f;
        [SerializeField] private float backgroundZoomDuration = 10f;
        [SerializeField] private float flashDuration = 0.25f;
        [SerializeField] private float cameraPunchDuration = 0.3f;

        private readonly List<Button> spawnedChoices = new List<Button>();
        private InputAction submitAction;

        public event Action AdvanceRequested;
        public event Action<int> ChoiceSelected;

        private void Awake()
        {
            advanceButton.onClick.AddListener(() => AdvanceRequested?.Invoke());
            submitAction = inputActions != null ? inputActions.FindActionMap("UI", false)?.FindAction("Submit", false) : null;
        }

        private void OnEnable()
        {
            GameEvents.OnScreenShakeRequested += HandleShakeRequested;
            GameEvents.OnScreenFlashRequested += HandleScreenFlashRequested;
            GameEvents.OnCameraPunchRequested += HandleCameraPunchRequested;

            if (submitAction == null) return;

            submitAction.Enable();
            submitAction.performed += OnSubmit;
        }

        private void OnDisable()
        {
            GameEvents.OnScreenShakeRequested -= HandleShakeRequested;
            GameEvents.OnScreenFlashRequested -= HandleScreenFlashRequested;
            GameEvents.OnCameraPunchRequested -= HandleCameraPunchRequested;

            if (submitAction == null) return;
            submitAction.performed -= OnSubmit;
            submitAction.Disable();
        }

        /// <summary>Comedic/shock beat: a quick white flash that dissolves away.</summary>
        private void HandleScreenFlashRequested()
        {
            if (flashOverlay == null) return;

            flashOverlay.DOKill();
            flashOverlay.color = new Color(1f, 1f, 1f, 1f);
            flashOverlay.DOFade(0f, flashDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        /// <summary>Quick punch-in on the background to emphasise a dramatic line.</summary>
        private void HandleCameraPunchRequested(float intensity)
        {
            if (backgroundImage == null) return;

            // Additive punch on top of the background's own continuous Ken Burns zoom tween — must not
            // DOKill() the rect here, that would also stop the unrelated slow zoom permanently.
            backgroundImage.rectTransform.DOPunchScale(Vector3.one * intensity, cameraPunchDuration, 1, 0.5f).SetLink(gameObject);
        }

        /// <summary>Dramatic beats shake the whole dialogue canvas, since VN runs on a screen-space overlay the 3D camera shake never reaches.</summary>
        private void HandleShakeRequested(float duration, float strength)
        {
            if (root == null || !root.activeInHierarchy) return;

            var rect = (RectTransform)root.transform;
            rect.DOKill();
            rect.DOShakeAnchorPos(duration, strength * 0.3f, 20, 90f, false, true).SetLink(gameObject);
        }

        private void OnSubmit(InputAction.CallbackContext context)
        {
            // Keyboard/gamepad advance, but only while the dialogue screen is the thing on screen.
            if (root != null && root.activeInHierarchy && advanceButton.gameObject.activeSelf)
                AdvanceRequested?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            root.SetActive(visible);

            if (!visible)
            {
                HideChoices();
                return;
            }

            PlayPanelEntrance();
        }

        private void PlayPanelEntrance()
        {
            if (textPanelGroup == null) return;

            var rect = (RectTransform)textPanelGroup.transform;
            float targetY = rect.anchoredPosition.y;

            DOTween.Kill(textPanelGroup);
            textPanelGroup.alpha = 0f;
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, targetY - panelRise);

            DOTween.Sequence()
                .Join(textPanelGroup.DOFade(1f, panelFadeIn))
                .Join(rect.DOAnchorPosY(targetY, panelFadeIn).SetEase(Ease.OutCubic))
                .SetTarget(textPanelGroup)
                .SetLink(gameObject);
        }

        public void SetSpeaker(string speaker, Color color)
        {
            bool hasSpeaker = !string.IsNullOrEmpty(speaker);

            speakerText.text = speaker;
            speakerText.color = color;
            if (speakerAccent != null) speakerAccent.color = color;
            if (speakerNameplate != null) speakerNameplate.SetActive(hasSpeaker);
        }

        public void SetBackground(Sprite sprite)
        {
            if (sprite == null || sprite == backgroundImage.sprite) return;

            // Hold the outgoing image on top, swap the base underneath, then dissolve the old one away.
            DOTween.Kill(backgroundFadeImage);
            backgroundFadeImage.sprite = backgroundImage.sprite;
            backgroundFadeImage.enabled = backgroundFadeImage.sprite != null;
            backgroundFadeImage.color = Color.white;
            backgroundFadeImage.rectTransform.localScale = backgroundImage.rectTransform.localScale;
            backgroundImage.sprite = sprite;

            // The empty-state panel is tinted near-black; a sprite must render untinted or it comes out invisible.
            backgroundImage.color = Color.white;

            backgroundFadeImage.DOFade(0f, backgroundCrossfade)
                .OnComplete(() => backgroundFadeImage.enabled = false)
                .SetLink(gameObject);

            StartBackgroundZoom();
        }

        /// <summary>A slow, unending zoom so a background never reads as a static slide, novel-game "Ken Burns" style.</summary>
        private void StartBackgroundZoom()
        {
            var rect = backgroundImage.rectTransform;
            DOTween.Kill(rect);
            if (backgroundZoom <= 1f || backgroundZoomDuration <= 0f) return;

            rect.localScale = Vector3.one;
            rect.DOScale(backgroundZoom, backgroundZoomDuration).SetEase(Ease.Linear).SetLink(gameObject);
        }

        public void SetDialogueText(string text)
        {
            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = int.MaxValue;
        }

        /// <summary>
        /// Reveals by raising the visible character count rather than rebuilding the string, so
        /// rich-text tags never get sliced in half and no garbage is generated per character.
        /// </summary>
        public Tween TypeDialogue(string text, float duration)
        {
            dialogueText.text = text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int total = dialogueText.textInfo.characterCount;
            int revealed = 0;

            return DOVirtual.Int(0, total, duration, value =>
                {
                    dialogueText.maxVisibleCharacters = value;
                    RaiseTypingSfxFor(revealed, value);
                    revealed = value;
                })
                .SetEase(Ease.Linear)
                .OnComplete(() => dialogueText.maxVisibleCharacters = int.MaxValue)
                .SetLink(gameObject);
        }

        /// <summary>Raises one typing-SFX event per non-whitespace glyph revealed since the last tween step.</summary>
        private void RaiseTypingSfxFor(int from, int to)
        {
            var info = dialogueText.textInfo;
            for (int i = from; i < to && i < info.characterCount; i++)
            {
                if (!char.IsWhiteSpace(info.characterInfo[i].character))
                    GameEvents.RaiseDialogueCharacterRevealed();
            }
        }

        public void SetNextIndicatorVisible(bool visible)
        {
            if (nextIndicator != null) nextIndicator.SetActive(visible);
        }

        public void ShowChoices(VNChoice[] choices)
        {
            ClearChoiceButtons();
            advanceButton.gameObject.SetActive(false);
            SetNextIndicatorVisible(false);

            for (int i = 0; i < choices.Length; i++)
            {
                int choiceIndex = i;
                var button = Instantiate(choiceButtonTemplate, choicesContainer);
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null) label.text = choices[i].text;

                button.onClick.AddListener(() => AnimateChoiceSelection(choiceIndex));
                button.WireButtonClickSound();
                spawnedChoices.Add(button);

                PlayChoiceEntrance(button, choiceIndex * choiceStagger);
            }
        }

        /// <summary>
        /// The chosen option grows and confirms, the rest fade away, and only then does the scenario move on —
        /// so picking a choice reads as a decision rather than an instant cut to the next line.
        /// </summary>
        private void AnimateChoiceSelection(int choiceIndex)
        {
            foreach (var button in spawnedChoices)
                button.interactable = false;

            var sequence = DOTween.Sequence().SetLink(gameObject);

            for (int i = 0; i < spawnedChoices.Count; i++)
            {
                var button = spawnedChoices[i];
                var group = button.GetComponent<CanvasGroup>();
                if (group == null) continue;

                if (i == choiceIndex)
                    sequence.Join(((RectTransform)button.transform).DOScale(1.08f, choiceSelectDuration).SetEase(Ease.OutBack));
                else
                    sequence.Join(group.DOFade(0.15f, choiceSelectDuration));
            }

            sequence.OnComplete(() => ChoiceSelected?.Invoke(choiceIndex));
        }

        private void PlayChoiceEntrance(Button button, float delay)
        {
            var group = button.GetComponent<CanvasGroup>();
            if (group == null) group = button.gameObject.AddComponent<CanvasGroup>();

            // Scale rather than position: the vertical layout group owns child positions and would fight a move tween.
            var rect = (RectTransform)button.transform;
            group.alpha = 0f;
            rect.localScale = new Vector3(1f, choiceEnterScale, 1f);

            DOTween.Sequence()
                .AppendInterval(delay)
                .Append(group.DOFade(1f, choiceFadeIn))
                .Join(rect.DOScale(Vector3.one, choiceFadeIn).SetEase(Ease.OutBack))
                .SetLink(button.gameObject);
        }

        public void HideChoices()
        {
            ClearChoiceButtons();
            advanceButton.gameObject.SetActive(true);
        }

        private void ClearChoiceButtons()
        {
            foreach (var button in spawnedChoices)
            {
                if (button != null) Destroy(button.gameObject);
            }
            spawnedChoices.Clear();
        }
    }
}
