#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using UnityEngine;
using System.Collections.Generic;

namespace TimeStamp
{
    /// <summary>
    /// 失眠音效管理器
    /// 负责播放脑波声、时间流动声等音效
    /// </summary>
    public class InsomniaAudioManager : MonoBehaviour
    {
        public static InsomniaAudioManager Instance { get; private set; }

        [Header("音频源")]
        [SerializeField] private AudioSource brainwaveSource;
        [SerializeField] private AudioSource timeFlowSource;
        [SerializeField] private AudioSource spaceDistortionSource;
        [SerializeField] private AudioSource memoryEntitySource;
        [SerializeField] private AudioSource crackleSource;

        [Header("音频数据")]
        [SerializeField] private AudioClip brainwaveClip;
        [SerializeField] private AudioClip timeFlowClip;
        [SerializeField] private AudioClip spaceDistortionClip;
        [SerializeField] private AudioClip memoryEntityClip;
        [SerializeField] private AudioClip crackleClip;

        [Header("参数")]
        [SerializeField] private float masterVolume = 1.0f;
        [SerializeField] private float brainwaveVolume = 0.3f;
        [SerializeField] private float timeFlowVolume = 0.2f;
        [SerializeField] private float spaceDistortionVolume = 0.1f;
        [SerializeField] private float memoryEntityVolume = 0.0f;
        [SerializeField] private float crackleVolume = 0.0f;

        [Header("混合参数")]
        [SerializeField] private float noiseIntensity = 0.5f;
        [SerializeField] private float pitchVariation = 0.2f;

        private bool isPlaying = false;
        private float currentInsomniaLevel = 0f;

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
            // 如果没有指定音频源，创建默认的
            if (brainwaveSource == null)
            {
                brainwaveSource = gameObject.AddComponent<AudioSource>();
            }
            if (timeFlowSource == null)
            {
                timeFlowSource = gameObject.AddComponent<AudioSource>();
            }
            if (spaceDistortionSource == null)
            {
                spaceDistortionSource = gameObject.AddComponent<AudioSource>();
            }
            if (memoryEntitySource == null)
            {
                memoryEntitySource = gameObject.AddComponent<AudioSource>();
            }
            if (crackleSource == null)
            {
                crackleSource = gameObject.AddComponent<AudioSource>();
            }

            // 设置音频源参数
            foreach (var source in new AudioSource[] { brainwaveSource, timeFlowSource, spaceDistortionSource, memoryEntitySource, crackleSource })
            {
                source.loop = true;
                source.playOnAwake = false;
            }
        }

        /// <summary>
        /// 开始播放失眠音效
        /// </summary>
        public void StartInsomniaAudio(float insomniaLevel)
        {
            if (isPlaying) return;

            currentInsomniaLevel = insomniaLevel;

            // 根据失眠等级设置音量
            SetVolumes(insomniaLevel);

            // 开始播放所有音效
            if (brainwaveClip != null) brainwaveSource.clip = brainwaveClip;
            if (timeFlowClip != null) timeFlowSource.clip = timeFlowClip;
            if (spaceDistortionClip != null) spaceDistortionSource.clip = spaceDistortionClip;
            if (memoryEntityClip != null) memoryEntitySource.clip = memoryEntityClip;
            if (crackleClip != null) crackleSource.clip = crackleClip;

            brainwaveSource.Play();
            timeFlowSource.Play();
            spaceDistortionSource.Play();

            if (memoryEntityClip != null)
                memoryEntitySource.Play();

            if (crackleClip != null)
                crackleSource.Play();

            isPlaying = true;

            Debug.Log($"[失眠音效] 开始播放 (等级: {insomniaLevel}%)");
        }

        /// <summary>
        /// 停止播放失眠音效
        /// </summary>
        public void StopInsomniaAudio()
        {
            foreach (var source in new AudioSource[] { brainwaveSource, timeFlowSource, spaceDistortionSource, memoryEntitySource, crackleSource })
            {
                source.Stop();
            }

            isPlaying = false;
            Debug.Log("[失眠音效] 已停止播放");
        }

