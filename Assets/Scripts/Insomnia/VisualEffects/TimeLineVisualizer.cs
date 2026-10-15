#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 时间线可视化系统
    /// 负责显示时间线的具象化效果
    /// </summary>
    public class TimeLineVisualizer : MonoBehaviour
    {
        [Header("时间线设置")]
        [SerializeField] private int timeLineCount = 4;
        [SerializeField] private float timeLineSpacing = 5.0f;
        [SerializeField] private float timeLineFlowSpeed = 1.0f;

        [Header("视觉效果")]
        [SerializeField] private Material timeLineMaterial;
        [SerializeField] private Shader timeLineShader;

        [SerializeField] private List<GameObject> timeLineObjects = new List<GameObject>();
        [SerializeField] private List<ParticleSystem> timeLineParticles = new List<ParticleSystem>();

        [Header("参数")]
        [SerializeField] private float visibilityThreshold = 0.1f;
        [SerializeField] private float colorShiftIntensity = 0.2f;
        [SerializeField] private float distortionIntensity = 0.5f;

        private float time = 0f;

        private void Awake()
        {
            InitializeTimeLines();
        }

        private void Start()
        {
            InitializeParticles();
        }

        private void InitializeTimeLines()
        {
            // 清理现有对象
            foreach (var obj in timeLineObjects)
            {
                if (obj != null) Destroy(obj);
            }
            timeLineObjects.Clear();

            // 创建新的时间线对象
            for (int i = 0; i < timeLineCount; i++)
            {
                GameObject timeLine = new GameObject($"TimeLine_{i}");
                timeLine.transform.SetParent(transform);

                // 创建线段
                LineRenderer lineRenderer = timeLine.AddComponent<LineRenderer>();
                lineRenderer.positionCount = 50;
                lineRenderer.material = timeLineMaterial ?? new Material(timeLineShader);
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
                lineRenderer.startColor = GetTimeLineColor(i);
                lineRenderer.endColor = GetTimeLineColor(i);

                timeLineObjects.Add(timeLine);
            }
        }

        private void InitializeParticles()
        {
            // 为每个时间线添加粒子效果
            foreach (var obj in timeLineObjects)
            {
                if (obj == null) continue;

                // 创建粒子系统
                ParticleSystem particles = obj.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.startLifetime = 2f;
                main.startSpeed = 3f;
                main.startSize = 0.1f;
                main.maxParticles = 100;
                main.loop = true;

                var emission = particles.emission;
                emission.rateOverTime = 20;

                timeLineParticles.Add(particles);
            }
        }

        private Color GetTimeLineColor(int index)
        {
            // 为不同时间线分配不同颜色
            Color[] colors = new Color[]
            {
                new Color(0.8f, 0.8f, 1.0f), // 主时间线 - 浅蓝
                new Color(1.0f, 0.8f, 0.8f), // 过去时间线 - 浅红
                new Color(0.8f, 1.0f, 0.8f), // 未来时间线 - 浅绿
                new Color(1.0f, 1.0f, 0.6f)  // 梦境世界 - 浅黄
            };

            return colors[index % colors.Length];
        }

        private void Update()
        {
            time += Time.deltaTime * timeLineFlowSpeed;

            for (int i = 0; i < timeLineCount; i++)
            {
                UpdateTimeLine(i);
            }
        }

        private void UpdateTimeLine(int index)
        {
            if (timeLineObjects.Count <= index || timeLineObjects[index] == null)
                return;

            LineRenderer lineRenderer = timeLineObjects[index].GetComponent<LineRenderer>();
            if (lineRenderer == null)
                return;

            Vector3[] positions = new Vector3[lineRenderer.positionCount];
            float offset = index * timeLineSpacing;

            for (int j = 0; j < positions.Length; j++)
            {
                float x = j * 0.5f - 10f;
                float y = Mathf.Sin(time + j * 0.3f + offset) * 2f;
                float z = j * 0.5f - 10f;

                // 应用扭曲
                float distortion = Mathf.Sin(time * 2f + j * 0.2f) * distortionIntensity;
                y += distortion;

                positions[j] = new Vector3(x, y, z);
            }

            lineRenderer.SetPositions(positions);

            // 更新粒子系统
            if (timeLineParticles.Count > index && timeLineParticles[index] != null)
            {
                var main = timeLineParticles[index].main;
                main.startColor = GetTimeLineColor(index);
            }
        }

        /// <summary>
        /// 更新时间线可见性
        /// </summary>
        public void UpdateVisibility(float visibility)
        {
            visibility = Mathf.Clamp01(visibility);

            for (int i = 0; i < timeLineCount; i++)
            {
                if (timeLineObjects.Count > i && timeLineObjects[i] != null)
                {
                    float targetOpacity = visibility;
                    if (i == 0) targetOpacity = visibility; // 主时间线总是可见
                    else targetOpacity = visibility * 0.5f; // 其他时间线更暗

                    Color color = GetTimeLineColor(i);
                    color.a = targetOpacity;

                    LineRenderer lineRenderer = timeLineObjects[i].GetComponent<LineRenderer>();
                    if (lineRenderer != null)
                    {
                        lineRenderer.startColor = color;
                        lineRenderer.endColor = color;
                    }
                }
            }
        }

        /// <summary>
        /// 更新颜色偏移强度
        /// </summary>
        public void UpdateColorShift(float intensity)
        {
            colorShiftIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 更新扭曲强度
        /// </summary>
        public void UpdateDistortion(float intensity)
        {
            distortionIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 更新流动速度
        /// </summary>
        public void UpdateFlowSpeed(float speed)
        {
            timeLineFlowSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// 获取当前可见性
        /// </summary>
        public float GetVisibility()
        {
            if (timeLineObjects.Count > 0 && timeLineObjects[0] != null)
            {
                LineRenderer lineRenderer = timeLineObjects[0].GetComponent<LineRenderer>();
                if (lineRenderer != null)
                {
                    return lineRenderer.startColor.a;
                }
            }
            return 0f;
        }

        /// <summary>
        /// 重置所有效果
        /// </summary>
        public void Reset()
        {
            UpdateVisibility(0.05f);
            UpdateColorShift(0.1f);
            UpdateDistortion(0.2f);
            UpdateFlowSpeed(1.0f);
        }
    }
}
#endif
