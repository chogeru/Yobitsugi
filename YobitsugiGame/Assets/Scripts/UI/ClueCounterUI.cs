using UnityEngine;
using TMPro;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    public class ClueCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterText;

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
            if (counterText != null)
                counterText.text = $"手がかり: {current} / {required}";
        }
    }
}
