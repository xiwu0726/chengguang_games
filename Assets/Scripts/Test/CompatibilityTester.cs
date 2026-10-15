#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

namespace TimeStamp
{
    /// <summary>
    /// 兼容性测试器
    /// 测试不同分辨率、平台、Unity版本的兼容性
    /// </summary>
    public class CompatibilityTester : MonoBehaviour
    {
        public static CompatibilityTester Instance { get; private set; }

        [Header("测试设置")]
        [SerializeField] private List<Resolution> testResolutions = new List<Resolution>();
        [SerializeField] private List<ScreenOrientation> testOrientations = new List<ScreenOrientation>();

        [Header("平台测试")]
        [SerializeField] private bool testDesktop = true;
        [SerializeField] private bool testMobile = true;
        [SerializeField] private bool testConsole = false;

        [Header("测试结果")]
        [SerializeField] private bool showTestResults = true;

        private List<CompatibilityResult> testResults = new List<CompatibilityResult>();

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

        private void Start()
        {
            InitializeTestResolutions();
            InitializeTestOrientations();
        }

        /// <summary>
        /// 初始化测试分辨率
        /// </summary>
        private void InitializeTestResolutions()
        {
            if (testResolutions.Count == 0)
            {
                // 添加常见的测试分辨率
                testResolutions = new List<Resolution>
                {
                    new Resolution { width = 1920, height = 1080 },
                    new Resolution { width = 2560, height = 1440 },
                    new Resolution { width = 3840, height = 2160 },
                    new Resolution { width = 1280, height = 720 },
                    new Resolution { width = 1366, height = 768 },
                    new Resolution { width = 720, height = 1280 }, // 竖屏
                    new Resolution { width = 1080, height = 1920 } // 竖屏
                };
            }
        }

        /// <summary>
        /// 初始化测试方向
        /// </summary>
        private void InitializeTestOrientations()
        {
            if (testOrientations.Count == 0)
            {
                testOrientations = new List<ScreenOrientation>
                {
                    ScreenOrientation.Portrait,
                    ScreenOrientation.LandscapeLeft,
                    ScreenOrientation.LandscapeRight,
                    ScreenOrientation.PortraitUpsideDown
                };
            }
        }

        /// <summary>
        /// 运行所有兼容性测试
        /// </summary>
        public void RunAllCompatibilityTests()
        {
            Debug.Log("[兼容性测试] 开始运行兼容性测试...");

            testResults.Clear();

            // 测试不同分辨率
            foreach (var resolution in testResolutions)
            {
                TestResolution(resolution);
            }

            // 测试不同方向
            foreach (var orientation in testOrientations)
            {
                TestOrientation(orientation);
            }

            // 测试平台
            TestPlatform();

            // 测试Unity版本
            TestUnityVersion();

            // 显示测试结果
            ShowTestResults();
        }

        /// <summary>
        /// 测试分辨率
        /// </summary>
        private void TestResolution(Resolution resolution)
        {
            try
            {
                Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
                bool isSupported = CheckResolutionSupport(resolution.width, resolution.height);

                var result = new CompatibilityResult
                {
                    TestType = "Resolution",
                    Parameter = $"{resolution.width}x{resolution.height}",
                    IsSupported = isSupported,
                    Message = isSupported ? "分辨率支持" : "分辨率不支持"
                };

                testResults.Add(result);
                Debug.Log($"[兼容性测试] 分辨率测试: {resolution.width}x{resolution.height} - {result.Message}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[兼容性测试] 分辨率测试失败: {e.Message}");
            }
        }

        /// <summary>
        /// 测试方向
        /// </summary>
        private void TestOrientation(ScreenOrientation orientation)
        {
            try
            {
                Screen.orientation = orientation;
                bool isSupported = CheckOrientationSupport(orientation);

                var result = new CompatibilityResult
                {
                    TestType = "Orientation",
                    Parameter = orientation.ToString(),
                    IsSupported = isSupported,
                    Message = isSupported ? "方向支持" : "方向不支持"
                };

                testResults.Add(result);
                Debug.Log($"[兼容性测试] 方向测试: {orientation} - {result.Message}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[兼容性测试] 方向测试失败: {e.Message}");
            }
        }

