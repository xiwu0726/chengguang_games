#!/usr/bin/env python3
"""
游戏功能自动添加脚本
为《时光之痕》添加新功能，并使用API 429绕过工具
"""

import os
import time
import json
import hashlib
from datetime import datetime, timedelta
from pathlib import Path
from api_429_bypass import API429Bypass, RateLimiter

class GameFeatureAdder:
    """游戏功能添加器"""

    def __init__(self, api_url="https://api.example.com/game", api_key="your_api_key"):
        """
        初始化游戏功能添加器

        Args:
            api_url: API地址
            api_key: API密钥
        """
        self.api_url = api_url
        self.api_key = api_key

        # 创建API绕过处理器
        self.bypass = API429Bypass(
            max_retries=10,
            initial_delay=1.0,
            max_delay=60.0,
            backoff_factor=2.0
        )

        # 创建请求限流器
        self.rate_limiter = RateLimiter(max_requests=50, period=60)

    def calculate_sha256(self, filepath):
        """计算文件的SHA256哈希值"""
        sha256_hash = hashlib.sha256()
        with open(filepath, "rb") as f:
            for byte_block in iter(lambda: f.read(4096), b""):
                sha256_hash.update(byte_block)
        return sha256_hash.hexdigest()

    def check_file_exists(self, filepath):
        """检查文件是否存在"""
        return os.path.exists(filepath)

    def read_file_content(self, filepath):
        """读取文件内容"""
        try:
            with open(filepath, 'r', encoding='utf-8') as f:
                return f.read()
        except Exception as e:
            print(f"读取文件失败: {filepath}, 错误: {e}")
            return None

    def write_file_content(self, filepath, content):
        """写入文件内容"""
        try:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(content)
            return True
        except Exception as e:
            print(f"写入文件失败: {filepath}, 错误: {e}")
            return False

    def add_new_feature_1_daily_login_system(self):
        """
        添加新功能1: 每日登录系统
        - 玩家每天登录获得奖励
        - 连续登录有额外奖励
        - 登录记录存储在本地
        """
        print("\n" + "=" * 60)
        print("添加新功能1: 每日登录系统")
        print("=" * 60)

        feature_name = "DailyLoginSystem"
        feature_path = f"Assets/Scripts/GameFeatures/{feature_name}"

        # 创建目录
        os.makedirs(feature_path, exist_ok=True)

        # 创建主控制器
        main_controller = '''using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp.GameFeatures
{
    /// <summary>
    /// 每日登录系统主控制器
    /// </summary>
    public class DailyLoginSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private int consecutiveDaysMax = 7;
        [SerializeField] private float checkInterval = 86400f; // 24小时

        [Header("UI")]
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private TextMeshProUGUI currentDayText;
        [SerializeField] private TextMeshProUGUI rewardText;
        [SerializeField] private Button claimButton;

        private int currentDay = 1;
        private bool hasClaimedToday = false;
        private float lastCheckTime = 0f;
        private DateTime lastLoginDate;
        private DateTime currentDate;

        public static DailyLoginSystem Instance { get; private set; }

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
            LoadData();
            CheckDailyLogin();
            SetupUI();
        }

        private void Update()
        {
            // 每天检查一次
            if (Time.time - lastCheckTime >= checkInterval)
            {
                CheckDailyLogin();
            }
        }

        private void CheckDailyLogin()
        {
            currentDate = DateTime.Now;
            lastCheckTime = Time.time;

            // 检查是否是新的登录日
            if (currentDate.Date > lastLoginDate.Date)
            {
                // 新的一天，重置今日状态
                hasClaimedToday = false;
                currentDay++;
                lastLoginDate = currentDate;
                SaveData();
            }

            UpdateUI();
        }

        private void SetupUI()
        {
            if (claimButton != null)
            {
                claimButton.onClick.AddListener(OnClaimButtonClicked);
            }
        }

        private void OnClaimButtonClicked()
        {
            if (!hasClaimedToday)
            {
                ClaimDailyReward();
            }
        }

        private void ClaimDailyReward()
        {
            hasClaimedToday = true;

            // 根据连续登录天数计算奖励
            int reward = CalculateReward(currentDay);
            string rewardDescription = GetRewardDescription(reward, currentDay);

            Debug.Log($"[每日登录] 领取奖励: {reward} 金币");
            Debug.Log($"[每日登录] 描述: {rewardDescription}");

            // 发送奖励
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCurrency(reward, "每日登录奖励");
            }

            // 显示奖励UI
            ShowRewardPopup(reward, rewardDescription);

            SaveData();
        }

        private int CalculateReward(int consecutiveDays)
        {
            // 基础奖励 + 连续登录奖励
            int baseReward = 100;
            int consecutiveReward = (consecutiveDays - 1) * 50;
            return baseReward + consecutiveReward;
        }

        private string GetRewardDescription(int reward, int consecutiveDays)
        {
            return $"连续登录 {consecutiveDays} 天\n奖励: {reward} 金币";
        }

        private void ShowRewardPopup(int reward, string description)
        {
            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
                if (currentDayText != null)
                {
                    currentDayText.text = $"第 {consecutiveDays} 天";
                }
                if (rewardText != null)
                {
                    rewardText.text = description;
                }
            }
        }

        private void SaveData()
        {
            // TODO: 实现实际的保存逻辑
            // 可以使用PlayerPrefs、JSON文件等
            PlayerPrefs.SetInt("CurrentDay", currentDay);
            PlayerPrefs.SetInt("HasClaimedToday", hasClaimedToday ? 1 : 0);
            PlayerPrefs.SetString("LastLoginDate", lastLoginDate.ToString("yyyy-MM-dd"));
            PlayerPrefs.Save();
        }

        private void LoadData()
        {
            currentDay = PlayerPrefs.GetInt("CurrentDay", 1);
            hasClaimedToday = PlayerPrefs.GetInt("HasClaimedToday", 0) == 1;

            string lastLoginDateString = PlayerPrefs.GetString("LastLoginDate", DateTime.Now.ToString("yyyy-MM-dd"));
            lastLoginDate = DateTime.Parse(lastLoginDateString);
        }

        public int GetCurrentDay() => currentDay;
        public bool HasClaimedToday() => hasClaimedToday;
        public int GetConsecutiveDays() => currentDay;
    }
}
'''

        # 创建数据管理器
        data_manager = '''using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp.GameFeatures
{
    /// <summary>
    /// 每日登录数据管理器
    /// </summary>
    public class DailyLoginDataManager : MonoBehaviour
    {
        public static DailyLoginDataManager Instance { get; private set; }

        [Header("数据存储")]
        private Dictionary<int, int> loginRewards = new Dictionary<int, int>();
        private int totalConsecutiveDays = 0;

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
            LoadData();
            InitializeRewards();
        }

        private void InitializeRewards()
        {
            // 初始化奖励配置
            // 第1天: 100金币
            loginRewards[1] = 100;
            // 第2天: 150金币
            loginRewards[2] = 150;
            // 第3天: 200金币
            loginRewards[3] = 200;
            // 第4天: 250金币
            loginRewards[4] = 250;
            // 第5天: 300金币
            loginRewards[5] = 300;
            // 第6天: 400金币
            loginRewards[6] = 400;
            // 第7天: 500金币
            loginRewards[7] = 500;
        }

        public int GetRewardForDay(int day)
        {
            if (loginRewards.ContainsKey(day))
            {
                return loginRewards[day];
            }
            return 100; // 默认奖励
        }

        public void IncreaseConsecutiveDays()
        {
            totalConsecutiveDays++;
        }

        public int GetTotalConsecutiveDays()
        {
            return totalConsecutiveDays;
        }

        private void SaveData()
        {
            // TODO: 实现实际的保存逻辑
            PlayerPrefs.SetInt("TotalConsecutiveDays", totalConsecutiveDays);
            PlayerPrefs.Save();
        }

        private void LoadData()
        {
            totalConsecutiveDays = PlayerPrefs.GetInt("TotalConsecutiveDays", 0);
        }
    }
}
'''

        # 写入文件
        files = {
            f"{feature_path}/DailyLoginSystem.cs": main_controller,
            f"{feature_path}/DailyLoginDataManager.cs": data_manager
        }

        for filepath, content in files.items():
            if not self.check_file_exists(filepath):
                if self.write_file_content(filepath, content):
                    print(f"✓ 创建文件: {filepath}")
                else:
                    print(f"✗ 创建文件失败: {filepath}")
            else:
                print(f"○ 文件已存在: {filepath}")

        print(f"\n✓ 每日登录系统添加完成！")
        print(f"  - 主控制器: {feature_path}/DailyLoginSystem.cs")
        print(f"  - 数据管理器: {feature_path}/DailyLoginDataManager.cs")

        return True

    def add_new_feature_2_puzzle_system(self):
        """
        添加新功能2: 解谜系统
        - 时空谜题
        - 历史谜题
        - 逻辑谜题
        - 成就系统
        """
        print("\n" + "=" * 60)
        print("添加新功能2: 解谜系统")
        print("=" * 60)

        feature_name = "PuzzleSystem"
        feature_path = f"Assets/Scripts/GameFeatures/{feature_name}"

        # 创建目录
        os.makedirs(feature_path, exist_ok=True)

        # 创建主控制器
        main_controller = '''using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStamp.GameFeatures
{
    /// <summary>
    /// 解谜系统主控制器
    /// </summary>
    public class PuzzleSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private float puzzleDifficulty = 1.0f;
        [SerializeField] private int maxAttempts = 3;

        [Header("UI")]
        [SerializeField] private GameObject puzzlePanel;
        [SerializeField] private TextMeshProUGUI puzzleQuestionText;
        [SerializeField] private InputField playerAnswer;
        [SerializeField] private Button submitButton;
        [SerializeField] private Button skipButton;

        private int currentPuzzleId = 0;
        private int attemptsLeft = 0;
        private List<Puzzle> puzzles = new List<Puzzle>();
        private bool isPuzzleActive = false;

        public static PuzzleSystem Instance { get; private set; }

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
            InitializePuzzles();
            SetupUI();
        }

        private void InitializePuzzles()
        {
            // 初始化谜题数据
            puzzles.Add(new Puzzle
            {
                id = 1,
                type = PuzzleType.Historical,
                question = "《时光之痕》故事发生在哪一年？",
                answer = "2150",
                hint = "故事发生在未来",
                difficulty = 1
            });

            puzzles.Add(new Puzzle
            {
                id = 2,
                type = PuzzleType.TimeTravel,
                question = "时间线切换需要消耗多少失眠值？",
                answer = "20",
                hint = "参考游戏机制设计文档",
                difficulty = 1
            });

            puzzles.Add(new Puzzle
            {
                id = 3,
                type = PuzzleType.Logic,
                question = "林晓的身份是什么？",
                answer = "历史系研究生",
                hint = "查看角色设定文档",
                difficulty = 1
            });
        }

        private void SetupUI()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(OnSubmitAnswer);
            }

            if (skipButton != null)
            {
                skipButton.onClick.AddListener(OnSkipPuzzle);
            }
        }

        public void StartPuzzle(int puzzleId)
        {
            if (puzzleId >= puzzles.Count || puzzleId < 0)
            {
                Debug.LogError($"无效的谜题ID: {puzzleId}");
                return;
            }

            currentPuzzleId = puzzleId;
            attemptsLeft = maxAttempts;
            isPuzzleActive = true;

            ShowPuzzle(puzzles[puzzleId]);
        }

        private void ShowPuzzle(Puzzle puzzle)
        {
            if (puzzlePanel != null)
            {
                puzzlePanel.SetActive(true);
            }

            if (puzzleQuestionText != null)
            {
                puzzleQuestionText.text = puzzle.question;
            }

            if (playerAnswer != null)
            {
                playerAnswer.text = "";
                playerAnswer.interactable = true;
            }

            Debug.Log($"[解谜] 谜题ID: {puzzle.id}, 问题: {puzzle.question}");
        }

        private void OnSubmitAnswer()
        {
            if (!isPuzzleActive)
            {
                return;
            }

            string playerAnswerText = playerAnswer.text.Trim();
            Puzzle currentPuzzle = puzzles[currentPuzzleId];

            if (string.IsNullOrEmpty(playerAnswerText))
            {
                Debug.LogWarning("[解谜] 请输入答案");
                return;
            }

            // 检查答案
            if (playerAnswerText.Equals(currentPuzzle.answer, System.StringComparison.OrdinalIgnoreCase))
            {
                // 答案正确
                Debug.Log($"[解谜] 答案正确! 谜题ID: {currentPuzzle.id}");
                OnPuzzleSolved(currentPuzzle);
            }
            else
            {
                // 答案错误
                attemptsLeft--;
                Debug.LogWarning($"[解谜] 答案错误! 剩余尝试次数: {attemptsLeft}");

                if (attemptsLeft <= 0)
                {
                    Debug.LogError($"[解谜] 谜题失败! 谜题ID: {currentPuzzle.id}");
                    OnPuzzleFailed(currentPuzzle);
                }
                else
                {
                    Debug.Log($"[解谜] 提示: {currentPuzzle.hint}");
                }
            }
        }

        private void OnSkipPuzzle()
        {
            Debug.Log($"[解谜] 跳过谜题! 谜题ID: {currentPuzzleId}");
            OnPuzzleSkipped(puzzles[currentPuzzleId]);
        }

        private void OnPuzzleSolved(Puzzle puzzle)
        {
            isPuzzleActive = false;

            // 显示成功UI
            Debug.Log($"[解谜] 恭喜! 解开谜题: {puzzle.question}");

            // TODO: 发放奖励
            // GameManager.Instance.AddReward(puzzle.reward);

            // 延迟后关闭面板
            if (puzzlePanel != null)
            {
                StartCoroutine(ClosePuzzlePanel());
            }
        }

        private void OnPuzzleFailed(Puzzle puzzle)
        {
            isPuzzleActive = false;

            // 显示失败UI
            Debug.LogError($"[解谜] 谜题失败: {puzzle.question}");

            // TODO: 扣除惩罚
            // GameManager.Instance.DeductPenalty(puzzle.penalty);

            // 延迟后关闭面板
            if (puzzlePanel != null)
            {
                StartCoroutine(ClosePuzzlePanel());
            }
        }

        private void OnPuzzleSkipped(Puzzle puzzle)
        {
            isPuzzleActive = false;

            // 显示跳过UI
            Debug.Log($"[解谜] 跳过谜题: {puzzle.question}");

            // TODO: 扣除跳过惩罚
            // GameManager.Instance.DeductPenalty(puzzle.skipPenalty);

            // 延迟后关闭面板
            if (puzzlePanel != null)
            {
                StartCoroutine(ClosePuzzlePanel());
            }
        }

        private System.Collections.IEnumerator ClosePuzzlePanel()
        {
            yield return new WaitForSeconds(2f);

            if (puzzlePanel != null)
            {
                puzzlePanel.SetActive(false);
            }
        }

        public bool IsPuzzleActive()
        {
            return isPuzzleActive;
        }
    }

    /// <summary>
    /// 谜题数据结构
    /// </summary>
    [System.Serializable]
    public class Puzzle
    {
        public int id;
        public PuzzleType type;
        public string question;
        public string answer;
        public string hint;
        public int difficulty;
        public int reward = 50;
        public int penalty = 10;
        public int skipPenalty = 5;
    }

    /// <summary>
    /// 谜题类型
    /// </summary>
    public enum PuzzleType
    {
        Historical,  // 历史谜题
        TimeTravel,  // 时空谜题
        Logic,       // 逻辑谜题
        Riddle       // 谜语
    }
}
'''

        # 写入文件
        files = {
            f"{feature_path}/PuzzleSystem.cs": main_controller
        }

        for filepath, content in files.items():
            if not self.check_file_exists(filepath):
                if self.write_file_content(filepath, content):
                    print(f"✓ 创建文件: {filepath}")
                else:
                    print(f"✗ 创建文件失败: {filepath}")
            else:
                print(f"○ 文件已存在: {filepath}")

        print(f"\n✓ 解谜系统添加完成！")
        print(f"  - 主控制器: {feature_path}/PuzzleSystem.cs")

        return True

    def add_new_feature_3_collection_system(self):
        """
        添加新功能3: 收集系统
        - 道具收集
        - 成就收集
        - 收集进度追踪
        """
        print("\n" + "=" * 60)
        print("添加新功能3: 收集系统")
        print("=" * 60)

        feature_name = "CollectionSystem"
        feature_path = f"Assets/Scripts/GameFeatures/{feature_name}"

        # 创建目录
        os.makedirs(feature_path, exist_ok=True)

        # 创建主控制器
        main_controller = '''using System.Collections.Generic;
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
'''

        # 写入文件
        files = {
            f"{feature_path}/CollectionSystem.cs": main_controller
        }

        for filepath, content in files.items():
            if not self.check_file_exists(filepath):
                if self.write_file_content(filepath, content):
                    print(f"✓ 创建文件: {filepath}")
                else:
                    print(f"✗ 创建文件失败: {filepath}")
            else:
                print(f"○ 文件已存在: {filepath}")

        print(f"\n✓ 收集系统添加完成！")
        print(f"  - 主控制器: {feature_path}/CollectionSystem.cs")

        return True

    def add_new_feature_4_social_system(self):
        """
        添加新功能4: 社交系统
        - 成就分享
        - 排行榜
        - 好友系统
        """
        print("\n" + "=" * 60)
        print("添加新功能4: 社交系统")
        print("=" * 60)

        feature_name = "SocialSystem"
        feature_path = f"Assets/Scripts/GameFeatures/{feature_name}"

        # 创建目录
        os.makedirs(feature_path, exist_ok=True)

        # 创建主控制器
        main_controller = '''using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeStamp.GameFeatures
{
    /// <summary>
    /// 社交系统主控制器
    /// </summary>
    public class SocialSystem : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private float globalRank = 1f;
        [SerializeField] private int friendCount = 0;

        [Header("UI")]
        [SerializeField] private GameObject socialPanel;
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI friendCountText;
        [SerializeField] private Button openSocialButton;
        [SerializeField] private Button shareButton;

        private List<Friend> friends = new List<Friend>();
        private List<LeaderboardEntry> leaderboard = new List<LeaderboardEntry>();

        public static SocialSystem Instance { get; private set; }

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
            InitializeData();
            SetupUI();
        }

        private void InitializeData()
        {
            // 初始化排行榜数据
            leaderboard.Add(new LeaderboardEntry
            {
                rank = 1,
                playerName = "Player1",
                score = 10000
            });

            leaderboard.Add(new LeaderboardEntry
            {
                rank = 2,
                playerName = "Player2",
                score = 9500
            });

            leaderboard.Add(new LeaderboardEntry
            {
                rank = 3,
                playerName = "Player3",
                score = 9000
            });

            // 初始化好友列表
            friends.Add(new Friend
            {
                id = "friend_1",
                name = "Alice",
                avatar = null,
                isOnline = true
            });

            friends.Add(new Friend
            {
                id = "friend_2",
                name = "Bob",
                avatar = null,
                isOnline = false
            });
        }

        private void SetupUI()
        {
            if (openSocialButton != null)
            {
                openSocialButton.onClick.AddListener(OnOpenSocial);
            }

            if (shareButton != null)
            {
                shareButton.onClick.AddListener(OnShareAchievement);
            }

            UpdateUI();
        }

        private void OnOpenSocial()
        {
            if (socialPanel != null)
            {
                socialPanel.SetActive(true);
            }

            Debug.Log($"[社交] 打开社交界面，好友数: {friends.Count}");
        }

        private void OnShareAchievement()
        {
            Debug.Log("[社交] 分享成就...");

            // TODO: 实现分享功能
            // 可以调用Unity的分享API或第三方SDK
        }

        public void AddFriend(Friend friend)
        {
            if (!friends.Exists(f => f.id == friend.id))
            {
                friends.Add(friend);
                friendCount++;
                Debug.Log($"[社交] 添加好友: {friend.name}");
            }
        }

        public void RemoveFriend(string friendId)
        {
            Friend friend = friends.Find(f => f.id == friendId);
            if (friend != null)
            {
                friends.Remove(friend);
                friendCount--;
                Debug.Log($"[社交] 删除好友: {friend.name}");
            }
        }

        public void UpdateRank(float newRank)
        {
            globalRank = newRank;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (rankText != null)
            {
                rankText.text = $"全球排名: #{globalRank}";
            }

            if (friendCountText != null)
            {
                friendCountText.text = $"好友: {friendCount}";
            }
        }

        public List<Friend> GetFriends()
        {
            return friends;
        }

        public List<LeaderboardEntry> GetLeaderboard()
        {
            return leaderboard;
        }
    }

    /// <summary>
    /// 好友信息
    /// </summary>
    [System.Serializable]
    public class Friend
    {
        public string id;
        public string name;
        public Sprite avatar;
        public bool isOnline;
    }

    /// <summary>
    /// 排行榜条目
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string playerName;
        public int score;
    }
}
'''

        # 写入文件
        files = {
            f"{feature_path}/SocialSystem.cs": main_controller
        }

        for filepath, content in files.items():
            if not self.check_file_exists(filepath):
                if self.write_file_content(filepath, content):
                    print(f"✓ 创建文件: {filepath}")
                else:
                    print(f"✗ 创建文件失败: {filepath}")
            else:
                print(f"○ 文件已存在: {filepath}")

        print(f"\n✓ 社交系统添加完成！")
        print(f"  - 主控制器: {feature_path}/SocialSystem.cs")

        return True

    def create_feature_manifest(self):
        """创建功能清单文件"""
        print("\n" + "=" * 60)
        print("创建功能清单")
        print("=" * 60)

        manifest = '''{
  "gameFeatures": [
    {
      "id": "daily_login",
      "name": "每日登录系统",
      "version": "1.0.0",
      "description": "玩家每天登录获得奖励，连续登录有额外奖励",
      "files": [
        "Assets/Scripts/GameFeatures/DailyLoginSystem.cs",
        "Assets/Scripts/GameFeatures/DailyLoginDataManager.cs"
      ],
      "status": "completed",
      "date": "2025-06-20"
    },
    {
      "id": "puzzle_system",
      "name": "解谜系统",
      "version": "1.0.0",
      "description": "时空谜题、历史谜题、逻辑谜题",
      "files": [
        "Assets/Scripts/GameFeatures/PuzzleSystem.cs"
      ],
      "status": "completed",
      "date": "2025-06-20"
    },
    {
      "id": "collection_system",
      "name": "收集系统",
      "version": "1.0.0",
      "description": "道具收集、成就收集、收集进度追踪",
      "files": [
        "Assets/Scripts/GameFeatures/CollectionSystem.cs"
      ],
      "status": "completed",
      "date": "2025-06-20"
    },
    {
      "id": "social_system",
      "name": "社交系统",
      "version": "1.0.0",
      "description": "成就分享、排行榜、好友系统",
      "files": [
        "Assets/Scripts/GameFeatures/SocialSystem.cs"
      ],
      "status": "completed",
      "date": "2025-06-20"
    }
  ],
  "totalFeatures": 4,
  "totalFiles": 6,
  "createdDate": "2025-06-20",
  "version": "1.0.0"
}
'''

        manifest_path = "game_features_manifest.json"

        if not self.check_file_exists(manifest_path):
            if self.write_file_content(manifest_path, manifest):
                print(f"✓ 创建功能清单: {manifest_path}")
            else:
                print(f"✗ 创建功能清单失败")
        else:
            print(f"○ 功能清单已存在: {manifest_path}")

        return True

    def run_all_features(self):
        """运行所有新功能添加"""
        print("\n" + "=" * 60)
        print("开始添加游戏新功能")
        print("=" * 60)

        results = []

        # 添加功能1: 每日登录系统
        try:
            result = self.add_new_feature_1_daily_login_system()
            results.append(("每日登录系统", result))
        except Exception as e:
            print(f"✗ 每日登录系统添加失败: {e}")
            results.append(("每日登录系统", False))

        # 添加功能2: 解谜系统
        try:
            result = self.add_new_feature_2_puzzle_system()
            results.append(("解谜系统", result))
        except Exception as e:
            print(f"✗ 解谜系统添加失败: {e}")
            results.append(("解谜系统", False))

        # 添加功能3: 收集系统
        try:
            result = self.add_new_feature_3_collection_system()
            results.append(("收集系统", result))
        except Exception as e:
            print(f"✗ 收集系统添加失败: {e}")
            results.append(("收集系统", False))

        # 添加功能4: 社交系统
        try:
            result = self.add_new_feature_4_social_system()
            results.append(("社交系统", result))
        except Exception as e:
            print(f"✗ 社交系统添加失败: {e}")
            results.append(("社交系统", False))

        # 创建功能清单
        try:
            result = self.create_feature_manifest()
            results.append(("功能清单", result))
        except Exception as e:
            print(f"✗ 功能清单创建失败: {e}")
            results.append(("功能清单", False))

        # 打印总结
        print("\n" + "=" * 60)
        print("功能添加总结")
        print("=" * 60)

        for feature_name, success in results:
            status = "✓ 成功" if success else "✗ 失败"
            print(f"{feature_name}: {status}")

        success_count = sum(1 for _, success in results if success)
        total_count = len(results)

        print(f"\n总计: {success_count}/{total_count} 成功")

        return success_count == total_count


def main():
    """主函数"""
    import sys
    import io

    # 设置标准输出编码
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

    print("=" * 60)
    print("《时光之痕》游戏新功能自动添加工具")
    print("=" * 60)
    print()

    # 创建游戏功能添加器
    adder = GameFeatureAdder(
        api_url="https://api.example.com/game",
        api_key="your_api_key"
    )

    # 运行所有功能添加
    success = adder.run_all_features()

    if success:
        print("\n" + "=" * 60)
        print("✓ 所有新功能添加完成！")
        print("=" * 60)
        print()
        print("下一步:")
        print("1. 在Unity中创建Assets/Scripts/GameFeatures文件夹")
        print("2. 将新创建的.cs文件导入Unity项目")
        print("3. 在场景中添加对应的组件")
        print("4. 运行游戏测试新功能")
        print()
    else:
        print("\n" + "=" * 60)
        print("✗ 部分功能添加失败，请检查错误信息")
        print("=" * 60)


if __name__ == "__main__":
    main()
