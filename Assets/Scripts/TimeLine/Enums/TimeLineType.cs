#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
namespace TimeStamp
{
    /// <summary>
    /// 时间线类型枚举
    /// </summary>
    public enum TimeLineType
    {
        /// <summary>
        /// 主时间线（2150年）
        /// </summary>
        Main = 0,

        /// <summary>
        /// 过去时间线（1950年代）
        /// </summary>
        Past = 1,

        /// <summary>
        /// 未来时间线（2200年）
        /// </summary>
        Future = 2,

        /// <summary>
        /// 梦境世界
        /// </summary>
        Dream = 3
    }

    /// <summary>
    /// 时间线切换方式
    /// </summary>
    public enum TimeLineSwitchMethod
    {
        /// <summary>
        /// 时间碎片切换
        /// </summary>
        TimeFragment = 0,

        /// <summary>
        /// 时空裂缝切换
        /// </summary>
        SpaceTimeCrack = 1,

        /// <summary>
        /// 记忆实体切换
        /// </summary>
        MemoryEntity = 2,

        /// <summary>
        /// 手动切换
        /// </summary>
        Manual = 3
    }

    /// <summary>
    /// 时间线配置
    /// </summary>
    [System.Serializable]
    public class TimeLineConfig
    {
        public TimeLineType type;
        public string name;
        public string year;
        public string description;

        // 视觉效果参数
        public Color primaryColor;
        public float distortionIntensity = 0.3f;
        public float colorShiftIntensity = 0.2f;
        public float timeFlowSpeed = 1.0f;
        public float transitionDuration = 1.0f;

        // 音效参数
        public float ambientVolume = 0.5f;
        public float timeFlowVolume = 0.3f;
        public float crackleVolume = 0.1f;
        public float environmentVolume = 0.4f;

        // 交互参数
        public bool canTravelTimeLines = false;
        public bool requiresSpecialAbility = false;
        public float cooldownTime = 0f; // seconds

        // 记忆碎片相关
        public int memoryShardCount = 0;
        public bool hasTimeFragment = false;
        public int timeFragmentLevel = 0; // 1-3

        public TimeLineConfig(TimeLineType type, string name, string year)
        {
            this.type = type;
            this.name = name;
            this.year = year;
            InitializeParameters();
        }

        private void InitializeParameters()
        {
            switch (type)
            {
                case TimeLineType.Main:
                    name = "主时间线";
                    year = "2150年";
                    description = "新东京，正常时间流逝";
                    primaryColor = new Color(0.8f, 0.8f, 1.0f);
                    distortionIntensity = 0.2f;
                    colorShiftIntensity = 0.1f;
                    timeFlowSpeed = 1.0f;
                    transitionDuration = 1.0f;
                    ambientVolume = 0.5f;
                    timeFlowVolume = 0.2f;
                    crackleVolume = 0.0f;
                    environmentVolume = 0.5f;
                    canTravelTimeLines = true;
                    requiresSpecialAbility = false;
                    cooldownTime = 0f;
                    memoryShardCount = 0;
                    hasTimeFragment = true;
                    timeFragmentLevel = 1;
                    break;

                case TimeLineType.Past:
                    name = "过去时间线";
                    year = "1950年代";
                    description = "历史图书馆、城市街道，复古风格";
                    primaryColor = new Color(1.0f, 0.8f, 0.8f);
                    distortionIntensity = 0.5f;
                    colorShiftIntensity = 0.3f;
                    timeFlowSpeed = 0.8f;
                    transitionDuration = 1.5f;
                    ambientVolume = 0.4f;
                    timeFlowVolume = 0.3f;
                    crackleVolume = 0.2f;
                    environmentVolume = 0.6f;
                    canTravelTimeLines = true;
                    requiresSpecialAbility = true;
                    cooldownTime = 30f;
                    memoryShardCount = 10;
                    hasTimeFragment = true;
                    timeFragmentLevel = 2;
                    break;

                case TimeLineType.Future:
                    name = "未来时间线";
                    year = "2200年";
                    description = "废墟城市、时空裂缝中心，荒凉废墟";
                    primaryColor = new Color(0.6f, 0.6f, 0.8f);
                    distortionIntensity = 0.8f;
                    colorShiftIntensity = 0.5f;
                    timeFlowSpeed = 1.5f;
                    transitionDuration = 2.0f;
                    ambientVolume = 0.3f;
                    timeFlowVolume = 0.5f;
                    crackleVolume = 0.4f;
                    environmentVolume = 0.4f;
                    canTravelTimeLines = true;
                    requiresSpecialAbility = true;
                    cooldownTime = 60f;
                    memoryShardCount = 15;
                    hasTimeFragment = true;
                    timeFragmentLevel = 3;
                    break;

                case TimeLineType.Dream:
                    name = "梦境世界";
                    year = "非线性时间";
                    description = "抽象空间，记忆实体，梦幻美丽";
                    primaryColor = new Color(1.0f, 1.0f, 0.8f);
                    distortionIntensity = 1.0f;
                    colorShiftIntensity = 0.8f;
                    timeFlowSpeed = 0.5f;
                    transitionDuration = 3.0f;
                    ambientVolume = 0.4f;
                    timeFlowVolume = 0.6f;
                    crackleVolume = 0.3f;
                    environmentVolume = 0.5f;
                    canTravelTimeLines = true;
                    requiresSpecialAbility = true;
                    cooldownTime = 0f;
                    memoryShardCount = 20;
                    hasTimeFragment = false;
                    timeFragmentLevel = 0;
                    break;
            }
        }
    }
}
#endif
