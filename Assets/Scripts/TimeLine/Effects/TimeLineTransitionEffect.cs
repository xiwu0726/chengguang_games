using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 时间线切换效果控制器
    /// 负责处理时间线切换时的视觉和音频转换效果
    /// </summary>
    public class TimeLineTransitionEffect : MonoBehaviour
    {
        [Header("视觉效果")]
        [SerializeField] private Material transitionMaterial;
        [SerializeField] private Shader transitionShader;
        [SerializeField] private float transitionDuration = 1.0f;
        [SerializeField] private float vignetteIntensity = 0.5f;
        [SerializeField] private float chromaticAberrationIntensity = 0.2f;
        [SerializeField] private float screenGlitchIntensity = 0.1f;

        [Header("音频效果")]
        [SerializeField] private float ambientVolume = 0.5f;
        [SerializeField] private float timeFlowVolume = 0.3f;
        [SerializeField] private float crackleVolume = 0.1f;
        [SerializeField] private float environmentVolume = 0.4f;

        [Header("时间线颜色")]
        [SerializeField] private Color mainLineColor = new Color(0.8f, 0.8f, 1.0f);
        [SerializeField] private Color pastLineColor = new Color(1.0f, 0.8f, 0.8f);
        [SerializeField] private Color futureLineColor = new Color(0.6f, 0.6f, 0.8f);
        [SerializeField] private Color dreamLineColor = new Color(1.0f, 1.0f, 0.8f);

        private float transitionProgress = 0f;
        private bool isTransitioning = false;
        private TimeLineType fromType = TimeLineType.Main;
        private TimeLineType toType = TimeLineType.Main;
        private float transitionTimer = 0f;

        private void Awake()
        {
            if (transitionMaterial == null && transitionShader != null)
            {
                transitionMaterial = new Material(transitionShader);
            }
        }

        private void Update()
        {
            if (isTransitioning)
            {
                transitionTimer += Time.deltaTime;
                transitionProgress = Mathf.Clamp01(transitionTimer / transitionDuration);

                UpdateTransitionEffect();

                if (transitionProgress >= 1f)
                {
                    FinishTransition();
                }
            }
        }

        private void UpdateTransitionEffect()
        {
            if (transitionMaterial == null) return;

            // 计算当前颜色混合
            Color fromColor = GetCurrentLineColor(fromType);
            Color toColor = GetCurrentLineColor(toType);
            Color currentColor = Color.Lerp(fromColor, toColor, transitionProgress);

            // 应用效果参数
            float distortion = transitionProgress * screenGlitchIntensity;
            float vignette = 1.0f - (transitionProgress * vignetteIntensity);
            float chromatic = transitionProgress * chromaticAberrationIntensity;

            // 更新材质参数
            transitionMaterial.SetFloat("_TransitionProgress", transitionProgress);
            transitionMaterial.SetColor("_TransitionColor", currentColor);
            transitionMaterial.SetFloat("_Distortion", distortion);
            transitionMaterial.SetFloat("_VignetteIntensity", vignette);
            transitionMaterial.SetFloat("_ChromaticAberration", chromatic);
            transitionMaterial.SetFloat("_TimeLineSpeed", transitionProgress * 2f);
        }

        private Color GetCurrentLineColor(TimeLineType type)
        {
            switch (type)
            {
                case TimeLineType.Main:
                    return mainLineColor;
                case TimeLineType.Past:
                    return pastLineColor;
                case TimeLineType.Future:
                    return futureLineColor;
                case TimeLineType.Dream:
                    return dreamLineColor;
                default:
                    return mainLineColor;
            }
        }

        /// <summary>
        /// 开始时间线切换
        /// </summary>
        public void StartTransition(TimeLineType from, TimeLineType to, float duration)
        {
            fromType = from;
            toType = to;
            transitionDuration = duration;
            transitionProgress = 0f;
            transitionTimer = 0f;
            isTransitioning = true;

            Debug.Log($"[时间线切换] {fromType} -> {toType}");
        }

        /// <summary>
        /// 完成切换
        /// </summary>
        private void FinishTransition()
        {
            isTransitioning = false;
            transitionProgress = 1f;
            Debug.Log($"[时间线切换] 完成");
        }

        /// <summary>
        /// 获取当前过渡进度 (0-1)
        /// </summary>
        public float GetTransitionProgress()
        {
            return transitionProgress;
        }

        /// <summary>
        /// 检查是否正在切换
        /// </summary>
        public bool IsTransitioning()
        {
            return isTransitioning;
        }

        /// <summary>
        /// 更新过渡持续时间
        /// </summary>
        public void UpdateDuration(float duration)
        {
            transitionDuration = duration;
        }

        /// <summary>
        /// 更新故障强度
        /// </summary>
        public void UpdateGlitchIntensity(float intensity)
        {
            screenGlitchIntensity = intensity;
        }

        /// <summary>
        /// 更新色差强度
        /// </summary>
        public void UpdateChromaticIntensity(float intensity)
        {
            chromaticAberrationIntensity = intensity;
        }

        /// <summary>
        /// 更新暗角强度
        /// </summary>
        public void UpdateVignetteIntensity(float intensity)
        {
            vignetteIntensity = intensity;
        }

        /// <summary>
        /// 获取当前时间线颜色
        /// </summary>
        public Color GetCurrentLineColor(TimeLineType type)
        {
            switch (type)
            {
                case TimeLineType.Main:
                    return mainLineColor;
                case TimeLineType.Past:
                    return pastLineColor;
                case TimeLineType.Future:
                    return futureLineColor;
                case TimeLineType.Dream:
                    return dreamLineColor;
                default:
                    return mainLineColor;
            }
        }

        /// <summary>
        /// 重置效果
        /// </summary>
        public void Reset()
        {
            transitionProgress = 0f;
            transitionTimer = 0f;
            isTransitioning = false;
            screenGlitchIntensity = 0.1f;
            chromaticAberrationIntensity = 0.2f;
            vignetteIntensity = 0.5f;
        }
    }
}
