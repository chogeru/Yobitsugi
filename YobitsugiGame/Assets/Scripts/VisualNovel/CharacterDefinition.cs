using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.VisualNovel
{
    [Serializable]
    public class CharacterExpression
    {
#if ODIN_INSPECTOR
        [HorizontalGroup("exp", width: 90), HideLabel, PreviewField(72, ObjectFieldAlignment.Left)]
#endif
        public Sprite sprite;

        [Tooltip("Key referenced from scenario lines, e.g. normal / smile / scared")]
#if ODIN_INSPECTOR
        [HorizontalGroup("exp"), LabelText("表情キー"), LabelWidth(70)]
#endif
        public string key = "normal";
    }

    /// <summary>Which reaction voice bank (Assets/Resources/Reactions/&lt;Boy|Girl&gt;) this character draws from.</summary>
    public enum ReactionVoice { None, Boy, Girl }

    /// <summary>One speaking character: display name, name colour and the portrait set used on stage.</summary>
    [CreateAssetMenu(menuName = "Yobitsugi/Character", fileName = "New Character")]
    public class CharacterDefinition : ScriptableObject
    {
        [Tooltip("Stable id used by save data. Defaults to the asset name if left empty.")]
        [SerializeField] private string characterId;

#if ODIN_INSPECTOR
        [BoxGroup("表示"), LabelText("表示名")]
#endif
        public string displayName;

#if ODIN_INSPECTOR
        [BoxGroup("表示"), LabelText("名前の色")]
#endif
        public Color nameColor = Color.white;

        [Tooltip("Reaction voice bank used for emotion SFX when an expression changes on stage. None plays nothing.")]
#if ODIN_INSPECTOR
        [BoxGroup("表示"), LabelText("リアクションボイス")]
#endif
        public ReactionVoice reactionVoice = ReactionVoice.None;

#if ODIN_INSPECTOR
        [BoxGroup("立ち絵"), LabelText("表情"), ListDrawerSettings(DefaultExpandedState = true)]
#endif
        public CharacterExpression[] expressions;

        [Tooltip("Portrait scale multiplier, for characters drawn at a different size.")]
#if ODIN_INSPECTOR
        [BoxGroup("立ち絵"), LabelText("拡大率")]
#endif
        public float portraitScale = 1f;

        [Tooltip("Pixel offset applied to this character's portrait within its slot.")]
#if ODIN_INSPECTOR
        [BoxGroup("立ち絵"), LabelText("位置オフセット")]
#endif
        public Vector2 portraitOffset;

        public string CharacterId => string.IsNullOrEmpty(characterId) ? name : characterId;
        public string SpeakerName => string.IsNullOrEmpty(displayName) ? name : displayName;

        /// <summary>Returns the sprite for an expression key, falling back to the first entry.</summary>
        public Sprite GetExpression(string key)
        {
            if (expressions == null || expressions.Length == 0) return null;

            if (!string.IsNullOrEmpty(key))
            {
                foreach (var expression in expressions)
                {
                    if (expression != null && string.Equals(expression.key, key, StringComparison.OrdinalIgnoreCase))
                        return expression.sprite;
                }
            }

            return expressions[0].sprite;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(characterId)) characterId = name;
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }
#endif
    }
}