        /// <summary>
        /// 测试平台
        /// </summary>
        private void TestPlatform()
        {
            var platform = Application.platform;
            bool isSupported = true;
            string message = "";

            switch (platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    message = isTestDesktop ? "Windows平台支持" : "Windows平台不支持";
                    break;
                case RuntimePlatform.AndroidPlayer:
                case RuntimePlatform.IPhonePlayer:
                    message = isTestMobile ? "移动平台支持" : "移动平台不支持";
                    break;
                case RuntimePlatform.XboxOne:
                case RuntimePlatform.PlayStation4:
                    message = isTestConsole ? "控制台平台支持" : "控制台平台不支持";
                    break;
                default:
                    message = "未知平台";
                    isSupported = false;
                    break;
            }

            var result = new CompatibilityResult
            {
                TestType = "Platform",
                Parameter = platform.ToString(),
                IsSupported = isSupported,
                Message = message
            };

            testResults.Add(result);
            Debug.Log($"[兼容性测试] 平台测试: {platform} - {message}");
        }

        /// <summary>
        /// 测试Unity版本
        /// </summary>
        private void TestUnityVersion()
        {
            string unityVersion = Application.unityVersion;
            float majorVersion = ParseUnityVersion(unityVersion);

            bool isSupported = majorVersion >= 2020f;
            string message = isSupported ? "Unity版本支持" : "Unity版本不支持";

            var result = new CompatibilityResult
            {
                TestType = "UnityVersion",
                Parameter = unityVersion,
                IsSupported = isSupported,
                Message = message
            };

            testResults.Add(result);
            Debug.Log($"[兼容性测试] Unity版本测试: {unityVersion} - {message}");
        }

        /// <summary>
        /// 检查分辨率支持
        /// </summary>
        private bool CheckResolutionSupport(int width, int height)
        {
            Resolution[] supportedResolutions = Screen.resolutions;
            foreach (var resolution in supportedResolutions)
            {
                if (resolution.width == width && resolution.height == height)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 检查方向支持
        /// </summary>
        private bool CheckOrientationSupport(ScreenOrientation orientation)
        {
            return true; // 所有Unity版本都支持基本的方向设置
        }

        /// <summary>
        /// 解析Unity版本
        /// </summary>
        private float ParseUnityVersion(string version)
        {
            string[] parts = version.Split('.');
            if (parts.Length >= 1 && int.TryParse(parts[0], out int major))
            {
                return major;
            }
            return 0f;
        }

        /// <summary>
        /// 显示测试结果
        /// </summary>
        private void ShowTestResults()
        {
            int supportedCount = 0;
            int totalTests = testResults.Count;

            foreach (var result in testResults)
            {
                if (result.IsSupported)
                {
                    supportedCount++;
                }
            }

            float successRate = (float)supportedCount / totalTests * 100f;

            string report = $@"
========================================
       《时光之痕》兼容性测试报告
========================================

总测试数: {totalTests}
支持: {supportedCount}
不支持: {totalTests - supportedCount}
成功率: {successRate:F2}%

详细结果:
";

            foreach (var result in testResults)
            {
                report += $"\n{result.TestType} ({result.Parameter}): {result.Message}";
            }

            Debug.Log(report);

            if (showTestResults)
            {
                Console.WriteLine(report);
            }
        }

        /// <summary>
        /// 获取测试结果
        /// </summary>
        public List<CompatibilityResult> GetTestResults()
        {
            return new List<CompatibilityResult>(testResults);
        }

        /// <summary>
        /// 检查是否所有测试都通过
        /// </summary>
        public bool AreAllTestsPassed()
        {
            foreach (var result in testResults)
            {
                if (!result.IsSupported)
                {
                    return false;
                }
            }
            return true;
        }
    }

    /// <summary>
    /// 兼容性测试结果
    /// </summary>
    public class CompatibilityResult
    {
        public string TestType { get; set; }
        public string Parameter { get; set; }
        public bool IsSupported { get; set; }
        public string Message { get; set; }
    }
}
#endif
