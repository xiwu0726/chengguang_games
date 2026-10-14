using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 记忆碎片系统
    /// 管理记忆碎片的收集、查看和管理
    /// </summary>
    public class MemoryShardSystem : MonoBehaviour
    {
        public static MemoryShardSystem Instance { get; private set; }

        [Header("记忆碎片数据")]
        [SerializeField] private List<MemoryShard> collectedMemoryShards = new List<MemoryShard>();
        [SerializeField] private int totalMemoryShards = 0;

        [Header("记忆碎片类型")]
        [SerializeField] private int personalMemoryCount = 0;
        [SerializeField] private int historicalMemoryCount = 0;
        [SerializeField] private int emotionalMemoryCount = 0;

        [Header("记忆查看器")]
        [SerializeField] private GameObject memoryViewerPanel;
        [SerializeField] private Text memoryTitleText;
        [SerializeField] private Text memoryContentText;
        [SerializeField] private Text memoryTypeText;
        [SerializeField] private Text memoryTimeText;

        [Header("事件")]
        public System.Action<MemoryShard> OnMemoryShardCollected;
        public System.Action<int, int> OnMemoryShardCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 收集记忆碎片
        /// </summary>
        public bool CollectMemoryShard(MemoryShard shard)
        {
            if (shard == null) return false;
            if (collectedMemoryShards.Contains(shard)) return false;

            collectedMemoryShards.Add(shard);

            // 更新类型统计
            UpdateTypeCount(shard.type);

            // 触发事件
            OnMemoryShardCollected?.Invoke(shard);
            OnMemoryShardCountChanged?.Invoke(collectedMemoryShards.Count, totalMemoryShards);

            Debug.Log($"[记忆碎片系统] 收集到: {shard.title} ({shard.type})");

            return true;
        }

        /// <summary>
        /// 更新类型统计
        /// </summary>
        private void UpdateTypeCount(MemoryShardType type)
        {
            switch (type)
            {
                case MemoryShardType.Personal:
                    personalMemoryCount++;
                    break;
                case MemoryShardType.Historical:
                    historicalMemoryCount++;
                    break;
                case MemoryShardType.Emotional:
                    emotionalMemoryCount++;
                    break;
            }
        }

        /// <summary>
        /// 检查是否已收集
        /// </summary>
        public bool HasMemoryShard(MemoryShard shard)
        {
            return collectedMemoryShards.Contains(shard);
        }

        /// <summary>
        /// 获取所有收集的记忆碎片
        /// </summary>
        public List<MemoryShard> GetAllCollectedShards()
        {
            return new List<MemoryShard>(collectedMemoryShards);
        }

        /// <summary>
        /// 根据ID获取记忆碎片
        /// </summary>
        public MemoryShard GetMemoryShardById(string id)
        {
            foreach (var shard in collectedMemoryShards)
            {
                if (shard.id == id)
                {
                    return shard;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据类型获取记忆碎片
        /// </summary>
        public List<MemoryShard> GetMemoryShardsByType(MemoryShardType type)
        {
            List<MemoryShard> result = new List<MemoryShard>();
            foreach (var shard in collectedMemoryShards)
            {
                if (shard.type == type)
                {
                    result.Add(shard);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取记忆碎片数量
        /// </summary>
        public int GetCollectedCount()
        {
            return collectedMemoryShards.Count;
        }

        /// <summary>
        /// 获取总记忆碎片数量
        /// </summary>
        public int GetTotalCount()
        {
            return totalMemoryShards;
        }

        /// <summary>
        /// 获取各类型数量
        /// </summary>
        public int GetPersonalMemoryCount()
        {
            return personalMemoryCount;
        }

        public int GetHistoricalMemoryCount()
        {
            return historicalMemoryCount;
        }

        public int GetEmotionalMemoryCount()
        {
            return emotionalMemoryCount;
        }

        /// <summary>
        /// 显示记忆碎片
        /// </summary>
        public void ShowMemoryShard(MemoryShard shard)
        {
            if (memoryViewerPanel == null)
            {
                Debug.LogWarning("[记忆碎片系统] 记忆查看器未设置");
                return;
            }

            if (shard == null)
            {
                Debug.LogWarning("[记忆碎片系统] 记忆碎片为空");
                return;
            }

            // 更新UI
            if (memoryTitleText != null)
            {
                memoryTitleText.text = shard.title;
            }

            if (memoryContentText != null)
            {
                memoryContentText.text = shard.content;
            }

            if (memoryTypeText != null)
            {
                memoryTypeText.text = shard.type.ToString();
            }

            if (memoryTimeText != null)
            {
                memoryTimeText.text = shard.year + "年";
            }

            // 显示面板
            memoryViewerPanel.SetActive(true);

            Debug.Log($"[记忆碎片系统] 显示: {shard.title}");
        }

        /// <summary>
        /// 隐藏记忆碎片
        /// </summary>
        public void HideMemoryShard()
        {
            if (memoryViewerPanel != null)
            {
                memoryViewerPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 打开记忆查看器面板
        /// </summary>
        public void OpenMemoryViewer()
        {
            if (memoryViewerPanel != null)
            {
                memoryViewerPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 关闭记忆查看器面板
        /// </summary>
        public void CloseMemoryViewer()
        {
            if (memoryViewerPanel != null)
            {
                memoryViewerPanel.SetActive(false);
            }
        }

        /// <summary>
        /// 清空所有记忆碎片
        /// </summary>
        public void ClearAllMemoryShards()
        {
            collectedMemoryShards.Clear();
            personalMemoryCount = 0;
            historicalMemoryCount = 0;
            emotionalMemoryCount = 0;
            OnMemoryShardCountChanged?.Invoke(0, totalMemoryShards);
            Debug.Log("[记忆碎片系统] 已清空所有记忆碎片");
        }

        /// <summary>
        /// 重置系统
        /// </summary>
        public void Reset()
        {
            ClearAllMemoryShards();
        }
    }

    /// <summary>
    /// 记忆碎片数据
    /// </summary>
    [System.Serializable]
    public class MemoryShard
    {
        public string id;
        public string title;
        public string content;
        public MemoryShardType type;
        public string year;
        public string location;
        public string relatedCharacter;

        public MemoryShard(string id, string title, string content, MemoryShardType type, string year, string location, string relatedCharacter)
        {
            this.id = id;
            this.title = title;
            this.content = content;
            this.type = type;
            this.year = year;
            this.location = location;
            this.relatedCharacter = relatedCharacter;
        }
    }

    /// <summary>
    /// 记忆碎片类型
    /// </summary>
    public enum MemoryShardType
    {
        /// <summary>
        /// 个人记忆
        /// </summary>
        Personal,

        /// <summary>
        /// 历史记忆
        /// </summary>
        Historical,

        /// <summary>
        /// 情感记忆
        /// </summary>
        Emotional
    }
}
