#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Dialogue.Examples
{
    /// <summary>
    /// 对话示例 - 展示如何使用对话系统
    /// </summary>
    public class DialogueExample : MonoBehaviour
    {
        [Header("Example Data")]
        [SerializeField] private Sprite characterSprite;
        [SerializeField] private DialogueManager dialogueManager;
        [SerializeField] private KeyChoiceManager keyChoiceManager;

        private void Start()
        {
            // 示例1：简单对话
            ExampleSimpleDialogue();

            // 示例2：选择对话
            ExampleChoiceDialogue();

            // 示例3：关键选择点
            ExampleKeyChoice();

            // 示例4：分支剧情
            ExampleBranchingStory();
        }

        /// <summary>
        /// 示例1：简单对话
        /// </summary>
        private void ExampleSimpleDialogue()
        {
            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine("艾莉丝", characterSprite, "你好，林晓。我一直在等你。"),
                new DialogueLine("艾莉丝", characterSprite, "关于时空裂缝的事情，我们需要谈谈。"),
                new DialogueLine("林晓", null, "我知道，我也有些事情想问你。")
            };

            dialogueManager.StartDialogue(lines);
        }

        /// <summary>
        /// 示例2：选择对话
        /// </summary>
        private void ExampleChoiceDialogue()
        {
            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine("艾莉丝", characterSprite, "我们有两个选择：", true)
                {
                    choices = new List<Choice>
                    {
                        new Choice("直接进入时空研究所", new ChoiceEffect
                        {
                            triggerDialogueEvent = "research_institute_event"
                        }),
                        new Choice("先去咖啡馆休息一下", new ChoiceEffect
                        {
                            insomniaChange = -10f, // 减少失眠值
                            triggerDialogueEvent = "cafe_event"
                        }),
                        new Choice("先回家整理思绪", new ChoiceEffect
                        {
                            insomniaChange = -15f,
                            triggerDialogueEvent = "home_event"
                        })
                    }
                }
            };

            dialogueManager.StartDialogue(lines);
        }

        /// <summary>
        /// 示例3：关键选择点
        /// </summary>
        private void ExampleKeyChoice()
        {
            // 配置关键选择点
            KeyChoiceManager.KeyChoice keyChoice = new KeyChoiceManager.KeyChoice
            {
                choiceId = "first_meeting_choice",
                choiceName = "初次见面",
                branchId = "alice_good",
                dialogueLineIndex = 1,
                choices = new List<Choice>
                {
                    new Choice("礼貌地打招呼", new ChoiceEffect
                    {
                        affectionChange = 10, // 增加好感度
                        triggerBranch = true,
                        branchId = "alice_good"
                    }),
                    new Choice("保持警惕", new ChoiceEffect
                    {
                        affectionChange = -10, // 减少好感度
                        triggerBranch = true,
                        branchId = "alice_bad"
                    }),
                    new Choice("直接询问她的目的", new ChoiceEffect
                    {
                        affectionChange = 5,
                        insomniaChange = 10f,
                        triggerBranch = true,
                        branchId = "alice_good"
                    })
                }
            };

            keyChoiceManager.keyChoices = new List<KeyChoiceManager.KeyChoice> { keyChoice };
        }

        /// <summary>
        /// 示例4：分支剧情
        /// </summary>
        private void ExampleBranchingStory()
        {
            // 进入默认分支
            dialogueManager.EnterBranch("default");

            // 添加对话行
            DialogueLine line1 = new DialogueLine("艾莉丝", characterSprite, "我们需要去调查时空裂缝的源头。");
            dialogueManager.AddDialogueLineToCurrentBranch(line1);

            DialogueLine line2 = new DialogueLine("林晓", null, "我知道，但我需要先准备一下。");
            dialogueManager.AddDialogueLineToCurrentBranch(line2);

            // 添加分支选择
            BranchChoice choice1 = new BranchChoice
            {
                text = "先去图书馆查找资料",
                targetBranchId = "library_search",
                effect = new ChoiceEffect
                {
                    unlockMemoryShard = true,
                    memoryShardId = "library_memory_1"
                }
            };

            dialogueManager.AddBranchChoice(choice1);

            BranchChoice choice2 = new BranchChoice
            {
                text = "直接去找雷恩",
                targetBranchId = "find_rain",
                effect = new ChoiceEffect
                {
                    affectionChange = 5
                }
            };

            dialogueManager.AddBranchChoice(choice2);

            // 进入下一个分支
            dialogueManager.EnterBranch("library_search");
        }

        /// <summary>
        /// 示例5：意识流对话（失眠状态）
        /// </summary>
        private void ExampleStreamOfConsciousnessDialogue()
        {
            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine("", null, "时间...流逝...记忆...碎片...", true)
                {
                    dialogueType = DialogueType.StreamOfConsciousness
                },
                new DialogueLine("", null, "你真的...想清楚了吗？", true)
                {
                    dialogueType = DialogueType.StreamOfConsciousness
                },
                new DialogueLine("", null, "选择...改变...命运...", true)
                {
                    dialogueType = DialogueType.StreamOfConsciousness
                }
            };

            dialogueManager.StartDialogue(lines, DialogueType.StreamOfConsciousness);
        }

        /// <summary>
        /// 示例6：梦境对话（记忆实体）
        /// </summary>
        private void ExampleDreamDialogue()
        {
            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine("记忆实体", null, "我是你的记忆...你想知道什么？", true)
                {
                    dialogueType = DialogueType.Dream
                },
                new DialogueLine("林晓", null, "我想要找回失去的记忆。", true)
                {
                    dialogueType = DialogueType.Dream
                },
                new DialogueLine("记忆实体", null, "有些记忆...并不适合被找回...", true)
                {
                    dialogueType = DialogueType.Dream
                }
            };

            dialogueManager.StartDialogue(lines, DialogueType.Dream);
        }

        /// <summary>
        /// 示例7：战斗对话
        /// </summary>
        private void ExampleCombatDialogue()
        {
            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine("时间领主", null, "你们终于来了...就像我预料的那样！", true)
                {
                    dialogueType = DialogueType.Combat
                },
                new DialogueLine("林晓", null, "我们不会让你破坏时空秩序！", true)
                {
                    dialogueType = DialogueType.Combat
                },
                new DialogueLine("时间领主", null, "那就看看你们有什么本事吧！", true)
                {
                    dialogueType = DialogueType.Combat
                }
            };

            dialogueManager.StartDialogue(lines, DialogueType.Combat);
        }

        /// <summary>
        /// 示例8：信息提示
        /// </summary>
        private void ExampleInfoDialogue()
        {
            List<DialogueLine> lines = new List<DialogueLine>
            {
                new DialogueLine("", null, "【系统提示】你获得了新的记忆碎片：图书馆的记忆", true)
                {
                    dialogueType = DialogueType.Info
                },
                new DialogueLine("", null, "【系统提示】失眠值增加：10%", true)
                {
                    dialogueType = DialogueType.Info
                }
            };

            dialogueManager.StartDialogue(lines, DialogueType.Info);
        }

        /// <summary>
        /// 示例9：完整的剧情流程
        /// </summary>
        private void ExampleCompleteStoryFlow()
        {
            // 场景1：初次见面
            EnterBranch("default");

            List<DialogueLine> meetingLines = new List<DialogueLine>
            {
                new DialogueLine("艾莉丝", characterSprite, "林晓，你来了。")
            };

            dialogueManager.AddDialogueLineToCurrentBranch(meetingLines[0]);
            dialogueManager.StartDialogue(meetingLines);

            // 触发关键选择
            TriggerKeyChoice("first_meeting_choice", 0, () =>
            {
                // 选择后的逻辑
                Debug.Log("Player chose option 0");
            });

            // 场景2：分支选择
            EnterBranch("alice_good");

            List<DialogueLine> branchLines = new List<DialogueLine>
            {
                new DialogueLine("艾莉丝", characterSprite, "很好，你能理解我的担忧。")
            };

            dialogueManager.AddDialogueLineToCurrentBranch(branchLines[0]);

            BranchChoice nextChoice = new BranchChoice
            {
                text = "我们去调查时空研究所",
                targetBranchId = "investigate_institute",
                effect = new ChoiceEffect
                {
                    triggerDialogueEvent = "start_investigation"
                }
            };

            dialogueManager.AddBranchChoice(nextChoice);

            dialogueManager.StartDialogue(branchLines);
        }

        /// <summary>
        /// 触发关键选择
        /// </summary>
        private void TriggerKeyChoice(string choiceId, int choiceIndex, System.Action onComplete)
        {
            keyChoiceManager.TriggerKeyChoice(choiceId, choiceIndex, onComplete);
        }

        /// <summary>
        /// 进入分支
        /// </summary>
        private void EnterBranch(string branchId)
        {
            dialogueManager.EnterBranch(branchId);
        }
    }
}
#endif
