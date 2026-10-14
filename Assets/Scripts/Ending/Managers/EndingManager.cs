using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Ending.Managers
{
    /// <summary>
    /// 结局管理器 - 统一管理所有结局的系统
    /// </summary>
    public class EndingManager : MonoBehaviour
    {
        [Header("Ending Data")]
        [SerializeField] private List<EndingData> endings;

        [Header("Settings")]
        [SerializeField] private bool autoCheckEndings = true;
        [SerializeField] private bool saveAchievements = true;

        private static EndingManager instance;
        private Dictionary<string, EndingData> endingMap = new Dictionary<string, EndingData>();
        private List<EndingData> achievedEndings = new List<EndingData>();
        private EndingData currentEnding = null;

        // 事件
        public event System.Action<EndingData> OnEndingAchieved;
        public event System.Action<EndingData> OnEndingTriggered;
        public event System.Action<string> OnEndingSceneLoaded;

        public static EndingManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<EndingManager>();
                    if (instance == null)
                    {
                        GameObject manager = new GameObject("EndingManager");
                        instance = manager.AddComponent<EndingManager>();
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

            InitializeEndingManager();
        }

        /// <summary>
        /// 初始化结局管理器
        /// </summary>
        private void InitializeEndingManager()
        {
            endingMap.Clear();
            achievedEndings.Clear();

            // 将结局列表转换为字典
            for (int i = 0; i < endings.Count; i++)
            {
                if (!endingMap.ContainsKey(endings[i].endingId))
                {
                    endingMap[endings[i].endingId] = endings[i];
                }
            }

            Debug.Log($"Initialized {endings.Count} endings.");

            // 加载已达成的结局
            LoadAchievedEndings();
        }

        /// <summary>
        /// 触发结局
        /// </summary>
        public void TriggerEnding(string endingId)
        {
            if (!endingMap.ContainsKey(endingId))
            {
                Debug.LogWarning($"Ending '{endingId}' not found!");
                return;
            }

            EndingData ending = endingMap[endingId];

            // 检查是否可重复达成
            if (!ending.canRepeat && ending.isAchieved)
            {
                Debug.LogWarning($"Ending '{ending.endingName}' has already been achieved!");
                return;
            }

            // 检查解锁条件
            if (!CheckUnlockConditions(ending))
            {
                Debug.LogWarning($"Ending '{ending.endingName}' conditions not met!");
                return;
            }

            // 检查触发条件
            if (ending.triggerCondition != null && !ending.triggerCondition.Invoke())
            {
                Debug.LogWarning($"Ending '{ending.endingName}' trigger condition not met!");
                return;
            }

            // 标记为已达成
            ending.isAchieved = true;
            ending.achieveCount++;

            // 添加到已达成列表
            if (!achievedEndings.Contains(ending))
            {
                achievedEndings.Add(ending);
            }

            // 触发事件
            OnEndingTriggered?.Invoke(ending);
            OnEndingAchieved?.Invoke(ending);

            // 播放结局音效
            AudioManager.Instance?.PlaySFX("Ending");

            // 保存成就
            if (saveAchievements)
            {
                SaveAchievedEndings();
            }

            Debug.Log($"Achieved ending: {ending.endingName} (ID: {endingId})");

            // 执行触发回调
            ending.onTrigger?.Invoke();
        }

        /// <summary>
        /// 检查解锁条件
        /// </summary>
        private bool CheckUnlockConditions(EndingData ending)
        {
            if (ending.unlockConditions == null || ending.unlockConditions.Count == 0)
            {
                return true;
            }

            foreach (string conditionId in ending.unlockConditions)
            {
                if (!IsConditionMet(conditionId))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 检查条件是否满足
        /// </summary>
        private bool IsConditionMet(string conditionId)
        {
            // 在实际项目中，这里会检查各种条件
            // 例如：好感度、失眠值、记忆碎片等
            return true;
        }

        /// <summary>
        /// 获取结局
        /// </summary>
        public EndingData GetEnding(string endingId)
        {
            return endingMap.ContainsKey(endingId) ? endingMap[endingId] : null;
        }

        /// <summary>
        /// 获取所有结局
        /// </summary>
        public List<EndingData> GetAllEndings()
        {
            return new List<EndingData>(endings);
        }

        /// <summary>
        /// 获取已达成的结局
        /// </summary>
        public List<EndingData> GetAchievedEndings()
        {
            return new List<EndingData>(achievedEndings);
        }

        /// <summary>
        /// 检查是否已达成特定结局
        /// </summary>
        public bool IsEndingAchieved(string endingId)
        {
            if (!endingMap.ContainsKey(endingId)) return false;

            return endingMap[endingId].isAchieved;
        }

        /// <summary>
        /// 获取结局达成次数
        /// </summary>
        public int GetAchieveCount(string endingId)
        {
            if (!endingMap.ContainsKey(endingId)) return 0;

            return endingMap[endingId].achieveCount;
        }

        /// <summary>
        /// 设置结局为已达成
        /// </summary>
        public void SetEndingAchieved(string endingId, bool achieved)
        {
            if (!endingMap.ContainsKey(endingId)) return;

            endingMap[endingId].isAchieved = achieved;

            if (achieved && !achievedEndings.Contains(endingMap[endingId]))
            {
                achievedEndings.Add(endingMap[endingId]);
            }
            else if (!achieved && achievedEndings.Contains(endingMap[endingId]))
            {
                achievedEndings.Remove(endingMap[endingId]);
            }
        }

        /// <summary>
        /// 加载已达成的结局
        /// </summary>
        private void LoadAchievedEndings()
        {
            // 在实际项目中，这里会从文件加载
            // 例如：SaveManager.Instance.LoadAchievedEndings();
            Debug.Log("Loaded achieved endings.");
        }

        /// <summary>
        /// 保存已达成的结局
        /// </summary>
        private void SaveAchievedEndings()
        {
            // 在实际项目中，这里会保存到文件
            // 例如：SaveManager.Instance.SaveAchievedEndings(achievedEndings);
            Debug.Log("Saved achieved endings.");
        }

        /// <summary>
        /// 设置当前结局
        /// </summary>
        public void SetCurrentEnding(EndingData ending)
        {
            currentEnding = ending;
        }

        /// <summary>
        /// 获取当前结局
        /// </summary>
        public EndingData GetCurrentEnding()
        {
            return currentEnding;
        }

        /// <summary>
        /// 检查是否自动检查结局
        /// </summary>
        public bool IsAutoCheckEndings()
        {
            return autoCheckEndings;
        }

        /// <summary>
        /// 设置是否自动检查结局
        /// </summary>
        public void SetAutoCheckEndings(bool autoCheck)
        {
            autoCheckEndings = autoCheck;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (saveAchievements)
            {
                SaveAchievedEndings();
            }
        }
    }
}
