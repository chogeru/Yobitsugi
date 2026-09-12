using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.VisualNovel
{
    [CreateAssetMenu(menuName = "Yobitsugi/VN Scene", fileName = "New VN Scene")]
    public class VNScene : ScriptableObject
    {
        [Tooltip("Stable identifier used by save data. Defaults to the asset name if left empty.")]
#if ODIN_INSPECTOR
        [BoxGroup("識別"), LabelText("シーンID")]
#endif
        [SerializeField] private string sceneId;

#if ODIN_INSPECTOR
        [BoxGroup("シナリオ"), LabelText("行"), ListDrawerSettings(ShowIndexLabels = true, DefaultExpandedState = false)]
#endif
        public VNLine[] lines;

        [Tooltip("Optional: automatically continue into another VN scene once this one finishes, without returning to exploration.")]
#if ODIN_INSPECTOR
        [BoxGroup("シナリオ"), LabelText("次のシーン(任意)")]
#endif
        public VNScene nextScene;

        [Tooltip("Optional: crossfades to this track when the scene starts. Leave empty to keep AudioService's default VN music.")]
#if ODIN_INSPECTOR
        [BoxGroup("シナリオ"), LabelText("このシーンのBGM(任意)")]
#endif
        public AudioClip music;

        public string SceneId => string.IsNullOrEmpty(sceneId) ? name : sceneId;

#if ODIN_INSPECTOR
        [BoxGroup("シナリオ"), ShowInInspector, ReadOnly, LabelText("行数")]
        private int LineCount => lines?.Length ?? 0;
#endif

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(sceneId))
                sceneId = name;
        }
#endif
    }
}
