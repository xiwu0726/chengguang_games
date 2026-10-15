#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Ending.Data
{
    /// <summary>
    /// 结局数据结构
    /// </summary>
    [System.Serializable]
    public class EndingData
    {
        /// <summary>
        /// 结局ID
        /// </summary>
        public string endingId;

        /// <summary>
        /// 结局名称
        /// </summary>
        public string endingName;

        /// <summary>
        /// 结局类型
        /// </summary>
        public EndType endType;

        /// <summary>
        /// 结局评级
        /// </summary>
        public EndingRating rating;

        /// <summary>
        /// 结局难度
        /// </summary>
        public EndingDifficulty difficulty;

        /// <summary>
        /// 结局描述
        /// </summary>
        public string description;

        /// <summary>
        /// 结局详细信息
        /// </summary>
        public string detail;

        /// <summary>
        /// 结局触发条件
        /// </summary>
        public List<EndingCondition> conditions;

        /// <summary>
        /// 结局CG集合
        /// </summary>
        public List<Sprite> cgCollection;

        /// <summary>
        /// 结局音乐
        /// </summary>
        public string backgroundMusic;

        /// <summary>
        /// 结局音效
        /// </summary>
        public List<string> soundEffects;

        /// <summary>
        /// 是否可重复达成
        /// </summary>
        public bool canRepeat = false;

        /// <summary>
        /// 是否已达成
        /// </summary>
        public bool isAchieved = false;

        /// <summary>
        /// 达成次数
        /// </summary>
        public int achieveCount = 0;

        /// <summary>
        /// 解锁条件
        /// </summary>
        public List<string> unlockConditions;

        /// <summary>
        /// 结局触发对话
        /// </summary>
        public List<DialogueLine> triggerDialogue;

        /// <summary>
        /// 触发条件
        /// </summary>
        public System.Func<bool> triggerCondition;

        public EndingData()
        {
            endingId = "";
            endingName = "";
            endType = EndType.Normal;
            rating = EndingRating.B;
            difficulty = EndingDifficulty.Normal;
            description = "";
            detail = "";
            conditions = new List<EndingCondition>();
            cgCollection = new List<Sprite>();
            backgroundMusic = "";
            soundEffects = new List<string>();
            canRepeat = false;
            isAchieved = false;
            achieveCount = 0;
            unlockConditions = new List<string>();
            triggerDialogue = new List<DialogueLine>();
        }

        public EndingData(string id, string name, EndType type)
        {
            this.endingId = id;
            this.endingName = name;
            this.endType = type;
            this.rating = EndingRating.B;
            this.difficulty = EndingDifficulty.Normal;
            this.description = "";
            this.detail = "";
            this.conditions = new List<EndingCondition>();
            this.cgCollection = new List<Sprite>();
            this.backgroundMusic = "";
            this.soundEffects = new List<string>();
            this.canRepeat = false;
            this.isAchieved = false;
            this.achieveCount = 0;
            this.unlockConditions = new List<string>();
            this.triggerDialogue = new List<DialogueLine>();
        }
    }

    /// <summary>
    /// 结局条件
    /// </summary>
    [System.Serializable]
    public class EndingCondition
    {
        /// <summary>
        /// 条件类型
        /// </summary>
        public ConditionType conditionType;

        /// <summary>
        /// 条件ID
        /// </summary>
        public string conditionId;

        /// <summary>
        /// 目标值
        /// </summary>
        public int targetValue;

        /// <summary>
        /// 操作符
        /// </summary>
        public ConditionOperator operatorType;

        /// <summary>
        /// 检查条件
        /// </summary>
        public System.Func<bool> checkCondition;

        public EndingCondition()
        {
            conditionType = ConditionType.Unknown;
            conditionId = "";
            targetValue = 0;
            operatorType = ConditionOperator.Equal;
            checkCondition = null;
        }

        public enum ConditionType
        {
            Unknown,
            Affection,        // 角色好感度
            Insomnia,         // 失眠值
            MemoryShard,      // 记忆碎片数量
            Branch,           // 剧情分支
            TimeLine,         // 时间线
            Item,             // 物品收集
            Achievement,      // 成就
            Choice,           // 选择
            None              // 无条件
        }

        public enum ConditionOperator
        {
            Equal,            // 等于
            NotEqual,         // 不等于
            Greater,          // 大于
            GreaterOrEqual,   // 大于等于
            Less,             // 小于
            LessOrEqual,      // 小于等于
            Contains          // 包含
        }
    }

    /// <summary>
    /// 结局触发数据
    /// </summary>
    [System.Serializable]
    public class EndingTriggerData
    {
        /// <summary>
        /// 结局ID
        /// </summary>
        public string endingId;

        /// <summary>
        /// 触发场景
        /// </summary>
        public string triggerScene;

        /// <summary>
        /// 触发时间
        /// </summary>
        public string triggerTime;

        /// <summary>
        /// 触发条件
        /// </summary>
        public System.Func<bool> triggerCondition;

        /// <summary>
        /// 触发后执行
        /// </summary>
        public System.Action onTrigger;

        /// <summary>
        /// 触发后清理
        /// </summary>
        public System.Action onCleanup;

        public EndingTriggerData()
        {
            endingId = "";
            triggerScene = "";
            triggerTime = "";
            triggerCondition = null;
            onTrigger = null;
            onCleanup = null;
        }

        public EndingTriggerData(string endingId, string scene, System.Func<bool> condition)
        {
            this.endingId = endingId;
            this.triggerScene = scene;
            this.triggerTime = "";
            this.triggerCondition = condition;
            this.onTrigger = null;
            this.onCleanup = null;
        }
    }
}
#endif
