using UnityEngine;
using System.Collections.Generic;

namespace TimeStamp
{
    /// <summary>
    /// 语音录制管理器
    /// 负责管理主要角色的配音
    /// </summary>
    public class VoiceRecordingManager : MonoBehaviour
    {
        public static VoiceRecordingManager Instance { get; private set; }

        [Header("语音数据")]
        [SerializeField] private AudioClip[] characterVoices;

        [Header("音量设置")]
        [SerializeField] private float voiceVolume = 1.0f;

        [Header("语音参数")]
        [SerializeField] private float voiceFadeInDuration = 0.3f;
        [SerializeField] private float voiceFadeOutDuration = 0.3f;

        private Dictionary<string, AudioClip> voiceDictionary = new Dictionary<string, AudioClip>();
        private AudioSource voiceSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeVoiceSource();
            BuildVoiceDictionary();
        }

        private void InitializeVoiceSource()
        {
            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.loop = false;
                voiceSource.playOnAwake = false;
            }
        }

        private void BuildVoiceDictionary()
        {
            // 主角林晓的语音
            voiceDictionary.Add("LinXiao_Intro", null); // 需要配音
            voiceDictionary.Add("LinXiao_Normal", null); // 需要配音
            voiceDictionary.Add("LinXiao_Surprise", null); // 需要配音
            voiceDictionary.Add("LinXiao_Sad", null); // 需要配音
            voiceDictionary.Add("LinXiao_Tension", null); // 需要配音

            // 艾莉丝的语音
            voiceDictionary.Add("Alice_Intro", null); // 需要配音
            voiceDictionary.Add("Alice_Normal", null); // 需要配音
            voiceDictionary.Add("Alice_Surprise", null); // 需要配音
            voiceDictionary.Add("Alice_Sad", null); // 需要配音
            voiceDictionary.Add("Alice_Tension", null); // 需要配音

            // 雷恩的语音
            voiceDictionary.Add("Rain_Intro", null); // 需要配音
            voiceDictionary.Add("Rain_Normal", null); // 需要配音
            voiceDictionary.Add("Rain_Surprise", null); // 需要配音
            voiceDictionary.Add("Rain_Sad", null); // 需要配音
            voiceDictionary.Add("Rain_Tension", null); // 需要配音

            // 小雅的语音
            voiceDictionary.Add("Xiaoya_Intro", null); // 需要配音
            voiceDictionary.Add("Xiaoya_Normal", null); // 需要配音
            voiceDictionary.Add("Xiaoya_Surprise", null); // 需要配音
            voiceDictionary.Add("Xiaoya_Sad", null); // 需要配音
            voiceDictionary.Add("Xiaoya_Tension", null); // 需要配音

            // 时间领主的语音
            voiceDictionary.Add("TimeLord_Intro", null); // 需要配音
            voiceDictionary.Add("TimeLord_Normal", null); // 需要配音
            voiceDictionary.Add("TimeLord_Surprise", null); // 需要配音
            voiceDictionary.Add("TimeLord_Sad", null); // 需要配音
            voiceDictionary.Add("TimeLord_Tension", null); // 需要配音

            // 记忆实体的语音
            voiceDictionary.Add("MemoryEntity_Father", null); // 需要配音
            voiceDictionary.Add("MemoryEntity_Mother", null); // 需要配音
            voiceDictionary.Add("MemoryEntity_Alice_Family", null); // 需要配音
            voiceDictionary.Add("MemoryEntity_Rain_Past", null); // 需要配音

            Debug.Log($"[语音管理器] 已建立 {voiceDictionary.Count} 个角色的语音映射");
        }

        #region Voice Playback

        /// <summary>
        /// 播放角色语音
        /// </summary>
        public void PlayCharacterVoice(string voiceKey, float volumeOverride = -1f)
        {
            if (!voiceDictionary.ContainsKey(voiceKey))
            {
                Debug.LogWarning($"[语音] 未找到语音键: {voiceKey}");
                return;
            }

            AudioClip clip = voiceDictionary[voiceKey];
            if (clip == null)
            {
                Debug.LogWarning($"[语音] 语音文件未加载: {voiceKey}");
                return;
            }

            float targetVolume = volumeOverride >= 0f ? volumeOverride : voiceVolume;

            StartCoroutine(FadeInVoice(clip, targetVolume));
        }

        /// <summary>
        /// 播放语音并淡出
        /// </summary>
        public void PlayCharacterVoiceWithFadeOut(string voiceKey, float volumeOverride = -1f)
        {
            if (!voiceDictionary.ContainsKey(voiceKey))
            {
                Debug.LogWarning($"[语音] 未找到语音键: {voiceKey}");
                return;
            }

            AudioClip clip = voiceDictionary[voiceKey];
            if (clip == null)
            {
                Debug.LogWarning($"[语音] 语音文件未加载: {voiceKey}");
                return;
            }

            float targetVolume = volumeOverride >= 0f ? volumeOverride : voiceVolume;

            StartCoroutine(FadeInVoiceWithFadeOut(clip, targetVolume));
        }

        private System.Collections.IEnumerator FadeInVoice(AudioClip clip, float targetVolume)
        {
            if (voiceSource == null) yield break;

            voiceSource.clip = clip;
            voiceSource.volume = 0f;
            voiceSource.Play();

            float elapsed = 0f;
            while (elapsed < voiceFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / voiceFadeInDuration;

                voiceSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            Debug.Log($"[语音] 播放: {clip.name}");
        }

        private System.Collections.IEnumerator FadeInVoiceWithFadeOut(AudioClip clip, float targetVolume)
        {
            if (voiceSource == null) yield break;

            voiceSource.clip = clip;
            voiceSource.volume = 0f;
            voiceSource.Play();

            float elapsed = 0f;
            while (elapsed < voiceFadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / voiceFadeInDuration;

                voiceSource.volume = Mathf.Lerp(0f, targetVolume, t);
                yield return null;
            }

            // 淡出
            elapsed = 0f;
            while (elapsed < voiceFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / voiceFadeOutDuration;

                voiceSource.volume = Mathf.Lerp(targetVolume, 0f, t);
                yield return null;
            }

            voiceSource.Stop();
        }

        /// <summary>
        /// 停止当前语音
        /// </summary>
        public void StopVoice()
        {
            if (voiceSource != null && voiceSource.isPlaying)
            {
                voiceSource.Stop();
                Debug.Log("[语音] 已停止");
            }
        }

        #endregion

        #region Voice Management

        /// <summary>
        /// 加载语音文件
        /// </summary>
        public void LoadVoiceFile(string voiceKey, AudioClip clip)
        {
            if (!voiceDictionary.ContainsKey(voiceKey))
            {
                Debug.LogWarning($"[语音] 未找到语音键: {voiceKey}");
                return;
            }

            voiceDictionary[voiceKey] = clip;
            Debug.Log($"[语音] 已加载: {voiceKey}");
        }

        /// <summary>
        /// 检查语音是否已加载
        /// </summary>
        public bool IsVoiceLoaded(string voiceKey)
        {
            return voiceDictionary.ContainsKey(voiceKey) && voiceDictionary[voiceKey] != null;
        }

        /// <summary>
        /// 获取未加载的语音列表
        /// </summary>
        public List<string> GetUnloadedVoices()
        {
            List<string> unloaded = new List<string>();

            foreach (var kvp in voiceDictionary)
            {
                if (kvp.Value == null)
                {
                    unloaded.Add(kvp.Key);
                }
            }

            return unloaded;
        }

        /// <summary>
        /// 清空所有语音
        /// </summary>
        public void ClearAllVoices()
        {
            foreach (var kvp in voiceDictionary)
            {
                kvp.Value = null;
            }

            StopVoice();
            Debug.Log("[语音] 已清空所有语音");
        }

        #endregion

        #region Volume Control

        public void SetVoiceVolume(float volume)
        {
            voiceVolume = Mathf.Clamp01(volume);
        }

        public float GetVoiceVolume()
        {
            return voiceVolume;
        }

        #endregion

        /// <summary>
        /// 获取语音数量
        /// </summary>
        public int GetTotalVoiceCount()
        {
            return voiceDictionary.Count;
        }

        /// <summary>
        /// 获取已加载的语音数量
        /// </summary>
        public int GetLoadedVoiceCount()
        {
            int count = 0;
            foreach (var kvp in voiceDictionary)
            {
                if (kvp.Value != null)
                    count++;
            }
            return count;
        }
    }
}
