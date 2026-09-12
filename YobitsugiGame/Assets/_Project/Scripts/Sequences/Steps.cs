using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Yobitsugi.VisualNovel;

namespace Yobitsugi.Sequences
{
    [Serializable]
    public class PlayVNSceneStep : SequenceStep
    {
        [SerializeField] private VNScene scene;

        public override UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken)
        {
            return scene == null ? UniTask.CompletedTask : context.Mode.PlayVNAsync(scene, cancellationToken);
        }
    }

    [Serializable]
    public class WaitStep : SequenceStep
    {
        [SerializeField] private float seconds = 1f;

        public override UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken)
        {
            return UniTask.Delay(TimeSpan.FromSeconds(seconds), DelayType.DeltaTime, PlayerLoopTiming.Update, cancellationToken);
        }
    }

    [Serializable]
    public class FadeStep : SequenceStep
    {
        [SerializeField] private bool toBlack = true;
        [SerializeField] private float duration = 0.35f;

        public override UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken)
        {
            if (context.Fader == null) return UniTask.CompletedTask;

            return toBlack
                ? context.Fader.FadeOutAsync(duration, cancellationToken)
                : context.Fader.FadeInAsync(duration, cancellationToken);
        }
    }

    [Serializable]
    public class SetActiveStep : SequenceStep
    {
        [SerializeField] private GameObject target;
        [SerializeField] private bool active = true;

        public override UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken)
        {
            if (target != null) target.SetActive(active);
            return UniTask.CompletedTask;
        }
    }

    [Serializable]
    public class TeleportStep : SequenceStep
    {
        [SerializeField] private Transform target;
        [SerializeField] private Transform destination;

        public override UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken)
        {
            if (target == null || destination == null) return UniTask.CompletedTask;

            // CharacterController overrides direct transform writes, so disable it across the teleport.
            var controller = target.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;

            target.SetPositionAndRotation(destination.position, destination.rotation);

            if (controller != null) controller.enabled = true;
            return UniTask.CompletedTask;
        }
    }

    [Serializable]
    public class ScreenShakeStep : SequenceStep
    {
        [SerializeField] private float duration = 0.4f;
        [SerializeField, Range(0f, 100f)] private float strength = 30f;

        public override UniTask ExecuteAsync(SequenceContext context, CancellationToken cancellationToken)
        {
            Yobitsugi.Core.GameEvents.RaiseScreenShakeRequested(duration, strength);
            return UniTask.CompletedTask;
        }
    }
}
