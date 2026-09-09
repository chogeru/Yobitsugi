using DG.Tweening;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.Interactables
{
    public class ClueItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string clueId;
        [SerializeField] private string displayName = "手がかり";
        [SerializeField] private float collectDuration = 0.35f;
        [SerializeField] private float collectRiseDistance = 0.4f;

        private Vector3 originalScale;
        private bool collecting;

        public string Id => string.IsNullOrEmpty(clueId) ? gameObject.name : clueId;
        public string InteractionPrompt => $"{displayName}を調べる";

        private void Awake() => originalScale = transform.localScale;

        /// <summary>
        /// GameManager.RestoreClueObjects re-enables uncollected clues on load by flipping SetActive directly —
        /// reset here so a clue that was mid-collect-animation in a previous session doesn't come back invisible or uninteractable.
        /// </summary>
        private void OnEnable()
        {
            collecting = false;
            transform.DOKill();
            transform.localScale = originalScale;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;
        }

        public bool CanInteract(GameObject interactor) => !collecting;

        public void Interact(GameObject interactor)
        {
            if (collecting) return;
            if (GameManager.Instance == null || !GameManager.Instance.CollectClue(Id)) return;

            collecting = true;
            PlayCollectAndHide();
        }

        /// <summary>A quick rise-and-shrink so picking up a clue reads as an action, not an object silently vanishing.</summary>
        private void PlayCollectAndHide()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            transform.DOKill();
            DOTween.Sequence()
                .Join(transform.DOMoveY(transform.position.y + collectRiseDistance, collectDuration).SetEase(Ease.OutCubic))
                .Join(transform.DOScale(Vector3.zero, collectDuration).SetEase(Ease.InBack))
                .OnComplete(() => gameObject.SetActive(false))
                .SetLink(gameObject);
        }
    }
}
