using System.Threading;
using Cysharp.Threading.Tasks;
using Yobitsugi.VisualNovel;

namespace Yobitsugi.Core
{
    /// <summary>The 2D(VN) / 3D(exploration) mode switch, as seen by triggers, sequences and UI.</summary>
    public interface IGameModeController
    {
        bool IsInVN { get; }

        void EnterVN(VNScene scene);

        /// <summary>Plays a VN scene and completes once it has finished and exploration has resumed.</summary>
        UniTask PlayVNAsync(VNScene scene, CancellationToken cancellationToken = default);

        void PauseForMenu();
        void ResumeFromMenu();
    }
}