        /// <summary>
        /// 更新音量设置
        /// </summary>
        public void UpdateVolumes(float insomniaLevel)
        {
            currentInsomniaLevel = insomniaLevel;
            SetVolumes(insomniaLevel);
        }

        private void SetVolumes(float insomniaLevel)
        {
            // 根据失眠等级计算音量
            float targetBrainwaveVolume = Mathf.Lerp(0.1f, 1.0f, insomniaLevel / 100f);
            float targetTimeFlowVolume = Mathf.Lerp(0.0f, 1.0f, insomniaLevel / 100f);
            float targetSpaceDistortionVolume = Mathf.Lerp(0.0f, 1.0f, insomniaLevel / 100f);
            float targetMemoryEntityVolume = Mathf.Lerp(0.0f, 1.0f, insomniaLevel / 100f);
            float targetCrackleVolume = Mathf.Lerp(0.0f, 1.0f, insomniaLevel / 100f);

            brainwaveSource.volume = targetBrainwaveVolume * masterVolume;
            timeFlowSource.volume = targetTimeFlowVolume * masterVolume;
            spaceDistortionSource.volume = targetSpaceDistortionVolume * masterVolume;
            memoryEntitySource.volume = targetMemoryEntityVolume * masterVolume;
            crackleSource.volume = targetCrackleVolume * masterVolume;
        }

        /// <summary>
        /// 设置主音量
        /// </summary>
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            SetVolumes(currentInsomniaLevel);
        }

        /// <summary>
        /// 获取脑波音量
        /// </summary>
        public float GetBrainwaveVolume()
        {
            return brainwaveSource.volume;
        }

        /// <summary>
        /// 获取时间流动音量
        /// </summary>
        public float GetTimeFlowVolume()
        {
            return timeFlowSource.volume;
        }

        /// <summary>
        /// 获取空间扭曲音量
        /// </summary>
        public float GetSpaceDistortionVolume()
        {
            return spaceDistortionSource.volume;
        }

        /// <summary>
        /// 获取记忆实体音量
        /// </summary>
        public float GetMemoryEntityVolume()
        {
            return memoryEntitySource.volume;
        }

        /// <summary>
        /// 获取撕裂音量
        /// </summary>
        public float GetCrackleVolume()
        {
            return crackleSource.volume;
        }

        /// <summary>
        /// 播放记忆实体对话
        /// </summary>
        public void PlayMemoryEntityDialogue(string dialogue)
        {
            if (memoryEntitySource != null && memoryEntitySource.isPlaying)
            {
                // 如果已经在播放，可以在这里添加逻辑来混合对话
                Debug.Log($"[记忆实体对话] {dialogue}");
            }
            else
            {
                // 播放对话音效
                Debug.Log($"[记忆实体对话] {dialogue}");
            }
        }

        /// <summary>
        /// 播放裂缝撕裂声
        /// </summary>
        public void PlayCrackleSound()
        {
            if (crackleSource != null && crackleClip != null)
            {
                crackleSource.PlayOneShot(crackleClip);
            }
        }

        /// <summary>
        /// 播放时间流动声（急促）
        /// </summary>
        public void PlayTimeFlowFast()
        {
            if (timeFlowSource != null)
            {
                timeFlowSource.pitch = 2.0f;
            }
        }

        /// <summary>
        /// 恢复时间流动声速度
        /// </summary>
        public void ResetTimeFlowSpeed()
        {
            if (timeFlowSource != null)
            {
                timeFlowSource.pitch = 1.0f;
            }
        }

        /// <summary>
        /// 检查是否正在播放音效
        /// </summary>
        public bool IsPlaying()
        {
            return isPlaying;
        }

        /// <summary>
        /// 重置所有音效
        /// </summary>
        public void Reset()
        {
            StopInsomniaAudio();
            SetVolumes(0f);
            masterVolume = 1.0f;
            noiseIntensity = 0.5f;
            pitchVariation = 0.2f;
        }
    }
}
#endif
