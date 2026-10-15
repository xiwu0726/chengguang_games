#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Ending.Examples
{
    /// <summary>
    /// 结局示例 - 展示如何使用多结局系统
    /// </summary>
    public class EndingExample : MonoBehaviour
    {
        [Header("Ending Manager")]
        [SerializeField] private EndingManager endingManager;

        [Header("Ending UI")]
        [SerializeField] private EndingUI endingUI;

        [Header("Example Data")]
        [SerializeField] private List<EndingData> endings;

        [Header("Character Stats")]
        [SerializeField] private CharacterAffectionManager affectionManager;
        [SerializeField] private InsomniaController insomniaController;
        [SerializeField] private MemoryShardSystem memoryShardSystem;

        private void Start()
        {
            // 初始化结局数据
            InitializeEndings();

            // 示例1：正常结局
            ExampleNormalEnding();

            // 示例2：英雄结局
            ExampleHeroicEnding();

            // 示例3：悲剧结局
            ExampleTragicEnding();

            // 示例4：开放结局
            ExampleOpenEnding();

            // 示例5：奇迹结局
            ExampleMiracleEnding();

            // 示例6：隐藏结局
            ExampleHiddenEnding();

            // 示例7：结局触发
            ExampleEndingTrigger();
        }

        /// <summary>
        /// 初始化结局数据
        /// </summary>
        private void InitializeEndings()
        {
            // 正常结局
            EndingData normalEnding = new EndingData("normal_ending", "正常结局", EndType.Normal)
            {
                description = "你成功解决了时空裂缝问题，世界恢复了正常。",
                detail = "你与队友们共同努力，最终找到了时空裂缝的源头并成功关闭了它。虽然过程艰难，但你没有放弃，最终保护了这个世界。",
                rating = EndingRating.B,
                difficulty = EndingDifficulty.Normal,
                canRepeat = true,
                triggerCondition = () => CheckNormalEndingConditions()
            };

            endings.Add(normalEnding);

            // 英雄结局
            EndingData heroicEnding = new EndingData("heroic_ending", "英雄结局", EndType.Heroic)
            {
                description = "你牺牲自己拯救了世界，成为了传奇英雄。",
                detail = "在面对最终危机时，你做出了艰难的选择——牺牲自己来拯救世界。你的牺牲感动了所有人，成为了永恒的传奇。",
                rating = EndingRating.S,
                difficulty = EndingDifficulty.Hard,
                canRepeat = false,
                triggerCondition = () => CheckHeroicEndingConditions()
            };

            endings.Add(heroicEnding);

            // 悲剧结局
            EndingData tragicEnding = new EndingData("tragic_ending", "悲剧结局", EndType.Tragic)
            {
                description = "你失败了，世界被毁灭，或者你失去了一切。",
                detail = "你在关键时刻做出了错误的选择，导致时空裂缝失控，世界被毁灭。你失去了所有朋友和亲人，只能独自承受这个后果。",
                rating = EndingRating.F,
                difficulty = EndingDifficulty.VeryHard,
                canRepeat = false,
                triggerCondition = () => CheckTragicEndingConditions()
            };

            endings.Add(tragicEnding);

            // 开放结局
            EndingData openEnding = new EndingData("open_ending", "开放结局", EndType.Open)
            {
                description = "你选择留下，继续探索时空，或者离开这个世界。",
                detail = "在解决完危机后，你面临着选择：是留下来继续探索时空的奥秘，还是离开这个世界去过平静的生活。你的选择将决定你的未来。",
                rating = EndingRating.A,
                difficulty = EndingDifficulty.Normal,
                canRepeat = true,
                triggerCondition = () => CheckOpenEndingConditions()
            };

            endings.Add(openEnding);

            // 奇迹结局
            EndingData miracleEnding = new EndingData("miracle_ending", "奇迹结局", EndType.Miracle)
            {
                description = "你创造了奇迹，拯救了世界并获得了完美结局。",
                detail = "在所有人都认为无望的情况下，你找到了意想不到的方法，成功拯救了世界。你的创造力和勇气感动了所有人，创造了真正的奇迹。",
                rating = EndingRating.S,
                difficulty = EndingDifficulty.VeryHard,
                canRepeat = false,
                triggerCondition = () => CheckMiracleEndingConditions()
            };

            endings.Add(miracleEnding);

            // 隐藏结局
            EndingData hiddenEnding = new EndingData("hidden_ending", "隐藏结局", EndType.Hidden)
            {
                description = "你发现了时空裂缝的真正秘密。",
                detail = "通过收集所有记忆碎片和完成特殊条件，你发现了时空裂缝的真正秘密。这个结局只有真正深入了解游戏的玩家才能达成。",
                rating = EndingRating.S,
                difficulty = EndingDifficulty.Hidden,
                canRepeat = false,
                triggerCondition = () => CheckHiddenEndingConditions()
            };

            endings.Add(hiddenEnding);

            // 设置结局列表
            if (endingManager != null)
            {
                endingManager.endings = endings;
            }
        }

        /// <summary>
        /// 示例1：正常结局
        /// </summary>
        private void ExampleNormalEnding()
        {
            Debug.Log("Example 1: Normal Ending");
            Debug.Log("Conditions: All main branches completed, no major deaths");
        }

        /// <summary>
        /// 示例2：英雄结局
        /// </summary>
        private void ExampleHeroicEnding()
        {
            Debug.Log("Example 2: Heroic Ending");
            Debug.Log("Conditions: All companions alive, sacrifice chosen");
        }

        /// <summary>
        /// 示例3：悲剧结局
        /// </summary>
        private void ExampleTragicEnding()
        {
            Debug.Log("Example 3: Tragic Ending");
            Debug.Log("Conditions: Major companion death, wrong choice at final moment");
        }

        /// <summary>
        /// 示例4：开放结局
        /// </summary>
        private void ExampleOpenEnding()
        {
            Debug.Log("Example 4: Open Ending");
            Debug.Log("Conditions: All companions alive, choose to leave world");
        }

        /// <summary>
        /// 示例5：奇迹结局
        /// </summary>
        private void ExampleMiracleEnding()
        {
            Debug.Log("Example 5: Miracle Ending");
            Debug.Log("Conditions: All companions alive, special items collected, perfect choices");
        }

        /// <summary>
        /// 示例6：隐藏结局
        /// </summary>
        private void ExampleHiddenEnding()
        {
            Debug.Log("Example 6: Hidden Ending");
            Debug.Log("Conditions: Collect all memory shards, achieve 100% affection with all characters");
        }

        /// <summary>
        /// 示例7：结局触发
        /// </summary>
        private void ExampleEndingTrigger()
        {
            // 添加结局触发器
            EndingTriggerData trigger = new EndingTriggerData("normal_ending", "FinalScene", () =>
            {
                // 检查是否满足正常结局条件
                return CheckNormalEndingConditions();
            });

            // 添加触发器
            if (endingManager != null && endingManager.GetComponent<EndingTrigger>() != null)
            {
                endingManager.GetComponent<EndingTrigger>().AddEndingTrigger(trigger);
            }
        }

        /// <summary>
        /// 检查正常结局条件
        /// </summary>
        private bool CheckNormalEndingConditions()
        {
            // 检查所有角色是否存活
            bool allCompanionsAlive = true;
            if (affectionManager != null)
            {
                allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                                     affectionManager.IsCompanionAlive("雷恩") &&
                                     affectionManager.IsCompanionAlive("小雅");
            }

            // 检查记忆碎片收集
            bool allMemoryShardsCollected = true;
            if (memoryShardSystem != null)
            {
                allMemoryShardsCollected = memoryShardSystem.GetMemoryShardCount() >= 50;
            }

            return allCompanionsAlive && allMemoryShardsCollected;
        }

        /// <summary>
        /// 检查英雄结局条件
        /// </summary>
        private bool CheckHeroicEndingConditions()
        {
            // 检查所有角色是否存活
            bool allCompanionsAlive = true;
            if (affectionManager != null)
            {
                allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                                     affectionManager.IsCompanionAlive("雷恩") &&
                                     affectionManager.IsCompanionAlive("小雅");
            }

            // 检查是否选择了牺牲
            bool sacrificeChosen = true; // 实际项目中需要检查玩家选择

            return allCompanionsAlive && sacrificeChosen;
        }

        /// <summary>
        /// 检查悲剧结局条件
        /// </summary>
        private bool CheckTragicEndingConditions()
        {
            // 检查是否失去了重要角色
            bool companionDied = false;
            if (affectionManager != null)
            {
                companionDied = !affectionManager.IsCompanionAlive("艾莉丝") ||
                               !affectionManager.IsCompanionAlive("雷恩") ||
                               !affectionManager.IsCompanionAlive("小雅");
            }

            // 检查是否做出了错误选择
            bool wrongChoice = true; // 实际项目中需要检查玩家选择

            return companionDied && wrongChoice;
        }

        /// <summary>
        /// 检查开放结局条件
        /// </summary>
        private bool CheckOpenEndingConditions()
        {
            // 检查所有角色是否存活
            bool allCompanionsAlive = true;
            if (affectionManager != null)
            {
                allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                                     affectionManager.IsCompanionAlive("雷恩") &&
                                     affectionManager.IsCompanionAlive("小雅");
            }

            // 检查是否选择了离开
            bool chooseToLeave = true; // 实际项目中需要检查玩家选择

            return allCompanionsAlive && chooseToLeave;
        }

        /// <summary>
        /// 检查奇迹结局条件
        /// </summary>
        private bool CheckMiracleEndingConditions()
        {
            // 检查所有角色是否存活
            bool allCompanionsAlive = true;
            if (affectionManager != null)
            {
                allCompanionsAlive = affectionManager.IsCompanionAlive("艾莉丝") &&
                                     affectionManager.IsCompanionAlive("雷恩") &&
                                     affectionManager.IsCompanionAlive("小雅");
            }

            // 检查是否收集了所有特殊物品
            bool allItemsCollected = true; // 实际项目中需要检查物品收集

            // 检查是否做出了完美选择
            bool perfectChoices = true; // 实际项目中需要检查玩家选择

            return allCompanionsAlive && allItemsCollected && perfectChoices;
        }

        /// <summary>
        /// 检查隐藏结局条件
        /// </summary>
        private bool CheckHiddenEndingConditions()
        {
            // 检查是否收集了所有记忆碎片
            bool allMemoryShardsCollected = true;
            if (memoryShardSystem != null)
            {
                allMemoryShardsCollected = memoryShardSystem.GetMemoryShardCount() >= 100;
            }

            // 检查是否与所有角色好感度达到100
            bool allAffection100 = true;
            if (affectionManager != null)
            {
                allAffection100 = affectionManager.GetAffection("艾莉丝") >= 100 &&
                                  affectionManager.GetAffection("雷恩") >= 100 &&
                                  affectionManager.GetAffection("小雅") >= 100;
            }

            return allMemoryShardsCollected && allAffection100;
        }

        /// <summary>
        /// 手动触发结局
        /// </summary>
        public void TriggerEnding(string endingId)
        {
            if (endingManager != null)
            {
                endingManager.TriggerEnding(endingId);
            }
        }

        /// <summary>
        /// 显示结局
        /// </summary>
        public void ShowEnding(string endingId)
        {
            EndingData ending = endingManager?.GetEnding(endingId);
            if (ending != null)
            {
                endingUI?.ShowEnding(ending);
            }
        }
    }
}
#endif
