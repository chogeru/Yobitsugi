using UnityEngine;

namespace Yobitsugi.Sequences
{
    /// <summary>Starts a <see cref="GameSequence"/> when the player enters this trigger volume.</summary>
    [RequireComponent(typeof(Collider))]
    public class SequenceTrigger : MonoBehaviour
    {
        [SerializeField] private GameSequence sequence;
        [SerializeField] private string requiredTag = "Player";
        [Tooltip("If true, re-entering the trigger volume after the first activation does nothing.")]
        [SerializeField] private bool triggerOnce = true;

        private bool hasFired;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnTriggerEnter(Collider other)
        {
            if (sequence == null || !other.CompareTag(requiredTag)) return;
            if (triggerOnce && hasFired) return;

            hasFired = true;
            sequence.Play();
        }
    }
}
