using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.VisualNovel
{
    public enum PortraitSlot { Left, CenterLeft, Center, CenterRight, Right }

    public enum PortraitAction { Show, Hide, SetExpression, Move, Emote }

    public enum PortraitTransition { Fade, SlideFromEdge, Pop }

    public enum PortraitEmote { Shake, Bounce, Nod, Sway }

    /// <summary>A stage direction applied before a line is shown: who is on stage, where, and how they react.</summary>
    [Serializable]
    public class VNPortraitCommand
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("cmd"), LabelText("キャラ"), LabelWidth(50)]
#endif
        public CharacterDefinition character;

#if ODIN_INSPECTOR
        [HorizontalGroup("cmd"), LabelText("動作"), LabelWidth(40)]
#endif
        public PortraitAction action = PortraitAction.Show;

#if ODIN_INSPECTOR
        [HorizontalGroup("cmd2"), LabelText("位置"), LabelWidth(50)]
        [ShowIf("@action == PortraitAction.Show || action == PortraitAction.Move")]
#endif
        public PortraitSlot slot = PortraitSlot.Center;

        [Tooltip("Expression key; empty uses the character's first expression.")]
#if ODIN_INSPECTOR
        [HorizontalGroup("cmd2"), LabelText("表情"), LabelWidth(40)]
        [ShowIf("@action == PortraitAction.Show || action == PortraitAction.SetExpression")]
#endif
        public string expression;

#if ODIN_INSPECTOR
        [HorizontalGroup("cmd3"), LabelText("演出"), LabelWidth(50)]
        [ShowIf("@action == PortraitAction.Show || action == PortraitAction.Hide")]
#endif
        public PortraitTransition transition = PortraitTransition.Fade;

#if ODIN_INSPECTOR
        [HorizontalGroup("cmd3"), LabelText("感情"), LabelWidth(40)]
        [ShowIf("@action == PortraitAction.Emote")]
#endif
        public PortraitEmote emote = PortraitEmote.Shake;
    }
}
