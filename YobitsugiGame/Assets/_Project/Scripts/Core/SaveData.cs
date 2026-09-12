using System;
using UnityEngine;

namespace Yobitsugi.Core
{
    [Serializable]
    public class SaveData
    {
        /// <summary>Bumped when the shape of this data changes, so old files can be migrated or rejected.</summary>
        public int version = CurrentVersion;
        public const int CurrentVersion = 1;

        public bool isInVN;
        public string vnSceneId;
        public int vnLineIndex;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public string[] collectedClueIds;

        // Parallel arrays because JsonUtility cannot serialize dictionaries.
        public string[] flagKeys;
        public int[] flagValues;

        public string savedAtDisplay;
    }
}
