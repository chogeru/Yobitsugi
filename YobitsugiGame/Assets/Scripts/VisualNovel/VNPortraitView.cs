using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Yobitsugi.Core;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.VisualNovel
{
    /// <summary>Renders character portraits on stage: entrances, exits, expression swaps, emotes and speaker highlighting.</summary>
    public class VNPortraitView : MonoBehaviour, IVNPortraitView
    {
        private class StagedPortrait
        {
            /// <summary>Outer transform: owns slot position and the speaking step-forward.</summary>
            public RectTransform Root;
            /// <summary>Inner transform: owns the idle breathing loop, so the two never fight over one property.</summary>
            public RectTransform Body;
            public Image Image;
            public PortraitSlot Slot;
            public string Expression;
            public Tween Breath;
        }

#if ODIN_INSPECTOR
        [Title("立ち絵ステージ", "キャラクターの登場・退場・表情・感情表現を描画する", TitleAlignments.Left)]
        [InfoBox("シナリオ側の使い方: VN Scene の各行にある「立ち絵」欄に指示を並べます。\n" +
                 "登場(Show) / 退場(Hide) / 表情変更(SetExpression) / 移動(Move) / 感情(Emote)\n" +
                 "話者に指定されたキャラは自動で明るく前に出て、それ以外は暗く小さくなります。\n" +
                 "立ち絵の画像は「Yobitsugi/Build Character Definitions from Art」で一括登録できます。")]
        [Required, BoxGroup("参照"), LabelText("配置先")]
#endif
        [Header("Refs")]
        [SerializeField] private RectTransform stage;

#if ODIN_INSPECTOR
        [Required, BoxGroup("参照"), LabelText("立ち絵の雛形")]
#endif
        [SerializeField] private Image portraitTemplate;

        [Header("Layout")]
        [Tooltip("Normalised horizontal positions for Left … Right slots. Left/Right are the standard two-character stage.")]
        [SerializeField] private float[] slotAnchors = { 0.26f, 0.38f, 0.5f, 0.62f, 0.74f };
        [Tooltip("Portrait box in reference-resolution pixels; art keeps its aspect inside it. " +
                 "Sized so a full-body standee fits under a 1080-tall screen with headroom.")]
        [SerializeField] private Vector2 portraitSize = new Vector2(900f, 1020f);
        [Tooltip("Vertical offset from the bottom edge; the text panel covers the legs, as novel games do.")]
        [SerializeField] private float baseOffsetY = 0f;

        [Header("Animation")]
        [SerializeField] private float enterDuration = 0.35f;
        [SerializeField] private float exitDuration = 0.25f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private float expressionCrossfade = 0.15f;
        [SerializeField] private float slideDistance = 160f;
        [SerializeField] private float emoteStrength = 24f;
        [Tooltip("A tiny dip held for a beat before a character exits, so leaving reads as a choice rather than a cut.")]
        [SerializeField] private float exitSettleDistance = 10f;
        [SerializeField] private float exitSettleDuration = 0.12f;

        [Header("Speaker highlight")]
        [SerializeField] private Color speakingTint = Color.white;
        [Tooltip("Non-speaking characters are dimmed to this tint — they stay on stage, just darker.")]
        [SerializeField] private Color idleTint = new Color(0.5f, 0.5f, 0.58f, 1f);
        [SerializeField] private float speakingScale = 1f;
        [Tooltip("Keep at 1 so dimmed characters darken without shifting size.")]
        [SerializeField] private float idleScale = 1f;
        [SerializeField] private float highlightDuration = 0.25f;
        [Tooltip("How far the speaking character steps toward the viewer.")]
        [SerializeField] private float speakingLift = 18f;

        [Header("Idle motion")]
        [Tooltip("Gentle breathing so a silent character never looks like a frozen image.")]
        [SerializeField] private float breathDistance = 7f;
        [SerializeField] private float breathSeconds = 2.6f;

        private readonly Dictionary<CharacterDefinition, StagedPortrait> staged =
            new Dictionary<CharacterDefinition, StagedPortrait>();

        public void Apply(VNPortraitCommand command, bool instant)
        {
            if (command == null || command.character == null) return;

            switch (command.action)
            {
                case PortraitAction.Show: Show(command, instant); break;
                case PortraitAction.Hide: Hide(command, instant); break;
                case PortraitAction.SetExpression: SetExpression(command, instant); break;
                case PortraitAction.Move: Move(command, instant); break;
                case PortraitAction.Emote: Emote(command, instant); break;
            }
        }

        private void Show(VNPortraitCommand command, bool instant)
        {
            if (staged.TryGetValue(command.character, out var existing))
            {
                // Already on stage: treat as a move + expression change rather than spawning a duplicate.
                Move(command, instant);
                SetExpression(command, instant);
                return;
            }

            // Root carries staging, body carries breathing: one property each, so tweens never overwrite each other.
            var rootGO = new GameObject(command.character.CharacterId, typeof(RectTransform));
            var root = (RectTransform)rootGO.transform;
            root.SetParent(stage, false);

            var image = Instantiate(portraitTemplate, root);
            image.gameObject.SetActive(true);
            image.sprite = command.character.GetExpression(command.expression);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var body = (RectTransform)image.transform;
            body.anchorMin = body.anchorMax = new Vector2(0.5f, 0f);
            body.pivot = new Vector2(0.5f, 0f);
            body.anchoredPosition = Vector2.zero;
            body.sizeDelta = portraitSize;

            var portrait = new StagedPortrait
            {
                Root = root,
                Body = body,
                Image = image,
                Slot = command.slot,
                Expression = command.expression,
            };
            staged[command.character] = portrait;

            PlaceAt(portrait, command.character, command.slot);
            var target = root.anchoredPosition;

            if (instant)
            {
                image.color = idleTint;
                StartBreathing(portrait);
                return;
            }

            GameEvents.RaiseCharacterReaction(command.character, command.expression);

            DOTween.Kill(root);
            switch (command.transition)
            {
                case PortraitTransition.SlideFromEdge:
                    float from = command.slot <= PortraitSlot.CenterLeft ? -slideDistance : slideDistance;
                    root.anchoredPosition = target + new Vector2(from, 0f);
                    root.DOAnchorPos(target, enterDuration).SetEase(Ease.OutCubic).SetLink(rootGO);
                    break;

                case PortraitTransition.Pop:
                    root.localScale = Vector3.one * 0.9f;
                    root.DOScale(Vector3.one, enterDuration).SetEase(Ease.OutBack).SetLink(rootGO);
                    break;

                default:
                    // Even a plain fade drifts up a little; a portrait that only fades looks pasted on.
                    root.anchoredPosition = target - new Vector2(0f, 26f);
                    root.DOAnchorPos(target, enterDuration).SetEase(Ease.OutCubic).SetLink(rootGO);
                    break;
            }

            var color = idleTint;
            color.a = 0f;
            image.color = color;
            image.DOFade(idleTint.a, enterDuration).SetLink(rootGO)
                .OnComplete(() => StartBreathing(portrait));
        }

        private void StartBreathing(StagedPortrait portrait)
        {
            portrait.Breath?.Kill();
            if (portrait.Body == null || breathDistance <= 0f) return;

            portrait.Body.anchoredPosition = Vector2.zero;
            portrait.Breath = portrait.Body
                .DOAnchorPosY(breathDistance, breathSeconds)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(Random.Range(0f, breathSeconds))   // stagger so characters do not breathe in sync
                .SetLink(portrait.Body.gameObject);
        }

        private void Hide(VNPortraitCommand command, bool instant)
        {
            if (!staged.TryGetValue(command.character, out var portrait)) return;
            staged.Remove(command.character);
            portrait.Breath?.Kill();

            if (instant || portrait.Root == null)
            {
                if (portrait.Root != null) Destroy(portrait.Root.gameObject);
                return;
            }

            var root = portrait.Root;
            DOTween.Kill(root);

            var sequence = DOTween.Sequence().SetLink(root.gameObject);

            if (exitSettleDistance > 0f && exitSettleDuration > 0f)
                sequence.Append(root.DOAnchorPosY(root.anchoredPosition.y - exitSettleDistance, exitSettleDuration).SetEase(Ease.InOutSine));

            switch (command.transition)
            {
                case PortraitTransition.SlideFromEdge:
                    float to = portrait.Slot <= PortraitSlot.CenterLeft ? -slideDistance : slideDistance;
                    sequence.Append(root.DOAnchorPos(root.anchoredPosition + new Vector2(to, 0f), exitDuration).SetEase(Ease.InCubic));
                    break;

                case PortraitTransition.Pop:
                    sequence.Append(root.DOScale(root.localScale * 0.9f, exitDuration).SetEase(Ease.InBack));
                    break;

                default:
                    sequence.Append(root.DOAnchorPos(root.anchoredPosition - new Vector2(0f, 26f), exitDuration).SetEase(Ease.InCubic));
                    break;
            }

            sequence.Join(portrait.Image.DOFade(0f, exitDuration));
            sequence.OnComplete(() => { if (root != null) Destroy(root.gameObject); });
        }

        private void SetExpression(VNPortraitCommand command, bool instant)
        {
            if (!staged.TryGetValue(command.character, out var portrait)) return;

            var sprite = command.character.GetExpression(command.expression);
            if (sprite == null || sprite == portrait.Image.sprite) return;

            portrait.Expression = command.expression;

            if (!instant)
                GameEvents.RaiseCharacterReaction(command.character, command.expression);

            if (instant || expressionCrossfade <= 0f)
            {
                portrait.Image.sprite = sprite;
                return;
            }

            // Fade the outgoing face out on a clone so the swap does not pop.
            var overlay = Instantiate(portrait.Image, portrait.Image.transform.parent);
            overlay.transform.SetSiblingIndex(portrait.Image.transform.GetSiblingIndex() + 1);
            overlay.gameObject.SetActive(true);
            portrait.Image.sprite = sprite;

            overlay.DOFade(0f, expressionCrossfade)
                .OnComplete(() => { if (overlay != null) Destroy(overlay.gameObject); })
                .SetLink(overlay.gameObject);

            // A small settle sells the change of mood better than a straight crossfade.
            portrait.Root.DOPunchScale(Vector3.one * 0.012f, expressionCrossfade * 2f, 1, 0.4f)
                .SetLink(portrait.Root.gameObject);
        }

        private void Move(VNPortraitCommand command, bool instant)
        {
            if (!staged.TryGetValue(command.character, out var portrait)) return;
            if (portrait.Slot == command.slot && !instant) return;

            portrait.Slot = command.slot;
            var current = portrait.Root.anchoredPosition;
            PlaceAt(portrait, command.character, command.slot);

            if (instant) return;

            var target = portrait.Root.anchoredPosition;
            portrait.Root.anchoredPosition = current;
            portrait.Root.DOAnchorPos(target, moveDuration).SetEase(Ease.InOutCubic).SetLink(portrait.Root.gameObject);
        }

        private void Emote(VNPortraitCommand command, bool instant)
        {
            if (instant || !staged.TryGetValue(command.character, out var portrait)) return;

            var rect = portrait.Root;
            var basePosition = rect.anchoredPosition;

            switch (command.emote)
            {
                case PortraitEmote.Shake:
                    rect.DOShakeAnchorPos(0.4f, new Vector2(emoteStrength, 0f), 18, 90f, false, false)
                        .OnComplete(() => rect.anchoredPosition = basePosition).SetLink(rect.gameObject);
                    break;

                case PortraitEmote.Bounce:
                    rect.DOAnchorPosY(basePosition.y + emoteStrength * 1.5f, 0.18f)
                        .SetEase(Ease.OutQuad).SetLoops(2, LoopType.Yoyo).SetLink(rect.gameObject);
                    break;

                case PortraitEmote.Nod:
                    rect.DOAnchorPosY(basePosition.y - emoteStrength * 0.6f, 0.14f)
                        .SetEase(Ease.InOutQuad).SetLoops(2, LoopType.Yoyo).SetLink(rect.gameObject);
                    break;

                case PortraitEmote.Sway:
                    rect.DOAnchorPosX(basePosition.x + emoteStrength * 0.8f, 0.5f)
                        .SetEase(Ease.InOutSine).SetLoops(2, LoopType.Yoyo).SetLink(rect.gameObject);
                    break;
            }
        }

        /// <summary>The speaker brightens and steps forward; the others darken and settle back.</summary>
        public void SetSpeaking(CharacterDefinition character, bool instant)
        {
            foreach (var pair in staged)
            {
                bool speaking = pair.Key == character;
                var portrait = pair.Value;
                if (portrait.Image == null) continue;

                var tint = speaking ? speakingTint : idleTint;
                var scale = Vector3.one * pair.Key.portraitScale * (speaking ? speakingScale : idleScale);
                float lift = speaking ? speakingLift : 0f;
                var position = SlotPosition(pair.Key, portrait.Slot) + new Vector2(0f, lift);

                if (instant)
                {
                    portrait.Image.color = tint;
                    portrait.Root.localScale = scale;
                    portrait.Root.anchoredPosition = position;
                    continue;
                }

                portrait.Image.DOColor(tint, highlightDuration).SetLink(portrait.Root.gameObject);
                portrait.Root.DOScale(scale, highlightDuration).SetEase(Ease.OutQuad).SetLink(portrait.Root.gameObject);
                portrait.Root.DOAnchorPos(position, highlightDuration).SetEase(Ease.OutQuad).SetLink(portrait.Root.gameObject);
            }
        }

        public void ClearAll(bool instant)
        {
            foreach (var pair in staged)
            {
                var portrait = pair.Value;
                portrait.Breath?.Kill();
                if (portrait.Root == null) continue;

                if (instant)
                {
                    Destroy(portrait.Root.gameObject);
                    continue;
                }

                var root = portrait.Root;
                portrait.Image.DOFade(0f, exitDuration)
                    .OnComplete(() => { if (root != null) Destroy(root.gameObject); })
                    .SetLink(root.gameObject);
            }

            staged.Clear();
        }

        private Vector2 SlotPosition(CharacterDefinition character, PortraitSlot slot)
        {
            return character.portraitOffset + new Vector2(0f, baseOffsetY);
        }

        private void PlaceAt(StagedPortrait portrait, CharacterDefinition character, PortraitSlot slot)
        {
            int index = Mathf.Clamp((int)slot, 0, slotAnchors.Length - 1);
            float anchorX = slotAnchors[index];

            var root = portrait.Root;
            root.anchorMin = new Vector2(anchorX, 0f);
            root.anchorMax = new Vector2(anchorX, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = portraitSize;
            root.anchoredPosition = SlotPosition(character, slot);
            root.localScale = Vector3.one * character.portraitScale;
        }
    }
}
