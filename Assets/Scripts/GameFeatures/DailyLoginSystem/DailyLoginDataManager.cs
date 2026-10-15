#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
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
#endif
