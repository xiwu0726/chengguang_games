#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 意识流对话系统
    /// 处理失眠状态下的对话跳跃和意识流效果
    /// </summary>
    public class StreamOfConsciousnessDialogue : MonoBehaviour
    {
        [Header("对话系统")]
        [SerializeField] private DialogueSystem dialogueSystem;
        [SerializeField] private float dialogueJumpProbability = 0.2f;
        [SerializeField] private float streamOfConsciousnessProbability = 0.5f;

        [Header("视觉效果")]
        [SerializeField] private float characterBlurIntensity = 0.5f;
        [SerializeField] private float textGlitchIntensity = 0.2f;
        [SerializeField] private float textSpeed = 1.0f;

        [Header("对话池")]
        [SerializeField] private List<string> normalDialoguePool = new List<string>();
        [SerializeField] private List<string> streamOfConsciousnessPool = new List<string>();
        [SerializeField] private List<string> memoryDialoguePool = new List<string>();

        private bool isStreamOfConsciousnessMode = false;
        private string currentDialogue = "";
        private int currentDialogueIndex = 0;

        private void Start()
        {
            InitializeDialoguePools();
        }

        private void InitializeDialoguePools()
        {
            // 初始化对话池（如果没有提供）
            if (normalDialoguePool.Count == 0)
            {
                normalDialoguePool = new List<string>
                {
                    "我睡不着...",
                    "时间似乎在流动...",
                    "记忆开始模糊了...",
                    "我听到了声音...",
                    "这里是哪里？"
                };
            }

            if (streamOfConsciousnessPool.Count == 0)
            {
                streamOfConsciousnessPool = new List<string>
                {
                    "不...这不是真的...",
                    "时间...时间在扭曲...",
                    "记忆...记忆在碎片化...",
                    "我听到了...过去的声音...",
                    "这里是...我的记忆...",
                    "不...别让我离开...",
                    "时间...时间线...交织...",
                    "我...我正在消失...",
                    "这是...梦境...还是现实...",
                    "时间...流动...无止境..."
                };
            }

            if (memoryDialoguePool.Count == 0)
            {
                memoryDialoguePool = new List<string>
                {
                    "我记得...那个夏天...",
                    "我记得...那个笑容...",
                    "我记得...那个声音...",
                    "我记得...那个承诺...",
                    "我记得...那个离别...",
                    "记忆...如此清晰...",
                    "记忆...如此痛苦...",
                    "记忆...如此美好..."
                };
            }
        }

        /// <summary>
        /// 开始意识流对话模式
        /// </summary>
        public void StartStreamOfConsciousnessMode()
        {
            isStreamOfConsciousnessMode = true;
            currentDialogueIndex = 0;
            Debug.Log("[意识流对话] 开始意识流模式");
        }

        /// <summary>
        /// 停止意识流对话模式
        /// </summary>
        public void StopStreamOfConsciousnessMode()
        {
            isStreamOfConsciousnessMode = false;
            currentDialogueIndex = 0;
            Debug.Log("[意识流对话] 停止意识流模式");
        }

        /// <summary>
        /// 更新对话（基于失眠状态）
        /// </summary>
        public void UpdateDialogueBasedOnInsomnia(float insomniaLevel)
        {
            // 检查是否应该进入意识流模式
            if (insomniaLevel > 60f && !isStreamOfConsciousnessMode)
            {
                StartStreamOfConsciousnessMode();
            }

            // 根据失眠等级决定对话类型
            if (isStreamOfConsciousnessMode)
            {
                ShowStreamOfConsciousnessDialogue();
            }
            else if (insomniaLevel > 30f)
            {
                ShowMemoryDialogue();
            }
            else
            {
                ShowNormalDialogue();
            }
        }

        /// <summary>
        /// 显示正常对话
        /// </summary>
        private void ShowNormalDialogue()
        {
            if (normalDialoguePool.Count == 0) return;

            // 根据跳跃概率选择对话
            if (Random.value < dialogueJumpProbability)
            {
                currentDialogueIndex = Random.Range(0, normalDialoguePool.Count);
            }

            string dialogue = normalDialoguePool[currentDialogueIndex];

            // 应用文本故障效果
            dialogue = ApplyTextGlitch(dialogue);

            // 更新对话系统
            if (dialogueSystem != null)
            {
                dialogueSystem.SetDialogue(dialogue);
            }
            else
            {
                Debug.Log($"[正常对话] {dialogue}");
            }
        }

        /// <summary>
        /// 显示意识流对话
        /// </summary>
        private void ShowStreamOfConsciousnessDialogue()
        {
            if (streamOfConsciousnessPool.Count == 0) return;

            // 意识流对话总是跳跃的
            currentDialogueIndex = Random.Range(0, streamOfConsciousnessPool.Count);

            string dialogue = streamOfConsciousnessPool[currentDialogueIndex];

            // 应用更强的文本故障效果
            dialogue = ApplyTextGlitch(dialogue, intensity: 0.5f);

            // 更新对话系统
            if (dialogueSystem != null)
            {
                dialogueSystem.SetDialogue(dialogue);
            }
            else
            {
                Debug.Log($"[意识流对话] {dialogue}");
            }
        }

        /// <summary>
        /// 显示记忆对话
        /// </summary>
        private void ShowMemoryDialogue()
        {
            if (memoryDialoguePool.Count == 0) return;

            // 根据跳跃概率选择对话
            if (Random.value < dialogueJumpProbability * 0.5f)
            {
                currentDialogueIndex = Random.Range(0, memoryDialoguePool.Count);
            }

            string dialogue = memoryDialoguePool[currentDialogueIndex];

            // 应用轻微的文本故障效果
            dialogue = ApplyTextGlitch(dialogue, intensity: 0.1f);

            // 更新对话系统
            if (dialogueSystem != null)
            {
                dialogueSystem.SetDialogue(dialogue);
            }
            else
            {
                Debug.Log($"[记忆对话] {dialogue}");
            }
        }

        /// <summary>
        /// 应用文本故障效果
        /// </summary>
        private string ApplyTextGlitch(string text, float intensity = 0.2f)
        {
            if (intensity <= 0f) return text;

            char[] chars = text.ToCharArray();
            int glitchCount = Mathf.FloorToInt(chars.Length * intensity * 0.1f);

            for (int i = 0; i < glitchCount; i++)
            {
                int index = Random.Range(0, chars.Length);
                chars[index] = (char)Random.Range(33, 127);
            }

            return new string(chars);
        }

        /// <summary>
        /// 更新对话跳跃概率
        /// </summary>
        public void UpdateDialogueJumpProbability(float probability)
        {
            dialogueJumpProbability = Mathf.Clamp01(probability);
        }

        /// <summary>
        /// 更新意识流概率
        /// </summary>
        public void UpdateStreamOfConsciousnessProbability(float probability)
        {
            streamOfConsciousnessProbability = Mathf.Clamp01(probability);
        }

        /// <summary>
        /// 更新文本故障强度
        /// </summary>
        public void UpdateTextGlitchIntensity(float intensity)
        {
            textGlitchIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 更新文本速度
        /// </summary>
        public void UpdateTextSpeed(float speed)
        {
            textSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// 检查是否在意识流模式
        /// </summary>
        public bool IsStreamOfConsciousnessMode()
        {
            return isStreamOfConsciousnessMode;
        }

        /// <summary>
        /// 添加对话到池
        /// </summary>
        public void AddDialogueToPool(List<string> dialoguePool, string dialogue)
        {
            if (!dialoguePool.Contains(dialogue))
            {
                dialoguePool.Add(dialogue);
            }
        }

        /// <summary>
        /// 清空对话池
        /// </summary>
        public void ClearDialoguePools()
        {
            normalDialoguePool.Clear();
            streamOfConsciousnessPool.Clear();
            memoryDialoguePool.Clear();
        }
    }
}
#endif
