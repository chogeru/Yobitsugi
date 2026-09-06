using System;
using UnityEngine;

namespace Yobitsugi.Core
{
    [Serializable]
    public class SaveData
    {
        public bool isInVN;
        public string vnSceneId;
        public int vnLineIndex;
        public Vector3 playerPosition;
        public Quaternion playerRotation;
        public string[] collectedClueIds;
        public string savedAtDisplay;
    }
}
