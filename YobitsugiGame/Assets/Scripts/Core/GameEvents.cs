using System;
using UnityEngine;
using Yobitsugi.VisualNovel;

namespace Yobitsugi.Core
{
    /// <summary>Decoupled hook points other systems (UI, audio, achievements…) subscribe to instead of referencing each other.</summary>
    public static class GameEvents
    {
        public static event Action<VNScene> OnVNSceneStarted;
        public static event Action<VNScene> OnVNSceneEnded;
        public static event Action<string, string> OnVNLineShown;

        /// <summary>A line's voice clip, or null to stop whatever is playing.</summary>
        public static event Action<AudioClip> OnVoiceRequested;
        public static event Action<bool> OnModeChanged;
        public static event Action<string> OnClueCollected;
        public static event Action<int> OnSaveCompleted;
        public static event Action<int> OnLoadCompleted;

        public static void RaiseVNSceneStarted(VNScene scene) => OnVNSceneStarted?.Invoke(scene);
        public static void RaiseVNSceneEnded(VNScene scene) => OnVNSceneEnded?.Invoke(scene);
        public static void RaiseVNLineShown(string speaker, string text) => OnVNLineShown?.Invoke(speaker, text);
        public static void RaiseVoiceRequested(AudioClip clip) => OnVoiceRequested?.Invoke(clip);
        public static void RaiseModeChanged(bool isInVN) => OnModeChanged?.Invoke(isInVN);
        public static void RaiseClueCollected(string clueId) => OnClueCollected?.Invoke(clueId);
        public static void RaiseSaveCompleted(int slot) => OnSaveCompleted?.Invoke(slot);
        public static void RaiseLoadCompleted(int slot) => OnLoadCompleted?.Invoke(slot);

        // Static events outlive play sessions when domain reload is disabled, so clear them on every play.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSubscriptions()
        {
            OnVNSceneStarted = null;
            OnVNSceneEnded = null;
            OnVNLineShown = null;
            OnVoiceRequested = null;
            OnModeChanged = null;
            OnClueCollected = null;
            OnSaveCompleted = null;
            OnLoadCompleted = null;
        }
    }
}
