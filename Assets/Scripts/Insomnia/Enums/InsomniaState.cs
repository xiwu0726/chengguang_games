#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System;

namespace TimeStamp
{
    /// <summary>
    /// 失眠状态分级
    /// </summary>
    public enum InsomniaState
    {
        /// <summary>
        /// 轻度失眠 (0-30%)
        /// </summary>
        Light = 0,

        /// <summary>
        /// 中度失眠 (30-60%)
        /// </summary>
        Medium = 1,

        /// <summary>
        /// 重度失眠 (60-90%)
        /// </summary>
        Heavy = 2,

        /// <summary>
        /// 临界失眠 (90-100%)
        /// </summary>
        Critical = 3
    }

    /// <summary>
    /// 失眠状态配置
    /// </summary>
    [Serializable]
    public class InsomniaStateConfig
    {
        public InsomniaState state;
        public string name;
        public float percentageRange; // 0.0f to 1.0f (0-30%, 30-60%, etc.)
        public int minInsomniaValue;
        public int maxInsomniaValue;

        // 视觉效果参数
        public float distortionIntensity = 0.5f; // 0.0f - 1.0f
        public float colorShiftIntensity = 0.2f; // 0.0f - 1.0f
        public float timeLineVisibility = 0.1f; // 0.0f - 1.0f
        public float memoryEntityIntensity = 0.0f; // 0.0f - 1.0f

        // 音效参数
        public float brainwaveVolume = 0.3f; // 0.0f - 1.0f
        public float timeFlowVolume = 0.2f; // 0.0f - 1.0f
        public float spaceDistortionVolume = 0.1f; // 0.0f - 1.0f
        public float memoryEntityVolume = 0.0f; // 0.0f - 1.0f
        public float crackleVolume = 0.0f; // 0.0f - 1.0f

        // 交互参数
        public bool canNormalDialogue = true;
        public bool hasStreamOfConsciousness = false;
        public float dialogueJumpProbability = 0.0f; // 0.0f - 1.0f
        public float choiceBoxStability = 1.0f; // 0.0f - 1.0f
        public float sceneTransitionDelay = 0.0f; // seconds
        public bool canTravelTimeLines = false;
        public bool canInteractWithMemoryEntities = false;

        public InsomniaStateConfig(InsomniaState state, int min, int max)
        {
            this.state = state;
            this.minInsomniaValue = min;
            this.maxInsomniaValue = max;
            this.percentageRange = (max - min) / 100.0f;
            InitializeParameters();
        }

        private void InitializeParameters()
        {
            switch (state)
            {
                case InsomniaState.Light:
                    name = "轻度失眠";
                    distortionIntensity = 0.2f;
                    colorShiftIntensity = 0.1f;
                    timeLineVisibility = 0.05f;
                    memoryEntityIntensity = 0.0f;
                    brainwaveVolume = 0.3f;
                    timeFlowVolume = 0.2f;
                    spaceDistortionVolume = 0.1f;
                    memoryEntityVolume = 0.0f;
                    crackleVolume = 0.0f;
                    canNormalDialogue = true;
                    hasStreamOfConsciousness = false;
                    dialogueJumpProbability = 0.0f;
                    choiceBoxStability = 1.0f;
                    sceneTransitionDelay = 0.0f;
                    canTravelTimeLines = false;
                    canInteractWithMemoryEntities = false;
                    break;

                case InsomniaState.Medium:
                    name = "中度失眠";
                    distortionIntensity = 0.5f;
                    colorShiftIntensity = 0.3f;
                    timeLineVisibility = 0.4f;
                    memoryEntityIntensity = 0.1f;
                    brainwaveVolume = 0.6f;
                    timeFlowVolume = 0.5f;
                    spaceDistortionVolume = 0.3f;
                    memoryEntityVolume = 0.2f;
                    crackleVolume = 0.1f;
                    canNormalDialogue = true;
                    hasStreamOfConsciousness = true;
                    dialogueJumpProbability = 0.2f;
                    choiceBoxStability = 0.8f;
                    sceneTransitionDelay = 0.5f;
                    canTravelTimeLines = false;
                    canInteractWithMemoryEntities = false;
                    break;

                case InsomniaState.Heavy:
                    name = "重度失眠";
                    distortionIntensity = 0.8f;
                    colorShiftIntensity = 0.6f;
                    timeLineVisibility = 0.7f;
                    memoryEntityIntensity = 0.5f;
                    brainwaveVolume = 0.9f;
                    timeFlowVolume = 0.8f;
                    spaceDistortionVolume = 0.6f;
                    memoryEntityVolume = 0.6f;
                    crackleVolume = 0.4f;
                    canNormalDialogue = false;
                    hasStreamOfConsciousness = true;
                    dialogueJumpProbability = 0.5f;
                    choiceBoxStability = 0.5f;
                    sceneTransitionDelay = 1.5f;
                    canTravelTimeLines = true;
                    canInteractWithMemoryEntities = true;
                    break;

                case InsomniaState.Critical:
                    name = "临界失眠";
                    distortionIntensity = 1.0f;
                    colorShiftIntensity = 0.8f;
                    timeLineVisibility = 1.0f;
                    memoryEntityIntensity = 1.0f;
                    brainwaveVolume = 1.0f;
                    timeFlowVolume = 1.0f;
                    spaceDistortionVolume = 0.9f;
                    memoryEntityVolume = 1.0f;
                    crackleVolume = 0.7f;
                    canNormalDialogue = false;
                    hasStreamOfConsciousness = true;
                    dialogueJumpProbability = 0.8f;
                    choiceBoxStability = 0.2f;
                    sceneTransitionDelay = 3.0f;
                    canTravelTimeLines = true;
                    canInteractWithMemoryEntities = true;
                    break;
            }
        }
    }
}
#endif
