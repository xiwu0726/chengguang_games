using System.Collections;
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
            return $"连续登录 {consecutiveDays} 天
奖励: {reward} 金币";
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
