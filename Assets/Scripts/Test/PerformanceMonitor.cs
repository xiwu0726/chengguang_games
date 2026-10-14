using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 性能监控器
    /// 监控帧率、内存、加载时间等性能指标
    /// </summary>
    public class PerformanceMonitor : MonoBehaviour
    {
        public static PerformanceMonitor Instance { get; private set; }

        [Header("监控设置")]
        [SerializeField] private bool autoMonitor = true;
        [SerializeField] private float updateInterval = 1f;
        [SerializeField] private int frameHistoryLength = 60;

        [Header("性能阈值")]
        [SerializeField] private int targetFps = 60;
        [SerializeField] private float maxMemoryMB = 500f;
        [SerializeField] private float maxLoadTimeSeconds = 30f;

        [Header("UI显示")]
        [SerializeField] private bool showOnScreen = false;
        [SerializeField] private bool logWarnings = true;

        private float currentFps = 0f;
        private float currentMemoryMB = 0f;
        private float currentLoadTime = 0f;
        private float lastFrameTime = 0f;
        private int frameCount = 0;

        private List<float> frameHistory = new List<float>();
        private float monitorTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            lastFrameTime = Time.realtimeSinceStartup;
        }

        private void Update()
        {
            if (!autoMonitor) return;

            monitorTimer += Time.deltaTime;
            if (monitorTimer >= updateInterval)
            {
                monitorTimer = 0f;
                UpdateMetrics();
            }

            // 计算FPS
            frameCount++;
            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - lastFrameTime >= 1f)
            {
                currentFps = frameCount / (currentTime - lastFrameTime);
                frameCount = 0;
                lastFrameTime = currentTime;

                // 添加到历史记录
                frameHistory.Add(currentFps);
                if (frameHistory.Count > frameHistoryLength)
                {
                    frameHistory.RemoveAt(0);
                }

                // 检查性能警告
                CheckPerformanceWarnings();
            }

            // 显示UI
            if (showOnScreen)
            {
                ShowPerformanceUI();
            }
        }

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private void UpdateMetrics()
        {
            // 获取内存使用量
            currentMemoryMB = (System.GC.GetTotalMemory(false) / (1024f * 1024f));

            // 获取场景加载时间
            if (SceneManager.sceneCount > 0)
            {
                Scene scene = SceneManager.GetSceneAt(0);
                currentLoadTime = scene.loadTime;
            }
        }

        /// <summary>
        /// 检查性能警告
        /// </summary>
        private void CheckPerformanceWarnings()
        {
            if (logWarnings)
            {
                if (currentFps < targetFps)
                {
                    Debug.LogWarning($"[性能监控] 帧率过低: {currentFps:F1} FPS (目标: {targetFps})");
                }

                if (currentMemoryMB > maxMemoryMB)
                {
                    Debug.LogWarning($"[性能监控] 内存使用过高: {currentMemoryMB:F1} MB (目标: {maxMemoryMB} MB)");
                }

                if (currentLoadTime > maxLoadTimeSeconds)
                {
                    Debug.LogWarning($"[性能监控] 场景加载时间过长: {currentLoadTime:F1}秒 (目标: {maxLoadTimeSeconds}秒)");
                }
            }
        }

        /// <summary>
        /// 显示性能UI
        /// </summary>
        private void ShowPerformanceUI()
        {
            // 这里可以创建UI显示性能指标
            Debug.Log($"[性能监控] FPS: {currentFps:F1} | 内存: {currentMemoryMB:F1} MB | 加载时间: {currentLoadTime:F1}秒");
        }

        /// <summary>
        /// 获取当前FPS
        /// </summary>
        public float GetCurrentFps()
        {
            return currentFps;
        }

        /// <summary>
        /// 获取当前内存使用量
        /// </summary>
        public float GetCurrentMemoryMB()
        {
            return currentMemoryMB;
        }

        /// <summary>
        /// 获取当前加载时间
        /// </summary>
        public float GetCurrentLoadTime()
        {
            return currentLoadTime;
        }

        /// <summary>
        /// 获取平均FPS
        /// </summary>
        public float GetAverageFps()
        {
            if (frameHistory.Count == 0) return 0f;

            float sum = 0f;
            foreach (var fps in frameHistory)
            {
                sum += fps;
            }
            return sum / frameHistory.Count;
        }

        /// <summary>
        /// 检查性能是否达标
        /// </summary>
        public bool IsPerformanceAcceptable()
        {
            return currentFps >= targetFps &&
                   currentMemoryMB <= maxMemoryMB &&
                   currentLoadTime <= maxLoadTimeSeconds;
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public PerformanceStats GetStats()
        {
            return new PerformanceStats
            {
                CurrentFps = currentFps,
                CurrentMemoryMB = currentMemoryMB,
                CurrentLoadTime = currentLoadTime,
                AverageFps = GetAverageFps(),
                TargetFps = targetFps,
                MaxMemoryMB = maxMemoryMB,
                MaxLoadTimeSeconds = maxLoadTimeSeconds,
                IsAcceptable = IsPerformanceAcceptable()
            };
        }

        /// <summary>
        /// 重置性能监控
        /// </summary>
        public void Reset()
        {
            frameHistory.Clear();
            currentFps = 0f;
            currentMemoryMB = 0f;
            currentLoadTime = 0f;
        }
    }

    /// <summary>
    /// 性能统计信息
    /// </summary>
    public class PerformanceStats
    {
        public float CurrentFps { get; set; }
        public float CurrentMemoryMB { get; set; }
        public float CurrentLoadTime { get; set; }
        public float AverageFps { get; set; }
        public int TargetFps { get; set; }
        public float MaxMemoryMB { get; set; }
        public float MaxLoadTimeSeconds { get; set; }
        public bool IsAcceptable { get; set; }
    }
}
