using System;
using DG.Tweening;
using UnityEngine;
using Yobitsugi.Core;

namespace Yobitsugi.VisualNovel
{
    [Serializable]
    public class VNPacing
    {
        public float charInterval = 0.03f;
        public float autoAdvanceDelay = 1.2f;
        public float skipAdvanceDelay = 0.05f;
    }

    /// <summary>
    /// Scenario playback logic. Plain C# (no MonoBehaviour) so it can be unit tested and reused
    /// behind any <see cref="IVNView"/> implementation.
    /// </summary>
    public class VNPresenter : IDisposable
    {
        private readonly IVNView view;
        private readonly VNPacing pacing;

        private VNScene currentScene;
        private int lineIndex;
        private Action onComplete;

        private Tween typeTween;
        private Tween autoTween;
        private bool isTyping;

        public bool IsPlaying => currentScene != null;
        public bool AutoMode { get; private set; }
        public bool SkipMode { get; private set; }
        public string CurrentSceneId => currentScene != null ? currentScene.SceneId : null;
        public int CurrentLineIndex => lineIndex;

        public VNPresenter(IVNView view, VNPacing pacing)
        {
            this.view = view;
            this.pacing = pacing ?? new VNPacing();

            view.AdvanceRequested += Advance;
            view.ChoiceSelected += SelectChoice;
            view.SetVisible(false);
        }

        public void Dispose()
        {
            view.AdvanceRequested -= Advance;
            view.ChoiceSelected -= SelectChoice;
            KillTweens();
        }

        public void HideUI()
        {
            KillTweens();
            view.SetVisible(false);
        }

        public void SetAuto(bool value)
        {
            AutoMode = value;
            if (value) SkipMode = false;
            RefreshPendingAdvance();
        }

        public void SetSkip(bool value)
        {
            SkipMode = value;
            if (value) AutoMode = false;

            // Turning skip on mid-line should finish that line immediately rather than waiting out the typewriter.
            if (value && isTyping)
            {
                CompleteTyping();
                return;
            }

            RefreshPendingAdvance();
        }

        public void StartScene(VNScene scene, Action completeCallback)
        {
            if (!HasContent(scene))
            {
                completeCallback?.Invoke();
                return;
            }

            KillTweens();
            currentScene = scene;
            lineIndex = 0;
            onComplete = completeCallback;
            view.SetVisible(true);
            GameEvents.RaiseVNSceneStarted(scene);
            ShowCurrentLine();
        }

        /// <summary>Re-enters a scene at a specific line, used when restoring a save.</summary>
        public void ResumeScene(VNScene scene, int atLineIndex, Action completeCallback)
        {
            if (!HasContent(scene))
            {
                completeCallback?.Invoke();
                return;
            }

            KillTweens();
            currentScene = scene;
            lineIndex = Mathf.Clamp(atLineIndex, 0, scene.lines.Length - 1);
            onComplete = completeCallback;
            view.SetVisible(true);
            ShowCurrentLine();
        }

        public void Advance()
        {
            if (currentScene == null) return;

            if (isTyping)
            {
                CompleteTyping();
                return;
            }

            if (HasChoices(currentScene.lines[lineIndex])) return;

            KillTweens();
            GoToLine(lineIndex + 1);
        }

        public void SelectChoice(int choiceIndex)
        {
            if (currentScene == null || isTyping) return;

            var line = currentScene.lines[lineIndex];
            if (line.choices == null || choiceIndex < 0 || choiceIndex >= line.choices.Length) return;

            var choice = line.choices[choiceIndex];
            int next = choice.nextLineIndex >= 0 ? choice.nextLineIndex : lineIndex + 1;

            KillTweens();
            GoToLine(next);
        }

        private static bool HasContent(VNScene scene) => scene != null && scene.lines != null && scene.lines.Length > 0;
        private static bool HasChoices(VNLine line) => line.choices != null && line.choices.Length > 0;

        private void GoToLine(int index)
        {
            if (index < 0 || index >= currentScene.lines.Length)
            {
                EndScene();
                return;
            }

            lineIndex = index;
            ShowCurrentLine();
        }

        private void ShowCurrentLine()
        {
            var line = currentScene.lines[lineIndex];

            view.HideChoices();
            view.SetNextIndicatorVisible(false);
            view.SetSpeaker(line.speaker);
            view.SetBackground(line.background);
            GameEvents.RaiseVNLineShown(line.speaker, line.text);

            KillTweens();

            string text = line.text ?? string.Empty;
            if (SkipMode || pacing.charInterval <= 0f || text.Length == 0)
            {
                view.SetDialogueText(text);
                OnLineFullyShown(line);
                return;
            }

            isTyping = true;
            typeTween = view.TypeDialogue(text, text.Length * pacing.charInterval)
                .OnComplete(() =>
                {
                    typeTween = null;
                    isTyping = false;
                    OnLineFullyShown(line);
                });
        }

        /// <summary>Snaps the typewriter to the full line; the tween's completion callback carries on from there.</summary>
        private void CompleteTyping()
        {
            if (typeTween != null && typeTween.IsActive())
                typeTween.Complete();
        }

        private void OnLineFullyShown(VNLine line)
        {
            if (HasChoices(line))
            {
                view.ShowChoices(line.choices);
                return;
            }

            view.SetNextIndicatorVisible(true);
            RefreshPendingAdvance();
        }

        /// <summary>(Re)schedules the auto/skip timer for the line currently on screen.</summary>
        private void RefreshPendingAdvance()
        {
            autoTween?.Kill();
            autoTween = null;

            if (currentScene == null || isTyping) return;
            if (HasChoices(currentScene.lines[lineIndex])) return;

            if (AutoMode)
                autoTween = DOVirtual.DelayedCall(pacing.autoAdvanceDelay, Advance);
            else if (SkipMode)
                autoTween = DOVirtual.DelayedCall(pacing.skipAdvanceDelay, Advance);
        }

        private void KillTweens()
        {
            typeTween?.Kill();
            typeTween = null;
            autoTween?.Kill();
            autoTween = null;
            isTyping = false;
        }

        private void EndScene()
        {
            var finishedScene = currentScene;
            GameEvents.RaiseVNSceneEnded(finishedScene);

            if (finishedScene.nextScene != null)
            {
                StartScene(finishedScene.nextScene, onComplete);
                return;
            }

            // The view stays up until the mode transition hides it mid-fade, so exploration never flashes behind it.
            currentScene = null;

            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
