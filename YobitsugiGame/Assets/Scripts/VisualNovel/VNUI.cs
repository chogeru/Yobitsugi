using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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
        [SerializeField] private Text speakerText;
        [SerializeField] private Text dialogueText;
        [SerializeField] private Button advanceButton;
        [SerializeField] private GameObject nextIndicator;
        [SerializeField] private Transform choicesContainer;
        [SerializeField] private Button choiceButtonTemplate;

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
            if (submitAction == null) return;

            submitAction.Enable();
            submitAction.performed += OnSubmit;
        }

        private void OnDisable()
        {
            if (submitAction == null) return;
            submitAction.performed -= OnSubmit;
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
            speakerText.text = speaker;
            speakerText.color = color;
        }

        public void SetBackground(Sprite sprite)
        {
            if (sprite == null || sprite == backgroundImage.sprite) return;

            // Hold the outgoing image on top, swap the base underneath, then dissolve the old one away.
            DOTween.Kill(backgroundFadeImage);
            backgroundFadeImage.sprite = backgroundImage.sprite;
            backgroundFadeImage.enabled = backgroundFadeImage.sprite != null;
            backgroundFadeImage.color = Color.white;
            backgroundImage.sprite = sprite;

            backgroundFadeImage.DOFade(0f, backgroundCrossfade)
                .OnComplete(() => backgroundFadeImage.enabled = false)
                .SetLink(gameObject);
        }

        public void SetDialogueText(string text) => dialogueText.text = text;

        public Tween TypeDialogue(string text, float duration)
        {
            dialogueText.text = string.Empty;
            return dialogueText.DOText(text, duration)
                .SetEase(Ease.Linear)
                .SetLink(gameObject);
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

                var label = button.GetComponentInChildren<Text>();
                if (label != null) label.text = choices[i].text;

                button.onClick.AddListener(() => ChoiceSelected?.Invoke(choiceIndex));
                spawnedChoices.Add(button);

                PlayChoiceEntrance(button, choiceIndex * choiceStagger);
            }
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
