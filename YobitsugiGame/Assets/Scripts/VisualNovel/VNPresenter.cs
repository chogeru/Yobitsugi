using System;
using System.Collections.Generic;
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

        [Tooltip("Extra pause after a voice clip ends before auto mode advances.")]
        public float voiceTailSeconds = 0.4f;
    }

    /// <summary>
    /// Scenario playback logic. Plain C# (no MonoBehaviour) so it can be unit tested and reused
    /// behind any <see cref="IVNView"/> implementation.
    /// </summary>
    public class VNPresenter : IDisposable
    {
        private readonly IVNView view;
        private readonly IVNPortraitView portraits;
        private readonly VNPacing pacing;

        private VNScene currentScene;
        private int lineIndex;
        private Action onComplete;

        private Tween typeTween;
        private Tween autoTween;
        private bool isTyping;
        private int[] visibleChoiceIndices;

        private bool AwaitingChoice => visibleChoiceIndices != null && visibleChoiceIndices.Length > 0;

        public bool IsPlaying => currentScene != null;
        public bool AutoMode { get; private set; }
        public bool SkipMode { get; private set; }
        public string CurrentSceneId => currentScene != null ? currentScene.SceneId : null;
        public int CurrentLineIndex => lineIndex;

        public VNPresenter(IVNView view, IVNPortraitView portraits, VNPacing pacing)
        {
            this.view = view;
            this.portraits = portraits;
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
            GameEvents.RaiseVoiceRequested(null);
            portraits?.ClearAll(true);
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
            portraits?.ClearAll(true);
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
            RebuildStageState();
            ShowCurrentLine();
        }

        /// <summary>
        /// Replays every stage direction up to the current line without animation, so a loaded save
        /// shows the portraits and background the player had on screen rather than an empty stage.
        /// </summary>
        private void RebuildStageState()
        {
            portraits?.ClearAll(true);

            for (int i = 0; i < lineIndex; i++)
            {
                var line = currentScene.lines[i];

                if (line.background != null) view.SetBackground(line.background);
                if (line.portraits == null) continue;

                foreach (var command in line.portraits)
                    portraits?.Apply(command, true);
            }
        }

        public void Advance()
        {
            if (currentScene == null) return;

            if (isTyping)
            {
                CompleteTyping();
                return;
            }

            if (AwaitingChoice) return;

            KillTweens();
            GoToLine(lineIndex + 1);
        }

        /// <summary><paramref name="visibleIndex"/> indexes the choices as displayed, which may be a filtered subset.</summary>
        public void SelectChoice(int visibleIndex)
        {
            if (currentScene == null || isTyping) return;

            var line = currentScene.lines[lineIndex];
            if (line.choices == null || visibleChoiceIndices == null) return;
            if (visibleIndex < 0 || visibleIndex >= visibleChoiceIndices.Length) return;

            var choice = line.choices[visibleChoiceIndices[visibleIndex]];
            if (!string.IsNullOrEmpty(choice.setFlag))
                StoryFlags.Instance?.Set(choice.setFlag, choice.setFlagValue);

            int next = choice.nextLineIndex >= 0 ? choice.nextLineIndex : lineIndex + 1;

            KillTweens();
            GoToLine(next);
        }

        private static bool HasContent(VNScene scene) => scene != null && scene.lines != null && scene.lines.Length > 0;
        private static bool HasChoices(VNLine line) => line.choices != null && line.choices.Length > 0;

        private void GoToLine(int index)
        {
            // Walk past any lines whose flag conditions exclude them on this playthrough.
            while (index >= 0 && index < currentScene.lines.Length && !PassesConditions(currentScene.lines[index]))
                index++;

            if (index < 0 || index >= currentScene.lines.Length)
            {
                EndScene();
                return;
            }

            lineIndex = index;
            ShowCurrentLine();
        }

        private static bool PassesConditions(VNLine line)
        {
            var flags = StoryFlags.Instance;
            if (flags == null) return true;

            if (!string.IsNullOrEmpty(line.requiredFlag) && !flags.GetBool(line.requiredFlag)) return false;
            if (!string.IsNullOrEmpty(line.forbiddenFlag) && flags.GetBool(line.forbiddenFlag)) return false;

            return true;
        }

        /// <summary>Choices whose required flag is unset are hidden; the mapping keeps indices aligned to the source list.</summary>
        private static VNChoice[] FilterChoices(VNChoice[] choices, out int[] sourceIndices)
        {
            var flags = StoryFlags.Instance;
            var visible = new List<VNChoice>(choices.Length);
            var indices = new List<int>(choices.Length);

            for (int i = 0; i < choices.Length; i++)
            {
                var choice = choices[i];
                if (flags != null && !string.IsNullOrEmpty(choice.requiredFlag) && !flags.GetBool(choice.requiredFlag))
                    continue;

                visible.Add(choice);
                indices.Add(i);
            }

            sourceIndices = indices.ToArray();
            return visible.ToArray();
        }

        private void ShowCurrentLine()
        {
            var line = currentScene.lines[lineIndex];

            if (!string.IsNullOrEmpty(line.setFlag))
                StoryFlags.Instance?.Set(line.setFlag, line.setFlagValue);

            visibleChoiceIndices = null;
            view.HideChoices();
            view.SetNextIndicatorVisible(false);
            view.SetSpeaker(line.SpeakerName, line.SpeakerColor);
            view.SetBackground(line.background);

            if (line.portraits != null)
            {
                foreach (var command in line.portraits)
                    portraits?.Apply(command, false);
            }
            portraits?.SetSpeaking(line.character, false);

            GameEvents.RaiseVNLineShown(line.SpeakerName, line.text);

            // Skip mode races past lines, so voice would only ever be cut off mid-word.
            GameEvents.RaiseVoiceRequested(SkipMode ? null : line.voice);

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
                var visible = FilterChoices(line.choices, out visibleChoiceIndices);
                if (visible.Length > 0)
                {
                    view.ShowChoices(visible);
                    return;
                }

                // Every option was filtered out: fall through so the scene cannot dead-end.
                visibleChoiceIndices = null;
            }

            view.SetNextIndicatorVisible(true);
            RefreshPendingAdvance();
        }

        /// <summary>(Re)schedules the auto/skip timer for the line currently on screen.</summary>
        private void RefreshPendingAdvance()
        {
            autoTween?.Kill();
            autoTween = null;

            if (currentScene == null || isTyping || AwaitingChoice) return;

            if (AutoMode)
                autoTween = DOVirtual.DelayedCall(AutoDelayFor(currentScene.lines[lineIndex]), Advance);
            else if (SkipMode)
                autoTween = DOVirtual.DelayedCall(pacing.skipAdvanceDelay, Advance);
        }

        /// <summary>A voiced line holds until the take finishes, so auto mode never talks over itself.</summary>
        private float AutoDelayFor(VNLine line)
        {
            if (line.voice == null) return pacing.autoAdvanceDelay;
            return Mathf.Max(pacing.autoAdvanceDelay, line.voice.length + pacing.voiceTailSeconds);
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
