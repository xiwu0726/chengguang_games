#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 时间线管理器
    /// 负责管理当前时间线、时间线配置和时间线切换
    /// </summary>
    public class TimeLineManager : MonoBehaviour
    {
        public static TimeLineManager Instance { get; private set; }

        [Header("时间线配置")]
        [SerializeField] private List<TimeLineConfig> timeLineConfigs;

        [Header("当前状态")]
        [SerializeField] private TimeLineType currentTimeline = TimeLineType.Main;
        [SerializeField] private int currentYear = 2150;

        [Header("冷却管理")]
        [SerializeField] private float currentCooldownTime = 0f;
        [SerializeField] private float cooldownTimer = 0f;

        [Header("事件")]
        public System.Action<TimeLineType, TimeLineType> OnTimeLineChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeTimeLineConfigs();
        }

        private void InitializeTimeLineConfigs()
        {
            if (timeLineConfigs == null || timeLineConfigs.Count == 0)
            {
                timeLineConfigs = new List<TimeLineConfig>
                {
                    new TimeLineConfig(TimeLineType.Main, "主时间线", "2150年"),
                    new TimeLineConfig(TimeLineType.Past, "过去时间线", "1950年代"),
                    new TimeLineConfig(TimeLineType.Future, "未来时间线", "2200年"),
                    new TimeLineConfig(TimeLineType.Dream, "梦境世界", "非线性时间")
                };
            }
        }

        /// <summary>
        /// 获取当前时间线类型
        /// </summary>
        public TimeLineType GetCurrentTimeline()
        {
            return currentTimeline;
        }

        /// <summary>
        /// 获取当前时间线配置
        /// </summary>
        public TimeLineConfig GetCurrentConfig()
        {
            return GetConfig(currentTimeline);
        }

        /// <summary>
        /// 获取指定时间线的配置
        /// </summary>
        public TimeLineConfig GetConfig(TimeLineType type)
        {
            for (int i = 0; i < timeLineConfigs.Count; i++)
            {
                if (timeLineConfigs[i].type == type)
                {
                    return timeLineConfigs[i];
                }
            }
            return timeLineConfigs[0];
        }

        /// <summary>
        /// 获取所有时间线配置
        /// </summary>
        public List<TimeLineConfig> GetAllConfigs()
        {
            return timeLineConfigs;
        }

        /// <summary>
        /// 切换到指定时间线
        /// </summary>
        public bool SwitchToTimeLine(TimeLineType targetTimeline, TimeLineSwitchMethod method = TimeLineSwitchMethod.Manual)
        {
            TimeLineConfig currentConfig = GetCurrentConfig();
            TimeLineConfig targetConfig = GetConfig(targetTimeline);

            // 检查冷却时间
            if (currentCooldownTime > 0f)
            {
                Debug.LogWarning($"[时间线切换] 冷却中，剩余时间: {currentCooldownTime:F1}秒");
                return false;
            }

            // 检查是否需要特殊能力
            if (targetConfig.requiresSpecialAbility)
            {
                // 检查玩家是否具有足够的能力（这里简化处理，实际需要检查玩家属性）
                Debug.LogWarning($"[时间线切换] 需要{targetConfig.name}的特殊能力");
                return false;
            }

            // 检查时间碎片等级
            if (targetConfig.hasTimeFragment && currentConfig.timeFragmentLevel < targetConfig.timeFragmentLevel)
            {
                Debug.LogWarning($"[时间线切换] 需要{targetConfig.timeFragmentLevel}级时间碎片");
                return false;
            }

            // 开始切换
            StartSwitch(targetTimeline, method);

            return true;
        }

        /// <summary>
        /// 开始切换流程
        /// </summary>
        private void StartSwitch(TimeLineType targetTimeline, TimeLineSwitchMethod method)
        {
            TimeLineType oldTimeline = currentTimeline;
            currentTimeline = targetTimeline;

            // 更新年份
            switch (currentTimeline)
            {
                case TimeLineType.Main:
                    currentYear = 2150;
                    break;
                case TimeLineType.Past:
                    currentYear = 1950;
                    break;
                case TimeLineType.Future:
                    currentYear = 2200;
                    break;
                case TimeLineType.Dream:
                    currentYear = 0; // 非线性时间
                    break;
            }

            // 设置冷却时间
            currentCooldownTime = GetConfig(targetTimeline).cooldownTime;

            // 触发事件
            OnTimeLineChanged?.Invoke(oldTimeline, currentTimeline);

            Debug.Log($"[时间线管理器] 切换到 {GetConfig(currentTimeline).name} ({currentYear})");
        }

        /// <summary>
        /// 更新冷却计时器
        /// </summary>
        private void Update()
        {
            if (currentCooldownTime > 0f)
            {
                cooldownTimer += Time.deltaTime;
                if (cooldownTimer >= currentCooldownTime)
                {
                    currentCooldownTime = 0f;
                    cooldownTimer = 0f;
                    Debug.Log("[时间线管理器] 冷却时间结束");
                }
            }
        }

        /// <summary>
        /// 获取当前冷却时间
        /// </summary>
        public float GetCurrentCooldownTime()
        {
            return currentCooldownTime;
        }

        /// <summary>
        /// 获取剩余冷却时间
        /// </summary>
        public float GetRemainingCooldownTime()
        {
            return currentCooldownTime - cooldownTimer;
        }

        /// <summary>
        /// 检查是否可以切换
        /// </summary>
        public bool CanSwitchToTimeLine(TimeLineType targetTimeline)
        {
            if (currentCooldownTime > 0f)
            {
                return false;
            }

            TimeLineConfig targetConfig = GetConfig(targetTimeline);
            if (targetConfig.requiresSpecialAbility)
            {
                return false;
            }

            if (targetConfig.hasTimeFragment)
            {
                TimeLineConfig currentConfig = GetCurrentConfig();
                return currentConfig.timeFragmentLevel >= targetConfig.timeFragmentLevel;
            }

            return true;
        }

        /// <summary>
        /// 检查是否可以穿越时间线
        /// </summary>
        public bool CanTravelTimeLines()
        {
            return GetCurrentConfig().canTravelTimeLines;
        }

        /// <summary>
        /// 获取当前年份
        /// </summary>
        public int GetCurrentYear()
        {
            return currentYear;
        }

        /// <summary>
        /// 设置当前时间线（用于剧情需要）
        /// </summary>
        public void ForceSwitchToTimeLine(TimeLineType targetTimeline)
        {
            TimeLineType oldTimeline = currentTimeline;
            currentTimeline = targetTimeline;

            switch (currentTimeline)
            {
                case TimeLineType.Main:
                    currentYear = 2150;
                    break;
                case TimeLineType.Past:
                    currentYear = 1950;
                    break;
                case TimeLineType.Future:
                    currentYear = 2200;
                    break;
                case TimeLineType.Dream:
                    currentYear = 0;
                    break;
            }

            currentCooldownTime = 0f;
            cooldownTimer = 0f;

            OnTimeLineChanged?.Invoke(oldTimeline, currentTimeline);

            Debug.Log($"[时间线管理器] 强制切换到 {GetConfig(currentTimeline).name}");
        }

        /// <summary>
        /// 重置到主时间线
        /// </summary>
        public void ResetToMainTimeline()
        {
            ForceSwitchToTimeLine(TimeLineType.Main);
        }

        /// <summary>
        /// 重置所有冷却时间
        /// </summary>
        public void ResetCooldowns()
        {
            currentCooldownTime = 0f;
            cooldownTimer = 0f;
        }

        /// <summary>
        /// 获取时间线切换方法
        /// </summary>
        public TimeLineSwitchMethod GetCurrentSwitchMethod()
        {
            // 这里可以根据剧情需要返回不同的切换方法
            // 简化处理，返回手动切换
            return TimeLineSwitchMethod.Manual;
        }
    }
}
#endif
