using DG.Tweening;
using UnityEngine;
using Yobitsugi.Core;
using Yobitsugi.VisualNovel;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Yobitsugi.Audio
{
    /// <summary>
    /// Sound driven entirely by <see cref="GameEvents"/>: no other system references audio,
    /// so clips can be added or changed without touching gameplay code.
    /// </summary>
    public class AudioService : MonoBehaviour
    {
#if ODIN_INSPECTOR
        [Title("サウンド", "ゲーム内イベントを購読して鳴らすだけの独立モジュール", TitleAlignments.Left)]
        [InfoBox("他のシステムはサウンドのことを一切知りません。GameEvents を購読しているだけなので、\n" +
                 "クリップを差し替えても・このオブジェクトを消しても、ゲーム進行には影響しません。\n" +
                 "反応するイベント: モード切替(BGM/環境音) / 台詞表示 / 手がかり取得 / セーブ完了")]
        [BoxGroup("再生元")]
#endif
        [Header("Sources")]
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource ambienceSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Music")]
        [SerializeField] private AudioClip vnMusic;
        [SerializeField] private AudioClip explorationAmbience;
        [SerializeField] private float musicFade = 1.2f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.4f;

        [Header("SFX")]
        [SerializeField] private AudioClip lineAdvanceClip;
        [SerializeField] private AudioClip clueClip;
        [SerializeField] private AudioClip saveClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;

        [Header("Voice")]
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;
        [Tooltip("Duck music and ambience by this factor while a line is voiced.")]
        [SerializeField, Range(0f, 1f)] private float voiceDucking = 0.5f;

        private void OnEnable()
        {
            GameEvents.OnModeChanged += HandleModeChanged;
            GameEvents.OnVNLineShown += HandleLineShown;
            GameEvents.OnVoiceRequested += HandleVoiceRequested;
            GameEvents.OnClueCollected += HandleClueCollected;
            GameEvents.OnSaveCompleted += HandleSaveCompleted;
        }

        private void OnDisable()
        {
            GameEvents.OnModeChanged -= HandleModeChanged;
            GameEvents.OnVNLineShown -= HandleLineShown;
            GameEvents.OnVoiceRequested -= HandleVoiceRequested;
            GameEvents.OnClueCollected -= HandleClueCollected;
            GameEvents.OnSaveCompleted -= HandleSaveCompleted;
        }

        /// <summary>Plays a line's voice, cutting the previous take. A null clip just stops playback.</summary>
        private void HandleVoiceRequested(AudioClip clip)
        {
            if (voiceSource == null) return;

            voiceSource.Stop();
            voiceSource.clip = clip;

            if (clip != null)
            {
                voiceSource.volume = voiceVolume;
                voiceSource.Play();
            }

            ApplyDucking(clip != null);
        }

        private void ApplyDucking(bool voicePlaying)
        {
            float factor = voicePlaying ? voiceDucking : 1f;

            DuckSource(musicSource, musicVolume * factor);
            DuckSource(ambienceSource, ambienceVolume * factor);
        }

        private void DuckSource(AudioSource source, float target)
        {
            if (source == null || !source.isPlaying) return;

            DOTween.Kill(source, complete: false);
            source.DOFade(target, 0.25f).SetTarget(source).SetLink(gameObject);
        }

        private void HandleModeChanged(bool isInVN)
        {
            CrossfadeTo(musicSource, isInVN ? vnMusic : null, musicVolume);
            CrossfadeTo(ambienceSource, isInVN ? null : explorationAmbience, ambienceVolume);
        }

        private void HandleLineShown(string speaker, string text) => PlaySfx(lineAdvanceClip);
        private void HandleClueCollected(string clueId) => PlaySfx(clueClip);
        private void HandleSaveCompleted(int slot) => PlaySfx(saveClip);

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private void CrossfadeTo(AudioSource source, AudioClip clip, float targetVolume)
        {
            if (source == null) return;
            if (source.clip == clip && source.isPlaying) return;

            DOTween.Kill(source);

            if (clip == null)
            {
                source.DOFade(0f, musicFade).OnComplete(source.Stop).SetTarget(source).SetLink(gameObject);
                return;
            }

            var sequence = DOTween.Sequence().SetTarget(source).SetLink(gameObject);
            if (source.isPlaying)
                sequence.Append(source.DOFade(0f, musicFade * 0.5f));

            sequence.AppendCallback(() =>
            {
                source.clip = clip;
                source.loop = true;
                source.volume = 0f;
                source.Play();
            });
            sequence.Append(source.DOFade(targetVolume, musicFade));
        }
    }
}
