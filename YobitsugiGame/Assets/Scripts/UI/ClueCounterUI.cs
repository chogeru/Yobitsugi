using DG.Tweening;
using UnityEngine;
using TMPro;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    public class ClueCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private float pulseScale = 1.35f;
        [SerializeField] private float pulseDuration = 0.4f;

        private int lastCount = -1;

        private void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnClueCountChanged += HandleClueCountChanged;
            HandleClueCountChanged(GameManager.Instance.ClueCount, GameManager.Instance.RequiredClueCount);
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnClueCountChanged -= HandleClueCountChanged;
        }

        private void HandleClueCountChanged(int current, int required)
        {
            if (counterText == null) return;

            bool increased = lastCount >= 0 && current > lastCount;
            lastCount = current;
            counterText.text = $"手がかり: {current} / {required}";

            if (increased) Pulse();
        }

        /// <summary>A quick punch-scale so picking up a clue reads on the HUD, not just as a number swap.</summary>
        private void Pulse()
        {
            var rect = counterText.transform;
            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.DOPunchScale(Vector3.one * (pulseScale - 1f), pulseDuration, 8, 0.7f).SetLink(gameObject);
        }
    }
}
