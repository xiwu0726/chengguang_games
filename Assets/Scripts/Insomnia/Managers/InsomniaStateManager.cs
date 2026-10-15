#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 失眠状态管理器
    /// 负责管理失眠值的增加、减少、状态转换
    /// </summary>
    public class InsomniaStateManager : MonoBehaviour
    {
        public static InsomniaStateManager Instance { get; private set; }

        [Header("状态配置")]
        [SerializeField] private InsomniaStateConfig[] stateConfigs;

        [Header("当前状态")]
        [SerializeField] private int currentInsomniaValue = 0;
        [SerializeField] private InsomniaState currentState = InsomniaState.Light;

        [Header("事件")]
        public System.Action<InsomniaState, InsomniaState> OnStateChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeStateConfigs();
        }

        private void InitializeStateConfigs()
        {
            if (stateConfigs == null || stateConfigs.Length == 0)
            {
                stateConfigs = new InsomniaStateConfig[]
                {
                    new InsomniaStateConfig(InsomniaState.Light, 0, 30),
                    new InsomniaStateConfig(InsomniaState.Medium, 30, 60),
                    new InsomniaStateConfig(InsomniaState.Heavy, 60, 90),
                    new InsomniaStateConfig(InsomniaState.Critical, 90, 100)
                };
            }
        }

        /// <summary>
        /// 获取当前失眠状态
        /// </summary>
        public InsomniaState GetCurrentState()
        {
            return currentState;
        }

        /// <summary>
        /// 获取当前失眠值
        /// </summary>
        public int GetCurrentInsomniaValue()
        {
            return currentInsomniaValue;
        }

        /// <summary>
        /// 获取当前状态配置
        /// </summary>
        public InsomniaStateConfig GetCurrentConfig()
        {
            for (int i = 0; i < stateConfigs.Length; i++)
            {
                if (stateConfigs[i].state == currentState)
                {
                    return stateConfigs[i];
                }
            }
            return stateConfigs[0];
        }

        /// <summary>
        /// 增加失眠值
        /// </summary>
        /// <param name="amount">增加的值（正数）</param>
        /// <param name="reason">增加原因</param>
        public void IncreaseInsomnia(int amount, string reason = "")
        {
            currentInsomniaValue = Mathf.Clamp(currentInsomniaValue + amount, 0, 100);

            InsomniaState newState = DetermineState();
            if (newState != currentState)
            {
                InsomniaState oldState = currentState;
                currentState = newState;
                OnStateChanged?.Invoke(oldState, newState);

                Debug.Log($"[失眠状态] {oldState} -> {newState} (值: {currentInsomniaValue}%, 原因: {reason})");
            }
        }

        /// <summary>
        /// 减少失眠值
        /// </summary>
        /// <param name="amount">减少的值（正数）</param>
        /// <param name="reason">减少原因</param>
        public void DecreaseInsomnia(int amount, string reason = "")
        {
            currentInsomniaValue = Mathf.Clamp(currentInsomniaValue - amount, 0, 100);

            InsomniaState newState = DetermineState();
            if (newState != currentState)
            {
                InsomniaState oldState = currentState;
                currentState = newState;
                OnStateChanged?.Invoke(oldState, newState);

                Debug.Log($"[失眠状态] {oldState} -> {newState} (值: {currentInsomniaValue}%, 原因: {reason})");
            }
        }

        /// <summary>
        /// 设置失眠值（用于重置或特定值设置）
        /// </summary>
        public void SetInsomniaValue(int value, string reason = "")
        {
            currentInsomniaValue = Mathf.Clamp(value, 0, 100);

            InsomniaState newState = DetermineState();
            if (newState != currentState)
            {
                InsomniaState oldState = currentState;
                currentState = newState;
                OnStateChanged?.Invoke(oldState, newState);

                Debug.Log($"[失眠状态] {oldState} -> {newState} (值: {currentInsomniaValue}%, 原因: {reason})");
            }
        }

        /// <summary>
        /// 根据当前值确定状态
        /// </summary>
        private InsomniaState DetermineState()
        {
            for (int i = stateConfigs.Length - 1; i >= 0; i--)
            {
                if (currentInsomniaValue >= stateConfigs[i].minInsomniaValue &&
                    currentInsomniaValue <= stateConfigs[i].maxInsomniaValue)
                {
                    return stateConfigs[i].state;
                }
            }
            return InsomniaState.Light;
        }

        /// <summary>
        /// 获取当前状态配置的详细信息
        /// </summary>
        public string GetStateDescription()
        {
            InsomniaStateConfig config = GetCurrentConfig();
            return $"{config.name} ({currentInsomniaValue}%)";
        }

        /// <summary>
        /// 检查是否可以正常对话
        /// </summary>
        public bool CanNormalDialogue()
        {
            return GetCurrentConfig().canNormalDialogue;
        }

        /// <summary>
        /// 检查是否有意识流对话
        /// </summary>
        public bool HasStreamOfConsciousness()
        {
            return GetCurrentConfig().hasStreamOfConsciousness;
        }

        /// <summary>
        /// 检查是否可以穿越时间线
        /// </summary>
        public bool CanTravelTimeLines()
        {
            return GetCurrentConfig().canTravelTimeLines;
        }

        /// <summary>
        /// 检查是否可以与记忆实体互动
        /// </summary>
        public bool CanInteractWithMemoryEntities()
        {
            return GetCurrentConfig().canInteractWithMemoryEntities;
        }

        /// <summary>
        /// 获取对话跳跃概率
        /// </summary>
        public float GetDialogueJumpProbability()
        {
            return GetCurrentConfig().dialogueJumpProbability;
        }

        /// <summary>
        /// 获取场景切换延迟
        /// </summary>
        public float GetSceneTransitionDelay()
        {
            return GetCurrentConfig().sceneTransitionDelay;
        }

        /// <summary>
        /// 获取选择框稳定性
        /// </summary>
        public float GetChoiceBoxStability()
        {
            return GetCurrentConfig().choiceBoxStability;
        }

        /// <summary>
        /// 重置状态（用于恢复）
        /// </summary>
        public void ResetState()
        {
            currentInsomniaValue = 0;
            currentState = InsomniaState.Light;
            OnStateChanged?.Invoke(InsomniaState.Heavy, InsomniaState.Light);
            Debug.Log("[失眠状态] 已重置为轻度失眠");
        }

        /// <summary>
        /// 切换到特定状态（用于调试或剧情需要）
        /// </summary>
        public void ForceState(InsomniaState state)
        {
            currentState = state;
            currentInsomniaValue = GetStateValueFromState(state);
            OnStateChanged?.Invoke(state, state);
            Debug.Log($"[失眠状态] 强制切换到 {state}");
        }

        private int GetStateValueFromState(InsomniaState state)
        {
            foreach (var config in stateConfigs)
            {
                if (config.state == state)
                {
                    return config.minInsomniaValue;
                }
            }
            return 0;
        }
    }
}
#endif
