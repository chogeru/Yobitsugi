using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    /// <summary>
    /// The title screen: shown at boot, in front of everything, until the player presses Start or Continue.
    /// Presence of this component is what tells <see cref="GameModeManager"/> not to auto-start the intro.
    /// </summary>
    public class TitleScreenController : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float fadeOutDuration = 0.6f;

        [SerializeField] private GameModeManager gameModeManager;
        [SerializeField] private SaveCoordinator saveCoordinator;

        private void Awake()
        {
            if (startButton != null) startButton.onClick.AddListener(HandleStart);
            if (continueButton != null) continueButton.onClick.AddListener(HandleContinue);
            if (quitButton != null) quitButton.onClick.AddListener(HandleQuit);
        }

        private void Start()
        {
            if (continueButton != null)
                continueButton.interactable = FindMostRecentSlot() != int.MinValue;

            // The screen boots on black (ScreenFader.startBlack); with no auto-intro to trigger the usual
            // transition, the title has to open its own curtain.
            IScreenFader fader = ScreenFader.Instance;
            fader?.FadeInAsync().Forget();

            // PlayerController locks and hides the cursor as soon as it's enabled (normally corrected an
            // instant later by GameModeManager's own mode-entry call) — but that call never happens until
            // Start/Continue is pressed, so without this the title's buttons are literally unreachable.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void HandleStart() => Dismiss(() => gameModeManager.BeginGame());

        private void HandleContinue()
        {
            int slot = FindMostRecentSlot();
            if (slot == int.MinValue) return;

            Dismiss(() => saveCoordinator.Load(slot));
        }

        private void HandleQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>Compares the numbered slots and the autosave slot by their "yyyy/MM/dd HH:mm" stamp (sortable as text) and returns the newest.</summary>
        private static int FindMostRecentSlot()
        {
            int bestSlot = int.MinValue;
            string bestStamp = null;

            for (int slot = SaveSystem.AutoSlot; slot < SaveSystem.SlotCount; slot++)
            {
                if (!SaveSystem.HasSave(slot)) continue;

                string stamp = SaveSystem.Load(slot)?.savedAtDisplay;
                if (stamp == null) continue;

                if (bestStamp == null || string.CompareOrdinal(stamp, bestStamp) > 0)
                {
                    bestStamp = stamp;
                    bestSlot = slot;
                }
            }

            return bestSlot;
        }

        private void Dismiss(System.Action onHidden)
        {
            if (startButton != null) startButton.interactable = false;
            if (continueButton != null) continueButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;

            if (canvasGroup == null)
            {
                gameObject.SetActive(false);
                onHidden?.Invoke();
                return;
            }

            canvasGroup.blocksRaycasts = false;
            DOTween.To(() => canvasGroup.alpha, a => canvasGroup.alpha = a, 0f, fadeOutDuration)
                .OnComplete(() =>
                {
                    gameObject.SetActive(false);
                    onHidden?.Invoke();
                });
        }
    }
}
