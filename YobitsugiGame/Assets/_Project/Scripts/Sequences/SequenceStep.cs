using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Yobitsugi.Core;
using Yobitsugi.UI;

namespace Yobitsugi.Sequences
{
    /// <summary>Services a step may use. Passed in rather than looked up, so steps stay testable and loosely coupled.</summary>
    public class SequenceContext
    {
        public IGameModeController Mode { get; }
        public IScreenFader Fader { get; }

        public SequenceContext(IGameModeController mode, IScreenFader fader)
        {
            Mode = mode;
            Fader = fader;
        }
    }

    /// <summary>
    /// One action in a scripted sequence. Concrete steps are [SerializeReference] entries on <see cref="GameSequence"/>,
    /// so designers can compose events in the inspector and new step types need no changes elsewhere.
    /// </summary>
    [Serializable]
    public abstract class SequenceStep
    {
        public abstract UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken);
    }
}
