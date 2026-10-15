#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStamp.GameFeatures
{
    /// <summary>
    /// 收集系统主控制器
    /// </summary>
    public class CollectionSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private float collectionProgress = 0f;
        [SerializeField] private int totalItems = 100;

        [Header("UI")]
        [SerializeField] private GameObject collectionPanel;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private TextMeshProUGUI itemCountText;
        [SerializeField] private Button openCollectionButton;

        private List<CollectionItem> collectedItems = new List<CollectionItem>();
        private List<CollectionItem> availableItems = new List<CollectionItem>();

        public static CollectionSystem Instance { get; private set; }

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

        private void Start()
        {
            InitializeItems();
            SetupUI();
        }

        private void InitializeItems()
        {
            // 初始化可收集物品
            availableItems.Add(new CollectionItem
            {
                id = "item_1",
                name = "时空碎片",
                description = "从时间裂缝中收集的碎片",
                icon = null,
                rarity = Rarity.Common,
                isCollected = false
            });

            availableItems.Add(new CollectionItem
            {
                id = "item_2",
                name = "记忆晶体",
                description = "强化记忆的晶体",
                icon = null,
                rarity = Rarity.Uncommon,
                isCollected = false
            });

            availableItems.Add(new CollectionItem
            {
                id = "item_3",
                name = "时间罗盘",
                description = "指引时间线方向的罗盘",
                icon = null,
                rarity = Rarity.Rare,
                isCollected = false
            });

            availableItems.Add(new CollectionItem
            {
                id = "item_4",
                name = "时空钥匙",
                description = "打开时间之门的钥匙",
                icon = null,
                rarity = Rarity.Epic,
                isCollected = false
            });

            availableItems.Add(new CollectionItem
            {
                id = "item_5",
                name = "永恒之心",
                description = "传说中能掌控时间的宝石",
                icon = null,
                rarity = Rarity.Legendary,
                isCollected = false
            });
        }

        private void SetupUI()
        {
            if (openCollectionButton != null)
            {
                openCollectionButton.onClick.AddListener(OnOpenCollection);
            }

            UpdateProgress();
        }

        private void OnOpenCollection()
        {
            if (collectionPanel != null)
            {
                collectionPanel.SetActive(true);
            }

            Debug.Log($"[收集] 打开收集界面，已收集: {collectedItems.Count}/{availableItems.Count}");
        }

        public void CollectItem(string itemId)
        {
            CollectionItem item = availableItems.Find(i => i.id == itemId);

            if (item == null || item.isCollected)
            {
                Debug.LogWarning($"[收集] 物品不存在或已收集: {itemId}");
                return;
            }

            // 收集物品
            item.isCollected = true;
            collectedItems.Add(item);

            Debug.Log($"[收集] 收集物品: {item.name}");
            Debug.Log($"[收集] 当前进度: {collectedItems.Count}/{availableItems.Count}");

            // 发放奖励
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddReward(10, "收集奖励");
            }

            UpdateProgress();
        }

        private void UpdateProgress()
        {
            float progress = (float)collectedItems.Count / totalItems * 100f;
            collectionProgress = progress;

            if (progressText != null)
            {
                progressText.text = $"进度: {collectedItems.Count}/{totalItems} ({progress:F1}%)";
            }

            if (itemCountText != null)
            {
                itemCountText.text = $"已收集: {collectedItems.Count}";
            }
        }

        public List<CollectionItem> GetCollectedItems()
        {
            return collectedItems;
        }

        public List<CollectionItem> GetAvailableItems()
        {
            return availableItems;
        }

        public float GetCollectionProgress()
        {
            return collectionProgress;
        }
    }

    /// <summary>
    /// 收集物品
    /// </summary>
    [System.Serializable]
    public class CollectionItem
    {
        public string id;
        public string name;
        public string description;
        public Sprite icon;
        public Rarity rarity;
        public bool isCollected;
    }

    /// <summary>
    /// 物品稀有度
    /// </summary>
    public enum Rarity
    {
        Common,      // 普通
        Uncommon,    // 罕见
        Rare,        // 稀有
        Epic,        // 史诗
        Legendary    // 传说
    }
}
#endif
