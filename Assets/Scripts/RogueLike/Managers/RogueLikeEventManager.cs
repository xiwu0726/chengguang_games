#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Managers
{
    /// <summary>
    /// 随机事件管理器 - 管理所有随机事件的触发和处理
    /// </summary>
    public class RogueLikeEventManager : MonoBehaviour
    {
        [Header("Event Data")]
        [SerializeField] private List<RogueLikeEventData> events;

        [Header("Settings")]
        [SerializeField] private float eventCheckInterval = 5f;
        [SerializeField] private bool autoTriggerEvents = true;
        [SerializeField] private bool logEvents = true;

        private static RogueLikeEventManager instance;
        private Dictionary<string, RogueLikeEventData> eventMap = new Dictionary<string, RogueLikeEventData>();
        private List<RogueLikeEventData> activeEvents = new List<RogueLikeEventData>();
        private float timer = 0f;
        private int currentPlayCount = 0;

        // 事件
        public event System.Action<RogueLikeEventData> OnEventTriggered;
        public event System.Action<RogueLikeEventData> OnEventCompleted;
        public event System.Action<RogueLikeEventData> OnEventCancelled;

        public static RogueLikeEventManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<RogueLikeEventManager>();
                    if (instance == null)
                    {
                        GameObject manager = new GameObject("RogueLikeEventManager");
                        instance = manager.AddComponent<RogueLikeEventManager>();
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

            InitializeEventManager();
        }

        /// <summary>
        /// 初始化随机事件管理器
        /// </summary>
        private void InitializeEventManager()
        {
            eventMap.Clear();
            activeEvents.Clear();

            // 将事件列表转换为字典
            for (int i = 0; i < events.Count; i++)
            {
                if (!eventMap.ContainsKey(events[i].eventId))
                {
                    eventMap[events[i].eventId] = events[i];
                }
            }

            Debug.Log($"Initialized {events.Count} rogue-like events.");

            // 加载已触发事件
            LoadTriggeredEvents();
        }

        /// <summary>
        /// 更新循环
        /// </summary>
        private void Update()
        {
            if (!autoTriggerEvents) return;

            timer += Time.deltaTime;

            if (timer >= eventCheckInterval)
            {
                timer = 0f;
                CheckForTriggerableEvents();
            }
        }

        /// <summary>
        /// 检查可触发的事件
        /// </summary>
        private void CheckForTriggerableEvents()
        {
            // 检查每个事件
            foreach (var eventData in events)
            {
                // 检查是否已达到最大触发次数
                if (!eventData.canRepeat && eventData.currentTriggerCount >= eventData.maxTriggerCount)
                {
                    continue;
                }

                // 检查是否满足解锁条件
                if (!CheckUnlockConditions(eventData))
                {
                    continue;
                }

                // 检查触发条件
                if (CheckTriggerConditions(eventData))
                {
                    // 随机决定是否触发
                    if (Random.value * 100f <= eventData.triggerProbability)
                    {
                        TriggerEvent(eventData);
                    }
                }
            }
        }

        /// <summary>
        /// 触发随机事件
        /// </summary>
        private void TriggerEvent(RogueLikeEventData eventData)
        {
            // 增加触发次数
            eventData.currentTriggerCount++;

            // 播放触发音效
            AudioManager.Instance?.PlaySFX("RogueLikeEvent");

            if (logEvents)
            {
                Debug.Log($"Triggered rogue-like event: {eventData.eventName} (ID: {eventData.eventId})");
            }

            // 触发事件
            OnEventTriggered?.Invoke(eventData);

            // 显示事件
            ShowEvent(eventData);

            // 保存已触发事件
            SaveTriggeredEvents();
        }

        /// <summary>
        /// 显示事件
        /// </summary>
        private void ShowEvent(RogueLikeEventData eventData)
        {
            // 在实际项目中，这里会显示事件UI
            // 例如：RogueLikeUI.Instance.ShowEvent(eventData);
            Debug.Log($"Showing event: {eventData.eventName}");
        }

        /// <summary>
        /// 检查解锁条件
        /// </summary>
        private bool CheckUnlockConditions(RogueLikeEventData eventData)
        {
            if (eventData.unlockConditions == null || eventData.unlockConditions.Count == 0)
            {
                return true;
            }

            foreach (string conditionId in eventData.unlockConditions)
            {
                if (!IsConditionMet(conditionId))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查触发条件
        /// </summary>
        private bool CheckTriggerConditions(RogueLikeEventData eventData)
        {
            if (eventData.triggerCondition != null)
            {
                return eventData.triggerCondition.Invoke();
            }

            // 检查事件条件
            if (eventData.triggerConditions != null && eventData.triggerConditions.Count > 0)
            {
                foreach (var condition in eventData.triggerConditions)
                {
                    if (!CheckCondition(condition))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        private bool CheckCondition(EventCondition condition)
        {
            // 在实际项目中，这里会检查各种条件
            // 例如：InsomniaStateManager.Instance.GetCurrentInsomniaLevel() >= condition.targetValue
            return true;
        }

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        private bool IsConditionMet(string conditionId)
        {
            // 在实际项目中，这里会检查各种条件
            return true;
        }

        /// <summary>
        /// 完成事件
        /// </summary>
        public void CompleteEvent(string eventId)
        {
            if (!eventMap.ContainsKey(eventId)) return;

            RogueLikeEventData eventData = eventMap[eventId];

            OnEventCompleted?.Invoke(eventData);

            if (logEvents)
            {
                Debug.Log($"Completed event: {eventData.eventName}");
            }

            // 保存已触发事件
            SaveTriggeredEvents();
        }

        /// <summary>
        /// 取消事件
        /// </summary>
        public void CancelEvent(string eventId)
        {
            if (!eventMap.ContainsKey(eventId)) return;

            RogueLikeEventData eventData = eventMap[eventId];

            OnEventCancelled?.Invoke(eventData);

            if (logEvents)
            {
                Debug.Log($"Cancelled event: {eventData.eventName}");
            }
        }

        /// <summary>
        /// 获取事件
        /// </summary>
        public RogueLikeEventData GetEvent(string eventId)
        {
            return eventMap.ContainsKey(eventId) ? eventMap[eventId] : null;
        }

        /// <summary>
        /// 获取所有事件
        /// </summary>
        public List<RogueLikeEventData> GetAllEvents()
        {
            return new List<RogueLikeEventData>(events);
        }

        /// <summary>
        /// 获取可触发的事件
        /// </summary>
        public List<RogueLikeEventData> GetTriggerableEvents()
        {
            List<RogueLikeEventData> triggerableEvents = new List<RogueLikeEventData>();

            foreach (var eventData in events)
            {
                // 检查是否已达到最大触发次数
                if (!eventData.canRepeat && eventData.currentTriggerCount >= eventData.maxTriggerCount)
                {
                    continue;
                }

                // 检查是否满足解锁条件
                if (!CheckUnlockConditions(eventData))
                {
                    continue;
                }

                // 检查触发条件
                if (CheckTriggerConditions(eventData))
                {
                    triggerableEvents.Add(eventData);
                }
            }

            return triggerableEvents;
        }

        /// <summary>
        /// 添加事件
        /// </summary>
        public void AddEvent(RogueLikeEventData eventData)
        {
            if (eventData == null) return;

            // 检查是否已存在
            if (eventMap.ContainsKey(eventData.eventId))
            {
                Debug.LogWarning($"Event '{eventData.eventId}' already exists!");
                return;
            }

            events.Add(eventData);
            eventMap[eventData.eventId] = eventData;

            Debug.Log($"Added rogue-like event: {eventData.eventName}");
        }

        /// <summary>
        /// 移除事件
        /// </summary>
        public void RemoveEvent(string eventId)
        {
            if (!eventMap.ContainsKey(eventId)) return;

            RogueLikeEventData eventData = eventMap[eventId];

            events.Remove(eventData);
            eventMap.Remove(eventId);

            Debug.Log($"Removed rogue-like event: {eventData.eventName}");
        }

        /// <summary>
        /// 重置事件触发次数
        /// </summary>
        public void ResetEventTriggerCount(string eventId)
        {
            if (!eventMap.ContainsKey(eventId)) return;

            eventMap[eventId].currentTriggerCount = 0;

            Debug.Log($"Reset event trigger count: {eventId}");
        }

        /// <summary>
        /// 加载已触发事件
        /// </summary>
        private void LoadTriggeredEvents()
        {
            // 在实际项目中，这里会从文件加载
            // 例如：SaveManager.Instance.LoadTriggeredEvents();
            Debug.Log("Loaded triggered events.");
        }

        /// <summary>
        /// 保存已触发事件
        /// </summary>
        private void SaveTriggeredEvents()
        {
            // 在实际项目中，这里会保存到文件
            // 例如：SaveManager.Instance.SaveTriggeredEvents(eventMap);
            Debug.Log("Saved triggered events.");
        }

        /// <summary>
        /// 设置当前游玩次数
        /// </summary>
        public void SetCurrentPlayCount(int count)
        {
            currentPlayCount = count;
        }

        /// <summary>
        /// 获取当前游玩次数
        /// </summary>
        public int GetCurrentPlayCount()
        {
            return currentPlayCount;
        }

        /// <summary>
        /// 检查是否自动触发事件
        /// </summary>
        public bool IsAutoTriggerEvents()
        {
            return autoTriggerEvents;
        }

        /// <summary>
        /// 设置是否自动触发事件
        /// </summary>
        public void SetAutoTriggerEvents(bool autoTrigger)
        {
            autoTriggerEvents = autoTrigger;
        }

        /// <summary>
        /// 检查是否记录事件
        /// </summary>
        public bool IsLogEvents()
        {
            return logEvents;
        }

        /// <summary>
        /// 设置是否记录事件
        /// </summary>
        public void SetLogEvents(bool log)
        {
            logEvents = log;
        }
    }
}
#endif
