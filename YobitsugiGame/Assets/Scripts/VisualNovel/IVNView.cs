using System;
using DG.Tweening;
using UnityEngine;

namespace Yobitsugi.VisualNovel
{
    /// <summary>
    /// MVP view contract for the dialogue screen: renders what it is told and reports player intent.
    /// It holds no scenario logic, so <see cref="VNPresenter"/> stays testable without a scene.
    /// </summary>
    public interface IVNView
    {
        event Action AdvanceRequested;
        event Action<int> ChoiceSelected;

        void SetVisible(bool visible);
        void SetSpeaker(string speaker, Color color);
        void SetBackground(Sprite sprite);
        void SetDialogueText(string text);

        /// <summary>Starts the character-by-character reveal; the presenter may complete it early to skip ahead.</summary>
        Tween TypeDialogue(string text, float duration);

        void SetNextIndicatorVisible(bool visible);
        void ShowChoices(VNChoice[] choices);
        void HideChoices();
    }
}
