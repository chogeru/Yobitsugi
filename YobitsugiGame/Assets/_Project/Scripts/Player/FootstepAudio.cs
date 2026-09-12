using UnityEngine;

namespace Yobitsugi.Player
{
    /// <summary>Plays a random footstep clip at a cadence driven by the controller's current move state.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class FootstepAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private AudioClip[] walkClips;
        [SerializeField] private AudioClip[] sprintClips;
        [SerializeField] private float walkInterval = 0.45f;
        [SerializeField] private float sprintInterval = 0.3f;
        [SerializeField] private float crouchIntervalMultiplier = 1.6f;
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
        [SerializeField, Range(0f, 1f)] private float crouchVolumeMultiplier = 0.4f;

        private PlayerController controller;
        private float stepTimer;

        private void Awake()
        {
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            if (!controller.IsMoving)
            {
                stepTimer = 0f;
                return;
            }

            stepTimer -= Time.deltaTime;
            if (stepTimer > 0f) return;

            bool sprinting = controller.IsSprinting;
            stepTimer = sprinting ? sprintInterval : walkInterval;
            if (controller.IsCrouching) stepTimer *= crouchIntervalMultiplier;

            PlayStep(sprinting);
        }

        private void PlayStep(bool sprinting)
        {
            var clips = sprinting ? sprintClips : walkClips;
            if (source == null || clips == null || clips.Length == 0) return;

            float stepVolume = volume * (controller.IsCrouching ? crouchVolumeMultiplier : 1f);
            source.PlayOneShot(clips[Random.Range(0, clips.Length)], stepVolume);
        }
    }
}
