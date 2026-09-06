using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.UI
{
    public class GameStateUI : MonoBehaviour
    {
        [SerializeField] private GameObject clearPanel;

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
            if (clearPanel != null) clearPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Restart()
        {
            GameManager.Instance?.RestartLevel();
        }
    }
}
