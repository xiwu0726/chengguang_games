using UnityEngine;
using System.Collections.Generic;

namespace TimeStamp
{
    /// <summary>
    /// 情感音乐管理器
    /// 负责管理不同情感类型的背景音乐
    /// </summary>
    public class EmotionalMusicManager : MonoBehaviour
    {
        public static EmotionalMusicManager Instance { get; private set; }

        [Header("情感音乐")]
        [SerializeField] private AudioSource emotionalSource;
        [SerializeField] private AudioClip joyMusic;        // 喜悦
        [SerializeField] private AudioClip sadnessMusic;    // 悲伤
        [SerializeField] private AudioClip tensionMusic;    // 紧张
        [SerializeField] private AudioClip nostalgiaMusic;  // 怀旧
        [SerializeField] private AudioClip hopeMusic;       // 希望
        [SerializeField] private AudioClip fearMusic;       // 恐惧

        [Header("音量设置")]
        [SerializeField] private float emotionalVolume = 0.6f;
        [SerializeField] private float fadeDuration = 3f;

        private bool isEmotionalMusicPlaying = false;
        private EmotionalState currentEmotionalState = EmotionalState.None;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSource();
        }

        private void InitializeAudioSource()
        {
            if (emotionalSource == null)
            {
                emotionalSource = gameObject.AddComponent<AudioSource>();
                emotionalSource.loop = true;
                emotionalSource.playOnAwake = false;
            }
        }

        #region Emotional Music Control

        /// <summary>
        /// 播放喜悦音乐
        /// </summary>
        public void PlayJoyMusic()
        {
            PlayEmotionalMusic(EmotionalState.Joy, joyMusic);
        }

        /// <summary>
        /// 播放悲伤音乐
        /// </summary>
        public void PlaySadnessMusic()
        {
            PlayEmotionalMusic(EmotionalState.Sadness, sadnessMusic);
        }

        /// <summary>
        /// 播放紧张音乐
        /// </summary>
        public void PlayTensionMusic()
        {
            PlayEmotionalMusic(EmotionalState.Tension, tensionMusic);
        }

        /// <summary>
        /// 播放怀旧音乐
        /// </summary>
        public void PlayNostalgiaMusic()
        {
            PlayEmotionalMusic(EmotionalState.Nostalgia, nostalgiaMusic);
        }

        /// <summary>
        /// 播放希望音乐
        /// </summary>
        public void PlayHopeMusic()
        {
            PlayEmotionalMusic(EmotionalState.Hope, hopeMusic);
        }

        /// <summary>
        /// 播放恐惧音乐
        /// </summary>
        public void PlayFearMusic()
        {
            PlayEmotionalMusic(EmotionalState.Fear, fearMusic);
        }

        /// <summary>
        /// 停止情感音乐
        /// </summary>
        public void StopEmotionalMusic()
        {
            if (emotionalSource != null && isEmotionalMusicPlaying)
            {
                StartCoroutine(FadeOutRoutine(emotionalSource, fadeDuration, () =>
                {
                    isEmotionalMusicPlaying = false;
                    currentEmotionalState = EmotionalState.None;
                    Debug.Log("[情感音乐] 已停止");
                }));
            }
        }

        #endregion

        #region Transition Methods

        /// <summary>
        /// 切换情感音乐
        /// </summary>
        public void TransitionToEmotionalMusic(EmotionalState newEmotionalState)
        {
            if (currentEmotionalState == newEmotionalState && isEmotionalMusicPlaying)
            {
                return; // 相同状态，不切换
            }

            // 先停止当前音乐
            StopEmotionalMusic();

            // 等待淡出完成后再播放新音乐
            StartCoroutine(PlayAfterFadeOut(newEmotionalState, fadeDuration));
        }

        private System.Collections.IEnumerator PlayAfterFadeOut(EmotionalState newEmotionalState, float delay)
        {
            yield return new WaitForSeconds(delay);

            switch (newEmotionalState)
            {
                case EmotionalState.Joy:
                    PlayJoyMusic();
                    break;
                case EmotionalState.Sadness:
                    PlaySadnessMusic();
                    break;
                case EmotionalState.Tension:
                    PlayTensionMusic();
                    break;
                case EmotionalState.Nostalgia:
                    PlayNostalgiaMusic();
                    break;
                case EmotionalState.Hope:
                    PlayHopeMusic();
                    break;
                case EmotionalState.Fear:
                    PlayFearMusic();
                    break;
                case EmotionalState.None:
                    StopEmotionalMusic();
                    break;
            }
        }

        #endregion

        #region Fade Effects

        private System.Collections.IEnumerator FadeInRoutine(AudioClip clip, float duration)
        {
            if (emotionalSource == null || clip == null) yield break;

            emotionalSource.clip = clip;
            emotionalSource.volume = 0f;
            emotionalSource.Play();
            isEmotionalMusicPlaying = true;
            currentEmotionalState = EmotionalState.None; // 情感音乐不设置情感状态

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                emotionalSource.volume = Mathf.Lerp(0f, emotionalVolume, t);
                yield return null;
            }

            Debug.Log($"[情感音乐] 淡入: {clip.name}");
        }

        private System.Collections.IEnumerator FadeOutRoutine(AudioSource source, float duration, System.Action onComplete)
        {
            float startVolume = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            source.Stop();
            source.volume = startVolume;

            onComplete?.Invoke();
        }

        #endregion

        #region Volume Control

        public void SetEmotionalVolume(float volume)
        {
            emotionalVolume = Mathf.Clamp01(volume);
        }

        public float GetEmotionalVolume()
        {
            return emotionalVolume;
        }

        #endregion

        /// <summary>
        /// 获取当前情感状态
        /// </summary>
        public EmotionalState GetCurrentEmotionalState()
        {
            return currentEmotionalState;
        }

        /// <summary>
        /// 是否正在播放情感音乐
        /// </summary>
        public bool IsPlaying()
        {
            return isEmotionalMusicPlaying && emotionalSource != null && emotionalSource.isPlaying;
        }
    }

    /// <summary>
    /// 情感状态枚举
    /// </summary>
    public enum EmotionalState
    {
        None,      // 无情感
        Joy,       // 喜悦
        Sadness,   // 悲伤
        Tension,   // 紧张
        Nostalgia, // 怀旧
        Hope,      // 希望
        Fear       // 恐惧
    }
}
