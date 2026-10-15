#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using UnityEngine;
using System.Collections.Generic;

namespace TimeStamp
{
    /// <summary>
    /// 特殊音乐管理器
    /// 负责管理特殊场景的音乐（时空穿越、记忆闪回、高潮时刻）
    /// </summary>
    public class SpecialMusicManager : MonoBehaviour
    {
        public static SpecialMusicManager Instance { get; private set; }

        [Header("特殊音乐")]
        [SerializeField] private AudioSource specialSource;
        [SerializeField] private AudioClip timeTravelMusic;       // 时空穿越
        [SerializeField] private AudioClip memoryFlashbackMusic;  // 记忆闪回
        [SerializeField] private AudioClip climaxMusic;           // 高潮时刻
        [SerializeField] private AudioClip victoryMusic;          // 胜利
        [SerializeField] private AudioClip defeatMusic;           // 失败

        [Header("音量设置")]
        [SerializeField] private float specialVolume = 0.8f;
        [SerializeField] private float fadeDuration = 2.5f;

        private bool isSpecialMusicPlaying = false;
        private SpecialMusicType currentSpecialMusicType = SpecialMusicType.None;

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
            if (specialSource == null)
            {
                specialSource = gameObject.AddComponent<AudioSource>();
                specialSource.loop = true;
                specialSource.playOnAwake = false;
            }
        }

        #region Special Music Control

        /// <summary>
        /// 播放时空穿越音乐
        /// </summary>
        public void PlayTimeTravelMusic()
        {
            PlaySpecialMusic(SpecialMusicType.TimeTravel, timeTravelMusic);
        }

        /// <summary>
        /// 播放记忆闪回音乐
        /// </summary>
        public void PlayMemoryFlashbackMusic()
        {
            PlaySpecialMusic(SpecialMusicType.MemoryFlashback, memoryFlashbackMusic);
        }

        /// <summary>
        /// 播放高潮时刻音乐
        /// </summary>
        public void PlayClimaxMusic()
        {
            PlaySpecialMusic(SpecialMusicType.Climax, climaxMusic);
        }

        /// <summary>
        /// 播放胜利音乐
        /// </summary>
        public void PlayVictoryMusic()
        {
            PlaySpecialMusic(SpecialMusicType.Victory, victoryMusic);
        }

        /// <summary>
        /// 播放失败音乐
        /// </summary>
        public void PlayDefeatMusic()
        {
            PlaySpecialMusic(SpecialMusicType.Defeat, defeatMusic);
        }

        /// <summary>
        /// 停止特殊音乐
        /// </summary>
        public void StopSpecialMusic()
        {
            if (specialSource != null && isSpecialMusicPlaying)
            {
                StartCoroutine(FadeOutRoutine(specialSource, fadeDuration, () =>
                {
                    isSpecialMusicPlaying = false;
                    currentSpecialMusicType = SpecialMusicType.None;
                    Debug.Log("[特殊音乐] 已停止");
                }));
            }
        }

        #endregion

        #region Transition Methods

        /// <summary>
        /// 切换特殊音乐
        /// </summary>
        public void TransitionToSpecialMusic(SpecialMusicType newSpecialMusicType)
        {
            if (currentSpecialMusicType == newSpecialMusicType && isSpecialMusicPlaying)
            {
                return; // 相同状态，不切换
            }

            // 先停止当前音乐
            StopSpecialMusic();

            // 等待淡出完成后再播放新音乐
            StartCoroutine(PlayAfterFadeOut(newSpecialMusicType, fadeDuration));
        }

        private System.Collections.IEnumerator PlayAfterFadeOut(SpecialMusicType newSpecialMusicType, float delay)
        {
            yield return new WaitForSeconds(delay);

            switch (newSpecialMusicType)
            {
                case SpecialMusicType.TimeTravel:
                    PlayTimeTravelMusic();
                    break;
                case SpecialMusicType.MemoryFlashback:
                    PlayMemoryFlashbackMusic();
                    break;
                case SpecialMusicType.Climax:
                    PlayClimaxMusic();
                    break;
                case SpecialMusicType.Victory:
                    PlayVictoryMusic();
                    break;
                case SpecialMusicType.Defeat:
                    PlayDefeatMusic();
                    break;
                case SpecialMusicType.None:
                    StopSpecialMusic();
                    break;
            }
        }

        #endregion

        #region Fade Effects

        private System.Collections.IEnumerator FadeInRoutine(AudioClip clip, float duration)
        {
            if (specialSource == null || clip == null) yield break;

            specialSource.clip = clip;
            specialSource.volume = 0f;
            specialSource.Play();
            isSpecialMusicPlaying = true;
            currentSpecialMusicType = SpecialMusicType.None;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                specialSource.volume = Mathf.Lerp(0f, specialVolume, t);
                yield return null;
            }

            Debug.Log($"[特殊音乐] 淡入: {clip.name}");
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

        public void SetSpecialVolume(float volume)
        {
            specialVolume = Mathf.Clamp01(volume);
        }

        public float GetSpecialVolume()
        {
            return specialVolume;
        }

        #endregion

        /// <summary>
        /// 获取当前特殊音乐类型
        /// </summary>
        public SpecialMusicType GetCurrentSpecialMusicType()
        {
            return currentSpecialMusicType;
        }

        /// <summary>
        /// 是否正在播放特殊音乐
        /// </summary>
        public bool IsPlaying()
        {
            return isSpecialMusicPlaying && specialSource != null && specialSource.isPlaying;
        }
    }

    /// <summary>
    /// 特殊音乐类型枚举
    /// </summary>
    public enum SpecialMusicType
    {
        None,          // 无
        TimeTravel,    // 时空穿越
        MemoryFlashback, // 记忆闪回
        Climax,        // 高潮时刻
        Victory,       // 胜利
        Defeat         // 失败
    }
}
#endif
