using Cysharp.Threading.Tasks;
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
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button quitButton;

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

        private void HandleStart() => DismissAsync(() => gameModeManager.BeginGame()).Forget();

        private void HandleContinue()
        {
            int slot = FindMostRecentSlot();
            if (slot == int.MinValue) return;

            DismissAsync(() => saveCoordinator.Load(slot)).Forget();
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

        /// <summary>
        /// Covers the screen in black FIRST, then hides the title and hands off — rather than fading the
        /// title out on its own timeline and only starting the black transition afterward, which left a
        /// gap where the already-loaded 3D exploration scene flashed through behind the disappearing title.
        /// </summary>
        private async UniTaskVoid DismissAsync(System.Action onHidden)
        {
            if (startButton != null) startButton.interactable = false;
            if (continueButton != null) continueButton.interactable = false;
            if (quitButton != null) quitButton.interactable = false;

            IScreenFader fader = ScreenFader.Instance;
            if (fader != null)
                await fader.FadeOutAsync();

            // Let at least one fully-black frame actually render before touching anything else — Restore()
            // teleports the player and toggles world objects in the same tick, and without this gap that
            // occasionally slipped through as a one-frame flash of the 3D scene before the black caught up.
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
            await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);

            // The screen is now fully black: safe to swap without anything showing through. Tell
            // GameModeManager not to fade to black again on top of this — it would just hold an extra
            // ~0.3s at black for no visual reason before finally applying VN/exploration mode.
            gameModeManager.SkipNextTransition();
            gameObject.SetActive(false);
            onHidden?.Invoke();
        }
    }
}
