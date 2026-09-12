using System.Collections.Generic;
using UnityEngine;

namespace Yobitsugi.VisualNovel
{
    /// <summary>Looks up VNScene assets by their stable SceneId instead of asset name/path, so save data survives renames.</summary>
    public static class VNSceneRegistry
    {
        private const string ResourcesFolder = "VNScenes";

        private static Dictionary<string, VNScene> map;

        public static VNScene Find(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return null;

            EnsureLoaded();
            return map.TryGetValue(sceneId, out var scene) ? scene : null;
        }

        /// <summary>Call after adding/removing VN Scene assets at runtime (e.g. after a fresh AssetBundle/Resources load) to force a re-scan.</summary>
        public static void Invalidate() => map = null;

        private static void EnsureLoaded()
        {
            if (map != null) return;

            map = new Dictionary<string, VNScene>();
            foreach (var scene in Resources.LoadAll<VNScene>(ResourcesFolder))
                map[scene.SceneId] = scene;
        }
    }
}
