using System.Collections.Generic;
using UnityEngine;

namespace Yobitsugi.VisualNovel
{
    /// <summary>
    /// Runtime lookup for the "Anime Boy/Girl" reaction SFX packs under
    /// Assets/Resources/Reactions/&lt;Boy|Girl&gt;. Clips are not tagged individually — instead each
    /// expression key is matched against candidate words baked into the pack's file names
    /// (e.g. "SFX_AnimeGirlAaaahAngry1.wav" for the "angry" expression).
    /// </summary>
    public static class VNReactionLibrary
    {
        public const string BoyFolder = "Reactions/Boy";
        public const string GirlFolder = "Reactions/Girl";

        /// <summary>Expression keys with no natural reaction bark; SetExpression/Show never plays a clip for these.</summary>
        private static readonly HashSet<string> SilentExpressions = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
        {
            "normal", "pose_idle", "pose_talk", "pose_alert", "eyes_closed",
        };

        /// <summary>Ordered candidate substrings per expression key; the first with any matching clip wins.</summary>
        private static readonly Dictionary<string, string[]> ExpressionKeywords =
            new Dictionary<string, string[]>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "angry", new[] { "Angry" } },
            { "embarrassed", new[] { "Embarassed", "Embarrassed" } },
            { "happy", new[] { "Happy", "Enthusiastic", "Proud", "Laugh" } },
            { "smile", new[] { "Happy", "Good", "Nice", "Great", "Awesome", "Proud" } },
            { "sad", new[] { "Sad", "Cry", "Disgusted" } },
            { "worried", new[] { "Tired", "Bored", "Sad", "Exhausted" } },
            { "scared", new[] { "Scared", "Injured", "Hurt" } },
            { "surprised", new[] { "Surprise", "Surprised" } },
        };

        private static Dictionary<ReactionVoice, AudioClip[]> banks;

        /// <summary>Returns a random clip for a character's reaction voice + expression key, or null for none/no match.</summary>
        public static AudioClip FindReaction(ReactionVoice voice, string expressionKey)
        {
            if (voice == ReactionVoice.None || string.IsNullOrEmpty(expressionKey)) return null;
            if (SilentExpressions.Contains(expressionKey)) return null;
            if (!ExpressionKeywords.TryGetValue(expressionKey, out var keywords)) return null;

            EnsureLoaded();
            if (!banks.TryGetValue(voice, out var clips) || clips.Length == 0) return null;

            foreach (var keyword in keywords)
            {
                var matches = MatchesFor(voice, keyword, clips);
                if (matches.Count > 0)
                    return matches[Random.Range(0, matches.Count)];
            }

            return null;
        }

        /// <summary>Call after adding or removing reaction clips at runtime to force a re-scan.</summary>
        public static void Invalidate() => banks = null;

        private static void EnsureLoaded()
        {
            if (banks != null) return;

            banks = new Dictionary<ReactionVoice, AudioClip[]>
            {
                [ReactionVoice.Boy] = Resources.LoadAll<AudioClip>(BoyFolder),
                [ReactionVoice.Girl] = Resources.LoadAll<AudioClip>(GirlFolder),
            };
        }

        private static List<AudioClip> MatchesFor(ReactionVoice voice, string keyword, AudioClip[] clips)
        {
            var result = new List<AudioClip>();
            foreach (var clip in clips)
            {
                if (clip != null && clip.name.IndexOf(keyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    result.Add(clip);
            }
            return result;
        }
    }
}
