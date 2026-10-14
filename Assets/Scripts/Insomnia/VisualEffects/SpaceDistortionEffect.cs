using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 空间扭曲效果控制器
    /// 负责实现视觉上的空间扭曲效果
    /// </summary>
    public class SpaceDistortionEffect : MonoBehaviour
    {
        [Header("效果参数")]
        [SerializeField] private float distortionIntensity = 0.5f;
        [SerializeField] private float distortionSpeed = 1.0f;
        [SerializeField] private float distortionRadius = 10.0f;

        [Header("材质")]
        [SerializeField] private Material distortionMaterial;
        [SerializeField] private Shader distortionShader;

        [Header("Mesh")]
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;

        private Vector3[] originalVertices;
        private float time = 0f;

        private void Awake()
        {
            if (distortionMaterial == null && distortionShader != null)
            {
                distortionMaterial = new Material(distortionShader);
                meshRenderer.material = distortionMaterial;
            }
        }

        private void Start()
        {
            if (meshFilter != null && meshFilter.mesh != null)
            {
                originalVertices = meshFilter.mesh.vertices.Clone() as Vector3[];
            }
        }

        private void Update()
        {
            time += Time.deltaTime * distortionSpeed;
            ApplyDistortion();
        }

        private void ApplyDistortion()
        {
            if (meshFilter == null || meshFilter.mesh == null || originalVertices == null)
                return;

            Vector3[] vertices = meshFilter.mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 original = originalVertices[i];
                Vector3 distorted = ApplyDistortionToVertex(original, i);
                vertices[i] = distorted;
            }

            meshFilter.mesh.vertices = vertices;
            meshFilter.mesh.RecalculateNormals();
        }

        private Vector3 ApplyDistortionToVertex(Vector3 vertex, int index)
        {
            // 计算顶点到中心的距离
            float distance = Vector3.Distance(vertex, Vector3.zero);

            // 距离中心越远，扭曲越大
            float distanceFactor = Mathf.Clamp01(distance / distortionRadius);

            // 使用正弦波和噪声函数创建扭曲
            float distortion = Mathf.Sin(time * 2f + distance * 0.5f) * distanceFactor * distortionIntensity;

            // 添加噪声效果
            float noise = Mathf.PerlinNoise(time * 0.5f + index * 0.1f, distance * 0.2f) * distanceFactor * distortionIntensity * 0.5f;

            // 计算扭曲方向（从中心向外）
            Vector3 direction = (vertex - Vector3.zero).normalized;
            Vector3 distortionVector = direction * (distortion + noise);

            return vertex + distortionVector;
        }

        /// <summary>
        /// 更新扭曲强度
        /// </summary>
        public void UpdateIntensity(float intensity)
        {
            distortionIntensity = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 更新扭曲速度
        /// </summary>
        public void UpdateSpeed(float speed)
        {
            distortionSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// 更新扭曲半径
        /// </summary>
        public void UpdateRadius(float radius)
        {
            distortionRadius = Mathf.Max(1f, radius);
        }

        /// <summary>
        /// 强制更新所有效果
        /// </summary>
        public void ForceUpdate()
        {
            if (meshFilter != null && meshFilter.mesh != null)
            {
                originalVertices = meshFilter.mesh.vertices.Clone() as Vector3[];
            }
        }

        /// <summary>
        /// 获取当前扭曲强度
        /// </summary>
        public float GetIntensity()
        {
            return distortionIntensity;
        }

        /// <summary>
        /// 重置效果
        /// </summary>
        public void Reset()
        {
            distortionIntensity = 0f;
            distortionSpeed = 1.0f;
            distortionRadius = 10.0f;
            time = 0f;
        }
    }
}
