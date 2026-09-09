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
        /// <summary>Speaker, text, and that line's voice clip (may be null) — the backlog keeps the clip so a line can be replayed.</summary>
        public static event Action<string, string, AudioClip> OnVNLineShown;

        /// <summary>A line's voice clip, or null to stop whatever is playing.</summary>
        public static event Action<AudioClip> OnVoiceRequested;
        public static event Action<bool> OnModeChanged;
        public static event Action<string> OnClueCollected;
        public static event Action<int> OnSaveCompleted;
        public static event Action<int> OnLoadCompleted;
        public static event Action OnGameCleared;

        /// <summary>Raised once per non-whitespace glyph as the typewriter reveals dialogue text.</summary>
        public static event Action OnDialogueCharacterRevealed;

        /// <summary>A character's expression changed on stage (entrance or SetExpression), with the expression key shown.</summary>
        public static event Action<CharacterDefinition, string> OnCharacterReaction;

        /// <summary>
        /// Requests a brief screen shake: duration in seconds, strength as an arbitrary 0-100 intensity.
        /// Exploration (3D camera) and VN (dialogue canvas) each convert it to their own units.
        /// </summary>
        public static event Action<float, float> OnScreenShakeRequested;

        public static void RaiseVNSceneStarted(VNScene scene) => OnVNSceneStarted?.Invoke(scene);
        public static void RaiseVNSceneEnded(VNScene scene) => OnVNSceneEnded?.Invoke(scene);
        public static void RaiseVNLineShown(string speaker, string text, AudioClip voice) => OnVNLineShown?.Invoke(speaker, text, voice);
        public static void RaiseVoiceRequested(AudioClip clip) => OnVoiceRequested?.Invoke(clip);
        public static void RaiseModeChanged(bool isInVN) => OnModeChanged?.Invoke(isInVN);
        public static void RaiseClueCollected(string clueId) => OnClueCollected?.Invoke(clueId);
        public static void RaiseSaveCompleted(int slot) => OnSaveCompleted?.Invoke(slot);
        public static void RaiseLoadCompleted(int slot) => OnLoadCompleted?.Invoke(slot);
        public static void RaiseGameCleared() => OnGameCleared?.Invoke();
        public static void RaiseDialogueCharacterRevealed() => OnDialogueCharacterRevealed?.Invoke();
        public static void RaiseCharacterReaction(CharacterDefinition character, string expressionKey) => OnCharacterReaction?.Invoke(character, expressionKey);
        public static void RaiseScreenShakeRequested(float duration, float strength) => OnScreenShakeRequested?.Invoke(duration, strength);

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
            OnGameCleared = null;
            OnDialogueCharacterRevealed = null;
            OnCharacterReaction = null;
            OnScreenShakeRequested = null;
        }
    }
}
