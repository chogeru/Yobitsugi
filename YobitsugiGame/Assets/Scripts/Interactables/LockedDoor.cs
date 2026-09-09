using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.Interactables
{
    public class LockedDoor : MonoBehaviour, IInteractable
    {
        [SerializeField] private int requiredClueCount;
        [SerializeField] private float openAngle = 100f;
        [SerializeField] private float openSpeed = 2f;
        [SerializeField] private Collider blockingCollider;

        private bool isOpen;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private CancellationTokenSource moveCts;

        public string InteractionPrompt
        {
            get
            {
                if (isOpen) return "閉める";
                if (IsUnlocked) return "開ける";

                int remaining = requiredClueCount - (GameManager.Instance != null ? GameManager.Instance.ClueCount : 0);
                return remaining > 0 ? $"鍵がかかっている(あと{remaining}つ手がかりが必要)" : "鍵がかかっている";
            }
        }

        private bool IsUnlocked => requiredClueCount <= 0 ||
            (GameManager.Instance != null && GameManager.Instance.HasEnoughClues(requiredClueCount));

        public bool CanInteract(GameObject interactor) => IsUnlocked;

        private void Awake()
        {
            closedRotation = transform.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        }

        private void OnDestroy()
        {
            moveCts?.Cancel();
        }

        public void Interact(GameObject interactor)
        {
            if (!IsUnlocked) return;

            isOpen = !isOpen;
            if (blockingCollider != null)
                blockingCollider.enabled = !isOpen;

            moveCts?.Cancel();
            moveCts = new CancellationTokenSource();
            RotateToAsync(isOpen ? openRotation : closedRotation, moveCts.Token).Forget();
        }

        private async UniTaskVoid RotateToAsync(Quaternion target, CancellationToken token)
        {
            while (!token.IsCancellationRequested && Quaternion.Angle(transform.localRotation, target) > 0.5f)
            {
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * openSpeed);
                await UniTask.Yield(PlayerLoopTiming.Update, token).SuppressCancellationThrow();
            }

            if (!token.IsCancellationRequested)
                transform.localRotation = target;
        }
    }
}
