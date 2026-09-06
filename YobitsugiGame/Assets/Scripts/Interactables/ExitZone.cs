using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ExitZone : MonoBehaviour
    {
        [SerializeField] private int requiredClueCount;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player") || GameManager.Instance == null) return;
            if (GameManager.Instance.HasEnoughClues(requiredClueCount))
                GameManager.Instance.ClearGame();
        }
    }
}
