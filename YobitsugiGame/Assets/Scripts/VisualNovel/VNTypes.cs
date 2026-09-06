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

        [Tooltip("Story flag set when this choice is picked. Leave empty for none.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("flag"), LabelText("設定フラグ"), LabelWidth(70)]
#endif
        public string setFlag;

#if ODIN_INSPECTOR
        [HorizontalGroup("flag", width: 100), LabelText("値"), LabelWidth(24)]
#endif
        public int setFlagValue = 1;

        [Tooltip("Only offer this choice when the flag is non-zero. Leave empty to always show it.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("cond"), LabelText("表示条件フラグ"), LabelWidth(90)]
#endif
        public string requiredFlag;
    }

    [System.Serializable]
    public class VNLine
    {
        [Tooltip("Speaking character; drives the name plate and portrait highlight. Leave empty for narration.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("head"), LabelText("話者"), LabelWidth(40)]
#endif
        public CharacterDefinition character;

        [Tooltip("Used when no character asset is set (narration or one-off speakers).")]
#if ODIN_INSPECTOR
        [HorizontalGroup("head"), LabelText("話者名(直接)"), LabelWidth(80)]
#endif
        public string speaker;

        [TextArea(2, 5)]
        public string text;

        [Tooltip("Leave empty to keep the current background")]
#if ODIN_INSPECTOR
        [PreviewField(48, ObjectFieldAlignment.Left), HorizontalGroup("bg", width: 60), HideLabel]
#endif
        public Sprite background;

        [Tooltip("Optional voice clip played when this line appears. In auto mode the line waits for it to finish.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("bg"), LabelText("ボイス"), LabelWidth(50)]
#endif
        public AudioClip voice;

        [Tooltip("Stage directions applied before this line is shown.")]
#if ODIN_INSPECTOR
        [ListDrawerSettings(DefaultExpandedState = true), LabelText("立ち絵")]
#endif
        public VNPortraitCommand[] portraits;

#if ODIN_INSPECTOR
        [ListDrawerSettings(DefaultExpandedState = true), LabelText("選択肢")]
#endif
        public VNChoice[] choices;

        [Tooltip("Story flag set when this line is shown. Leave empty for none.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("flag"), LabelText("設定フラグ"), LabelWidth(70)]
#endif
        public string setFlag;

#if ODIN_INSPECTOR
        [HorizontalGroup("flag", width: 100), LabelText("値"), LabelWidth(24)]
#endif
        public int setFlagValue = 1;

        [Tooltip("Skip this line unless the flag is non-zero. Leave empty to always show it.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("cond"), LabelText("表示条件フラグ"), LabelWidth(90)]
#endif
        public string requiredFlag;

        [Tooltip("Skip this line when the flag is non-zero.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("cond"), LabelText("除外フラグ"), LabelWidth(70)]
#endif
        public string forbiddenFlag;

        public string SpeakerName => character != null ? character.SpeakerName : speaker;
        public Color SpeakerColor => character != null ? character.nameColor : Color.white;
    }
}
