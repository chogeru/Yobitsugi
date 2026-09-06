using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yobitsugi.Player;
using Yobitsugi.UI;
using Yobitsugi.VisualNovel;

namespace Yobitsugi.Core
{
    /// <summary>Owns the 2D(VN) / 3D(exploration) switch. UI visibility is driven by GameEvents, not by references from here.</summary>
    public class GameModeManager : MonoBehaviour, IGameModeController, ISaveParticipant
    {
        public static GameModeManager Instance { get; private set; }

        [SerializeField] private PlayerController player;
        [SerializeField] private VNManager vnManager;
        [SerializeField] private VNScene introScene;

        public bool IsInVN { get; private set; }
        public int SaveOrder => 10;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            if (introScene != null)
                EnterVN(introScene);
            else
                ApplyExplorationMode();
        }

        public void EnterVN(VNScene scene)
        {
            RunTransition(() =>
            {
                ApplyVNMode();
                vnManager.StartScene(scene, ExitVN);
            });
        }

        public async UniTask PlayVNAsync(VNScene scene, CancellationToken cancellationToken = default)
        {
            var completion = new UniTaskCompletionSource();

            RunTransition(() =>
            {
                ApplyVNMode();
                vnManager.StartScene(scene, () =>
                {
                    ExitVN();
                    completion.TrySetResult();
                });
            });

            await completion.Task.AttachExternalCancellation(cancellationToken);
        }

        private void ExitVN() => RunTransition(ApplyExplorationMode);

        /// <summary>Fades through black when a fader is present, otherwise switches instantly.</summary>
        private static void RunTransition(Action atBlack)
        {
            IScreenFader fader = ScreenFader.Instance;
            if (fader != null)
                fader.Transition(atBlack);
            else
                atBlack();
        }

        private void ApplyVNMode()
        {
            IsInVN = true;

            if (player != null) player.CanMove = false;
            SetCursorLocked(false);

            GameEvents.RaiseModeChanged(true);
        }

        private void ApplyExplorationMode()
        {
            IsInVN = false;

            if (vnManager != null) vnManager.HideUI();
            if (player != null) player.CanMove = true;
            SetCursorLocked(true);

            GameEvents.RaiseModeChanged(false);
        }

        /// <summary>Freezes exploration (without touching VN state) while a system menu is open on top.</summary>
        public void PauseForMenu()
        {
            if (IsInVN) return;

            if (player != null) player.CanMove = false;
            SetCursorLocked(false);
        }

        public void ResumeFromMenu()
        {
            if (IsInVN) return;

            if (player != null) player.CanMove = true;
            SetCursorLocked(true);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // --- ISaveParticipant ---

        public void Capture(SaveData data)
        {
            // Between a scene ending and the fade completing, IsInVN is still true but nothing is playing —
            // trust the presenter so the save never points at a scene that is not there.
            bool vnActive = IsInVN && vnManager != null && vnManager.IsPlaying;

            data.isInVN = vnActive;
            data.vnSceneId = vnActive ? vnManager.CurrentSceneId : null;
            data.vnLineIndex = vnActive ? vnManager.CurrentLineIndex : 0;

            if (player != null)
            {
                data.playerPosition = player.transform.position;
                data.playerRotation = player.transform.rotation;
            }
        }

        public void Restore(SaveData data)
        {
            if (player != null)
            {
                // The CharacterController overrides transform writes, so disable it across the teleport.
                var controller = player.GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;

                player.transform.SetPositionAndRotation(data.playerPosition, data.playerRotation);

                if (controller != null) controller.enabled = true;
            }

            VNScene scene = data.isInVN ? VNSceneRegistry.Find(data.vnSceneId) : null;
            RunTransition(() =>
            {
                if (scene != null)
                {
                    ApplyVNMode();
                    vnManager.ResumeScene(scene, data.vnLineIndex, ExitVN);
                }
                else
                {
                    ApplyExplorationMode();
                }
            });
        }
    }
}
