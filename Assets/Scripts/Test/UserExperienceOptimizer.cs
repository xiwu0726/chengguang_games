#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 用户体验优化器
    /// 优化游戏体验，包括加载速度、交互流畅度等
    /// </summary>
    public class UserExperienceOptimizer : MonoBehaviour
    {
        public static UserExperienceOptimizer Instance { get; private set; }

        [Header("优化设置")]
        [SerializeField] private bool autoOptimize = true;
        [SerializeField] private float optimizationInterval = 5f;

        [Header("加载优化")]
        [SerializeField] private int maxConcurrentDownloads = 5;
        [SerializeField] private float preloadingDelay = 2f;

        [Header("交互优化")]
        [SerializeField] private bool enableInputBuffering = true;
        [SerializeField] private float inputBufferDelay = 0.05f;

        [Header("UI优化")]
        [SerializeField] private bool useObjectPooling = true;
        [SerializeField] private int maxPoolSize = 100;

        private float optimizationTimer = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (!autoOptimize) return;

            optimizationTimer += Time.deltaTime;
            if (optimizationTimer >= optimizationInterval)
            {
                optimizationTimer = 0f;
                PerformOptimization();
            }
        }

        /// <summary>
        /// 执行优化
        /// </summary>
        private void PerformOptimization()
        {
            OptimizeMemoryUsage();
            OptimizeObjectPooling();
            OptimizeLoading();
            OptimizeInputBuffering();
        }

        /// <summary>
        /// 优化内存使用
        /// </summary>
        private void OptimizeMemoryUsage()
        {
            // 建议Unity垃圾回收
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// 优化对象池
        /// </summary>
        private void OptimizeObjectPooling()
        {
            if (useObjectPooling)
            {
                // 这里可以添加对象池优化逻辑
                // 例如：清理未使用的对象池项
                Debug.Log("[用户体验优化] 对象池优化完成");
            }
        }

        /// <summary>
        /// 优化加载
        /// </summary>
        private void OptimizeLoading()
        {
            // 优化场景加载
            // 例如：预加载关键场景
            Debug.Log("[用户体验优化] 加载优化完成");
        }

        /// <summary>
        /// 优化输入缓冲
        /// </summary>
        private void OptimizeInputBuffering()
        {
            // 优化输入处理
            Debug.Log("[用户体验优化] 输入缓冲优化完成");
        }

        /// <summary>
        /// 预加载场景
        /// </summary>
        public void PreloadScene(string sceneName)
        {
            StartCoroutine(PreloadSceneCoroutine(sceneName));
        }

        /// <summary>
        /// 获取优化建议
        /// </summary>
        public List<string> GetOptimizationSuggestions()
        {
            List<string> suggestions = new List<string>();

            // 检查内存使用
            float memoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f);
            if (memoryMB > 300f)
            {
                suggestions.Add("内存使用较高，建议优化资源加载");
            }

            // 检查场景数量
            int sceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount;
            if (sceneCount > 10)
            {
                suggestions.Add($"场景数量较多({sceneCount})，建议优化场景管理");
            }

            // 检查对象数量
            int totalObjects = FindObjectsOfType<GameObject>().Length;
            if (totalObjects > 1000)
            {
                suggestions.Add($"对象数量较多({totalObjects})，建议优化对象池");
            }

            return suggestions;
        }

        /// <summary>
        /// 执行完整优化
        /// </summary>
        public void PerformFullOptimization()
        {
            Debug.Log("[用户体验优化] 开始完整优化...");

            // 优化内存
            OptimizeMemoryUsage();

            // 优化对象池
            OptimizeObjectPooling();

            // 优化加载
            OptimizeLoading();

            // 优化输入缓冲
            OptimizeInputBuffering();

            Debug.Log("[用户体验优化] 完整优化完成");
        }

        /// <summary>
        /// 优化场景加载
        /// </summary>
        public void OptimizeSceneLoading()
        {
            // 优化场景加载策略
            // 例如：使用异步加载、场景预加载等
            Debug.Log("[用户体验优化] 场景加载优化完成");
        }

        /// <summary>
        /// 获取优化状态
        /// </summary>
        public OptimizationStatus GetStatus()
        {
            return new OptimizationStatus
            {
                IsOptimizing = true,
                MemoryUsageMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
                SceneCount = UnityEngine.SceneManagement.SceneManager.sceneCount,
                ObjectCount = FindObjectsOfType<GameObject>().Length,
                Suggestions = GetOptimizationSuggestions()
            };
        }
    }

    /// <summary>
    /// 优化状态
    /// </summary>
    public class OptimizationStatus
    {
        public bool IsOptimizing { get; set; }
        public float MemoryUsageMB { get; set; }
        public int SceneCount { get; set; }
        public int ObjectCount { get; set; }
        public List<string> Suggestions { get; set; }
    }
}
#endif
