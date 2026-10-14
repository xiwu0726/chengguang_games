using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 时间线切换器
    /// 处理具体的时间线切换逻辑和动画
    /// </summary>
    public class TimeLineSwitcher : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private TimeLineManager timeLineManager;
        [SerializeField] private TimeLineTransitionEffect transitionEffect;
        [SerializeField] private InsomniaController insomniaController;

        [Header("切换动画")]
        [SerializeField] private float defaultTransitionDuration = 1.0f;
        [SerializeField] private float pastTransitionDuration = 1.5f;
        [SerializeField] private float futureTransitionDuration = 2.0f;
        [SerializeField] private float dreamTransitionDuration = 3.0f;

        [Header("切换方式")]
        [SerializeField] private bool allowTimeFragmentSwitch = true;
        [SerializeField] private bool allowCrackSwitch = true;
        [SerializeField] private bool allowMemoryEntitySwitch = true;

        private void Awake()
        {
            if (timeLineManager == null)
            {
                timeLineManager = FindObjectOfType<TimeLineManager>();
            }

            if (transitionEffect == null)
            {
                transitionEffect = FindObjectOfType<TimeLineTransitionEffect>();
            }

            if (insomniaController == null)
            {
                insomniaController = FindObjectOfType<InsomniaController>();
            }
        }

        /// <summary>
        /// 切换到指定时间线
        /// </summary>
        public bool SwitchToTimeLine(TimeLineType targetTimeline, TimeLineSwitchMethod method = TimeLineSwitchMethod.Manual)
        {
            if (timeLineManager == null)
            {
                Debug.LogError("[时间线切换器] TimeLineManager未设置");
                return false;
            }

            // 检查是否可以切换
            if (!timeLineManager.CanSwitchToTimeLine(targetTimeline))
            {
                return false;
            }

            // 检查是否允许该切换方式
            if (!IsSwitchMethodAllowed(method))
            {
                Debug.LogWarning($"[时间线切换器] 不允许使用{method}切换");
                return false;
            }

            // 检查失眠状态
            if (!CanSwitchWithCurrentInsomnia(targetTimeline))
            {
                return false;
            }

            // 开始切换
            StartSwitch(targetTimeline, method);

            return true;
        }

        /// <summary>
        /// 检查切换方式是否被允许
        /// </summary>
        private bool IsSwitchMethodAllowed(TimeLineSwitchMethod method)
        {
            switch (method)
            {
                case TimeLineSwitchMethod.TimeFragment:
                    return allowTimeFragmentSwitch;
                case TimeLineSwitchMethod.SpaceTimeCrack:
                    return allowCrackSwitch;
                case TimeLineSwitchMethod.MemoryEntity:
                    return allowMemoryEntitySwitch;
                case TimeLineSwitchMethod.Manual:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 检查当前失眠状态是否允许切换
        /// </summary>
        private bool CanSwitchWithCurrentInsomnia(TimeLineType targetTimeline)
        {
            if (insomniaController == null)
            {
                return true;
            }

            float insomniaLevel = insomniaController.GetCurrentInsomniaValue();

            // 检查是否可以穿越时间线
            if (!insomniaController.CanTravelTimeLines())
            {
                Debug.LogWarning($"[时间线切换器] 当前失眠状态不允许穿越时间线");
                return false;
            }

            // 检查目标时间线是否需要更高等级的失眠
            TimeLineConfig targetConfig = timeLineManager.GetConfig(targetTimeline);
            if (targetConfig.timeFragmentLevel > insomniaLevel / 10f)
            {
                Debug.LogWarning($"[时间线切换器] 需要更高等级的失眠状态");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 开始切换流程
        /// </summary>
        private void StartSwitch(TimeLineType targetTimeline, TimeLineSwitchMethod method)
        {
            // 获取当前时间线
            TimeLineType oldTimeline = timeLineManager.GetCurrentTimeline();

            // 获取目标时间线配置
            TimeLineConfig targetConfig = timeLineManager.GetConfig(targetTimeline);
            TimeLineConfig oldConfig = timeLineManager.GetConfig(oldTimeline);

            // 计算过渡持续时间
            float transitionDuration = GetTransitionDuration(targetTimeline);

            // 增加失眠值
            IncreaseInsomnia(targetTimeline);

            // 开始视觉效果
            if (transitionEffect != null)
            {
                transitionEffect.StartTransition(oldTimeline, targetTimeline, transitionDuration);
            }

            // 播放切换音效
            PlaySwitchSound(method);

            // 更新时间线管理器
            timeLineManager.StartSwitch(targetTimeline, method);

            // 更新音频系统
            UpdateAudioForTimeLine(targetConfig);

            // 更新时间线可视化
            UpdateTimeLineVisualizer(targetConfig);
        }

        /// <summary>
        /// 获取过渡持续时间
        /// </summary>
        private float GetTransitionDuration(TimeLineType targetTimeline)
        {
            switch (targetTimeline)
            {
                case TimeLineType.Past:
                    return pastTransitionDuration;
                case TimeLineType.Future:
                    return futureTransitionDuration;
                case TimeLineType.Dream:
                    return dreamTransitionDuration;
                default:
                    return defaultTransitionDuration;
            }
        }

        /// <summary>
        /// 增加失眠值
        /// </summary>
        private void IncreaseInsomnia(TimeLineType targetTimeline)
        {
            if (insomniaController == null) return;

            int insomniaIncrease = 0;

            switch (targetTimeline)
            {
                case TimeLineType.Past:
                    insomniaIncrease = 15;
                    break;
                case TimeLineType.Future:
                    insomniaIncrease = 25;
                    break;
                case TimeLineType.Dream:
                    insomniaIncrease = 30;
                    break;
                default:
                    insomniaIncrease = 5;
                    break;
            }

            insomniaController.IncreaseInsomnia(insomniaIncrease, $"切换到{targetTimeline}");
        }

        /// <summary>
        /// 播放切换音效
        /// </summary>
        private void PlaySwitchSound(TimeLineSwitchMethod method)
        {
            // 这里可以播放相应的音效
            // 简化处理，只打印日志
            Debug.Log($"[时间线切换器] 播放切换音效: {method}");
        }

        /// <summary>
        /// 更新音频系统以适应新时间线
        /// </summary>
        private void UpdateAudioForTimeLine(TimeLineConfig config)
        {
            // 这里可以更新音频管理器以适应新时间线的环境音
            // 简化处理
            Debug.Log($"[时间线切换器] 更新音频系统: {config.name}");
        }

        /// <summary>
        /// 更新时间线可视化
        /// </summary>
        private void UpdateTimeLineVisualizer(TimeLineConfig config)
        {
            // 这里可以更新时间线可视化系统
            // 简化处理
            Debug.Log($"[时间线切换器] 更新时间线可视化: {config.name}");
        }

        /// <summary>
        /// 手动切换到过去时间线
        /// </summary>
        public bool SwitchToPast(TimeLineSwitchMethod method = TimeLineSwitchMethod.TimeFragment)
        {
            return SwitchToTimeLine(TimeLineType.Past, method);
        }

        /// <summary>
        /// 手动切换到未来时间线
        /// </summary>
        public bool SwitchToFuture(TimeLineSwitchMethod method = TimeLineSwitchMethod.TimeFragment)
        {
            return SwitchToTimeLine(TimeLineType.Future, method);
        }

        /// <summary>
        /// 手动切换到梦境世界
        /// </summary>
        public bool SwitchToDream(TimeLineSwitchMethod method = TimeLineSwitchMethod.MemoryEntity)
        {
            return SwitchToTimeLine(TimeLineType.Dream, method);
        }

        /// <summary>
        /// 手动切换到主时间线
        /// </summary>
        public bool SwitchToMain(TimeLineSwitchMethod method = TimeLineSwitchMethod.TimeFragment)
        {
            return SwitchToTimeLine(TimeLineType.Main, method);
        }

        /// <summary>
        /// 检查是否可以切换到指定时间线
        /// </summary>
        public bool CanSwitchTo(TimeLineType targetTimeline)
        {
            return timeLineManager != null && timeLineManager.CanSwitchToTimeLine(targetTimeline);
        }

        /// <summary>
        /// 设置是否允许时间碎片切换
        /// </summary>
        public void SetAllowTimeFragmentSwitch(bool allow)
        {
            allowTimeFragmentSwitch = allow;
        }

        /// <summary>
        /// 设置是否允许裂缝切换
        /// </summary>
        public void SetAllowCrackSwitch(bool allow)
        {
            allowCrackSwitch = allow;
        }

        /// <summary>
        /// 设置是否允许记忆实体切换
        /// </summary>
        public void SetAllowMemoryEntitySwitch(bool allow)
        {
            allowMemoryEntitySwitch = allow;
        }
    }
}
