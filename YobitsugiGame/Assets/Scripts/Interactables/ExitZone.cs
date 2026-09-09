using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yobitsugi.Core;
using Yobitsugi.VisualNovel;

namespace Yobitsugi.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class ExitZone : MonoBehaviour
    {
        [Serializable]
        private class EndingBranch
        {
            [Tooltip("Chosen when this flag has reached the required value (checked top to bottom; first match wins).")]
            public string requiredFlag;
            public int requiredValue = 1;
            public VNScene endingScene;
        }

        [SerializeField] private int requiredClueCount;

        [Header("Ending")]
        [Tooltip("Checked top to bottom; the first branch whose flag matches plays. Leave empty to just clear with no ending scene.")]
        [SerializeField] private EndingBranch[] endingBranches;
        [Tooltip("Played when no branch above matches. Leave empty to clear with no ending scene.")]
        [SerializeField] private VNScene defaultEndingScene;

        private bool triggered;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered) return;
            if (!other.CompareTag("Player") || GameManager.Instance == null) return;
            if (!GameManager.Instance.HasEnoughClues(requiredClueCount)) return;

            triggered = true;
            PlayEndingThenClearAsync().Forget();
        }

        private async UniTaskVoid PlayEndingThenClearAsync()
        {
            var scene = SelectEndingScene();
            if (scene != null && GameModeManager.Instance != null)
                await GameModeManager.Instance.PlayVNAsync(scene);

            GameManager.Instance.ClearGame();
        }

        /// <summary>The player's choices (recorded as StoryFlags) decide which ending scene, if any, plays before the clear screen.</summary>
        private VNScene SelectEndingScene()
        {
            var flags = StoryFlags.Instance;
            if (flags != null && endingBranches != null)
            {
                foreach (var branch in endingBranches)
                {
                    if (branch == null || string.IsNullOrEmpty(branch.requiredFlag)) continue;
                    if (flags.Get(branch.requiredFlag) == branch.requiredValue)
                        return branch.endingScene;
                }
            }

            return defaultEndingScene;
        }
    }
}
