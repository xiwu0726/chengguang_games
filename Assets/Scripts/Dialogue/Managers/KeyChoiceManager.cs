#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Dialogue.Managers
{
    /// <summary>
    /// 关键选择点管理器 - 管理所有关键选择点的配置和触发
    /// </summary>
    public class KeyChoiceManager : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private List<KeyChoice> keyChoices;

        [Header("Settings")]
        [SerializeField] private bool logChoices = true;

        private Dictionary<string, KeyChoice> keyChoiceMap = new Dictionary<string, KeyChoice>();
        private int currentBranchId = 0;

        // 事件
        public event System.Action<string, int> OnChoiceMade;
        public event System.Action<string> OnBranchChanged;

        private void Awake()
        {
            InitializeKeyChoices();
        }

        /// <summary>
        /// 初始化关键选择点
        /// </summary>
        private void InitializeKeyChoices()
        {
            keyChoiceMap.Clear();

            for (int i = 0; i < keyChoices.Count; i++)
            {
                keyChoiceMap[keyChoices[i].choiceId] = keyChoices[i];
            }

            Debug.Log($"Initialized {keyChoices.Count} key choices.");
        }

        /// <summary>
        /// 触发关键选择
        /// </summary>
        public void TriggerKeyChoice(string choiceId, int choiceIndex, System.Action onChoiceMade)
        {
            if (!keyChoiceMap.ContainsKey(choiceId))
            {
                Debug.LogWarning($"Key choice '{choiceId}' not found!");
                onChoiceMade?.Invoke();
                return;
            }

            KeyChoice keyChoice = keyChoiceMap[choiceId];

            // 记录选择
            if (logChoices)
            {
                Debug.Log($"Key Choice '{keyChoice.choiceName}' triggered. Branch: {keyChoice.branchId}, Choice: {choiceIndex}");
            }

            // 触发选择事件
            OnChoiceMade?.Invoke(choiceId, choiceIndex);

            // 执行选择效果
            ExecuteChoiceEffects(keyChoice, choiceIndex);

            // 执行回调
            onChoiceMade?.Invoke();
        }

        /// <summary>
        /// 执行选择效果
        /// </summary>
        private void ExecuteChoiceEffects(KeyChoice keyChoice, int choiceIndex)
        {
            Choice choice = keyChoice.choices[choiceIndex];

            // 应用选择效果
            ApplyChoiceEffect(choice);

            // 检查是否改变剧情分支
            if (choice.effect.triggerBranch)
            {
                ChangeBranch(keyChoice.branchId);
            }

            // 检查是否解锁记忆碎片
            if (choice.effect.unlockMemoryShard)
            {
                MemoryShardSystem.Instance?.CollectMemoryShard(choice.effect.memoryShardId);
            }

            // 检查是否改变时间线
            if (choice.effect.changeTimeLine)
            {
                TimeLineController.Instance?.SwitchToTimeLine((TimeLineType)choice.effect.targetTimeLine);
            }

            // 检查是否触发对话事件
            if (choice.effect.triggerDialogueEvent)
            {
                DialogueManager.Instance?.TriggerDialogueEvent(choice.effect.dialogueEventId);
            }
        }

        /// <summary>
        /// 应用选择效果
        /// </summary>
        private void ApplyChoiceEffect(Choice choice)
        {
            // 应用好感度变化
            if (choice.effect.changeAffection)
            {
                CharacterAffectionManager.Instance?.ChangeAffection(choice.effect.affectionChange);
            }

            // 应用失眠值变化
            if (choice.effect.changeInsomnia)
            {
                InsomniaController.Instance?.ChangeInsomnia(choice.effect.insomniaChange);
            }

            // 播放音效
            AudioManager.Instance?.PlaySFX("Choice");
        }

        /// <summary>
        /// 改变剧情分支
        /// </summary>
        private void ChangeBranch(string branchId)
        {
            if (currentBranchId == branchId) return;

            currentBranchId = branchId;

            if (logChoices)
            {
                Debug.Log($"Branch changed to: {branchId}");
            }

            OnBranchChanged?.Invoke(branchId);
        }

        /// <summary>
        /// 获取当前分支ID
        /// </summary>
        public string GetCurrentBranchId()
        {
            return currentBranchId.ToString();
        }

        /// <summary>
        /// 重置所有选择状态
        /// </summary>
        public void ResetAllChoices()
        {
            foreach (var keyChoice in keyChoices)
            {
                foreach (var choice in keyChoice.choices)
                {
                    choice.isSelected = false;
                }
            }
        }

        /// <summary>
        /// 检查特定选择是否已选择
        /// </summary>
        public bool IsChoiceSelected(string choiceId, int choiceIndex)
        {
            if (!keyChoiceMap.ContainsKey(choiceId)) return false;

            KeyChoice keyChoice = keyChoiceMap[choiceId];

            if (choiceIndex < 0 || choiceIndex >= keyChoice.choices.Count) return false;

            return keyChoice.choices[choiceIndex].isSelected;
        }

        /// <summary>
        /// 获取特定选择的效果
        /// </summary>
        public ChoiceEffect GetChoiceEffect(string choiceId, int choiceIndex)
        {
            if (!keyChoiceMap.ContainsKey(choiceId)) return new ChoiceEffect();

            KeyChoice keyChoice = keyChoiceMap[choiceId];

            if (choiceIndex < 0 || choiceIndex >= keyChoice.choices.Count) return new ChoiceEffect();

            return keyChoice.choices[choiceIndex].effect;
        }

        /// <summary>
        /// 获取关键选择数量
        /// </summary>
        public int GetKeyChoiceCount()
        {
            return keyChoices.Count;
        }

        /// <summary>
        /// 获取特定选择点的所有选择
        /// </summary>
        public List<Choice> GetChoicesForKeyChoice(string choiceId)
        {
            if (!keyChoiceMap.ContainsKey(choiceId)) return new List<Choice>();

            return keyChoiceMap[choiceId].choices;
        }
    }

    /// <summary>
    /// 关键选择点数据结构
    /// </summary>
    [System.Serializable]
    public class KeyChoice
    {
        /// <summary>
        /// 选择点ID
        /// </summary>
        public string choiceId;

        /// <summary>
        /// 选择点名称
        /// </summary>
        public string choiceName;

        /// <summary>
        /// 所属剧情分支ID
        /// </summary>
        public string branchId;

        /// <summary>
        /// 选择选项列表
        /// </summary>
        public List<Choice> choices;

        /// <summary>
        /// 选择位置（对话行索引）
        /// </summary>
        public int dialogueLineIndex;

        /// <summary>
        /// 触发条件（可选）
        /// </summary>
        public System.Func<bool> triggerCondition;

        public KeyChoice()
        {
            choiceId = "";
            choiceName = "";
            branchId = "default";
            choices = new List<Choice>();
            dialogueLineIndex = -1;
            triggerCondition = null;
        }

        public KeyChoice(string id, string name, string branch, List<Choice> choices, int dialogueLineIndex = -1)
        {
            this.choiceId = id;
            this.choiceName = name;
            this.branchId = branch;
            this.choices = choices;
            this.dialogueLineIndex = dialogueLineIndex;
            this.triggerCondition = null;
        }
    }
}
#endif
