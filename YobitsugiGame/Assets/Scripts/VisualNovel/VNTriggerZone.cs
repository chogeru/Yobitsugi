using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.VisualNovel
{
    [RequireComponent(typeof(Collider))]
    public class VNTriggerZone : MonoBehaviour
    {
        [SerializeField] private VNScene scene;
        [SerializeField] private bool oneShot = true;

        private bool triggered;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered && oneShot) return;
            if (!other.CompareTag("Player")) return;
            if (GameModeManager.Instance == null || GameModeManager.Instance.IsInVN) return;

            triggered = true;
            GameModeManager.Instance.EnterVN(scene);
        }
    }
}
