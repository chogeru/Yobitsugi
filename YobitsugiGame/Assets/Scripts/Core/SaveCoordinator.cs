using System;
using System.Collections.Generic;
using UnityEngine;

namespace Yobitsugi.Core
{
    /// <summary>
    /// Gathers every <see cref="ISaveParticipant"/> in the scene and drives save/load through them,
    /// so adding new persisted state means implementing the interface — nothing here changes.
    /// </summary>
    public class SaveCoordinator : MonoBehaviour
    {
        public static SaveCoordinator Instance { get; private set; }

        private readonly List<ISaveParticipant> participants = new List<ISaveParticipant>();

        private void Awake()
        {
            Instance = this;
            Refresh();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Re-scans the scene. Call after spawning systems that persist state.</summary>
        public void Refresh()
        {
            participants.Clear();
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (behaviour is ISaveParticipant participant)
                    participants.Add(participant);
            }

            participants.Sort((a, b) => a.SaveOrder.CompareTo(b.SaveOrder));
        }

        public void Save(int slot)
        {
            var data = new SaveData
            {
                collectedClueIds = Array.Empty<string>(),
                savedAtDisplay = DateTime.Now.ToString("yyyy/MM/dd HH:mm"),
            };

            foreach (var participant in participants)
                participant.Capture(data);

            if (SaveSystem.Save(slot, data))
                GameEvents.RaiseSaveCompleted(slot);
        }

        public bool Load(int slot)
        {
            var data = SaveSystem.Load(slot);
            if (data == null) return false;

            foreach (var participant in participants)
                participant.Restore(data);

            GameEvents.RaiseLoadCompleted(slot);
            return true;
        }
    }
}
