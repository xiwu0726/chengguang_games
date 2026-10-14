using System.Collections.Generic;
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
