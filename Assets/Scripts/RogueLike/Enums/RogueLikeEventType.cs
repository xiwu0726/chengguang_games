#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
namespace TimeOfFragments.RogueLike.Enums
{
    /// <summary>
    /// 随机事件类型枚举
    /// </summary>
    public enum RogueLikeEventType
    {
        /// <summary>
        /// 正常事件 - 对剧情有轻微影响
        /// </summary>
        Normal,

        /// <summary>
        /// 重要事件 - 对剧情有重大影响
        /// </summary>
        Important,

        /// <summary>
        /// 随机事件 - 完全随机的事件
        /// </summary>
        Random,

        /// <summary>
        /// 危险事件 - 可能带来负面影响
        /// </summary>
        Dangerous,

        /// <summary>
        /// 有益事件 - 明显的正面效果
        /// </summary>
        Beneficial,

        /// <summary>
        /// 特殊事件 - 特殊的剧情事件
        /// </summary>
        Special,

        /// <summary>
        /// 隐藏事件 - 需要特定条件触发
        /// </summary>
        Hidden
    }

    /// <summary>
    /// 事件触发条件
    /// </summary>
    public enum EventTriggerCondition
    {
        /// <summary>
        /// 无条件触发
        /// </summary>
        Always,

        /// <summary>
        /// 失眠值达到特定值
        /// </summary>
        InsomniaLevel,

        /// <summary>
        /// 时间线切换后触发
        /// </summary>
        AfterTimeLineSwitch,

        /// <summary>
        /// 收集记忆碎片后触发
        /// </summary>
        AfterMemoryShardCollection,

        /// <summary>
        /// 角色好感度达到特定值
        /// </summary>
        AfterAffinityChange,

        /// <summary>
        /// 剧情分支切换后触发
        /// </summary>
        AfterBranchChange,

        /// <summary>
        /// 随机概率触发
        /// </summary>
        RandomProbability
    }

    /// <summary>
    /// 事件稀有度
    /// </summary>
    public enum EventRarity
    {
        /// <summary>
        /// 常见事件 - 触发概率高
        /// </summary>
        Common,

        /// <summary>
        /// 稀有事件 - 触发概率中等
        /// </summary>
        Rare,

        /// <summary>
        /// 传奇事件 - 触发概率低
        /// </summary>
        Legendary,

        /// <summary>
        /// 传说事件 - 触发概率极低
        /// </summary>
        Mythic
    }
}
#endif
