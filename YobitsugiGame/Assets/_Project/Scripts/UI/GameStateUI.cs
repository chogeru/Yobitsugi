using DG.Tweening;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    public class GameStateUI : MonoBehaviour
    {
        [SerializeField] private GameObject clearPanel;
        [Tooltip("Beat held on the final scene before the clear panel appears, so the moment isn't cut short.")]
        [SerializeField] private float revealDelay = 0.6f;
        [SerializeField] private float fadeDuration = 0.6f;

        private CanvasGroup clearPanelGroup;

        private void Awake()
        {
            if (clearPanel == null) return;

            clearPanelGroup = clearPanel.GetComponent<CanvasGroup>();
            if (clearPanelGroup == null) clearPanelGroup = clearPanel.AddComponent<CanvasGroup>();
        }

        private void Start()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnGameCleared += HandleCleared;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.OnGameCleared -= HandleCleared;
        }

        private void HandleCleared()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (clearPanel == null) return;

            clearPanelGroup.alpha = 0f;
            clearPanel.SetActive(true);

            DOVirtual.DelayedCall(revealDelay, () => clearPanelGroup.DOFade(1f, fadeDuration).SetLink(gameObject))
                .SetLink(gameObject);
        }

        public void Restart()
        {
            GameManager.Instance?.RestartLevel();
        }
    }
}
