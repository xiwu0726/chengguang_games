#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Dialogue.Systems
{
    /// <summary>
    /// 分支剧情逻辑系统 - 管理多分支剧情的执行和状态
    /// </summary>
    public class BranchingStorySystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool autoSaveBranchState = true;
        [SerializeField] private int maxBranchDepth = 3;

        private string currentBranchId = "default";
        private Dictionary<string, Branch> branches = new Dictionary<string, Branch>();
        private Dictionary<string, BranchPath> branchPaths = new Dictionary<string, BranchPath>();
        private Stack<string> branchHistory = new Stack<string>();
        private int currentDepth = 0;

        // 事件
        public event System.Action<string> OnBranchEntered;
        public event System.Action<string> OnBranchExited;
        public event System.Action<string, string> OnBranchTransition;

        private void Awake()
        {
            InitializeBranches();
        }

        /// <summary>
        /// 初始化剧情分支
        /// </summary>
        private void InitializeBranches()
        {
            // 从配置文件加载分支定义（这里使用示例）
            AddBranch("default", "默认主线", "主线剧情", 0);
            AddBranch("alice_good", "艾莉丝好感线", "艾莉丝好感线", 1);
            AddBranch("alice_bad", "艾莉丝负面线", "艾莉丝负面线", 1);
            AddBranch("rain_good", "雷恩好感线", "雷恩好感线", 1);
            AddBranch("rain_bad", "雷恩负面线", "雷恩负面线", 1);
            AddBranch("xiaoya_good", "小雅好感线", "小雅好感线", 1);
            AddBranch("xiaoya_bad", "小雅负面线", "小雅负面线", 1);
            AddBranch("true_end", "真结局", "真结局", 2);
            AddBranch("bad_end_1", "坏结局1", "坏结局1", 2);
            AddBranch("bad_end_2", "坏结局2", "坏结局2", 2);

            Debug.Log($"Initialized {branches.Count} branches.");
        }

        /// <summary>
        /// 添加剧情分支
        /// </summary>
        private void AddBranch(string branchId, string displayName, string description, int depth)
        {
            if (depth > maxBranchDepth)
            {
                Debug.LogWarning($"Branch '{branchId}' exceeds max depth {maxBranchDepth}!");
                return;
            }

            Branch branch = new Branch
            {
                branchId = branchId,
                displayName = displayName,
                description = description,
                depth = depth,
                dialogueLines = new List<DialogueLine>(),
                choices = new List<BranchChoice>()
            };

            branches[branchId] = branch;
        }

        /// <summary>
        /// 进入剧情分支
        /// </summary>
        public void EnterBranch(string branchId, System.Action onBranchComplete = null)
        {
            if (!branches.ContainsKey(branchId))
            {
                Debug.LogWarning($"Branch '{branchId}' not found!");
                return;
            }

            Branch branch = branches[branchId];

            // 检查分支深度
            if (currentDepth + branch.depth > maxBranchDepth)
            {
                Debug.LogWarning($"Cannot enter branch '{branchId}' - depth limit exceeded!");
                return;
            }

            // 记录历史
            branchHistory.Push(currentBranchId);

            // 更新当前分支
            string previousBranchId = currentBranchId;
            currentBranchId = branchId;
            currentDepth += branch.depth;

            // 触发事件
            OnBranchEntered?.Invoke(branchId);
            OnBranchTransition?.Invoke(previousBranchId, branchId);

            if (logChoices)
            {
                Debug.Log($"Entered branch: {branch.displayName} (ID: {branchId})");
            }

            // 自动保存
            if (autoSaveBranchState)
            {
                SaveBranchState(branchId);
            }
        }

        /// <summary>
        /// 退出当前分支
        /// </summary>
        public void ExitBranch(System.Action onBranchComplete = null)
        {
            if (branchHistory.Count == 0)
            {
                Debug.LogWarning("No branch to exit!");
                return;
            }

            string previousBranchId = currentBranchId;
            string previousBranchDisplayName = branches.ContainsKey(previousBranchId) ? branches[previousBranchId].displayName : "unknown";

            // 恢复上一个分支
            currentBranchId = branchHistory.Pop();
            currentDepth -= branches[previousBranchId].depth;

            // 触发事件
            OnBranchExited?.Invoke(previousBranchId);
            OnBranchTransition?.Invoke(previousBranchId, currentBranchId);

            if (logChoices)
            {
                Debug.Log($"Exited branch: {previousBranchDisplayName} (ID: {previousBranchId})");
            }

            // 触发完成回调
            onBranchComplete?.Invoke();
        }

        /// <summary>
        /// 切换到指定分支
        /// </summary>
        public void SwitchToBranch(string branchId, System.Action onBranchComplete = null)
        {
            ExitBranch(() =>
            {
                EnterBranch(branchId, onBranchComplete);
            });
        }

        /// <summary>
        /// 添加对话行到当前分支
        /// </summary>
        public void AddDialogueLine(DialogueLine dialogueLine)
        {
            if (!branches.ContainsKey(currentBranchId))
            {
                Debug.LogWarning($"Branch '{currentBranchId}' not found!");
                return;
            }

            branches[currentBranchId].dialogueLines.Add(dialogueLine);
        }

        /// <summary>
        /// 添加分支选择
        /// </summary>
        public void AddBranchChoice(BranchChoice branchChoice)
        {
            if (!branches.ContainsKey(currentBranchId))
            {
                Debug.LogWarning($"Branch '{currentBranchId}' not found!");
                return;
            }

            branches[currentBranchId].choices.Add(branchChoice);
        }

        /// <summary>
        /// 获取当前分支
        /// </summary>
        public Branch GetCurrentBranch()
        {
            return branches.ContainsKey(currentBranchId) ? branches[currentBranchId] : null;
        }

        /// <summary>
        /// 获取指定分支
        /// </summary>
        public Branch GetBranch(string branchId)
        {
            return branches.ContainsKey(branchId) ? branches[branchId] : null;
        }

        /// <summary>
        /// 获取所有分支
        /// </summary>
        public List<Branch> GetAllBranches()
        {
            return new List<Branch>(branches.Values);
        }

        /// <summary>
        /// 获取分支路径
        /// </summary>
        public BranchPath GetBranchPath(string fromBranchId, string toBranchId)
        {
            if (branchPaths.ContainsKey($"{fromBranchId}_{toBranchId}"))
            {
                return branchPaths[$"{fromBranchId}_{toBranchId}"];
            }

            return null;
        }

        /// <summary>
        /// 检查是否在指定分支中
        /// </summary>
        public bool IsInBranch(string branchId)
        {
            return currentBranchId == branchId;
        }

        /// <summary>
        /// 检查是否在默认分支中
        /// </summary>
        public bool IsInDefaultBranch()
        {
            return currentBranchId == "default";
        }

        /// <summary>
        /// 重置到默认分支
        /// </summary>
        public void ResetToDefaultBranch()
        {
            while (currentBranchId != "default")
            {
                ExitBranch();
            }
        }

        /// <summary>
        /// 保存分支状态
        /// </summary>
        private void SaveBranchState(string branchId)
        {
            // 在实际项目中，这里会将分支状态保存到文件或数据库
            // 例如：SaveManager.Instance.SaveBranchState(branchId, currentBranchId);
            Debug.Log($"Saved branch state: {branchId}");
        }

        /// <summary>
        /// 加载分支状态
        /// </summary>
        public void LoadBranchState(string branchId)
        {
            // 在实际项目中，这里会从文件或数据库加载分支状态
            EnterBranch(branchId);
        }

        /// <summary>
        /// 获取当前分支ID
        /// </summary>
        public string GetCurrentBranchId()
        {
            return currentBranchId;
        }

        /// <summary>
        /// 获取分支历史
        /// </summary>
        public List<string> GetBranchHistory()
        {
            return new List<string>(branchHistory);
        }

        /// <summary>
        /// 获取当前深度
        /// </summary>
        public int GetCurrentDepth()
        {
            return currentDepth;
        }

        /// <summary>
        /// 检查是否可以进入新分支
        /// </summary>
        public bool CanEnterBranch(string branchId)
        {
            if (!branches.ContainsKey(branchId)) return false;

            Branch branch = branches[branchId];

            // 检查深度限制
            if (currentDepth + branch.depth > maxBranchDepth) return false;

            return true;
        }

        private bool logChoices = true;

        /// <summary>
        /// 获取分支数量
        /// </summary>
        public int GetBranchCount()
        {
            return branches.Count;
        }
    }

    /// <summary>
    /// 剧情分支数据结构
    /// </summary>
    [System.Serializable]
    public class Branch
    {
        /// <summary>
        /// 分支ID
        /// </summary>
        public string branchId;

        /// <summary>
        /// 显示名称
        /// </summary>
        public string displayName;

        /// <summary>
        /// 描述
        /// </summary>
        public string description;

        /// <summary>
        /// 分支深度
        /// </summary>
        public int depth;

        /// <summary>
        /// 对话行列表
        /// </summary>
        public List<DialogueLine> dialogueLines;

        /// <summary>
        /// 分支选择列表
        /// </summary>
        public List<BranchChoice> choices;

        public Branch()
        {
            branchId = "";
            displayName = "";
            description = "";
            depth = 0;
            dialogueLines = new List<DialogueLine>();
            choices = new List<BranchChoice>();
        }
    }

    /// <summary>
    /// 分支选择
    /// </summary>
    [System.Serializable]
    public class BranchChoice
    {
        /// <summary>
        /// 选择文本
        /// </summary>
        public string text;

        /// <summary>
        /// 目标分支ID
        /// </summary>
        public string targetBranchId;

        /// <summary>
        /// 触发条件
        /// </summary>
        public System.Func<bool> triggerCondition;

        /// <summary>
        /// 选择效果
        /// </summary>
        public ChoiceEffect effect;

        public BranchChoice()
        {
            text = "";
            targetBranchId = "";
            triggerCondition = null;
            effect = new ChoiceEffect();
        }

        public BranchChoice(string text, string targetBranchId)
        {
            this.text = text;
            this.targetBranchId = targetBranchId;
            this.triggerCondition = null;
            this.effect = new ChoiceEffect();
        }
    }

    /// <summary>
    /// 分支路径数据结构
    /// </summary>
    [System.Serializable]
    public class BranchPath
    {
        /// <summary>
        /// 源分支ID
        /// </summary>
        public string fromBranchId;

        /// <summary>
        /// 目标分支ID
        /// </summary>
        public string toBranchId;

        /// <summary>
        /// 路径描述
        /// </summary>
        public string description;

        /// <summary>
        /// 路径类型
        /// </summary>
        public PathType pathType;

        /// <summary>
        /// 路径条件
        /// </summary>
        public System.Func<bool> pathCondition;

        public BranchPath()
        {
            fromBranchId = "";
            toBranchId = "";
            description = "";
            pathType = PathType.Normal;
            pathCondition = null;
        }

        public enum PathType
        {
            Normal,
            Hidden,
            Secret
        }
    }
}
#endif
