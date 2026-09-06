using System;
using UnityEngine;
using UnityEngine.InputSystem;
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

        [Header("Labels")]
        [SerializeField] private Text autoButtonLabel;
        [SerializeField] private Text skipButtonLabel;
        [SerializeField] private Text backlogText;
        [SerializeField] private Text slotPanelTitle;
        [SerializeField] private Text[] slotButtonLabels;

        [Header("Systems")]
        [SerializeField] private VisualNovel.VNManager vnManager;
        [SerializeField] private GameModeManager gameModeManager;
        [SerializeField] private SaveCoordinator saveCoordinator;

        [Tooltip("Project input actions; the UI/Cancel action opens and backs out of the menu.")]
        [SerializeField] private InputActionAsset inputActions;

        private SystemMenuPresenter presenter;
        private InputAction cancelAction;

        public event Action MenuToggleRequested;
        public event Action AutoToggleRequested;
        public event Action SkipToggleRequested;
        public event Action BacklogRequested;
        public event Action SaveRequested;
        public event Action LoadRequested;
        public event Action RestartRequested;
        public event Action BackRequested;
        public event Action<int> SlotSelected;

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

            cancelAction = inputActions != null ? inputActions.FindActionMap("UI", false)?.FindAction("Cancel", false) : null;
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
        }

        private void OnDestroy() => presenter?.Dispose();

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
    }
}
