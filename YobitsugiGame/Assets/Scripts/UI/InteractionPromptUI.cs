using UnityEngine;
using UnityEngine.UI;
using Yobitsugi.Core;
using Yobitsugi.Player;

namespace Yobitsugi.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private GameObject panel;
        [SerializeField] private Text promptText;

        private void OnEnable()
        {
            if (interactor != null)
                interactor.OnFocusChanged += HandleFocusChanged;
            SetVisible(false);
        }

        private void OnDisable()
        {
            if (interactor != null)
                interactor.OnFocusChanged -= HandleFocusChanged;
        }

        private void HandleFocusChanged(IInteractable target)
        {
            if (target == null)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            if (promptText != null)
                promptText.text = target.InteractionPrompt;
        }

        private void SetVisible(bool visible)
        {
            if (panel != null) panel.SetActive(visible);
        }
    }
}
