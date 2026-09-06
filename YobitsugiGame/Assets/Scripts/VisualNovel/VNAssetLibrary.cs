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

        private static Dictionary<string, CharacterDefinition> characters;
        private static Dictionary<string, Sprite> backgrounds;

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
