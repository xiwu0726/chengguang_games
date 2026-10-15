#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Ending
{
    /// <summary>
    /// 结局控制器 - 统一管理所有结局相关的系统
    /// </summary>
    public class EndingController : MonoBehaviour
    {
        [Header("Ending Manager")]
        [SerializeField] private EndingManager endingManager;

        [Header("Ending Trigger")]
        [SerializeField] private EndingTrigger endingTrigger;

        [Header("Ending UI")]
        [SerializeField] private EndingUI endingUI;

        [Header("Settings")]
        [SerializeField] private bool autoCheckEndings = true;
        [SerializeField] private bool autoShowEnding = true;

        private static EndingController instance;

        public static EndingController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EndingController>();
                    if (instance == null)
                    {
                        GameObject controller = new GameObject("EndingController");
                        instance = controller.AddComponent<EndingController>();
                    }
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeEndingController();
        }

        /// <summary>
        /// 初始化结局控制器
        /// </summary>
        private void InitializeEndingController()
        {
            if (endingManager == null)
                endingManager = EndingManager.Instance;

            if (endingTrigger == null)
                endingTrigger = FindObjectOfType<EndingTrigger>();

            if (endingUI == null)
                endingUI = FindObjectOfType<EndingUI>();

            // 设置事件监听
            if (endingTrigger != null)
            {
                endingTrigger.SetAutoCheck(autoCheckEndings);
            }

            if (endingUI != null)
            {
                endingUI.OnContinueClicked += OnContinueClicked;
                endingUI.OnRetryClicked += OnRetryClicked;
                endingUI.OnNewGameClicked += OnNewGameClicked;
                endingUI.OnChoiceClicked += OnChoiceClicked;
            }

            if (endingManager != null)
            {
                endingManager.OnEndingAchieved += OnEndingAchieved;
            }

            Debug.Log("EndingController initialized successfully.");
        }

        /// <summary>
        /// 触发结局
        /// </summary>
        public void TriggerEnding(string endingId)
        {
            endingManager?.TriggerEnding(endingId);
        }

        /// <summary>
        /// 显示结局
        /// </summary>
        public void ShowEnding(EndingData ending)
        {
            endingUI?.ShowEnding(ending);
        }

        /// <summary>
        /// 隐藏结局
        /// </summary>
        public void HideEnding()
        {
            endingUI?.HideEnding();
        }

        /// <summary>
        /// 检查特定结局
        /// </summary>
        public void CheckEnding(string endingId)
        {
            endingTrigger?.CheckEndingCondition(endingId);
        }

        /// <summary>
        /// 检查所有结局
        /// </summary>
        public void CheckAllEndings()
        {
            endingTrigger?.CheckAllEndings();
        }

        /// <summary>
        /// 添加结局触发器
        /// </summary>
        public void AddEndingTrigger(EndingTriggerData triggerData)
        {
            endingTrigger?.AddEndingTrigger(triggerData);
        }

        /// <summary>
        /// 移除结局触发器
        /// </summary>
        public void RemoveEndingTrigger(string endingId)
        {
            endingTrigger?.RemoveEndingTrigger(endingId);
        }

        /// <summary>
        /// 设置结局触发器自动检查
        /// </summary>
        public void SetEndingTriggerAutoCheck(bool autoCheck)
        {
            autoCheckEndings = autoCheck;
            endingTrigger?.SetAutoCheck(autoCheck);
        }

        /// <summary>
        /// 设置是否自动显示结局
        /// </summary>
        public void SetAutoShowEnding(bool autoShow)
        {
            autoShowEnding = autoShow;
        }

        /// <summary>
        /// 获取当前结局
        /// </summary>
        public EndingData GetCurrentEnding()
        {
            return endingUI?.GetCurrentEnding();
        }

        /// <summary>
        /// 检查是否显示结局
        /// </summary>
        public bool IsShowingEnding()
        {
            return endingUI?.IsShowing() ?? false;
        }

        /// <summary>
        /// 获取已达成的结局
        /// </summary>
        public List<EndingData> GetAchievedEndings()
        {
            return endingManager?.GetAchievedEndings() ?? new List<EndingData>();
        }

        /// <summary>
        /// 检查是否已达成结局
        /// </summary>
        public bool IsEndingAchieved(string endingId)
        {
            return endingManager?.IsEndingAchieved(endingId) ?? false;
        }

        /// <summary>
        /// 设置结局为已达成
        /// </summary>
        public void SetEndingAchieved(string endingId, bool achieved)
        {
            endingManager?.SetEndingAchieved(endingId, achieved);
        }

        // 事件处理

        private void OnContinueClicked()
        {
            Debug.Log("Continue button clicked.");
        }

        private void OnRetryClicked()
        {
            Debug.Log("Retry button clicked. Restarting game...");
            // 在实际项目中，这里会重启游戏
        }

        private void OnNewGameClicked()
        {
            Debug.Log("New Game button clicked. Starting new game...");
            // 在实际项目中，这里会开始新游戏
        }

        private void OnChoiceClicked()
        {
            Debug.Log("Choice button clicked.");
            OnChoiceClicked?.Invoke();
        }

        private void OnEndingAchieved(EndingData ending)
        {
            Debug.Log($"Achieved ending: {ending.endingName}");

            // 如果设置了自动显示结局
            if (autoShowEnding)
            {
                ShowEnding(ending);
            }
        }

        public event System.Action OnChoiceClicked;

        // 清理资源

        private void OnDestroy()
        {
            if (endingUI != null)
            {
                endingUI.OnContinueClicked -= OnContinueClicked;
                endingUI.OnRetryClicked -= OnRetryClicked;
                endingUI.OnNewGameClicked -= OnNewGameClicked;
                endingUI.OnChoiceClicked -= OnChoiceClicked;
            }

            if (endingManager != null)
            {
                endingManager.OnEndingAchieved -= OnEndingAchieved;
            }
        }
    }
}
#endif
