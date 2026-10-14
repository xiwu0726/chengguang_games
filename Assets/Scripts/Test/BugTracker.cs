using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// Bug跟踪系统
    /// 记录、分类和追踪bug
    /// </summary>
    public class BugTracker : MonoBehaviour
    {
        public static BugTracker Instance { get; private set; }

        [Header("Bug跟踪")]
        [SerializeField] private bool autoLogBugs = true;
        [SerializeField] private bool showBugUI = false;

        private List<Bug> bugs = new List<Bug>();
        private List<Bug> fixedBugs = new List<Bug>();
        private List<Bug> pendingBugs = new List<Bug>();

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
        /// 记录bug
        /// </summary>
        public void LogBug(string title, string description, BugPriority priority = BugPriority.Medium, string location = "")
        {
            Bug bug = new Bug
            {
                id = Guid.NewGuid().ToString(),
                title = title,
                description = description,
                priority = priority,
                location = location,
                status = BugStatus.Open,
                createdAt = DateTime.Now,
                reportedBy = "开发团队"
            };

            bugs.Add(bug);
            pendingBugs.Add(bug);

            Debug.LogError($"[Bug跟踪] 记录Bug: {title} (优先级: {priority})");
        }

        /// <summary>
        /// 标记bug为已修复
        /// </summary>
        public bool FixBug(string bugId)
        {
            Bug bug = bugs.Find(b => b.id == bugId);
            if (bug == null)
            {
                Debug.LogWarning($"[Bug跟踪] 未找到Bug: {bugId}");
                return false;
            }

            bug.status = BugStatus.Fixed;
            bug.fixedAt = DateTime.Now;

            fixedBugs.Add(bug);
            pendingBugs.Remove(bug);

            Debug.Log($"[Bug跟踪] Bug已修复: {bug.title}");
            return true;
        }

        /// <summary>
        /// 标记bug为待修复
        /// </summary>
        public bool MarkBugAsPending(string bugId)
        {
            Bug bug = bugs.Find(b => b.id == bugId);
            if (bug == null)
            {
                Debug.LogWarning($"[Bug跟踪] 未找到Bug: {bugId}");
                return false;
            }

            bug.status = BugStatus.Pending;
            pendingBugs.Add(bug);

            Debug.Log($"[Bug跟踪] Bug标记为待修复: {bug.title}");
            return true;
        }

        /// <summary>
        /// 获取所有bug
        /// </summary>
        public List<Bug> GetAllBugs()
        {
            return new List<Bug>(bugs);
        }

        /// <summary>
        /// 获取未修复的bug
        /// </summary>
        public List<Bug> GetPendingBugs()
        {
            return new List<Bug>(pendingBugs);
        }

        /// <summary>
        /// 获取已修复的bug
        * </summary>
        public List<Bug> GetFixedBugs()
        {
            return new List<Bug>(fixedBugs);
        }

        /// <summary>
        /// 获取指定优先级的bug
        /// </summary>
        public List<Bug> GetBugsByPriority(BugPriority priority)
        {
            List<Bug> result = new List<Bug>();
            foreach (var bug in bugs)
            {
                if (bug.priority == priority)
                {
                    result.Add(bug);
                }
            }
            return result;
        }

        /// <summary>
        /// 获取bug统计信息
        /// </summary>
        public BugStatistics GetStatistics()
        {
            return new BugStatistics
            {
                TotalBugs = bugs.Count,
                FixedBugs = fixedBugs.Count,
                PendingBugs = pendingBugs.Count,
                CriticalBugs = GetBugsByPriority(BugPriority.Critical).Count,
                HighBugs = GetBugsByPriority(BugPriority.High).Count,
                MediumBugs = GetBugsByPriority(BugPriority.Medium).Count,
                LowBugs = GetBugsByPriority(BugPriority.Low).Count
            };
        }

        /// <summary>
        /// 生成bug报告
        /// </summary>
        public string GenerateBugReport()
        {
            var stats = GetStatistics();

            string report = $@"
========================================
         《时光之痕》Bug报告
========================================

总Bug数: {stats.TotalBugs}
已修复: {stats.FixedBugs}
待修复: {stats.PendingBugs}
未修复: {stats.TotalBugs - stats.FixedBugs}

优先级统计:
- 严重: {stats.CriticalBugs}
- 高: {stats.HighBugs}
- 中: {stats.MediumBugs}
- 低: {stats.LowBugs}

详细Bug列表:
";

            foreach (var bug in bugs)
            {
                report += $"\n[{bug.status}] [{bug.priority}] {bug.title}";
                report += $"\n  描述: {bug.description}";
                report += $"\n  位置: {bug.location}";
                report += $"\n  记录时间: {bug.createdAt}";
                report += $"\n";
            }

            return report;
        }

        /// <summary>
        /// 显示bug报告
        /// </summary>
        public void ShowBugReport()
        {
            string report = GenerateBugReport();
            Debug.Log(report);
            Console.WriteLine(report);
        }
    }

    /// <summary>
    /// Bug优先级
    /// </summary>
    public enum BugPriority
    {
        Critical,
        High,
        Medium,
        Low
    }

    /// <summary>
    /// Bug状态
    /// </summary>
    public enum BugStatus
    {
        Open,
        Pending,
        Fixed
    }

    /// <summary>
    /// Bug数据
    /// </summary>
    public class Bug
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public BugPriority priority { get; set; }
        public string location { get; set; }
        public BugStatus status { get; set; }
        public string reportedBy { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime? fixedAt { get; set; }
    }

    /// <summary>
    /// Bug统计信息
    /// </summary>
    public class BugStatistics
    {
        public int TotalBugs { get; set; }
        public int FixedBugs { get; set; }
        public int PendingBugs { get; set; }
        public int CriticalBugs { get; set; }
        public int HighBugs { get; set; }
        public int MediumBugs { get; set; }
        public int LowBugs { get; set; }
    }
}
