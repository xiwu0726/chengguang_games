namespace TimeOfFragments.Dialogue.Enums
{
    /// <summary>
    /// 对话类型枚举
    /// </summary>
    public enum DialogueType
    {
        /// <summary>
        /// 普通对话 - 正常的对话交流
        /// </summary>
        Normal,

        /// <summary>
        /// 选择对话 - 需要玩家做出选择
        /// </summary>
        Choice,

        /// <summary>
        /// 意识流对话 - 失眠状态下的特殊对话
        /// </summary>
        StreamOfConsciousness,

        /// <summary>
        /// 梦境对话 - 来自记忆实体的对话
        /// </summary>
        Dream,

        /// <summary>
        /// 信息提示 - 系统信息展示
        /// </summary>
        Info,

        /// <summary>
        /// 战斗对话 - 战斗场景中的对话
        /// </summary>
        Combat
    }
}
