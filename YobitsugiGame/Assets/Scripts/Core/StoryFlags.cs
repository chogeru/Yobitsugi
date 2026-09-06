using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yobitsugi.Core
{
    /// <summary>
    /// Named story state (choices made, events seen, counters) that scenario data can set and branch on.
    /// Persisted through <see cref="ISaveParticipant"/>, so flags survive save/load like everything else.
    /// </summary>
    public class StoryFlags : MonoBehaviour, ISaveParticipant
    {
        public static StoryFlags Instance { get; private set; }

        private readonly Dictionary<string, int> values = new Dictionary<string, int>();

        public int SaveOrder => 5;

        public event Action<string, int> OnFlagChanged;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int Get(string key) => !string.IsNullOrEmpty(key) && values.TryGetValue(key, out int value) ? value : 0;

        public bool GetBool(string key) => Get(key) != 0;

        public void Set(string key, int value)
        {
            if (string.IsNullOrEmpty(key)) return;

            values[key] = value;
            OnFlagChanged?.Invoke(key, value);
        }

        public void SetBool(string key, bool value) => Set(key, value ? 1 : 0);

        public void Add(string key, int delta) => Set(key, Get(key) + delta);

        public void Clear()
        {
            values.Clear();
        }

        // --- ISaveParticipant ---

        public void Capture(SaveData data)
        {
            var keys = new List<string>(values.Count);
            var numbers = new List<int>(values.Count);

            foreach (var pair in values)
            {
                keys.Add(pair.Key);
                numbers.Add(pair.Value);
            }

            // JsonUtility cannot serialize a dictionary, so store it as parallel arrays.
            data.flagKeys = keys.ToArray();
            data.flagValues = numbers.ToArray();
        }

        public void Restore(SaveData data)
        {
            values.Clear();

            if (data.flagKeys == null || data.flagValues == null) return;

            int count = Mathf.Min(data.flagKeys.Length, data.flagValues.Length);
            for (int i = 0; i < count; i++)
            {
                if (!string.IsNullOrEmpty(data.flagKeys[i]))
                    values[data.flagKeys[i]] = data.flagValues[i];
            }
        }
    }
}
