using System;
using UnityEngine;

namespace Yobitsugi.VisualNovel
{
    /// <summary>Scene-side composition root: binds the view to <see cref="VNPresenter"/> and exposes it to other systems.</summary>
    public class VNManager : MonoBehaviour
    {
        public static VNManager Instance { get; private set; }

        [Tooltip("The dialogue view component (VNUI by default).")]
        [SerializeField] private VNUI view;
        [SerializeField] private VNPortraitView portraitView;
        [SerializeField] private VNPacing pacing = new VNPacing();

        private VNPresenter presenter;

        public VNPresenter Presenter => presenter;

        public bool IsPlaying => presenter.IsPlaying;
        public bool AutoMode => presenter.AutoMode;
        public bool SkipMode => presenter.SkipMode;
        public string CurrentSceneId => presenter.CurrentSceneId;
        public int CurrentLineIndex => presenter.CurrentLineIndex;

        private void Awake()
        {
            Instance = this;
            presenter = new VNPresenter(view, portraitView, pacing);
        }

        private void OnDestroy()
        {
            presenter?.Dispose();
            if (Instance == this) Instance = null;
        }

        public void StartScene(VNScene scene, Action completeCallback) => presenter.StartScene(scene, completeCallback);
        public void ResumeScene(VNScene scene, int atLineIndex, Action completeCallback) => presenter.ResumeScene(scene, atLineIndex, completeCallback);
        public void Advance() => presenter.Advance();
        public void SelectChoice(int index) => presenter.SelectChoice(index);
        public void SetAuto(bool value) => presenter.SetAuto(value);
        public void SetSkip(bool value) => presenter.SetSkip(value);
        public void HideUI() => presenter.HideUI();
    }
}
