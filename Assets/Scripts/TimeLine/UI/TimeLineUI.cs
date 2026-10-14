using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStamp
{
    /// <summary>
    /// 时间线UI管理器
    /// 处理时间线选择、信息显示和状态反馈
    /// </summary>
    public class TimeLineUI : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private TimeLineManager timeLineManager;
        [SerializeField] private TimeLineSwitcher timeLineSwitcher;
        [SerializeField] private TransitionEffect transitionEffect;

        [Header("时间线选择UI")]
        [SerializeField] private GameObject timeLineSelectorPanel;
        [SerializeField] private Button mainTimelineButton;
        [SerializeField] private Button pastTimelineButton;
        [SerializeField] private Button futureTimelineButton;
        [SerializeField] private Button dreamTimelineButton;
        [SerializeField] private Button closeButton;

        [Header("信息显示")]
        [SerializeField] private Text currentTimelineText;
        [SerializeField] private Text currentYearText;
        [SerializeField] private Text timelineDescriptionText;
        [SerializeField] private Text cooldownText;

        [Header("冷却倒计时")]
        [SerializeField] private Image cooldownBar;
        [SerializeField] private float cooldownBarSpeed = 1.0f;

        [Header("视觉效果")]
        [SerializeField] private Image timelineIcon;
        [SerializeField] private Color mainTimelineColor = new Color(0.8f, 0.8f, 1.0f);
        [SerializeField] private Color pastTimelineColor = new Color(1.0f, 0.8f, 0.8f);
        [SerializeField] private Color futureTimelineColor = new Color(0.6f, 0.6f, 0.8f);
        [SerializeField] private Color dreamTimelineColor = new Color(1.0f, 1.0f, 0.8f);

        private bool isPanelOpen = false;

        private void Awake()
        {
            if (timeLineManager == null)
            {
                timeLineManager = FindObjectOfType<TimeLineManager>();
            }

            if (timeLineSwitcher == null)
            {
                timeLineSwitcher = FindObjectOfType<TimeLineSwitcher>();
            }

            InitializeUI();
        }

        private void Start()
        {
            UpdateTimeLineInfo();
        }

        private void Update()
        {
            UpdateCooldownDisplay();
        }

        private void InitializeUI()
        {
            // 设置按钮点击事件
            if (mainTimelineButton != null)
            {
                mainTimelineButton.onClick.AddListener(() => OnTimelineButtonClicked(TimeLineType.Main));
            }

            if (pastTimelineButton != null)
            {
                pastTimelineButton.onClick.AddListener(() => OnTimelineButtonClicked(TimeLineType.Past));
            }

            if (futureTimelineButton != null)
            {
                futureTimelineButton.onClick.AddListener(() => OnTimelineButtonClicked(TimeLineType.Future));
            }

            if (dreamTimelineButton != null)
            {
                dreamTimelineButton.onClick.AddListener(() => OnTimelineButtonClicked(TimeLineType.Dream));
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => ClosePanel());
            }

            // 初始化面板状态
            if (timeLineSelectorPanel != null)
            {
                timeLineSelectorPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 打开时间线选择面板
        /// </summary>
        public void OpenPanel()
        {
            if (timeLineSelectorPanel != null)
            {
                timeLineSelectorPanel.SetActive(true);
                isPanelOpen = true;
                UpdateTimeLineInfo();
            }
        }

        /// <summary>
        /// 关闭时间线选择面板
        /// </summary>
        public void ClosePanel()
        {
            if (timeLineSelectorPanel != null)
            {
                timeLineSelectorPanel.SetActive(false);
                isPanelOpen = false;
            }
        }

        /// <summary>
        /// 时间线按钮点击事件
        /// </summary>
        private void OnTimelineButtonClicked(TimeLineType targetTimeline)
        {
            if (timeLineManager != null && timeLineSwitcher != null)
            {
                bool success = timeLineSwitcher.SwitchToTimeLine(targetTimeline);
                if (success)
                {
                    ClosePanel();
                    Debug.Log($"[时间线UI] 切换到{targetTimeline}");
                }
                else
                {
                    Debug.LogWarning($"[时间线UI] 无法切换到{targetTimeline}");
                    ShowSwitchError(targetTimeline);
                }
            }
        }

        /// <summary>
        /// 显示切换错误
        /// </summary>
        private void ShowSwitchError(TimeLineType targetTimeline)
        {
            // 这里可以显示错误提示
            Debug.LogWarning($"[时间线UI] 无法切换到{targetTimeline}");
        }

        /// <summary>
        /// 更新时间线信息显示
        /// </summary>
        private void UpdateTimeLineInfo()
        {
            if (timeLineManager == null) return;

            TimeLineConfig config = timeLineManager.GetCurrentConfig();
            int year = timeLineManager.GetCurrentYear();

            // 更新文本信息
            if (currentTimelineText != null)
            {
                currentTimelineText.text = config.name;
            }

            if (currentYearText != null)
            {
                currentYearText.text = year.ToString() + "年";
            }

            if (timelineDescriptionText != null)
            {
                timelineDescriptionText.text = config.description;
            }

            // 更新颜色
            UpdateTimelineColor(config.type);

            // 更新按钮状态
            UpdateButtonStates();
        }

        /// <summary>
        /// 更新时间线颜色
        /// </summary>
        private void UpdateTimelineColor(TimeLineType type)
        {
            Color color;

            switch (type)
            {
                case TimeLineType.Main:
                    color = mainTimelineColor;
                    break;
                case TimeLineType.Past:
                    color = pastTimelineColor;
                    break;
                case TimeLineType.Future:
                    color = futureTimelineColor;
                    break;
                case TimeLineType.Dream:
                    color = dreamTimelineColor;
                    break;
                default:
                    color = mainTimelineColor;
                    break;
            }

            if (currentTimelineText != null)
            {
                currentTimelineText.color = color;
            }

            if (timelineIcon != null)
            {
                timelineIcon.color = color;
            }
        }

        /// <summary>
        /// 更新按钮状态
        /// </summary>
        private void UpdateButtonStates()
        {
            if (timeLineManager == null || timeLineSwitcher == null) return;

            TimeLineType currentType = timeLineManager.GetCurrentTimeline();

            // 禁用当前时间线按钮
            DisableButton(mainTimelineButton, currentType == TimeLineType.Main);
            DisableButton(pastTimelineButton, currentType == TimeLineType.Past);
            DisableButton(futureTimelineButton, currentType == TimeLineType.Future);
            DisableButton(dreamTimelineButton, currentType == TimeLineType.Dream);

            // 检查每个按钮是否可以点击
            CheckButtonAvailability(mainTimelineButton, TimeLineType.Main);
            CheckButtonAvailability(pastTimelineButton, TimeLineType.Past);
            CheckButtonAvailability(futureTimelineButton, TimeLineType.Future);
            CheckButtonAvailability(dreamTimelineButton, TimeLineType.Dream);
        }

        /// <summary>
        /// 禁用按钮
        /// </summary>
        private void DisableButton(Button button, bool disable)
        {
            if (button != null)
            {
                button.interactable = !disable;
            }
        }

        /// <summary>
        /// 检查按钮可用性
        /// </summary>
        private void CheckButtonAvailability(Button button, TimeLineType targetTimeline)
        {
            if (button != null && timeLineSwitcher != null)
            {
                bool canSwitch = timeLineSwitcher.CanSwitchTo(targetTimeline);
                button.interactable = canSwitch;
            }
        }

        /// <summary>
        /// 更新冷却显示
        /// </summary>
        private void UpdateCooldownDisplay()
        {
            if (timeLineManager == null) return;

            float remainingCooldown = timeLineManager.GetRemainingCooldownTime();

            if (cooldownText != null)
            {
                if (remainingCooldown > 0f)
                {
                    cooldownText.text = $"冷却中: {remainingCooldown:F1}秒";
                    cooldownText.enabled = true;
                }
                else
                {
                    cooldownText.text = "";
                    cooldownText.enabled = false;
                }
            }

            if (cooldownBar != null)
            {
                float cooldownTime = timeLineManager.GetCurrentCooldownTime();
                if (cooldownTime > 0f)
                {
                    float progress = cooldownTimer / cooldownTime;
                    cooldownBar.fillAmount = progress;
                }
                else
                {
                    cooldownBar.fillAmount = 0f;
                }
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
        /// 检查面板是否打开
        /// </summary>
        public bool IsPanelOpen()
        {
            return isPanelOpen;
        }

        /// <summary>
        /// 设置是否显示冷却倒计时
        /// </summary>
        public void SetShowCooldown(bool show)
        {
            if (cooldownText != null)
            {
                cooldownText.enabled = show;
            }
            if (cooldownBar != null)
            {
                cooldownBar.gameObject.SetActive(show);
            }
        }

        /// <summary>
        /// 重置UI
        /// </summary>
        public void Reset()
        {
            ClosePanel();
            UpdateTimeLineInfo();
            if (cooldownBar != null)
            {
                cooldownBar.fillAmount = 0f;
            }
        }
    }
}
