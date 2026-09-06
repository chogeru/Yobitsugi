using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.Interactables
{
    public class ClueItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private string clueId;
        [SerializeField] private string displayName = "手がかり";

        public string Id => string.IsNullOrEmpty(clueId) ? gameObject.name : clueId;
        public string InteractionPrompt => $"{displayName}を調べる";

        public bool CanInteract(GameObject interactor) => true;

        public void Interact(GameObject interactor)
        {
            if (GameManager.Instance != null && GameManager.Instance.CollectClue(Id))
            {
                gameObject.SetActive(false);
            }
        }
    }
}
