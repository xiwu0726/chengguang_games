#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
namespace TimeOfFragments.Ending.Enums
{
    /// <summary>
    /// 结局类型枚举
    /// </summary>
    public enum EndType
    {
        /// <summary>
        /// 正常结局 - 主角成功解决时空裂缝问题，世界恢复正常
        /// </summary>
        Normal,

        /// <summary>
        /// 英雄结局 - 主角牺牲自己拯救世界，成为传奇英雄
        /// </summary>
        Heroic,

        /// <summary>
        /// 悲剧结局 - 主角失败，世界被毁灭，或主角失去一切
        /// </summary>
        Tragic,

        /// <summary>
        /// 开放结局 - 主角选择留下，继续探索时空，或离开世界
        /// </summary>
        Open,

        /// <summary>
        /// 奇迹结局 - 主角创造奇迹，拯救世界并获得完美结局
        /// </summary>
        Miracle,

        /// <summary>
        /// 隐藏结局 - 需要满足特定条件才能解锁
        /// </summary>
        Hidden
    }

    /// <summary>
    /// 结局评级
    /// </summary>
    public enum EndingRating
    {
        /// <summary>
        /// S级 - 完美结局
        /// </summary>
        S,

        /// <summary>
        ///A级 - 优秀结局
        /// </summary>
        A,

        /// <summary>
        /// B级 - 良好结局
        /// </summary>
        B,

        /// <summary>
        /// C级 - 一般结局
        /// </summary>
        C,

        /// <summary>
        /// D级 - 差结局
        /// </summary>
        D,

        /// <summary>
        /// F级 - 失败结局
        /// </summary>
        F
    }

    /// <summary>
    /// 结局难度
    /// </summary>
    public enum EndingDifficulty
    {
        /// <summary>
        /// 简单 - 容易达成
        /// </summary>
        Easy,

        /// <summary>
        /// 普通 - 正常难度
        /// </summary>
        Normal,

        /// <summary>
        /// 困难 - 较难达成
        /// </summary>
        Hard,

        /// <summary>
        /// 极难 - 非常难达成
        /// </summary>
        VeryHard,

        /// <summary>
        /// 隐藏 - 需要特殊条件
        /// </summary>
        Hidden
    }
}
#endif
