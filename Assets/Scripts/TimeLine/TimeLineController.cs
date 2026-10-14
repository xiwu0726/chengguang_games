using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 时间线系统主控制器
    /// 协调所有时间线相关子系统
    /// </summary>
    public class TimeLineController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private TimeLineManager timeLineManager;
        [SerializeField] private TimeLineSwitcher timeLineSwitcher;
        [SerializeField] private TimeLineTransitionEffect transitionEffect;
        [SerializeField] private TimeLineUI timeLineUI;

        [Header("自动更新")]
        [SerializeField] private bool autoUpdate = true;
        [SerializeField] private float updateInterval = 0.5f;

        private float updateTimer = 0f;

        private void Awake()
        {
            InitializeSubsystems();
        }

        private void Start()
        {
            UpdateAllSubsystems();
        }

        private void Update()
        {
            if (autoUpdate)
            {
                updateTimer += Time.deltaTime;
                if (updateTimer >= updateInterval)
                {
                    updateTimer = 0f;
                    UpdateAllSubsystems();
                }
            }
        }

        private void InitializeSubsystems()
        {
            // 如果没有设置时间线管理器，创建默认的
            if (timeLineManager == null)
            {
                timeLineManager = FindObjectOfType<TimeLineManager>();
                if (timeLineManager == null)
                {
                    timeLineManager = gameObject.AddComponent<TimeLineManager>();
                }
            }

            // 如果没有设置时间线切换器，创建默认的
            if (timeLineSwitcher == null)
            {
                timeLineSwitcher = FindObjectOfType<TimeLineSwitcher>();
                if (timeLineSwitcher == null)
                {
                    timeLineSwitcher = gameObject.AddComponent<TimeLineSwitcher>();
                }
            }

            // 如果没有设置转换效果，创建默认的
            if (transitionEffect == null)
            {
                transitionEffect = FindObjectOfType<TimeLineTransitionEffect>();
                if (transitionEffect == null)
                {
                    transitionEffect = gameObject.AddComponent<TimeLineTransitionEffect>();
                }
            }

            // 如果没有设置UI，创建默认的
            if (timeLineUI == null)
            {
                timeLineUI = FindObjectOfType<TimeLineUI>();
                if (timeLineUI == null)
                {
                    timeLineUI = gameObject.AddComponent<TimeLineUI>();
                }
            }
        }

        /// <summary>
        /// 更新所有子系统
        /// </summary>
        private void UpdateAllSubsystems()
        {
            // 确保所有子系统正常运行
            if (timeLineManager != null)
            {
                TimeLineConfig config = timeLineManager.GetCurrentConfig();
                Debug.Log($"[时间线系统] 当前: {config.name} ({timeLineManager.GetCurrentYear()}年)");
            }

            if (timeLineUI != null)
            {
                timeLineUI.UpdateTimeLineInfo();
            }
        }

        /// <summary>
        /// 切换到指定时间线
        /// </summary>
        public bool SwitchToTimeLine(TimeLineType targetTimeline, TimeLineSwitchMethod method = TimeLineSwitchMethod.Manual)
        {
            if (timeLineSwitcher != null)
            {
                return timeLineSwitcher.SwitchToTimeLine(targetTimeline, method);
            }
            return false;
        }

        /// <summary>
        /// 打开时间线选择面板
        /// </summary>
        public void OpenTimeLineSelector()
        {
            if (timeLineUI != null)
            {
                timeLineUI.OpenPanel();
            }
        }

        /// <summary>
        /// 关闭时间线选择面板
        /// </summary>
        public void CloseTimeLineSelector()
        {
            if (timeLineUI != null)
            {
                timeLineUI.ClosePanel();
            }
        }

        /// <summary>
        /// 获取当前时间线类型
        /// </summary>
        public TimeLineType GetCurrentTimeline()
        {
            return timeLineManager != null ? timeLineManager.GetCurrentTimeline() : TimeLineType.Main;
        }

        /// <summary>
        /// 获取当前时间线配置
        /// </summary>
        public TimeLineConfig GetCurrentConfig()
        {
            return timeLineManager != null ? timeLineManager.GetCurrentConfig() : null;
        }

        /// <summary>
        /// 获取当前年份
        /// </summary>
        public int GetCurrentYear()
        {
            return timeLineManager != null ? timeLineManager.GetCurrentYear() : 2150;
        }

        /// <summary>
        /// 检查是否可以切换到指定时间线
        /// </summary>
        public bool CanSwitchTo(TimeLineType targetTimeline)
        {
            return timeLineSwitcher != null && timeLineSwitcher.CanSwitchTo(targetTimeline);
        }

        /// <summary>
        /// 获取当前冷却时间
        /// </summary>
        public float GetCurrentCooldownTime()
        {
            return timeLineManager != null ? timeLineManager.GetRemainingCooldownTime() : 0f;
        }

        /// <summary>
        /// 检查是否可以穿越时间线
        /// </summary>
        public bool CanTravelTimeLines()
        {
            return timeLineManager != null && timeLineManager.CanTravelTimeLines();
        }

        /// <summary>
        /// 重置到主时间线
        /// </summary>
        public void ResetToMainTimeline()
        {
            if (timeLineSwitcher != null)
            {
                timeLineSwitcher.SwitchToMain();
            }
        }

        /// <summary>
        /// 强制设置时间线（用于剧情需要）
        /// </summary>
        public void ForceSwitchToTimeLine(TimeLineType targetTimeline)
        {
            if (timeLineManager != null)
            {
                timeLineManager.ForceSwitchToTimeLine(targetTimeline);
            }
        }

        /// <summary>
        /// 重置所有系统
        /// </summary>
        public void ResetAll()
        {
            if (timeLineManager != null)
            {
                timeLineManager.ResetToMainTimeline();
            }

            if (timeLineUI != null)
            {
                timeLineUI.Reset();
            }

            Debug.Log("[时间线系统] 已重置");
        }

        /// <summary>
        /// 设置是否允许时间碎片切换
        /// </summary>
        public void SetAllowTimeFragmentSwitch(bool allow)
        {
            if (timeLineSwitcher != null)
            {
                timeLineSwitcher.SetAllowTimeFragmentSwitch(allow);
            }
        }

        /// <summary>
        /// 设置是否允许裂缝切换
        /// </summary>
        public void SetAllowCrackSwitch(bool allow)
        {
            if (timeLineSwitcher != null)
            {
                timeLineSwitcher.SetAllowCrackSwitch(allow);
            }
        }

        /// <summary>
        /// 设置是否允许记忆实体切换
        /// </summary>
        public void SetAllowMemoryEntitySwitch(bool allow)
        {
            if (timeLineSwitcher != null)
            {
                timeLineSwitcher.SetAllowMemoryEntitySwitch(allow);
            }
        }

        /// <summary>
        /// 设置自动更新
        /// </summary>
        public void SetAutoUpdate(bool autoUpdate)
        {
            this.autoUpdate = autoUpdate;
        }

        /// <summary>
        /// 检查是否正在切换
        /// </summary>
        public bool IsTransitioning()
        {
            return transitionEffect != null && transitionEffect.IsTransitioning();
        }

        /// <summary>
        /// 获取切换进度
        /// </summary>
        public float GetTransitionProgress()
        {
            return transitionEffect != null ? transitionEffect.GetTransitionProgress() : 0f;
        }
    }
}
