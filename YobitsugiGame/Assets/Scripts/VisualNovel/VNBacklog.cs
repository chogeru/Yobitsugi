using System;
using System.Collections.Generic;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.VisualNovel
{
    public readonly struct BacklogEntry
    {
        public readonly string Speaker;
        public readonly string Text;
        public readonly AudioClip Voice;

        public BacklogEntry(string speaker, string text, AudioClip voice)
        {
            Speaker = speaker;
            Text = text;
            Voice = voice;
        }

        public string DisplayText => string.IsNullOrEmpty(Speaker) ? Text : $"{Speaker}: {Text}";
    }

    /// <summary>Capped history of shown dialogue lines. Fed by events, so the UI never has to know about VNManager.</summary>
    public static class VNBacklog
    {
        private const int MaxEntries = 120;

        private static readonly List<BacklogEntry> entries = new List<BacklogEntry>();

        public static IReadOnlyList<BacklogEntry> Entries => entries;
        public static event Action OnChanged;

        public static void Clear()
        {
            entries.Clear();
            OnChanged?.Invoke();
        }

        private static void Record(string speaker, string text, AudioClip voice)
        {
            entries.Add(new BacklogEntry(speaker, text, voice));
            if (entries.Count > MaxEntries)
                entries.RemoveRange(0, entries.Count - MaxEntries);

            OnChanged?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            entries.Clear();
            OnChanged = null;
            GameEvents.OnVNLineShown -= Record;
            GameEvents.OnVNLineShown += Record;
        }
    }
}
