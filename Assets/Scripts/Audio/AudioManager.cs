#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using UnityEngine;
using System.Collections.Generic;

namespace TimeStamp
{
    /// <summary>
    /// 音频管理器 - 统一管理所有音频资源
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("背景音乐")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioClip mainTheme;
        [SerializeField] private AudioClip sceneTheme;
        [SerializeField] private AudioClip pastTheme;
        [SerializeField] private AudioClip futureTheme;
        [SerializeField] private AudioClip dreamTheme;

        [Header("音效")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip uiClick;
        [SerializeField] private AudioClip uiHover;
        [SerializeField] private AudioClip doorOpen;
        [SerializeField] private AudioClip bookOpen;
        [SerializeField] private AudioClip timeFragmentUse;
        [SerializeField] private AudioClip timeJump;
        [SerializeField] private AudioClip memoryCollect;

        [Header("音量设置")]
        [SerializeField] private float masterVolume = 1.0f;
        [SerializeField] private float bgmVolume = 0.7f;
        [SerializeField] private float sfxVolume = 0.8f;

        private bool isBgmPlaying = false;
        private float currentBgmTransitionProgress = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
        }

        private void InitializeAudioSources()
        {
            // BGM音源
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            // 音效音源
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
        }

        #region BGM Methods

        /// <summary>
        /// 播放主旋律
        /// </summary>
        public void PlayMainTheme()
        {
            if (mainTheme != null && bgmSource != null)
            {
                bgmSource.clip = mainTheme;
                bgmSource.volume = bgmVolume * masterVolume;
                bgmSource.Play();
                isBgmPlaying = true;
                Debug.Log("[BGM] 播放主旋律");
            }
        }

        /// <summary>
        /// 播放场景背景音乐
        /// </summary>
        public void PlaySceneMusic(SceneType sceneType)
        {
            switch (sceneType)
            {
                case SceneType.NewTokyo:
                    PlaySceneMusic(sceneTheme);
                    break;
                case SceneType.TimeResearchLab:
                    PlaySceneMusic(sceneTheme);
                    break;
                case SceneType.HistoricalLibrary:
                    PlaySceneMusic(pastTheme);
                    break;
                case SceneType.FutureRuins:
                    PlaySceneMusic(futureTheme);
                    break;
                case SceneType.DreamWorld:
                    PlaySceneMusic(dreamTheme);
                    break;
                default:
                    PlaySceneMusic(sceneTheme);
                    break;
            }
        }

        private void PlaySceneMusic(AudioClip clip)
        {
            if (clip != null && bgmSource != null)
            {
                bgmSource.clip = clip;
                bgmSource.volume = bgmVolume * masterVolume;
                bgmSource.Play();
                isBgmPlaying = true;
                Debug.Log($"[BGM] 播放场景音乐: {clip.name}");
            }
        }

        /// <summary>
        /// 停止BGM
        /// </summary>
        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
                isBgmPlaying = false;
                Debug.Log("[BGM] 已停止");
            }
        }

        /// <summary>
        /// 切换BGM（淡入淡出效果）
        /// </summary>
        public void CrossFadeBGM(AudioClip newClip, float duration = 2f)
        {
            if (bgmSource == null || newClip == null) return;

            StartCoroutine(CrossFadeRoutine(newClip, duration));
        }

        private System.Collections.IEnumerator CrossFadeRoutine(AudioClip newClip, float duration)
        {
            float startVolume = bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // 淡出旧BGM
                if (isBgmPlaying)
                {
                    bgmSource.volume = Mathf.Lerp(startVolume, 0f, t);
                }

                yield return null;
            }

            // 淡入新BGM
            bgmSource.clip = newClip;
            bgmSource.Play();
            bgmSource.volume = 0f;

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                bgmSource.volume = Mathf.Lerp(0f, bgmVolume * masterVolume, t);
                yield return null;
            }

            isBgmPlaying = true;
            Debug.Log($"[BGM] 淡入新音乐: {newClip.name}");
        }

        #endregion

        #region SFX Methods

        /// <summary>
        /// 播放音效
        /// </summary>
        public void PlaySFX(AudioClip clip)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * masterVolume);
                Debug.Log($"[SFX] 播放: {clip.name}");
            }
        }

        /// <summary>
        /// UI点击音效
        /// </summary>
        public void PlayUIClick()
        {
            PlaySFX(uiClick);
        }

        /// <summary>
        /// UI悬停音效
        /// </summary>
        public void PlayUIHover()
        {
            PlaySFX(uiHover);
        }

        /// <summary>
        /// 门打开音效
        /// </summary>
        public void PlayDoorOpen()
        {
            PlaySFX(doorOpen);
        }

        /// <summary>
        /// 书打开音效
        /// </summary>
        public void PlayBookOpen()
        {
            PlaySFX(bookOpen);
        }

        /// <summary>
        /// 使用时间碎片音效
        /// </summary>
        public void PlayTimeFragmentUse()
        {
            PlaySFX(timeFragmentUse);
        }

        /// <summary>
        /// 时间跳跃音效
        /// </summary>
        public void PlayTimeJump()
        {
            PlaySFX(timeJump);
        }

        /// <summary>
        /// 收集记忆碎片音效
        /// </summary>
        public void PlayMemoryCollect()
        {
            PlaySFX(memoryCollect);
        }

        #endregion

        #region Volume Control

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        /// <summary>
        /// 设置BGM音量
        /// </summary>
        public void SetBGMVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        /// <summary>
        /// 设置SFX音量
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = bgmVolume * masterVolume;
            }
        }

        #endregion

        /// <summary>
        /// 获取当前BGM状态
        /// </summary>
        public bool IsBGMPlaying()
        {
            return isBgmPlaying && bgmSource != null && bgmSource.isPlaying;
        }

        /// <summary>
        /// 重置所有音频
        /// </summary>
        public void Reset()
        {
            StopBGM();
            sfxSource.Stop();
            masterVolume = 1.0f;
            bgmVolume = 0.7f;
            sfxVolume = 0.8f;
        }
    }

    /// <summary>
    /// 场景类型枚举
    /// </summary>
    public enum SceneType
    {
        NewTokyo,           // 新东京
        TimeResearchLab,    // 时空研究所
        HistoricalLibrary,  // 历史图书馆
        FutureRuins,        // 未来废墟
        DreamWorld          // 梦境世界
    }
}
#endif
