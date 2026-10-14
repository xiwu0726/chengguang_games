using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 失眠状态系统主控制器
    /// 协调所有失眠状态子系统
    /// </summary>
    public class InsomniaController : MonoBehaviour
    {
        [Header("引用")]
        [SerializeField] private InsomniaStateManager stateManager;
        [SerializeField] private SpaceDistortionEffect spaceDistortionEffect;
        [SerializeField] private TimeLineVisualizer timeLineVisualizer;
        [SerializeField] private InsomniaAudioManager audioManager;
        [SerializeField] private StreamOfConsciousnessDialogue dialogueSystem;
        [SerializeField] private DreamSceneManager dreamSceneManager;

        [Header("自动更新")]
        [SerializeField] private bool autoUpdateOnInsomniaChange = true;
        [SerializeField] private float updateInterval = 0.5f;

        private float updateTimer = 0f;

        private void Awake()
        {
            InitializeSubsystems();
        }

        private void Start()
        {
            UpdateAllSubsystems();
        }

        private void Update()
        {
            if (autoUpdateOnInsomniaChange)
            {
                updateTimer += Time.deltaTime;
                if (updateTimer >= updateInterval)
                {
                    updateTimer = 0f;
                    UpdateAllSubsystems();
                }
            }
        }

        private void InitializeSubsystems()
        {
            // 如果没有设置状态管理器，创建默认的
            if (stateManager == null)
            {
                stateManager = FindObjectOfType<InsomniaStateManager>();
                if (stateManager == null)
                {
                    stateManager = gameObject.AddComponent<InsomniaStateManager>();
                }
            }

            // 如果没有设置空间扭曲效果，创建默认的
            if (spaceDistortionEffect == null)
            {
                spaceDistortionEffect = FindObjectOfType<SpaceDistortionEffect>();
                if (spaceDistortionEffect == null)
                {
                    spaceDistortionEffect = gameObject.AddComponent<SpaceDistortionEffect>();
                }
            }

            // 如果没有设置时间线可视化，创建默认的
            if (timeLineVisualizer == null)
            {
                timeLineVisualizer = FindObjectOfType<TimeLineVisualizer>();
                if (timeLineVisualizer == null)
                {
                    timeLineVisualizer = gameObject.AddComponent<TimeLineVisualizer>();
                }
            }

            // 如果没有设置音频管理器，创建默认的
            if (audioManager == null)
            {
                audioManager = FindObjectOfType<InsomniaAudioManager>();
                if (audioManager == null)
                {
                    audioManager = gameObject.AddComponent<InsomniaAudioManager>();
                }
            }

            // 如果没有设置对话系统，创建默认的
            if (dialogueSystem == null)
            {
                dialogueSystem = FindObjectOfType<StreamOfConsciousnessDialogue>();
                if (dialogueSystem == null)
                {
                    dialogueSystem = gameObject.AddComponent<StreamOfConsciousnessDialogue>();
                }
            }

            // 如果没有设置梦境场景管理器，创建默认的
            if (dreamSceneManager == null)
            {
                dreamSceneManager = FindObjectOfType<DreamSceneManager>();
                if (dreamSceneManager == null)
                {
                    dreamSceneManager = gameObject.AddComponent<DreamSceneManager>();
                }
            }
        }

        /// <summary>
        /// 更新所有子系统
        /// </summary>
        private void UpdateAllSubsystems()
        {
            if (stateManager == null) return;

            InsomniaState state = stateManager.GetCurrentState();
            float insomniaValue = stateManager.GetCurrentInsomniaValue();
            InsomniaStateConfig config = stateManager.GetCurrentConfig();

            // 更新空间扭曲效果
            UpdateSpaceDistortion(config);

            // 更新时间线可视化
            UpdateTimeLineVisualizer(config);

            // 更新音频系统
            UpdateAudioSystem(config);

            // 更新对话系统
            UpdateDialogueSystem(config);

            // 更新梦境场景
            UpdateDreamScene(config);
        }

        /// <summary>
        /// 更新空间扭曲效果
        /// </summary>
        private void UpdateSpaceDistortion(InsomniaStateConfig config)
        {
            if (spaceDistortionEffect != null)
            {
                spaceDistortionEffect.UpdateIntensity(config.distortionIntensity);
                spaceDistortionEffect.UpdateSpeed(config.distortionIntensity * 2f + 0.5f);
            }
        }

        /// <summary>
        /// 更新时间线可视化
        /// </summary>
        private void UpdateTimeLineVisualizer(InsomniaStateConfig config)
        {
            if (timeLineVisualizer != null)
            {
                timeLineVisualizer.UpdateVisibility(config.timeLineVisibility);
                timeLineVisualizer.UpdateColorShift(config.colorShiftIntensity);
                timeLineVisualizer.UpdateDistortion(config.distortionIntensity);
                timeLineVisualizer.UpdateFlowSpeed(config.distortionIntensity * 2f + 0.5f);
            }
        }

        /// <summary>
        /// 更新音频系统
        /// </summary>
        private void UpdateAudioSystem(InsomniaStateConfig config)
        {
            if (audioManager != null)
            {
                float insomniaLevel = config.maxInsomniaValue / 100f;
                audioManager.UpdateVolumes(insomniaLevel);
            }
        }

        /// <summary>
        /// 更新对话系统
        /// </summary>
        private void UpdateDialogueSystem(InsomniaStateConfig config)
        {
            if (dialogueSystem != null)
            {
                float insomniaLevel = config.maxInsomniaValue / 100f;
                dialogueSystem.UpdateDialogueBasedOnInsomnia(insomniaLevel);

                dialogueSystem.UpdateDialogueJumpProbability(config.dialogueJumpProbability);
                dialogueSystem.UpdateTextGlitchIntensity(config.distortionIntensity);
                dialogueSystem.UpdateTextSpeed(config.distortionIntensity * 2f + 0.5f);
            }
        }

        /// <summary>
        /// 更新梦境场景
        /// </summary>
        private void UpdateDreamScene(InsomniaStateConfig config)
        {
            if (dreamSceneManager != null)
            {
                float insomniaLevel = config.maxInsomniaValue / 100f;

                if (insomniaLevel > 0.6f)
                {
                    dreamSceneManager.ActivateDreamScene();
                }
                else
                {
                    dreamSceneManager.DeactivateDreamScene();
                }

                dreamSceneManager.UpdateDreamSceneOpacity(insomniaLevel * 0.8f);
                dreamSceneManager.UpdateMemoryEntitySpawnProbability(insomniaLevel);
                dreamSceneManager.UpdateInteractionPermission(config.canInteractWithMemoryEntities);
                dreamSceneManager.UpdateEntityFloatSpeed(config.distortionIntensity * 2f + 0.5f);
            }
        }

        /// <summary>
        /// 手动增加失眠值
        /// </summary>
        public void IncreaseInsomnia(int amount, string reason = "")
        {
            if (stateManager != null)
            {
                stateManager.IncreaseInsomnia(amount, reason);
            }
        }

        /// <summary>
        /// 手动减少失眠值
        /// </summary>
        public void DecreaseInsomnia(int amount, string reason = "")
        {
            if (stateManager != null)
            {
                stateManager.DecreaseInsomnia(amount, reason);
            }
        }

        /// <summary>
        /// 设置失眠值
        /// </summary>
        public void SetInsomniaValue(int value, string reason = "")
        {
            if (stateManager != null)
            {
                stateManager.SetInsomniaValue(value, reason);
            }
        }

        /// <summary>
        /// 重置失眠状态
        /// </summary>
        public void ResetInsomnia()
        {
            if (stateManager != null)
            {
                stateManager.ResetState();
            }

            if (spaceDistortionEffect != null)
            {
                spaceDistortionEffect.Reset();
            }

            if (timeLineVisualizer != null)
            {
                timeLineVisualizer.Reset();
            }

            if (audioManager != null)
            {
                audioManager.Reset();
            }

            if (dialogueSystem != null)
            {
                dialogueSystem.StopStreamOfConsciousnessMode();
            }

            if (dreamSceneManager != null)
            {
                dreamSceneManager.Reset();
            }

            Debug.Log("[失眠系统] 已重置");
        }

        /// <summary>
        /// 强制设置失眠状态
        /// </summary>
        public void ForceInsomniaState(InsomniaState state)
        {
            if (stateManager != null)
            {
                stateManager.ForceState(state);
            }

            UpdateAllSubsystems();
        }

        /// <summary>
        /// 检查是否可以正常对话
        /// </summary>
        public bool CanNormalDialogue()
        {
            return stateManager != null && stateManager.CanNormalDialogue();
        }

        /// <summary>
        /// 检查是否可以穿越时间线
        /// </summary>
        public bool CanTravelTimeLines()
        {
            return stateManager != null && stateManager.CanTravelTimeLines();
        }

        /// <summary>
        /// 检查是否可以与记忆实体互动
        /// </summary>
        public bool CanInteractWithMemoryEntities()
        {
            return stateManager != null && stateManager.CanInteractWithMemoryEntities();
        }

        /// <summary>
        /// 获取当前失眠状态
        /// </summary>
        public InsomniaState GetCurrentState()
        {
            return stateManager != null ? stateManager.GetCurrentState() : InsomniaState.Light;
        }

        /// <summary>
        /// 获取当前失眠值
        /// </summary>
        public int GetCurrentInsomniaValue()
        {
            return stateManager != null ? stateManager.GetCurrentInsomniaValue() : 0;
        }

        /// <summary>
        * 获取当前状态配置
        /// </summary>
        public InsomniaStateConfig GetCurrentConfig()
        {
            return stateManager != null ? stateManager.GetCurrentConfig() : null;
        }

        /// <summary>
        /// 检查是否处于临界失眠
        /// </summary>
        public bool IsCriticalInsomnia()
        {
            return stateManager != null && stateManager.GetCurrentState() == InsomniaState.Critical;
        }

        /// <summary>
        /// 检查是否处于重度失眠
        /// </summary>
        public bool IsHeavyInsomnia()
        {
            return stateManager != null && stateManager.GetCurrentState() >= InsomniaState.Heavy;
        }

        /// <summary>
        /// 检查是否处于中度失眠
        /// </summary>
        public bool IsMediumInsomnia()
        {
            return stateManager != null && stateManager.GetCurrentState() >= InsomniaState.Medium;
        }

        /// <summary>
        /// 检查是否处于轻度失眠
        /// </summary>
        public bool IsLightInsomnia()
        {
            return stateManager != null && stateManager.GetCurrentState() >= InsomniaState.Light;
        }
    }
}
