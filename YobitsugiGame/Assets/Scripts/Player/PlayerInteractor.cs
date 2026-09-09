using System;
using UnityEngine;
using UnityEngine.InputSystem;
using Yobitsugi.Core;

namespace Yobitsugi.Player
{
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private Camera interactionCamera;
        [SerializeField] private float interactRange = 2.5f;
        [SerializeField] private LayerMask interactableMask = ~0;

        public IInteractable Current { get; private set; }

        public event Action<IInteractable> OnFocusChanged;

        private void Update()
        {
            IInteractable found = null;

            if (interactionCamera != null &&
                Physics.Raycast(interactionCamera.transform.position, interactionCamera.transform.forward,
                    out RaycastHit hit, interactRange, interactableMask, QueryTriggerInteraction.Collide))
            {
                // Keep a locked/blocked target as Current so its prompt (e.g. "鍵がかかっている") still shows —
                // CanInteract only gates the actual interaction below, in OnInteract.
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (found != Current)
            {
                Current = found;
                OnFocusChanged?.Invoke(Current);
            }
        }

        public void OnInteract(InputValue value)
        {
            if (!value.isPressed) return;
            if (Current != null && Current.CanInteract(gameObject))
                Current.Interact(gameObject);
        }
    }
}
