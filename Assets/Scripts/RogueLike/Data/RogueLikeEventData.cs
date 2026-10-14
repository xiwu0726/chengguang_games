using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Data
{
    /// <summary>
    /// 随机事件数据结构
    /// </summary>
    [System.Serializable]
    public class RogueLikeEventData
    {
        /// <summary>
        /// 事件ID
        /// </summary>
        public string eventId;

        /// <summary>
        /// 事件名称
        /// </summary>
        public string eventName;

        /// <summary>
        /// 事件类型
        /// </summary>
        public RogueLikeEventType eventType;

        /// <summary>
        /// 事件稀有度
        /// </summary>
        public EventRarity rarity;

        /// <summary>
        /// 事件描述
        /// </summary>
        public string description;

        /// <summary>
        /// 事件详细信息
        /// </summary>
        public string detail;

        /// <summary>
        /// 触发条件
        /// </summary>
        public List<EventCondition> triggerConditions;

        /// <summary>
        /// 触发概率（百分比）
        /// </summary>
        public float triggerProbability;

        /// <summary>
        /// 触发时机
        /// </summary>
        public EventTriggerCondition triggerTiming;

        /// <summary>
        /// 事件持续时间
        /// </summary>
        public float duration;

        /// <summary>
        /// 事件效果
        /// </summary>
        public List<RogueLikeEffect> effects;

        /// <summary>
        /// 事件选择选项
        /// </summary>
        public List<RogueLikeChoice> choices;

        /// <summary>
        /// 事件CG
        /// </summary>
        public List<Sprite> cgCollection;

        /// <summary>
        /// 事件背景音乐
        /// </summary>
        public string backgroundMusic;

        /// <summary>
        /// 是否可重复触发
        /// </summary>
        public bool canRepeat = false;

        /// <summary>
        /// 触发次数限制
        /// </summary>
        public int maxTriggerCount = 1;

        /// <summary>
        /// 已触发次数
        /// </summary>
        public int currentTriggerCount = 0;

        /// <summary>
        /// 解锁条件
        /// </summary>
        public List<string> unlockConditions;

        /// <summary>
        /// 触发对话
        /// </summary>
        public List<DialogueLine> triggerDialogue;

        /// <summary>
        /// 触发条件
        /// </summary>
        public System.Func<bool> triggerCondition;

        public RogueLikeEventData()
        {
            eventId = "";
            eventName = "";
            eventType = RogueLikeEventType.Normal;
            rarity = EventRarity.Common;
            description = "";
            detail = "";
            triggerConditions = new List<EventCondition>();
            triggerProbability = 10f;
            triggerTiming = EventTriggerCondition.Always;
            duration = 0f;
            effects = new List<RogueLikeEffect>();
            choices = new List<RogueLikeChoice>();
            cgCollection = new List<Sprite>();
            backgroundMusic = "";
            canRepeat = false;
            maxTriggerCount = 1;
            currentTriggerCount = 0;
            unlockConditions = new List<string>();
            triggerDialogue = new List<DialogueLine>();
        }

        public RogueLikeEventData(string id, string name, RogueLikeEventType type)
        {
            this.eventId = id;
            this.eventName = name;
            this.eventType = type;
            this.rarity = EventRarity.Common;
            this.description = "";
            this.detail = "";
            this.triggerConditions = new List<EventCondition>();
            this.triggerProbability = 10f;
            this.triggerTiming = EventTriggerCondition.Always;
            this.duration = 0f;
            this.effects = new List<RogueLikeEffect>();
            this.choices = new List<RogueLikeChoice>();
            this.cgCollection = new List<Sprite>();
            this.backgroundMusic = "";
            this.canRepeat = false;
            this.maxTriggerCount = 1;
            this.currentTriggerCount = 0;
            this.unlockConditions = new List<string>();
            this.triggerDialogue = new List<DialogueLine>();
        }
    }

    /// <summary>
    /// 事件条件
    /// </summary>
    [System.Serializable]
    public class EventCondition
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

        public EventCondition()
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
            InsomniaLevel,
            TimeLine,
            MemoryShardCount,
            BranchCount,
            Affinity,
            PlayCount,
            None
        }

        public enum ConditionOperator
        {
            Equal,
            NotEqual,
            Greater,
            GreaterOrEqual,
            Less,
            LessOrEqual,
            Contains
        }
    }

    /// <summary>
    /// 事件效果
    /// </summary>
    [System.Serializable]
    public class RogueLikeEffect
    {
        /// <summary>
        /// 效果类型
        /// </summary>
        public EffectType effectType;

        /// <summary>
        /// 效果值
        /// </summary>
        public float effectValue;

        /// <summary>
        /// 持续时间
        /// </summary>
        public float duration;

        /// <summary>
        /// 是否永久效果
        /// </summary>
        public bool isPermanent = false;

        /// <summary>
        /// 效果描述
        /// </summary>
        public string description;

        /// <summary>
        /// 效果回调
        /// </summary>
        public System.Action<RogueLikeEffect> onEffectApply;

        public RogueLikeEffect()
        {
            effectType = EffectType.Unknown;
            effectValue = 0f;
            duration = 0f;
            isPermanent = false;
            description = "";
            onEffectApply = null;
        }

        public enum EffectType
        {
            Unknown,
            InsomniaIncrease,
            InsomniaDecrease,
            MemoryShardUnlock,
            TimeShardUnlock,
            AffinityIncrease,
            AffinityDecrease,
            BranchUnlock,
            TimeLineUnlock,
            ItemUnlock,
            AchievementUnlock,
            HiddenEventUnlock
        }
    }

    /// <summary>
    /// 事件选择选项
    /// </summary>
    [System.Serializable]
    public class RogueLikeChoice
    {
        /// <summary>
        /// 选项文本
        /// </summary>
        public string text;

        /// <summary>
        /// 选项效果
        /// </summary>
        public List<RogueLikeEffect> effects;

        /// <summary>
        /// 是否已选择
        /// </summary>
        public bool isSelected = false;

        /// <summary>
        /// 触发回调
        /// </summary>
        public System.Action<RogueLikeChoice> onSelected;

        public RogueLikeChoice()
        {
            text = "";
            effects = new List<RogueLikeEffect>();
            isSelected = false;
            onSelected = null;
        }

        public RogueLikeChoice(string text, List<RogueLikeEffect> effects)
        {
            this.text = text;
            this.effects = effects;
            this.isSelected = false;
            this.onSelected = null;
        }
    }
}
