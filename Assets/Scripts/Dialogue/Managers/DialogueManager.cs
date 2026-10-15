#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Dialogue.Managers
{
    /// <summary>
    /// 对话管理器 - 统一管理所有对话系统的核心控制器
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        [Header("Dialogue System")]
        [SerializeField] private DialogueSystem dialogueSystem;

        [Header("Character Sprite System")]
        [SerializeField] private CharacterSpriteSystem characterSpriteSystem;

        [Header("Choice System")]
        [SerializeField] private ChoiceSystem choiceSystem;

        [Header("Key Choice Manager")]
        [SerializeField] private KeyChoiceManager keyChoiceManager;

        [Header("Branching Story System")]
        [SerializeField] private BranchingStorySystem branchingStorySystem;

        [Header("Settings")]
        [SerializeField] private bool autoStartDialogue = false;
        [SerializeField] private string autoStartDialogueId = "";

        private static DialogueManager instance;

        public static DialogueManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DialogueManager>();
                    if (instance == null)
                    {
                        GameObject manager = new GameObject("DialogueManager");
                        instance = manager.AddComponent<DialogueManager>();
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

            InitializeDialogueManager();
        }

        /// <summary>
        /// 初始化对话管理器
        /// </summary>
        private void InitializeDialogueManager()
        {
            if (dialogueSystem == null)
                Debug.LogError("DialogueSystem is not assigned!");

            if (characterSpriteSystem == null)
                Debug.LogError("CharacterSpriteSystem is not assigned!");

            if (choiceSystem == null)
                Debug.LogError("ChoiceSystem is not assigned!");

            if (keyChoiceManager == null)
                Debug.LogError("KeyChoiceManager is not assigned!");

            if (branchingStorySystem == null)
                Debug.LogError("BranchingStorySystem is not assigned!");

            // 设置事件监听
            dialogueSystem.OnDialogueComplete += OnDialogueComplete;
            choiceSystem.OnChoiceSelected += OnChoiceSelected;
            keyChoiceManager.OnChoiceMade += OnKeyChoiceMade;
            keyChoiceManager.OnBranchChanged += OnBranchChanged;
            branchingStorySystem.OnBranchEntered += OnBranchEntered;
            branchingStorySystem.OnBranchExited += OnBranchExited;

            // 初始化分支系统
            branchingStorySystem.InitializeBranches();

            Debug.Log("DialogueManager initialized successfully.");
        }

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(List<DialogueLine> lines, DialogueType type = DialogueType.Normal)
        {
            if (dialogueSystem == null)
            {
                Debug.LogError("DialogueSystem is not available!");
                return;
            }

            dialogueSystem.StartDialogue(lines, type);
        }

        /// <summary>
        /// 停止对话
        /// </summary>
        public void StopDialogue()
        {
            if (dialogueSystem == null) return;

            dialogueSystem.SkipTyping();
        }

        /// <summary>
        /// 显示角色立绘
        /// </summary>
        public void ShowCharacter(string characterName, Sprite sprite, CharacterSpriteSystem.CharacterPosition position = CharacterSpriteSystem.CharacterPosition.Center)
        {
            if (characterSpriteSystem == null) return;

            characterSpriteSystem.SetSprite(sprite);
            characterSpriteSystem.SetPosition(position);
            characterSpriteSystem.Show();
        }

        /// <summary>
        /// 隐藏角色立绘
        /// </summary>
        public void HideCharacter()
        {
            if (characterSpriteSystem == null) return;

            characterSpriteSystem.Hide();
        }

        /// <summary>
        /// 设置角色位置
        /// </summary>
        public void SetCharacterPosition(CharacterSpriteSystem.CharacterPosition position)
        {
            if (characterSpriteSystem == null) return;

            characterSpriteSystem.SetPosition(position);
        }

        /// <summary>
        /// 触发关键选择
        /// </summary>
        public void TriggerKeyChoice(string choiceId, int choiceIndex)
        {
            if (keyChoiceManager == null)
            {
                Debug.LogError("KeyChoiceManager is not available!");
                return;
            }

            keyChoiceManager.TriggerKeyChoice(choiceId, choiceIndex, null);
        }

        /// <summary>
        /// 进入剧情分支
        /// </summary>
        public void EnterBranch(string branchId)
        {
            if (branchingStorySystem == null)
            {
                Debug.LogError("BranchingStorySystem is not available!");
                return;
            }

            branchingStorySystem.EnterBranch(branchId);
        }

        /// <summary>
        /// 退出当前分支
        /// </summary>
        public void ExitBranch()
        {
            if (branchingStorySystem == null)
            {
                Debug.LogError("BranchingStorySystem is not available!");
                return;
            }

            branchingStorySystem.ExitBranch();
        }

        /// <summary>
        /// 切换到指定分支
        /// </summary>
        public void SwitchToBranch(string branchId)
        {
            if (branchingStorySystem == null)
            {
                Debug.LogError("BranchingStorySystem is not available!");
                return;
            }

            branchingStorySystem.SwitchToBranch(branchId);
        }

        /// <summary>
        /// 添加对话行到当前分支
        /// </summary>
        public void AddDialogueLineToCurrentBranch(DialogueLine dialogueLine)
        {
            if (branchingStorySystem == null)
            {
                Debug.LogError("BranchingStorySystem is not available!");
                return;
            }

            branchingStorySystem.AddDialogueLine(dialogueLine);
        }

        /// <summary>
        /// 添加分支选择
        /// </summary>
        public void AddBranchChoice(BranchChoice branchChoice)
        {
            if (branchingStorySystem == null)
            {
                Debug.LogError("BranchingStorySystem is not available!");
                return;
            }

            branchingStorySystem.AddBranchChoice(branchChoice);
        }

        /// <summary>
        /// 获取当前分支
        /// </summary>
        public BranchingStorySystem.Branch GetCurrentBranch()
        {
            if (branchingStorySystem == null) return null;

            return branchingStorySystem.GetCurrentBranch();
        }

        /// <summary>
        /// 获取当前分支ID
        /// </summary>
        public string GetCurrentBranchId()
        {
            if (branchingStorySystem == null) return "";

            return branchingStorySystem.GetCurrentBranchId();
        }

        /// <summary>
        /// 检查对话系统是否正在运行
        /// </summary>
        public bool IsDialogueRunning()
        {
            return dialogueSystem != null && !dialogueSystem.IsComplete();
        }

        /// <summary>
        /// 检查是否正在打字
        /// </summary>
        public bool IsTyping()
        {
            return dialogueSystem != null && dialogueSystem.IsTyping();
        }

        /// <summary>
        /// 快速完成打字
        /// </summary>
        public void SkipTyping()
        {
            dialogueSystem?.SkipTyping();
        }

        /// <summary>
        /// 触发对话事件
        /// </summary>
        public void TriggerDialogueEvent(string eventId)
        {
            Debug.Log($"Triggering dialogue event: {eventId}");

            // 在实际项目中，这里会加载并执行特定的对话事件
            // 例如：LoadDialogueEvent(eventId);
        }

        /// <summary>
        /// 保存对话进度
        /// </summary>
        public void SaveDialogueProgress()
        {
            Debug.Log("Saving dialogue progress...");

            // 在实际项目中，这里会保存对话进度到文件或数据库
            // 例如：SaveManager.Instance.SaveDialogueProgress(currentBranchId, dialogueLines);
        }

        /// <summary>
        /// 加载对话进度
        /// </summary>
        public void LoadDialogueProgress()
        {
            Debug.Log("Loading dialogue progress...");

            // 在实际项目中，这里会从文件或数据库加载对话进度
            // 例如：SaveManager.Instance.LoadDialogueProgress();
        }

        // 事件处理

        private void OnDialogueComplete()
        {
            Debug.Log("Dialogue complete.");
        }

        private void OnChoiceSelected(int choiceIndex)
        {
            Debug.Log($"Choice selected: {choiceIndex}");
        }

        private void OnKeyChoiceMade(string choiceId, int choiceIndex)
        {
            Debug.Log($"Key choice made: {choiceId} - {choiceIndex}");
        }

        private void OnBranchChanged(string branchId)
        {
            Debug.Log($"Branch changed to: {branchId}");
        }

        private void OnBranchEntered(string branchId)
        {
            Debug.Log($"Branch entered: {branchId}");
        }

        private void OnBranchExited(string branchId)
        {
            Debug.Log($"Branch exited: {branchId}");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        private void OnDestroy()
        {
            if (dialogueSystem != null)
            {
                dialogueSystem.OnDialogueComplete -= OnDialogueComplete;
            }

            if (choiceSystem != null)
            {
                choiceSystem.OnChoiceSelected -= OnChoiceSelected;
            }

            if (keyChoiceManager != null)
            {
                keyChoiceManager.OnChoiceMade -= OnKeyChoiceMade;
                keyChoiceManager.OnBranchChanged -= OnBranchChanged;
            }

            if (branchingStorySystem != null)
            {
                branchingStorySystem.OnBranchEntered -= OnBranchEntered;
                branchingStorySystem.OnBranchExited -= OnBranchExited;
            }
        }
    }
}
#endif
