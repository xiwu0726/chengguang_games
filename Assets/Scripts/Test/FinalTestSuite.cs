#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 《时光之痕》最终测试套件
    /// 测试所有核心系统的功能和性能
    /// </summary>
    public class FinalTestSuite
    {
        private TestingFramework testingFramework;
        private PerformanceMonitor performanceMonitor;
        private CompatibilityTester compatibilityTester;
        private BugTracker bugTracker;

        public FinalTestSuite()
        {
            testingFramework = FindObjectOfType<TestingFramework>();
            performanceMonitor = FindObjectOfType<PerformanceMonitor>();
            compatibilityTester = FindObjectOfType<CompatibilityTester>();
            bugTracker = FindObjectOfType<BugTracker>();
        }

        /// <summary>
        /// 运行所有最终测试
        /// </summary>
        public void RunAllFinalTests()
        {
            Debug.Log("========================================");
            Debug.Log("开始《时光之痕》最终测试套件");
            Debug.Log("========================================");

            // 1. 功能测试
            RunFunctionalityTests();

            // 2. 性能测试
            RunPerformanceTests();

            // 3. 兼容性测试
            RunCompatibilityTests();

            // 4. 用户体验测试
            RunUserExperienceTests();

            // 5. Bug修复测试
            RunBugFixTests();

            // 6. 生成最终测试报告
            GenerateFinalReport();
        }

        /// <summary>
        /// 运行功能测试
        /// </summary>
        private void RunFunctionalityTests()
        {
            Debug.Log("\n[功能测试] 开始...");

            // 测试失眠状态系统
            TestInsomniaStateSystem();

            // 测试时间线切换系统
            TestTimeLineSwitchingSystem();

            // 测试记忆碎片系统
            TestMemoryShardSystem();

            // 测试对话系统
            TestDialogueSystem();

            // 测试多结局系统
            TestEndingSystem();

            // 测试音频系统
            TestAudioSystem();

            Debug.Log("[功能测试] 完成");
        }

        /// <summary>
        /// 测试失眠状态系统
        /// </summary>
        private void TestInsomniaStateSystem()
        {
            Debug.Log("  - 测试失眠状态系统...");

            if (testingFramework != null)
            {
                testingFramework.AddTestCase(new InsomniaStateTestCase());
            }

            // 快速检查
            var insomniaController = FindObjectOfType<InsomniaController>();
            if (insomniaController != null)
            {
                Debug.Log("    ✓ InsomniaController已初始化");
                Debug.Log($"    ✓ 当前状态: {insomniaController.GetCurrentState()}");
                Debug.Log($"    ✓ 当前失眠值: {insomniaController.GetCurrentInsomniaValue()}%");
            }
            else
            {
                Debug.LogError("    ✗ InsomniaController未找到");
                bugTracker.LogBug(
                    "InsomniaController未初始化",
                    "失眠状态系统控制器未找到",
                    BugPriority.High,
                    "Testing"
                );
            }
        }

        /// <summary>
        /// 测试时间线切换系统
        /// </summary>
        private void TestTimeLineSwitchingSystem()
        {
            Debug.Log("  - 测试时间线切换系统...");

            if (testingFramework != null)
            {
                testingFramework.AddTestCase(new TimeLineSwitchingTestCase());
            }

            // 快速检查
            var timeLineController = FindObjectOfType<TimeLineController>();
            if (timeLineController != null)
            {
                Debug.Log("    ✓ TimeLineController已初始化");
                Debug.Log($"    ✓ 当前时间线: {timeLineController.GetCurrentTimeline()}");
                Debug.Log($"    ✓ 当前年份: {timeLineController.GetCurrentYear()}");
            }
            else
            {
                Debug.LogError("    ✗ TimeLineController未找到");
                bugTracker.LogBug(
                    "TimeLineController未初始化",
                    "时间线切换系统控制器未找到",
                    BugPriority.High,
                    "Testing"
                );
            }
        }

        /// <summary>
        /// 测试记忆碎片系统
        /// </summary>
        private void TestMemoryShardSystem()
        {
            Debug.Log("  - 测试记忆碎片系统...");

            if (testingFramework != null)
            {
                testingFramework.AddTestCase(new MemoryShardSystemTestCase());
            }

            // 快速检查
            var memoryShardSystem = FindObjectOfType<MemoryShardSystem>();
            if (memoryShardSystem != null)
            {
                Debug.Log("    ✓ MemoryShardSystem已初始化");
                Debug.Log($"    ✓ 已收集记忆碎片: {memoryShardSystem.GetCollectedCount()}");

                // 测试添加记忆碎片
                var testShard = new MemoryShard(
                    "test-id",
                    "测试记忆",
                    "这是测试记忆内容",
                    MemoryShardType.Personal,
                    "2150",
                    "新东京",
                    "林晓"
                );
                memoryShardSystem.CollectMemoryShard(testShard);
                Debug.Log($"    ✓ 添加测试记忆碎片: {testShard.title}");
            }
            else
            {
                Debug.LogError("    ✗ MemoryShardSystem未找到");
                bugTracker.LogBug(
                    "MemoryShardSystem未初始化",
                    "记忆碎片收集系统未找到",
                    BugPriority.High,
                    "Testing"
                );
            }
        }

        /// <summary>
        /// 测试对话系统
        /// </summary>
        private void TestDialogueSystem()
        {
            Debug.Log("  - 测试对话系统...");

            if (testingFramework != null)
            {
                testingFramework.AddTestCase(new DialogueSystemTestCase());
            }

            // 快速检查
            var dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem != null)
            {
                Debug.Log("    ✓ DialogueSystem已初始化");
            }
            else
            {
                Debug.LogError("    ✗ DialogueSystem未找到");
                bugTracker.LogBug(
                    "DialogueSystem未初始化",
                    "对话系统未找到",
                    BugPriority.High,
                    "Testing"
                );
            }
        }

        /// <summary>
        /// 测试多结局系统
        /// </summary>
        private void TestEndingSystem()
        {
            Debug.Log("  - 测试多结局系统...");

            if (testingFramework != null)
            {
                testingFramework.AddTestCase(new EndingSystemTestCase());
            }

            // 快速检查
            var endingManager = FindObjectOfType<EndingManager>();
            if (endingManager != null)
            {
                Debug.Log("    ✓ EndingManager已初始化");
            }
            else
            {
                Debug.LogError("    ✗ EndingManager未找到");
                bugTracker.LogBug(
                    "EndingManager未初始化",
                    "多结局系统管理器未找到",
                    BugPriority.High,
                    "Testing"
                );
            }
        }

        /// <summary>
        /// 测试音频系统
        /// </summary>
        private void TestAudioSystem()
        {
            Debug.Log("  - 测试音频系统...");

            if (testingFramework != null)
            {
                testingFramework.AddTestCase(new AudioSystemTestCase());
            }

            // 快速检查
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager != null)
            {
                Debug.Log("    ✓ AudioManager已初始化");
            }
            else
            {
                Debug.LogError("    ✗ AudioManager未找到");
                bugTracker.LogBug(
                    "AudioManager未初始化",
                    "音频系统管理器未找到",
                    BugPriority.High,
                    "Testing"
                );
            }
        }

        /// <summary>
        /// 运行性能测试
        /// </summary>
        private void RunPerformanceTests()
        {
            Debug.Log("\n[性能测试] 开始...");

            // 运行性能监控
            if (performanceMonitor != null)
            {
                Debug.Log($"  - 当前FPS: {performanceMonitor.GetCurrentFps():F1}");
                Debug.Log($"  - 当前内存: {performanceMonitor.GetCurrentMemoryMB():F1} MB");
                Debug.Log($"  - 平均FPS: {performanceMonitor.GetAverageFps():F1}");

                var stats = performanceMonitor.GetStats();
                Debug.Log($"  - 目标FPS: {stats.TargetFps}");
                Debug.Log($"  - 最大内存: {stats.MaxMemoryMB} MB");
                Debug.Log($"  - 性能是否达标: {stats.IsAcceptable}");
            }

            // 运行兼容性测试
            if (compatibilityTester != null)
            {
                Debug.Log("  - 运行兼容性测试...");
                compatibilityTester.RunAllCompatibilityTests();
            }

            Debug.Log("[性能测试] 完成");
        }

        /// <summary>
        /// 运行兼容性测试
        /// </summary>
        private void RunCompatibilityTests()
        {
            Debug.Log("\n[兼容性测试] 开始...");

            if (compatibilityTester != null)
            {
                Debug.Log($"  - 测试平台: {Application.platform}");
                Debug.Log($"  - 测试分辨率: {Screen.width}x{Screen.height}");
                Debug.Log($"  - 测试方向: {Screen.orientation}");
                Debug.Log($"  - Unity版本: {Application.unityVersion}");

                var results = compatibilityTester.GetTestResults();
                int passed = 0;
                foreach (var result in results)
                {
                    if (result.IsSupported)
                    {
                        passed++;
                    }
                }

                float successRate = (float)passed / results.Count * 100f;
                Debug.Log($"  - 兼容性测试成功率: {successRate:F2}%");
            }

            Debug.Log("[兼容性测试] 完成");
        }

        /// <summary>
        /// 运行用户体验测试
        /// </summary>
        private void RunUserExperienceTests()
        {
            Debug.Log("\n[用户体验测试] Start...");

            var optimizer = FindObjectOfType<UserExperienceOptimizer>();
            if (optimizer != null)
            {
                Debug.Log("  - 执行用户体验优化...");
                optimizer.PerformFullOptimization();

                var status = optimizer.GetStatus();
                Debug.Log($"  - 内存使用: {status.MemoryUsageMB:F1} MB");
                Debug.Log($"  - 场景数量: {status.SceneCount}");
                Debug.Log($"  - 对象数量: {status.ObjectCount}");

                if (status.Suggestions.Count > 0)
                {
                    Debug.Log("  - 优化建议:");
                    foreach (var suggestion in status.Suggestions)
                    {
                        Debug.Log($"    • {suggestion}");
                    }
                }
            }

            Debug.Log("[用户体验测试] 完成");
        }

        /// <summary>
        /// 运行Bug修复测试
        /// </summary>
        private void RunBugFixTests()
        {
            Debug.Log("\n[Bug修复测试] Start...");

            if (bugTracker != null)
            {
                var stats = bugTracker.GetStatistics();
                Debug.Log($"  - 总Bug数: {stats.TotalBugs}");
                Debug.Log($"  - 已修复: {stats.FixedBugs}");
                Debug.Log($"  - 待修复: {stats.PendingBugs}");
                Debug.Log($"  - 未修复: {stats.TotalBugs - stats.FixedBugs}");
                Debug.Log($"  - 严重Bug: {stats.CriticalBugs}");
                Debug.Log($"  - 高优先级Bug: {stats.HighBugs}");
                Debug.Log($"  - 中优先级Bug: {stats.MediumBugs}");
                Debug.Log($"  - 低优先级Bug: {stats.LowBugs}");

                if (stats.TotalBugs > 0)
                {
                    Debug.LogWarning("  ⚠ 发现Bug，请查看bug报告");
                    bugTracker.ShowBugReport();
                }
                else
                {
                    Debug.Log("  ✓ 未发现Bug");
                }
            }

            Debug.Log("[Bug修复测试] 完成");
        }

        /// <summary>
        /// 生成最终测试报告
        /// </summary>
        private void GenerateFinalReport()
        {
            Debug.Log("\n========================================");
            Debug.Log("《时光之痕》最终测试报告");
            Debug.Log("========================================");

            // 测试框架统计
            if (testingFramework != null)
            {
                var frameworkStats = testingFramework.GetStatistics();
                Debug.Log($"\n功能测试:");
                Debug.Log($"  - 通过: {frameworkStats.PassedTests}/{frameworkStats.TotalTests}");
                Debug.Log($"  - 失败: {frameworkStats.FailedTests}/{frameworkStats.TotalTests}");
                Debug.Log($"  - 成功率: {frameworkStats.SuccessRate:F2}%");
            }

            // 性能统计
            if (performanceMonitor != null)
            {
                var perfStats = performanceMonitor.GetStats();
                Debug.Log($"\n性能指标:");
                Debug.Log($"  - 当前FPS: {perfStats.CurrentFps:F1}");
                Debug.Log($"  - 平均FPS: {perfStats.AverageFps:F1}");
                Debug.Log($"  - 内存使用: {perfStats.CurrentMemoryMB:F1} MB");
                Debug.Log($"  - 场景加载时间: {perfStats.CurrentLoadTime:F1}秒");
                Debug.Log($"  - 性能是否达标: {perfStats.IsAcceptable}");
            }

            // Bug统计
            if (bugTracker != null)
            {
                var bugStats = bugTracker.GetStatistics();
                Debug.Log($"\nBug统计:");
                Debug.Log($"  - 总Bug数: {bugStats.TotalBugs}");
                Debug.Log($"  - 已修复: {bugStats.FixedBugs}");
                Debug.Log($"  - 待修复: {bugStats.PendingBugs}");
                Debug.Log($"  - 未修复: {bugStats.TotalBugs - bugStats.FixedBugs}");
            }

            // 兼容性统计
            if (compatibilityTester != null)
            {
                var results = compatibilityTester.GetTestResults();
                int passed = 0;
                foreach (var result in results)
                {
                    if (result.IsSupported)
                    {
                        passed++;
                    }
                }

                float successRate = (float)passed / results.Count * 100f;
                Debug.Log($"\n兼容性测试:");
                Debug.Log($"  - 通过: {passed}/{results.Count}");
                Debug.Log($"  - 成功率: {successRate:F2}%");
            }

            Debug.Log("\n========================================");
            Debug.Log("最终测试完成");
            Debug.Log("========================================");
        }
    }

    // ============ 测试用例 ============

    /// <summary>
    /// 失眠状态系统测试用例
    /// </summary>
    public class InsomniaStateTestCase : TestingFramework.ITestCase
    {
        public string TestName => "失眠状态系统测试";

        public void Test()
        {
            var insomniaController = FindObjectOfType<InsomniaController>();
            if (insomniaController == null)
            {
                throw new System.Exception("InsomniaController未找到");
            }

            // 测试4个等级
            insomniaController.ForceState(InsomniaState.Light);
            Assert.IsTrue(insomniaController.GetCurrentInsomniaValue() >= 0 && insomniaController.GetCurrentInsomniaValue() <= 30);

            insomniaController.ForceState(InsomniaState.Medium);
            Assert.IsTrue(insomniaController.GetCurrentInsomniaValue() >= 30 && insomniaController.GetCurrentInsomniaValue() <= 60);

            insomniaController.ForceState(InsomniaState.Heavy);
            Assert.IsTrue(insomniaController.GetCurrentInsomniaValue() >= 60 && insomniaController.GetCurrentInsomniaValue() <= 90);

            insomniaController.ForceState(InsomniaState.Critical);
            Assert.IsTrue(insomniaController.GetCurrentInsomniaValue() >= 90 && insomniaController.GetCurrentInsomniaValue() <= 100);

            // 测试状态权限
            Assert.IsTrue(insomniaController.IsLightInsomnia());
            Assert.IsTrue(insomniaController.IsMediumInsomnia());
            Assert.IsTrue(insomniaController.IsHeavyInsomnia());
            Assert.IsTrue(insomniaController.IsCriticalInsomnia());

            // 测试恢复功能
            insomniaController.ResetInsomnia();
            Assert.IsTrue(insomniaController.GetCurrentInsomniaValue() == 0);
            Assert.IsTrue(insomniaController.GetCurrentState() == InsomniaState.Light);
        }

    }

    /// <summary>
    /// 时间线切换系统测试用例
    /// </summary>
    public class TimeLineSwitchingTestCase : TestingFramework.ITestCase
    {
        public string TestName => "时间线切换系统测试";

        public void Test()
        {
            var timeLineController = FindObjectOfType<TimeLineController>();
            if (timeLineController == null)
            {
                throw new System.Exception("TimeLineController未找到");
            }

            // 测试4个时间线类型
            timeLineController.ForceSwitchToTimeLine(TimeLineType.Main);
            Assert.AreEqual(TimeLineType.Main, timeLineController.GetCurrentTimeline());

            timeLineController.ForceSwitchToTimeLine(TimeLineType.Past);
            Assert.AreEqual(TimeLineType.Past, timeLineController.GetCurrentTimeline());

            timeLineController.ForceSwitchToTimeLine(TimeLineType.Future);
            Assert.AreEqual(TimeLineType.Future, timeLineController.GetCurrentTimeline());

            timeLineController.ForceSwitchToTimeLine(TimeLineType.Dream);
            Assert.AreEqual(TimeLineType.Dream, timeLineController.GetCurrentTimeline());

            // 测试获取配置
            var config = timeLineController.GetCurrentConfig();
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.name);

            // 测试年份
            Assert.AreEqual(2150, timeLineController.GetCurrentYear());

            // 测试重置
            timeLineController.ResetToMainTimeline();
            Assert.AreEqual(TimeLineType.Main, timeLineController.GetCurrentTimeline());
            Assert.AreEqual(2150, timeLineController.GetCurrentYear());
        }


    }

    /// <summary>
    /// 记忆碎片系统测试用例
    /// </summary>
    public class MemoryShardSystemTestCase : TestingFramework.ITestCase
    {
        public string TestName => "记忆碎片系统测试";

        public void Test()
        {
            var memoryShardSystem = FindObjectOfType<MemoryShardSystem>();
            if (memoryShardSystem == null)
            {
                throw new System.Exception("MemoryShardSystem未找到");
            }

            // 测试添加记忆碎片
            var testShard = new MemoryShard(
                "test-id-1",
                "测试记忆1",
                "这是测试记忆内容1",
                MemoryShardType.Personal,
                "2150",
                "新东京",
                "林晓"
            );

            bool collected = memoryShardSystem.CollectMemoryShard(testShard);
            Assert.IsTrue(collected);
            Assert.IsTrue(memoryShardSystem.HasMemoryShard(testShard));

            // 测试获取记忆碎片
            var foundShard = memoryShardSystem.GetMemoryShardById("test-id-1");
            Assert.IsNotNull(foundShard);
            Assert.AreEqual("测试记忆1", foundShard.title);

            // 测试按类型查询
            var personalShards = memoryShardSystem.GetMemoryShardsByType(MemoryShardType.Personal);
            Assert.IsTrue(personalShards.Count > 0);

            // 测试统计
            Assert.AreEqual(1, memoryShardSystem.GetCollectedCount());

            // 测试显示记忆碎片
            memoryShardSystem.ShowMemoryShard(testShard);
            memoryShardSystem.CloseMemoryViewer();

            // 测试清空
            memoryShardSystem.ClearAllMemoryShards();
            Assert.AreEqual(0, memoryShardSystem.GetCollectedCount());
        }



    }

    /// <summary>
    /// 对话系统测试用例
    /// </summary>
    public class DialogueSystemTestCase : TestingFramework.ITestCase
    {
        public string TestName => "对话系统测试";

        public void Test()
        {
            var dialogueSystem = FindObjectOfType<DialogueSystem>();
            if (dialogueSystem == null)
            {
                throw new System.Exception("DialogueSystem未找到");
            }

            // 测试对话设置
            dialogueSystem.SetDialogue("测试对话内容");
            Assert.AreEqual("测试对话内容", dialogueSystem.GetCurrentDialogue());

            // 测试角色设置
            dialogueSystem.SetCharacter("林晓");
            Assert.AreEqual("林晓", dialogueSystem.GetCurrentCharacter());

            // 测试角色立绘
            dialogueSystem.SetCharacterSprite("character_01");
            Assert.AreEqual("character_01", dialogueSystem.GetCharacterSprite());

            // 测试选择框
            dialogueSystem.SetChoice("选项1", "选项2", "选项3");
            var choices = dialogueSystem.GetChoices();
            Assert.AreEqual(3, choices.Count);
        }



    }

    /// <summary>
    /// 多结局系统测试用例
    /// </summary>
    public class EndingSystemTestCase : TestingFramework.ITestCase
    {
        public string TestName => "多结局系统测试";

        public void Test()
        {
            var endingManager = FindObjectOfType<EndingManager>();
            if (endingManager == null)
            {
                throw new System.Exception("EndingManager未找到");
            }

            // 测试6种结局类型
            endingManager.SetEnding(EndingType.Normal);
            Assert.AreEqual(EndingType.Normal, endingManager.GetCurrentEnding());

            endingManager.SetEnding(EndingType.Heroic);
            Assert.AreEqual(EndingType.Heroic, endingManager.GetCurrentEnding());

            endingManager.SetEnding(EndingType.Tragic);
            Assert.AreEqual(EndingType.Tragic, endingManager.GetCurrentEnding());

            endingManager.SetEnding(EndingType.Open);
            Assert.AreEqual(EndingType.Open, endingManager.GetCurrentEnding());

            endingManager.SetEnding(EndingType.Miracle);
            Assert.AreEqual(EndingType.Miracle, endingManager.GetCurrentEnding());

            endingManager.SetEnding(EndingType.Hidden);
            Assert.AreEqual(EndingType.Hidden, endingManager.GetCurrentEnding());

            // 测试条件检查
            var normalEnding = endingManager.GetEndingConfig(EndingType.Normal);
            Assert.IsNotNull(normalEnding);

            // 测试UI显示
            endingManager.ShowEndingUI();
            endingManager.HideEndingUI();

            // 测试重置
            endingManager.ResetToEndings();
            Assert.AreEqual(EndingType.Normal, endingManager.GetCurrentEnding());
        }


    }

    /// <summary>
    /// 音频系统测试用例
    /// </summary>
    public class AudioSystemTestCase : TestingFramework.ITestCase
    {
        public string TestName => "音频系统测试";

        public void Test()
        {
            var audioManager = FindObjectOfType<AudioManager>();
            if (audioManager == null)
            {
                throw new System.Exception("AudioManager未找到");
            }

            // 测试BGM播放
            audioManager.PlayBGM("bgm_main");
            Assert.IsTrue(audioManager.IsBGMPlaying("bgm_main"));

            // 测试音效播放
            audioManager.PlaySFX("sfx_click");
            Assert.IsTrue(audioManager.IsSFXPlaying("sfx_click"));

            // 测试音量控制
            audioManager.SetMasterVolume(0.5f);
            Assert.AreEqual(0.5f, audioManager.GetMasterVolume());

            // 测试淡入淡出
            audioManager.FadeInBGM(2f);
            audioManager.FadeOutBGM(2f);

            // 测试情感音乐
            var emotionalManager = FindObjectOfType<EmotionalMusicManager>();
            if (emotionalManager != null)
            {
                emotionalManager.SetEmotion(Emotion.Happy);
                Assert.AreEqual(Emotion.Happy, emotionalManager.GetCurrentEmotion());
            }
        }



    }
}
#endif
