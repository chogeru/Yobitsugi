using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using Yobitsugi.Core;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.UI
{
    /// <summary>MVP view + composition root for the system menu. Wires Unity widgets to <see cref="SystemMenuPresenter"/>.</summary>
    public class SystemMenuView : MonoBehaviour, ISystemMenuView
    {
#if ODIN_INSPECTOR
        [Title("システムメニュー(View)", "オート/スキップ・ログ・セーブ/ロードのUI", TitleAlignments.Left)]
        [InfoBox("MVPのView側です。ここにあるのは「表示」と「押された事の通知」だけで、\n" +
                 "実際の処理は MonoBehaviour ではない SystemMenuPresenter が行います。\n" +
                 "開閉は画面右上のボタン、またはキーボードのEsc / ゲームパッドのキャンセルボタンです。")]
        [FoldoutGroup("パネル")]
#endif
        [Header("Panels")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject backlogPanel;
        [SerializeField] private GameObject slotPanel;

        [Header("Buttons")]
        [SerializeField] private Button menuButton;
        [SerializeField] private Button autoButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button backlogButton;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button[] backButtons;
        [SerializeField] private Button[] slotButtons;
        [Tooltip("The reserved autosave slot's button, shown only in the Load list.")]
        [SerializeField] private Button autoSlotButton;

        [Header("Volume")]
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Slider voiceVolumeSlider;

        [Header("Labels")]
        [SerializeField] private TMP_Text autoButtonLabel;
        [SerializeField] private TMP_Text skipButtonLabel;
        [SerializeField] private TMP_Text backlogText;
        [SerializeField] private TMP_Text slotPanelTitle;
        [SerializeField] private TMP_Text[] slotButtonLabels;
        [SerializeField] private TMP_Text autoSlotButtonLabel;

        [Header("Systems")]
        [SerializeField] private VisualNovel.VNManager vnManager;
        [SerializeField] private GameModeManager gameModeManager;
        [SerializeField] private SaveCoordinator saveCoordinator;

        [Tooltip("Project input actions; the UI/Cancel action opens and backs out of the menu.")]
        [SerializeField] private InputActionAsset inputActions;

        [Header("Feedback")]
        [Tooltip("The slot panel title flashes this color when a save completes, so saving has a visible confirmation beyond the SFX.")]
        [SerializeField] private Color saveFlashColor = new Color(0.65f, 1f, 0.7f, 1f);

        private SystemMenuPresenter presenter;
        private InputAction cancelAction;
        private Color slotPanelTitleDefaultColor;

        public event Action MenuToggleRequested;
        public event Action AutoToggleRequested;
        public event Action SkipToggleRequested;
        public event Action BacklogRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action RestartRequested;
        public event Action BackRequested;
        public event Action<int> SlotSelected;
        public event Action AutoSlotSelected;
        public event Action<int> BacklogEntryClicked;

        public int SlotCount => slotButtons.Length;

        private void Awake()
        {
            menuButton.onClick.AddListener(() => MenuToggleRequested?.Invoke());
            autoButton.onClick.AddListener(() => AutoToggleRequested?.Invoke());
            skipButton.onClick.AddListener(() => SkipToggleRequested?.Invoke());
            backlogButton.onClick.AddListener(() => BacklogRequested?.Invoke());
            saveButton.onClick.AddListener(() => SaveRequested?.Invoke());
            loadButton.onClick.AddListener(() => LoadRequested?.Invoke());
            restartButton.onClick.AddListener(() => RestartRequested?.Invoke());

            foreach (var button in backButtons)
                button.onClick.AddListener(() => BackRequested?.Invoke());

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int index = i;
                slotButtons[i].onClick.AddListener(() => SlotSelected?.Invoke(index));
            }

            if (autoSlotButton != null)
                autoSlotButton.onClick.AddListener(() => AutoSlotSelected?.Invoke());

            cancelAction = inputActions != null ? inputActions.FindActionMap("UI", false)?.FindAction("Cancel", false) : null;

            if (slotPanelTitle != null) slotPanelTitleDefaultColor = slotPanelTitle.color;
            GameEvents.OnSaveCompleted += HandleSaveCompleted;

            SetupBacklogLinks();
        }

        /// <summary>Wires TMP `&lt;link&gt;` clicks on the backlog text to voice-replay, without needing a scroll-list of per-line buttons.</summary>
        private void SetupBacklogLinks()
        {
            if (backlogText == null) return;

            var linkHandler = backlogText.gameObject.GetComponent<TMPLinkClickHandler>();
            if (linkHandler == null) linkHandler = backlogText.gameObject.AddComponent<TMPLinkClickHandler>();

            linkHandler.Initialize(backlogText);
            linkHandler.LinkClicked += linkId =>
            {
                if (int.TryParse(linkId, out int index))
                    BacklogEntryClicked?.Invoke(index);
            };
        }

        /// <summary>Volume is a device/user preference, not save data, so it's read from and written straight to AudioService/PlayerPrefs.</summary>
        private void SetupVolumeSliders()
        {
            var audio = Audio.AudioService.Instance;
            if (audio == null) return;

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.SetValueWithoutNotify(audio.UserMusicVolume);
                musicVolumeSlider.onValueChanged.AddListener(audio.SetUserMusicVolume);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.SetValueWithoutNotify(audio.UserSfxVolume);
                sfxVolumeSlider.onValueChanged.AddListener(audio.SetUserSfxVolume);
            }

            if (voiceVolumeSlider != null)
            {
                voiceVolumeSlider.SetValueWithoutNotify(audio.UserVoiceVolume);
                voiceVolumeSlider.onValueChanged.AddListener(audio.SetUserVoiceVolume);
            }
        }

        private void OnEnable()
        {
            if (cancelAction == null) return;

            cancelAction.Enable();
            cancelAction.performed += OnCancel;
        }

        private void OnDisable()
        {
            if (cancelAction == null) return;
            cancelAction.performed -= OnCancel;
        }

        private void OnCancel(InputAction.CallbackContext context) => BackRequested?.Invoke();

        private void Start()
        {
            presenter = new SystemMenuPresenter(this, gameModeManager, vnManager.Presenter, saveCoordinator,
                () => GameManager.Instance?.RestartLevel());

            // Deferred to Start: Awake ordering between components is undefined, and AudioService.Instance
            // was sometimes still null when this ran from Awake, silently skipping slider setup.
            SetupVolumeSliders();
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            GameEvents.OnSaveCompleted -= HandleSaveCompleted;
        }

        /// <summary>Punches and flashes the slot panel title so a save has a visible confirmation, not just the SFX.</summary>
        private void HandleSaveCompleted(int slot)
        {
            if (slotPanelTitle == null) return;

            var rect = slotPanelTitle.transform;
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * 0.12f, 0.35f, 6, 0.6f).SetLink(gameObject);

            DOTween.Kill(slotPanelTitle);
            slotPanelTitle.color = saveFlashColor;
            slotPanelTitle.DOColor(slotPanelTitleDefaultColor, 0.6f).SetLink(gameObject);
        }

        public void ShowPanel(SystemMenuPanel panel)
        {
            menuPanel.SetActive(panel == SystemMenuPanel.Menu);
            backlogPanel.SetActive(panel == SystemMenuPanel.Backlog);
            slotPanel.SetActive(panel == SystemMenuPanel.Slots);
        }

        public bool IsPanelVisible(SystemMenuPanel panel) => panel switch
        {
            SystemMenuPanel.Menu => menuPanel.activeSelf,
            SystemMenuPanel.Backlog => backlogPanel.activeSelf,
            SystemMenuPanel.Slots => slotPanel.activeSelf,
            SystemMenuPanel.None => !menuPanel.activeSelf && !backlogPanel.activeSelf && !slotPanel.activeSelf,
            _ => false,
        };

        public void SetToggleLabels(bool autoOn, bool skipOn)
        {
            autoButtonLabel.text = $"オート: {(autoOn ? "ON" : "OFF")}";
            skipButtonLabel.text = $"スキップ: {(skipOn ? "ON" : "OFF")}";
        }

        public void SetBacklogText(string text) => backlogText.text = text;

        public void SetSlotsTitle(string title) => slotPanelTitle.text = title;

        public void SetSlot(int index, string label, bool interactable)
        {
            if (index < 0 || index >= slotButtons.Length) return;

            slotButtonLabels[index].text = label;
            slotButtons[index].interactable = interactable;
        }

        public void SetAutoSlot(string label, bool interactable, bool visible)
        {
            if (autoSlotButton == null) return;

            autoSlotButton.gameObject.SetActive(visible);
            autoSlotButton.interactable = interactable;
            if (autoSlotButtonLabel != null) autoSlotButtonLabel.text = label;
        }
    }
}
