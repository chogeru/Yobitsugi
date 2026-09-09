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
        public static AudioService Instance { get; private set; }

        private const string PrefMusicVolume = "audio.music_volume";
        private const string PrefSfxVolume = "audio.sfx_volume";
        private const string PrefVoiceVolume = "audio.voice_volume";

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
        [Tooltip("Design-time mix baseline; the player's own BGM slider multiplies on top of this.")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.4f;

        [Header("SFX")]
        [SerializeField] private AudioClip lineAdvanceClip;
        [SerializeField] private AudioClip clueClip;
        [SerializeField] private AudioClip saveClip;
        [Tooltip("Played once when GameManager.ClearGame fires; the BGM fades out to let it land.")]
        [SerializeField] private AudioClip clearFanfareClip;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.7f;

        [Header("Typing SFX")]
        [Tooltip("Clips played (one at random) per non-whitespace glyph revealed by the typewriter. Pick a set from Assets/DialogTextSFX or DialogTextVolumeII.")]
        [SerializeField] private AudioClip[] typingClips;
        [SerializeField, Range(0f, 1f)] private float typingVolume = 0.5f;
        [Tooltip("Minimum time between typing blips, so a fast typewriter does not turn into a buzz.")]
        [SerializeField] private float typingMinInterval = 0.045f;
        private float lastTypingPlayTime = -999f;

        [Header("Character Reactions")]
        [Tooltip("Random Boy/Girl reaction bark played when a character's expression changes on stage.")]
        [SerializeField, Range(0f, 1f)] private float reactionVolume = 0.85f;

        [Header("Voice")]
        [SerializeField, Range(0f, 1f)] private float voiceVolume = 1f;
        [Tooltip("Duck music and ambience by this factor while a line is voiced.")]
        [SerializeField, Range(0f, 1f)] private float voiceDucking = 0.5f;

        [Header("User Volume (persisted across sessions)")]
        [SerializeField, Range(0f, 1f)] private float userMusicVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float userSfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float userVoiceVolume = 1f;

        public float UserMusicVolume => userMusicVolume;
        public float UserSfxVolume => userSfxVolume;
        public float UserVoiceVolume => userVoiceVolume;

        private float EffectiveMusicVolume => musicVolume * userMusicVolume;
        private float EffectiveAmbienceVolume => ambienceVolume * userMusicVolume;
        private float EffectiveSfxVolume => sfxVolume * userSfxVolume;
        private float EffectiveTypingVolume => typingVolume * userSfxVolume;
        private float EffectiveReactionVolume => reactionVolume * userSfxVolume;
        private float EffectiveVoiceVolume => voiceVolume * userVoiceVolume;

        private void Awake()
        {
            Instance = this;

            userMusicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, userMusicVolume);
            userSfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, userSfxVolume);
            userVoiceVolume = PlayerPrefs.GetFloat(PrefVoiceVolume, userVoiceVolume);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            GameEvents.OnModeChanged += HandleModeChanged;
            GameEvents.OnVNSceneStarted += HandleSceneStarted;
            GameEvents.OnVNLineShown += HandleLineShown;
            GameEvents.OnVoiceRequested += HandleVoiceRequested;
            GameEvents.OnClueCollected += HandleClueCollected;
            GameEvents.OnSaveCompleted += HandleSaveCompleted;
            GameEvents.OnDialogueCharacterRevealed += HandleDialogueCharacterRevealed;
            GameEvents.OnCharacterReaction += HandleCharacterReaction;
            GameEvents.OnGameCleared += HandleGameCleared;
        }

        private void OnDisable()
        {
            GameEvents.OnModeChanged -= HandleModeChanged;
            GameEvents.OnVNSceneStarted -= HandleSceneStarted;
            GameEvents.OnVNLineShown -= HandleLineShown;
            GameEvents.OnVoiceRequested -= HandleVoiceRequested;
            GameEvents.OnClueCollected -= HandleClueCollected;
            GameEvents.OnSaveCompleted -= HandleSaveCompleted;
            GameEvents.OnDialogueCharacterRevealed -= HandleDialogueCharacterRevealed;
            GameEvents.OnCharacterReaction -= HandleCharacterReaction;
            GameEvents.OnGameCleared -= HandleGameCleared;
        }

        // --- User volume (system menu sliders) ---

        public void SetUserMusicVolume(float value)
        {
            userMusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefMusicVolume, userMusicVolume);

            ApplyDucking(voiceSource != null && voiceSource.isPlaying);
        }

        public void SetUserSfxVolume(float value)
        {
            userSfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefSfxVolume, userSfxVolume);
        }

        public void SetUserVoiceVolume(float value)
        {
            userVoiceVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(PrefVoiceVolume, userVoiceVolume);

            if (voiceSource != null && voiceSource.isPlaying)
                voiceSource.volume = EffectiveVoiceVolume;
        }

        /// <summary>Plays a line's voice, cutting the previous take. A null clip just stops playback.</summary>
        private void HandleVoiceRequested(AudioClip clip)
        {
            if (voiceSource == null) return;

            voiceSource.Stop();
            voiceSource.clip = clip;

            if (clip != null)
            {
                voiceSource.volume = EffectiveVoiceVolume;
                voiceSource.Play();
            }

            ApplyDucking(clip != null);
        }

        private void ApplyDucking(bool voicePlaying)
        {
            float factor = voicePlaying ? voiceDucking : 1f;

            DuckSource(musicSource, EffectiveMusicVolume * factor);
            DuckSource(ambienceSource, EffectiveAmbienceVolume * factor);
        }

        private void DuckSource(AudioSource source, float target)
        {
            if (source == null || !source.isPlaying) return;

            DOTween.Kill(source, complete: false);
            source.DOFade(target, 0.25f).SetTarget(source).SetLink(gameObject);
        }

        private void HandleModeChanged(bool isInVN)
        {
            // Entering VN: HandleSceneStarted picks the track (per-scene override or the vnMusic default).
            if (!isInVN) CrossfadeTo(musicSource, null, EffectiveMusicVolume);
            CrossfadeTo(ambienceSource, isInVN ? null : explorationAmbience, EffectiveAmbienceVolume);
        }

        /// <summary>Each VN scene may override the default track, so chapters can carry their own BGM.</summary>
        private void HandleSceneStarted(VNScene scene)
        {
            var clip = scene != null && scene.music != null ? scene.music : vnMusic;
            CrossfadeTo(musicSource, clip, EffectiveMusicVolume);
        }

        private void HandleLineShown(string speaker, string text, AudioClip voice) => PlaySfx(lineAdvanceClip);
        private void HandleClueCollected(string clueId) => PlaySfx(clueClip);
        private void HandleSaveCompleted(int slot) => PlaySfx(saveClip);

        /// <summary>The moment deserves quiet: fade the BGM out from under the fanfare instead of layering them.</summary>
        private void HandleGameCleared()
        {
            CrossfadeTo(musicSource, null, EffectiveMusicVolume);
            PlaySfx(clearFanfareClip);
        }

        private void HandleDialogueCharacterRevealed()
        {
            if (typingClips == null || typingClips.Length == 0 || sfxSource == null) return;
            if (Time.unscaledTime - lastTypingPlayTime < typingMinInterval) return;

            lastTypingPlayTime = Time.unscaledTime;
            sfxSource.PlayOneShot(typingClips[Random.Range(0, typingClips.Length)], EffectiveTypingVolume);
        }

        private void HandleCharacterReaction(CharacterDefinition character, string expressionKey)
        {
            if (character == null || sfxSource == null) return;

            var clip = VNReactionLibrary.FindReaction(character.reactionVoice, expressionKey);
            if (clip != null) sfxSource.PlayOneShot(clip, EffectiveReactionVolume);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.PlayOneShot(clip, EffectiveSfxVolume);
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
