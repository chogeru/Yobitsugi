using System;
using System.Collections.Generic;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.VisualNovel
{
    /// <summary>Capped history of shown dialogue lines. Fed by events, so the UI never has to know about VNManager.</summary>
    public static class VNBacklog
    {
        private const int MaxEntries = 120;

        private static readonly List<string> entries = new List<string>();

        public static IReadOnlyList<string> Entries => entries;
        public static event Action OnChanged;

        public static void Clear()
        {
            entries.Clear();
            OnChanged?.Invoke();
        }

        private static void Record(string speaker, string text)
        {
            entries.Add(string.IsNullOrEmpty(speaker) ? text : $"{speaker}: {text}");
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
