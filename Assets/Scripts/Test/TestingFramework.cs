using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TimeStamp
{
    /// <summary>
    /// 测试框架主控制器
    /// 管理所有测试功能
    /// </summary>
    public class TestingFramework : MonoBehaviour
    {
        public static TestingFramework Instance { get; private set; }

        [Header("测试管理")]
        [SerializeField] private bool autoRunTestsOnStart = false;
        [SerializeField] private float autoRunDelay = 5f;

        [Header("测试报告")]
        [SerializeField] private bool showTestReportOnComplete = true;
        [SerializeField] private bool saveTestReportToFile = true;

        private List<ITestCase> testCases = new List<ITestCase>();
        private int passedTests = 0;
        private int failedTests = 0;
        private int totalTests = 0;

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
            if (autoRunTestsOnStart)
            {
                Invoke("RunAllTests", autoRunDelay);
            }
        }

        /// <summary>
        /// 添加测试用例
        /// </summary>
        public void AddTestCase(ITestCase testCase)
        {
            if (testCase != null)
            {
                testCases.Add(testCase);
                totalTests++;
                Debug.Log($"[测试框架] 添加测试用例: {testCase.TestName}");
            }
        }

        /// <summary>
        /// 运行所有测试
        /// </summary>
        public void RunAllTests()
        {
            Debug.Log("[测试框架] 开始运行所有测试...");

            passedTests = 0;
            failedTests = 0;

            foreach (var testCase in testCases)
            {
                try
                {
                    testCase.Test();
                    passedTests++;
                    Debug.Log($"[测试框架] ✓ {testCase.TestName} 通过");
                }
                catch (Exception e)
                {
                    failedTests++;
                    Debug.LogError($"[测试框架] ✗ {testCase.TestName} 失败: {e.Message}");
                }
            }

            Debug.Log($"[测试框架] 测试完成: {passedTests}/{totalTests} 通过, {failedTests}/{totalTests} 失败");

            if (showTestReportOnComplete)
            {
                ShowTestReport();
            }

            if (saveTestReportToFile)
            {
                SaveTestReport();
            }
        }

        /// <summary>
        /// 运行单个测试
        /// </summary>
        public bool RunTestCase(string testName)
        {
            var testCase = testCases.Find(t => t.TestName == testName);
            if (testCase == null)
            {
                Debug.LogError($"[测试框架] 未找到测试用例: {testName}");
                return false;
            }

            try
            {
                testCase.Test();
                Debug.Log($"[测试框架] ✓ {testName} 通过");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[测试框架] ✗ {testName} 失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 显示测试报告
        /// </summary>
        private void ShowTestReport()
        {
            string report = $@"
========================================
         《时光之痕》测试报告
========================================

总测试数: {totalTests}
通过: {passedTests}
失败: {failedTests}
成功率: {(totalTests > 0 ? (passedTests * 100f / totalTests).ToString("F2") + "%" : "0%")}

详细结果:
";

            foreach (var testCase in testCases)
            {
                report += $"\n{testCase.TestName}: {(passedTests > 0 && testCases.IndexOf(testCase) < passedTests ? "通过" : "失败")}";
            }

            Debug.Log(report);

            // 在控制台显示
            Console.WriteLine(report);
        }

        /// <summary>
        /// 保存测试报告到文件
        /// </summary>
        private void SaveTestReport()
        {
            string report = $@"
《时光之痕》测试报告
生成时间: {DateTime.Now}

总测试数: {totalTests}
通过: {passedTests}
失败: {failedTests}
成功率: {(totalTests > 0 ? (passedTests * 100f / totalTests).ToString("F2") + "%" : "0%")}

详细结果:
";

            foreach (var testCase in testCases)
            {
                report += $"\n{testCase.TestName}: {(passedTests > 0 && testCases.IndexOf(testCase) < passedTests ? "通过" : "失败")}";
            }

            string filePath = System.IO.Path.Combine(Application.persistentDataPath, "TestReport.txt");
            System.IO.File.WriteAllText(filePath, report);

            Debug.Log($"[测试框架] 测试报告已保存到: {filePath}");
        }

        /// <summary>
        /// 获取测试统计信息
        /// </summary>
        public TestStatistics GetStatistics()
        {
            return new TestStatistics
            {
                TotalTests = totalTests,
                PassedTests = passedTests,
                FailedTests = failedTests,
                SuccessRate = totalTests > 0 ? (passedTests * 100f / totalTests) : 0f
            };
        }

        /// <summary>
        /// 重置测试状态
        /// </summary>
        public void Reset()
        {
            testCases.Clear();
            passedTests = 0;
            failedTests = 0;
            totalTests = 0;
            Debug.Log("[测试框架] 测试状态已重置");
        }
    }

    /// <summary>
    /// 测试统计信息
    /// </summary>
    public class TestStatistics
    {
        public int TotalTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public float SuccessRate { get; set; }
    }

    /// <summary>
    /// 测试用例接口
    /// </summary>
    public interface ITestCase
    {
        string TestName { get; }
        void Test();
    }
}
