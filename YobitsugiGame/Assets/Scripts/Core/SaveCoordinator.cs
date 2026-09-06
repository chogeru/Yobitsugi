using System;
using System.Collections.Generic;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.Core
{
    /// <summary>
    /// Gathers every <see cref="ISaveParticipant"/> in the scene and drives save/load through them,
    /// so adding new persisted state means implementing the interface — nothing here changes.
    /// </summary>
    public class SaveCoordinator : MonoBehaviour
    {
        public static SaveCoordinator Instance { get; private set; }

#if ODIN_INSPECTOR
        [Title("セーブ管理", "保存対象を自動で集めて、まとめて読み書きする", TitleAlignments.Left)]
        [InfoBox("保存する情報を増やしたい時は、そのクラスに ISaveParticipant を実装するだけです。\n" +
                 "このコンポーネントはシーン内から自動で見つけるため、変更は不要です。\n" +
                 "現在の参加者: GameManager(手がかり) / StoryFlags(フラグ) / GameModeManager(位置とノベル進行)")]
        [ShowInInspector, ReadOnly, LabelText("保存対象"), ListDrawerSettings(IsReadOnly = true)]
        private string[] ParticipantNames => participants.ConvertAll(p => p.GetType().Name).ToArray();
#endif

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
