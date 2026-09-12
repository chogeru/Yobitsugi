namespace Yobitsugi.Core
{
    /// <summary>Implement on any system that owns part of the save state; SaveCoordinator collects them automatically.</summary>
    public interface ISaveParticipant
    {
        /// <summary>Lower runs first on both capture and restore.</summary>
        int SaveOrder { get; }

        void Capture(SaveData data);
        void Restore(SaveData data);
    }
}
