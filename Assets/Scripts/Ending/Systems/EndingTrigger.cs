#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Ending.Systems
{
    /// <summary>
    /// 结局触发器 - 管理结局的触发逻辑
    /// </summary>
    public class EndingTrigger : MonoBehaviour
    {
        [Header("Ending Manager")]
        [SerializeField] private EndingManager endingManager;

        [Header("Settings")]
        [SerializeField] private float checkInterval = 1f;
        [SerializeField] private bool autoCheck = true;

        private float timer = 0f;
        private List<EndingTriggerData> activeTriggers = new List<EndingTriggerData>();

        private void Awake()
        {
            if (endingManager == null)
            {
                endingManager = EndingManager.Instance;
            }
        }

        private void Update()
        {
            if (!autoCheck) return;

            timer += Time.deltaTime;

            if (timer >= checkInterval)
            {
                timer = 0f;
                CheckActiveTriggers();
            }
        }

        /// <summary>
        /// 检查激活的触发器
        /// </summary>
        private void CheckActiveTriggers()
        {
            for (int i = activeTriggers.Count - 1; i >= 0; i--)
            {
                EndingTriggerData trigger = activeTriggers[i];

                // 检查触发条件
                if (trigger.triggerCondition != null && trigger.triggerCondition.Invoke())
                {
                    // 触发结局
                    TriggerEnding(trigger.endingId);

                    // 执行清理
                    trigger.onCleanup?.Invoke();

                    // 从激活列表中移除
                    activeTriggers.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// 触发结局
        /// </summary>
        private void TriggerEnding(string endingId)
        {
            endingManager?.TriggerEnding(endingId);
        }

        /// <summary>
        /// 添加结局触发器
        /// </summary>
        public void AddEndingTrigger(EndingTriggerData triggerData)
        {
            if (triggerData == null) return;

            // 检查是否已存在
            if (activeTriggers.Contains(triggerData)) return;

            activeTriggers.Add(triggerData);
            Debug.Log($"Added ending trigger: {triggerData.endingId}");
        }

        /// <summary>
        /// 移除结局触发器
        /// </summary>
        public void RemoveEndingTrigger(string endingId)
        {
            activeTriggers.RemoveAll(t => t.endingId == endingId);
            Debug.Log($"Removed ending trigger: {endingId}");
        }

        /// <summary>
        /// 清除所有触发器
        /// </summary>
        public void ClearAllTriggers()
        {
            activeTriggers.Clear();
            Debug.Log("Cleared all ending triggers.");
        }

        /// <summary>
        /// 获取激活的触发器数量
        /// </summary>
        public int GetActiveTriggerCount()
        {
            return activeTriggers.Count;
        }

        /// <summary>
        /// 检查特定触发器是否激活
        /// </summary>
        public bool IsTriggerActive(string endingId)
        {
            return activeTriggers.Exists(t => t.endingId == endingId);
        }

        /// <summary>
        /// 检查是否自动检查
        /// </summary>
        public bool IsAutoCheck()
        {
            return autoCheck;
        }

        /// <summary>
        /// 设置是否自动检查
        /// </summary>
        public void SetAutoCheck(bool autoCheck)
        {
            this.autoCheck = autoCheck;
        }

        /// <summary>
        /// 检查所有结局条件
        /// </summary>
        public void CheckAllEndings()
        {
            foreach (var ending in endingManager.GetAllEndings())
            {
                CheckEnding(ending);
            }
        }

        /// <summary>
        /// 检查特定结局
        /// </summary>
        public void CheckEnding(EndingData ending)
        {
            if (ending == null) return;

            // 检查触发条件
            if (ending.triggerCondition != null && ending.triggerCondition.Invoke())
            {
                TriggerEnding(ending.endingId);
            }
        }

        /// <summary>
        /// 检查结局条件
        /// </summary>
        public bool CheckEndingCondition(string endingId)
        {
            EndingData ending = endingManager?.GetEnding(endingId);
            if (ending == null) return false;

            return ending.triggerCondition?.Invoke() ?? false;
        }

        /// <summary>
        /// 获取所有激活的触发器
        /// </summary>
        public List<EndingTriggerData> GetActiveTriggers()
        {
            return new List<EndingTriggerData>(activeTriggers);
        }
    }
}
#endif
