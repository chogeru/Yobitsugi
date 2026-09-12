using System.Collections.Generic;
using UnityEngine;

namespace Yobitsugi.VisualNovel
{
    /// <summary>
    /// Runtime lookup for scenario assets by stable id (characters, backgrounds).
    /// Ids keep save data and scenario text independent of file names and folder layout.
    /// </summary>
    public static class VNAssetLibrary
    {
        public const string CharactersFolder = "Characters";
        public const string BackgroundsFolder = "Backgrounds";
        public const string VoicesFolder = "Voices";

        private static Dictionary<string, CharacterDefinition> characters;
        private static Dictionary<string, Sprite> backgrounds;
        private static Dictionary<string, AudioClip> voices;

        public static CharacterDefinition FindCharacter(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;

            EnsureCharacters();
            return characters.TryGetValue(characterId, out var character) ? character : null;
        }

        public static Sprite FindBackground(string backgroundId)
        {
            if (string.IsNullOrEmpty(backgroundId)) return null;

            EnsureBackgrounds();
            return backgrounds.TryGetValue(backgroundId, out var sprite) ? sprite : null;
        }

        /// <summary>Voice clips are addressed by file name, e.g. "Intro_000_Mio".</summary>
        public static AudioClip FindVoice(string voiceId)
        {
            if (string.IsNullOrEmpty(voiceId)) return null;

            if (voices == null)
            {
                voices = new Dictionary<string, AudioClip>();
                foreach (var clip in Resources.LoadAll<AudioClip>(VoicesFolder))
                    voices[clip.name] = clip;
            }

            return voices.TryGetValue(voiceId, out var voice) ? voice : null;
        }

        public static IReadOnlyCollection<CharacterDefinition> AllCharacters()
        {
            EnsureCharacters();
            return characters.Values;
        }

        /// <summary>Call after adding or removing assets at runtime to force a re-scan.</summary>
        public static void Invalidate()
        {
            characters = null;
            backgrounds = null;
            voices = null;
        }

        private static void EnsureCharacters()
        {
            if (characters != null) return;

            characters = new Dictionary<string, CharacterDefinition>();
            foreach (var character in Resources.LoadAll<CharacterDefinition>(CharactersFolder))
                characters[character.CharacterId] = character;
        }

        private static void EnsureBackgrounds()
        {
            if (backgrounds != null) return;

            backgrounds = new Dictionary<string, Sprite>();
            foreach (var sprite in Resources.LoadAll<Sprite>(BackgroundsFolder))
                backgrounds[sprite.name] = sprite;
        }
    }
}
