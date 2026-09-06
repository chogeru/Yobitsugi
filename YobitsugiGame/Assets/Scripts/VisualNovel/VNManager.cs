using System;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.VisualNovel
{
    /// <summary>Scene-side composition root: binds the view to <see cref="VNPresenter"/> and exposes it to other systems.</summary>
    public class VNManager : MonoBehaviour
    {
        public static VNManager Instance { get; private set; }

#if ODIN_INSPECTOR
        [Title("ノベル進行管理", "台詞送り・選択肢・オート/スキップの入口", TitleAlignments.Left)]
        [InfoBox("実際の進行ロジックは MonoBehaviour ではない VNPresenter が持っています。\n" +
                 "このコンポーネントは「シーン上の部品(View)と Presenter を繋ぐ」だけの役割です。")]
        [Required("台詞を表示する View が必要です"), BoxGroup("参照"), LabelText("台詞ビュー")]
#endif
        [Tooltip("The dialogue view component (VNUI by default).")]
        [SerializeField] private VNUI view;

#if ODIN_INSPECTOR
        [BoxGroup("参照"), LabelText("立ち絵ビュー")]
        [InfoBox("未設定でも動作しますが、立ち絵は表示されません。", InfoMessageType.None)]
#endif
        [SerializeField] private VNPortraitView portraitView;

#if ODIN_INSPECTOR
        [BoxGroup("進行速度"), HideLabel]
        [InfoBox("文字送り間隔・オート待機時間・スキップ間隔(いずれも秒)")]
#endif
        [SerializeField] private VNPacing pacing = new VNPacing();

        private VNPresenter presenter;

        public VNPresenter Presenter => presenter;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, BoxGroup("実行時の状態"), LabelText("再生中")]
#endif
        public bool IsPlaying => presenter != null && presenter.IsPlaying;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, BoxGroup("実行時の状態"), LabelText("オート")]
#endif
        public bool AutoMode => presenter != null && presenter.AutoMode;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, BoxGroup("実行時の状態"), LabelText("スキップ")]
#endif
        public bool SkipMode => presenter != null && presenter.SkipMode;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, BoxGroup("実行時の状態"), LabelText("再生中シーンID")]
#endif
        public string CurrentSceneId => presenter?.CurrentSceneId;

#if ODIN_INSPECTOR
        [ShowInInspector, ReadOnly, BoxGroup("実行時の状態"), LabelText("現在行")]
#endif
        public int CurrentLineIndex => presenter?.CurrentLineIndex ?? 0;

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
