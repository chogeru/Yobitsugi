using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Yobitsugi.UI
{
    public interface IScreenFader
    {
        /// <summary>Fades to black, runs <paramref name="atBlack"/>, then fades back in.</summary>
        void Transition(Action atBlack, float duration = -1f);

        UniTask FadeOutAsync(float duration = -1f, CancellationToken cancellationToken = default);
        UniTask FadeInAsync(float duration = -1f, CancellationToken cancellationToken = default);
    }
}
