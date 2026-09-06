using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.VisualNovel
{
    [System.Serializable]
    public class VNChoice
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("choice"), LabelWidth(50)]
#endif
        public string text;

        [Tooltip("-1 = go to the next line in order")]
#if ODIN_INSPECTOR
        [HorizontalGroup("choice", width: 120), LabelText("→ 行"), LabelWidth(34)]
#endif
        public int nextLineIndex = -1;
    }

    [System.Serializable]
    public class VNLine
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("head"), LabelText("話者"), LabelWidth(40)]
#endif
        public string speaker;

        [TextArea(2, 5)]
        public string text;

        [Tooltip("Leave empty to keep the current background")]
#if ODIN_INSPECTOR
        [PreviewField(48, ObjectFieldAlignment.Left), HorizontalGroup("head", width: 60), HideLabel]
#endif
        public Sprite background;

#if ODIN_INSPECTOR
        [ListDrawerSettings(DefaultExpandedState = true), LabelText("選択肢")]
#endif
        public VNChoice[] choices;
    }
}
