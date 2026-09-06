using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yobitsugi.Core;
using Yobitsugi.UI;

namespace Yobitsugi.Sequences
{
    /// <summary>
    /// An ordered list of steps composed in the inspector (VN scene → wait → fade → move the player…).
    /// Add new behaviour by writing a <see cref="SequenceStep"/>; this runner never changes.
    /// </summary>
    public class GameSequence : MonoBehaviour
    {
        [SerializeReference] private List<SequenceStep> steps = new List<SequenceStep>();
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool playOnce = true;

        private CancellationTokenSource cts;
        private bool hasPlayed;

        public bool IsPlaying { get; private set; }

        private void Start()
        {
            if (playOnStart) Play();
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
        }

        public void Play()
        {
            if (IsPlaying || (playOnce && hasPlayed)) return;
            PlayAsync().Forget();
        }

        public async UniTask PlayAsync()
        {
            if (IsPlaying || (playOnce && hasPlayed)) return;

            IsPlaying = true;
            hasPlayed = true;

            cts?.Cancel();
            cts?.Dispose();
            cts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());

            var context = new SequenceContext(GameModeManager.Instance, ScreenFader.Instance);

            try
            {
                foreach (var step in steps)
                {
                    if (step == null) continue;
                    await step.ExecuteAsync(context, cts.Token);
                }
            }
            catch (System.OperationCanceledException)
            {
                // Destroyed or interrupted — nothing to clean up beyond the token.
            }
            finally
            {
                IsPlaying = false;
            }
        }
    }
}
