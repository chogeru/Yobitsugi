using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
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
            public Image Image;
            public RectTransform Rect;
            public PortraitSlot Slot;
            public string Expression;
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
        [Tooltip("Normalised horizontal positions for Left … Right slots.")]
        [SerializeField] private float[] slotAnchors = { 0.16f, 0.32f, 0.5f, 0.68f, 0.84f };
        [SerializeField] private Vector2 portraitSize = new Vector2(700f, 1000f);

        [Header("Animation")]
        [SerializeField] private float enterDuration = 0.35f;
        [SerializeField] private float exitDuration = 0.25f;
        [SerializeField] private float moveDuration = 0.3f;
        [SerializeField] private float expressionCrossfade = 0.15f;
        [SerializeField] private float slideDistance = 160f;
        [SerializeField] private float emoteStrength = 24f;

        [Header("Speaker highlight")]
        [SerializeField] private Color speakingTint = Color.white;
        [SerializeField] private Color idleTint = new Color(0.55f, 0.55f, 0.62f, 1f);
        [SerializeField] private float speakingScale = 1f;
        [SerializeField] private float idleScale = 0.97f;
        [SerializeField] private float highlightDuration = 0.25f;

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

            var image = Instantiate(portraitTemplate, stage);
            image.gameObject.SetActive(true);
            image.sprite = command.character.GetExpression(command.expression);
            image.preserveAspect = true;
            image.raycastTarget = false;

            var rect = (RectTransform)image.transform;
            rect.sizeDelta = portraitSize;

            var portrait = new StagedPortrait
            {
                Image = image,
                Rect = rect,
                Slot = command.slot,
                Expression = command.expression,
            };
            staged[command.character] = portrait;

            PlaceAt(portrait, command.character, command.slot);
            var target = rect.anchoredPosition;

            if (instant)
            {
                image.color = idleTint;
                rect.localScale = Vector3.one * command.character.portraitScale * idleScale;
                return;
            }

            DOTween.Kill(rect);
            switch (command.transition)
            {
                case PortraitTransition.SlideFromEdge:
                    float from = command.slot <= PortraitSlot.CenterLeft ? -slideDistance : slideDistance;
                    rect.anchoredPosition = target + new Vector2(from, 0f);
                    rect.DOAnchorPos(target, enterDuration).SetEase(Ease.OutCubic).SetLink(image.gameObject);
                    break;

                case PortraitTransition.Pop:
                    rect.localScale = Vector3.one * command.character.portraitScale * 0.85f;
                    rect.DOScale(Vector3.one * command.character.portraitScale, enterDuration)
                        .SetEase(Ease.OutBack).SetLink(image.gameObject);
                    break;
            }

            var color = idleTint;
            color.a = 0f;
            image.color = color;
            image.DOFade(idleTint.a, enterDuration).SetLink(image.gameObject);
        }

        private void Hide(VNPortraitCommand command, bool instant)
        {
            if (!staged.TryGetValue(command.character, out var portrait)) return;
            staged.Remove(command.character);

            if (instant || portrait.Image == null)
            {
                if (portrait.Image != null) Destroy(portrait.Image.gameObject);
                return;
            }

            var rect = portrait.Rect;
            DOTween.Kill(rect);

            var sequence = DOTween.Sequence().SetLink(portrait.Image.gameObject);
            if (command.transition == PortraitTransition.SlideFromEdge)
            {
                float to = portrait.Slot <= PortraitSlot.CenterLeft ? -slideDistance : slideDistance;
                sequence.Join(rect.DOAnchorPos(rect.anchoredPosition + new Vector2(to, 0f), exitDuration).SetEase(Ease.InCubic));
            }
            else if (command.transition == PortraitTransition.Pop)
            {
                sequence.Join(rect.DOScale(rect.localScale * 0.85f, exitDuration).SetEase(Ease.InBack));
            }

            var image = portrait.Image;
            sequence.Join(image.DOFade(0f, exitDuration));
            sequence.OnComplete(() => { if (image != null) Destroy(image.gameObject); });
        }

        private void SetExpression(VNPortraitCommand command, bool instant)
        {
            if (!staged.TryGetValue(command.character, out var portrait)) return;

            var sprite = command.character.GetExpression(command.expression);
            if (sprite == null || sprite == portrait.Image.sprite) return;

            portrait.Expression = command.expression;

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
        }

        private void Move(VNPortraitCommand command, bool instant)
        {
            if (!staged.TryGetValue(command.character, out var portrait)) return;
            if (portrait.Slot == command.slot && !instant) return;

            portrait.Slot = command.slot;
            var current = portrait.Rect.anchoredPosition;
            PlaceAt(portrait, command.character, command.slot);

            if (instant) return;

            var target = portrait.Rect.anchoredPosition;
            portrait.Rect.anchoredPosition = current;
            portrait.Rect.DOAnchorPos(target, moveDuration).SetEase(Ease.InOutCubic).SetLink(portrait.Image.gameObject);
        }

        private void Emote(VNPortraitCommand command, bool instant)
        {
            if (instant || !staged.TryGetValue(command.character, out var portrait)) return;

            var rect = portrait.Rect;
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

        public void SetSpeaking(CharacterDefinition character, bool instant)
        {
            foreach (var pair in staged)
            {
                bool speaking = pair.Key == character;
                var portrait = pair.Value;
                if (portrait.Image == null) continue;

                var tint = speaking ? speakingTint : idleTint;
                var scale = Vector3.one * pair.Key.portraitScale * (speaking ? speakingScale : idleScale);

                if (instant)
                {
                    portrait.Image.color = tint;
                    portrait.Rect.localScale = scale;
                    continue;
                }

                portrait.Image.DOColor(tint, highlightDuration).SetLink(portrait.Image.gameObject);
                portrait.Rect.DOScale(scale, highlightDuration).SetEase(Ease.OutQuad).SetLink(portrait.Image.gameObject);
            }
        }

        public void ClearAll(bool instant)
        {
            foreach (var pair in staged)
            {
                if (pair.Value.Image == null) continue;

                if (instant)
                {
                    Destroy(pair.Value.Image.gameObject);
                    continue;
                }

                var image = pair.Value.Image;
                image.DOFade(0f, exitDuration)
                    .OnComplete(() => { if (image != null) Destroy(image.gameObject); })
                    .SetLink(image.gameObject);
            }

            staged.Clear();
        }

        private void PlaceAt(StagedPortrait portrait, CharacterDefinition character, PortraitSlot slot)
        {
            int index = Mathf.Clamp((int)slot, 0, slotAnchors.Length - 1);
            float anchorX = slotAnchors[index];

            var rect = portrait.Rect;
            rect.anchorMin = new Vector2(anchorX, 0f);
            rect.anchorMax = new Vector2(anchorX, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = character.portraitOffset;
            rect.localScale = Vector3.one * character.portraitScale;
        }
    }
}
