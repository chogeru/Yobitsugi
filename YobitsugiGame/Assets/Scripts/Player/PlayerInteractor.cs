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
                found = hit.collider.GetComponentInParent<IInteractable>();
                if (found != null && !found.CanInteract(gameObject))
                    found = null;
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
            Current?.Interact(gameObject);
        }
    }
}
