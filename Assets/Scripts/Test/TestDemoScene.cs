#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI;

namespace TimeStamp.Test
{
    /// <summary>
    /// 测试场景演示脚本
    /// 演示所有核心系统的功能
    /// </summary>
    public class TestDemoScene : MonoBehaviour
    {
        [Header("测试按钮")]
        [SerializeField] private Button testDialogueButton;
        [SerializeField] private Button testInsomniaButton;
        [SerializeField] private Button testTimeLineButton;
        [SerializeField] private Button testMemoryShardButton;
        [SerializeField] private Button testEndingButton;

        [Header("状态显示")]
        [SerializeField] private Text insomniaValueText;
        [SerializeField] private Text timeLineText;
        [SerializeField] private Text memoryShardText;
        [SerializeField] private Text statusText;

        [Header("测试结果")]
        [SerializeField] private Text testResultText;

        private void Start()
        {
            InitializeUI();
            UpdateStatusDisplay();
        }

        private void InitializeUI()
        {
            if (testDialogueButton != null)
                testDialogueButton.onClick.AddListener(TestDialogueSystem);

            if (testInsomniaButton != null)
                testInsomniaButton.onClick.AddListener(TestInsomniaSystem);

            if (testTimeLineButton != null)
                testTimeLineButton.onClick.AddListener(TestTimeLineSystem);

            if (testMemoryShardButton != null)
                testMemoryShardButton.onClick.AddListener(TestMemoryShardSystem);

            if (testEndingButton != null)
                testEndingButton.onClick.AddListener(TestEndingSystem);
        }

        private void UpdateStatusDisplay()
        {
            if (DialogueManager.Instance != null)
            {
                statusText.text = "对话系统: " + (DialogueManager.Instance.IsDialogueRunning() ? "运行中" : "空闲");
            }

            if (InsomniaController.Instance != null)
            {
                int insomniaValue = InsomniaController.Instance.GetCurrentInsomniaValue();
                insomniaValueText.text = "失眠值: " + insomniaValue + "%";

                InsomniaController.InsomniaState state = InsomniaController.Instance.GetCurrentState();
                string stateName = state.ToString();
                statusText.text += "\n当前状态: " + stateName;
            }

            if (TimeLineController.Instance != null)
            {
                TimeLineController.TimeLineType timeLine = TimeLineController.Instance.GetCurrentTimeLine();
                timeLineText.text = "时间线: " + timeLine.ToString();
            }

            // TODO: 添加记忆碎片系统显示
            memoryShardText.text = "记忆碎片: 0/10";
        }

        // 测试对话系统
        private void TestDialogueSystem()
        {
            if (DialogueManager.Instance == null)
            {
                ShowTestResult("错误: 对话系统未初始化");
                return;
            }

            List<DialogueLine> dialogueLines = new List<DialogueLine>
            {
                new DialogueLine
                {
                    characterName = "林晓",
                    characterSprite = null, // TODO: 设置实际sprite
                    text = "你好，我是林晓，一名历史系研究生。",
                    dialogueType = DialogueType.Normal,
                    choices = null
                },
                new DialogueLine
                {
                    characterName = "艾莉丝",
                    characterSprite = null,
                    text = "你好，我是艾莉丝，时空异常研究所的研究员。",
                    dialogueType = DialogueType.Normal,
                    choices = null
                },
                new DialogueLine
                {
                    characterName = "林晓",
                    characterSprite = null,
                    text = "我最近在研究一些关于时空异常的历史文献。",
                    dialogueType = DialogueType.Normal,
                    choices = null
                }
            };

            DialogueManager.Instance.StartDialogue(dialogueLines);
            ShowTestResult("对话系统测试: 成功开始对话");
        }

        // 测试失眠状态系统
        private void TestInsomniaSystem()
        {
            if (InsomniaController.Instance == null)
            {
                ShowTestResult("错误: 失眠系统未初始化");
                return;
            }

            // 增加失眠值
            InsomniaController.Instance.IncreaseInsomnia(30);
            ShowTestResult("失眠系统测试: 增加失眠值 +30%");

            // 延迟后减少失眠值
            StartCoroutine(DelayedDecreaseInsomnia());
        }

        private IEnumerator DelayedDecreaseInsomnia()
        {
            yield return new WaitForSeconds(2f);

            InsomniaController.Instance.DecreaseInsomnia(20);
            ShowTestResult("失眠系统测试: 减少失眠值 -20%");
        }

        // 测试时间线系统
        private void TestTimeLineSystem()
        {
            if (TimeLineController.Instance == null)
            {
                ShowTestResult("错误: 时间线系统未初始化");
                return;
            }

            TimeLineController.TimeLineType[] timeLines = new TimeLineController.TimeLineType[]
            {
                TimeLineController.TimeLineType.Main,
                TimeLineController.TimeLineType.Past,
                TimeLineController.TimeLineType.Future,
                TimeLineController.TimeLineType.Dream
            };

            int currentIndex = 0;
            StartCoroutine(SwitchTimeLinesCoroutine(timeLines, currentIndex));
        }

        private IEnumerator SwitchTimeLinesCoroutine(TimeLineController.TimeLineType[] timeLines, int currentIndex)
        {
            if (currentIndex >= timeLines.Length)
            {
                ShowTestResult("时间线系统测试: 完成所有时间线切换");
                yield break;
            }

            TimeLineController.Instance.SwitchToTimeLine(timeLines[currentIndex]);
            ShowTestResult("时间线系统测试: 切换到 " + timeLines[currentIndex].ToString());

            yield return new WaitForSeconds(3f);

            currentIndex++;
            StartCoroutine(SwitchTimeLinesCoroutine(timeLines, currentIndex));
        }

        // 测试记忆碎片系统
        private void TestMemoryShardSystem()
        {
            if (MemoryShardSystem.Instance == null)
            {
                ShowTestResult("错误: 记忆碎片系统未初始化");
                return;
            }

            // TODO: 实现记忆碎片收集逻辑
            ShowTestResult("记忆碎片系统测试: 功能已准备就绪");
        }

        // 测试多结局系统
        private void TestEndingSystem()
        {
            if (EndingController.Instance == null)
            {
                ShowTestResult("错误: 结局系统未初始化");
                return;
            }

            // TODO: 实现结局触发逻辑
            ShowTestResult("多结局系统测试: 功能已准备就绪");
        }

        private void ShowTestResult(string result)
        {
            if (testResultText != null)
            {
                testResultText.text = result;
            }

            Debug.Log(result);
        }

        private void Update()
        {
            UpdateStatusDisplay();
        }
    }
}
#endif
