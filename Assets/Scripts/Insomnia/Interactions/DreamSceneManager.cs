#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeStamp
{
    /// <summary>
    /// 梦境场景管理器
    /// 处理失眠状态下的梦境场景和记忆实体互动
    /// </summary>
    public class DreamSceneManager : MonoBehaviour
    {
        [Header("梦境场景设置")]
        [SerializeField] private GameObject dreamSceneObject;
        [SerializeField] private List<GameObject> dreamElements = new List<GameObject>();

        [Header("记忆实体设置")]
        [SerializeField] private List<GameObject> memoryEntities = new List<GameObject>();
        [SerializeField] private float memoryEntitySpawnProbability = 0.3f;
        [SerializeField] private float memoryEntityDespawnProbability = 0.2f;

        [Header("视觉效果")]
        [SerializeField] private float dreamSceneOpacity = 0.0f;
        [SerializeField] private float dreamSceneTransitionSpeed = 0.5f;
        [SerializeField] private float entityFloatSpeed = 1.0f;

        [Header("交互")]
        [SerializeField] private bool canInteractWithEntities = false;

        private bool isDreamSceneActive = false;
        private float currentOpacity = 0f;
        private float time = 0f;

        private void Start()
        {
            InitializeDreamScene();
            InitializeMemoryEntities();
        }

        private void InitializeDreamScene()
        {
            if (dreamSceneObject == null)
            {
                // 创建梦境场景对象
                dreamSceneObject = new GameObject("DreamScene");
                dreamSceneObject.transform.SetParent(transform);
            }

            // 设置初始状态
            dreamSceneObject.SetActive(false);
            SetDreamSceneOpacity(0f);
        }

        private void InitializeMemoryEntities()
        {
            // 清理现有实体
            foreach (var entity in memoryEntities)
            {
                if (entity != null) Destroy(entity);
            }
            memoryEntities.Clear();

            // 初始化实体（如果没有提供）
            if (memoryEntities.Count == 0)
            {
                memoryEntities.Add(CreateMemoryEntity("记忆碎片_A"));
                memoryEntities.Add(CreateMemoryEntity("记忆碎片_B"));
                memoryEntities.Add(CreateMemoryEntity("记忆碎片_C"));
            }

            // 设置初始状态
            foreach (var entity in memoryEntities)
            {
                if (entity != null)
                {
                    entity.SetActive(false);
                }
            }
        }

        private GameObject CreateMemoryEntity(string name)
        {
            GameObject entity = new GameObject(name);
            entity.transform.SetParent(transform);

            // 添加粒子效果
            ParticleSystem particles = entity.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 3f;
            main.startSpeed = 2f;
            main.startSize = 0.15f;
            main.maxParticles = 50;
            main.loop = true;
            main.startColor = new Color(1f, 0.8f, 0.6f, 0.8f);

            var emission = particles.emission;
            emission.rateOverTime = 10;

            // 添加发光效果
            Light light = entity.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 2f;
            light.color = new Color(1f, 0.8f, 0.6f);

            return entity;
        }

        private void Update()
        {
            time += Time.deltaTime;

            if (isDreamSceneActive)
            {
                UpdateDreamScene();
                UpdateMemoryEntities();
            }
        }

        private void UpdateDreamScene()
        {
            // 平滑过渡到目标不透明度
            currentOpacity = Mathf.Lerp(currentOpacity, dreamSceneOpacity, Time.deltaTime * dreamSceneTransitionSpeed);
            SetDreamSceneOpacity(currentOpacity);
        }

        private void UpdateMemoryEntities()
        {
            // 根据概率生成和销毁实体
            if (canInteractWithEntities && Random.value < memoryEntitySpawnProbability)
            {
                SpawnRandomMemoryEntity();
            }

            if (Random.value < memoryEntityDespawnProbability)
            {
                DespawnRandomMemoryEntity();
            }

            // 更新实体位置（浮动效果）
            foreach (var entity in memoryEntities)
            {
                if (entity != null && entity.activeSelf)
                {
                    entity.transform.position = new Vector3(
                        entity.transform.position.x + Mathf.Sin(time * entityFloatSpeed) * 0.01f,
                        entity.transform.position.y + Mathf.Cos(time * entityFloatSpeed) * 0.01f,
                        entity.transform.position.z + Mathf.Sin(time * entityFloatSpeed * 0.5f) * 0.01f
                    );
                }
            }
        }

        /// <summary>
        /// 激活梦境场景
        /// </summary>
        public void ActivateDreamScene()
        {
            isDreamSceneActive = true;
            dreamSceneObject.SetActive(true);
            Debug.Log("[梦境场景] 已激活");
        }

        /// <summary>
        /// 停用梦境场景
        /// </summary>
        public void DeactivateDreamScene()
        {
            isDreamSceneActive = false;
            currentOpacity = 0f;
            SetDreamSceneOpacity(0f);
            dreamSceneObject.SetActive(false);

            // 清除所有实体
            foreach (var entity in memoryEntities)
            {
                if (entity != null) entity.SetActive(false);
            }

            Debug.Log("[梦境场景] 已停用");
        }

        /// <summary>
        /// 生成随机记忆实体
        /// </summary>
        private void SpawnRandomMemoryEntity()
        {
            // 找到第一个不活跃的实体
            foreach (var entity in memoryEntities)
            {
                if (entity != null && !entity.activeSelf)
                {
                    entity.SetActive(true);
                    entity.transform.position = Random.onUnitSphere * 5f;
                    return;
                }
            }

            // 如果没有可用的实体，创建新的
            GameObject newEntity = CreateMemoryEntity($"记忆碎片_{System.Guid.NewGuid().ToString().Substring(0, 4)}");
            newEntity.SetActive(true);
            newEntity.transform.position = Random.onUnitSphere * 5f;
            memoryEntities.Add(newEntity);
        }

        /// <summary>
        /// 销毁随机记忆实体
        /// </summary>
        private void DespawnRandomMemoryEntity()
        {
            List<GameObject> activeEntities = new List<GameObject>();

            foreach (var entity in memoryEntities)
            {
                if (entity != null && entity.activeSelf)
                {
                    activeEntities.Add(entity);
                }
            }

            if (activeEntities.Count > 0)
            {
                GameObject entityToDespawn = activeEntities[Random.Range(0, activeEntities.Count)];
                entityToDespawn.SetActive(false);
            }
        }

        /// <summary>
        /// 与记忆实体互动
        /// </summary>
        public void InteractWithMemoryEntity(GameObject entity)
        {
            if (!canInteractWithEntities || entity == null || !entity.activeSelf)
            {
                Debug.LogWarning("[梦境场景] 无法与记忆实体互动");
                return;
            }

            // 生成互动事件
            Debug.Log($"[记忆实体互动] 与 {entity.name} 互动");
            // 这里可以添加与实体对话、收集记忆碎片等逻辑
        }

        /// <summary>
        /// 设置梦境场景不透明度
        /// </summary>
        private void SetDreamSceneOpacity(float opacity)
        {
            if (dreamSceneObject != null)
            {
                dreamSceneObject.GetComponent<SpriteRenderer>()?.SetAlpha(opacity);
            }
        }

        /// <summary>
        /// 更新梦境场景不透明度
        /// </summary>
        public void UpdateDreamSceneOpacity(float opacity)
        {
            dreamSceneOpacity = Mathf.Clamp01(opacity);
        }

        /// <summary>
        /// 更新记忆实体生成概率
        /// </summary>
        public void UpdateMemoryEntitySpawnProbability(float probability)
        {
            memoryEntitySpawnProbability = Mathf.Clamp01(probability);
        }

        /// <summary>
        /// 更新记忆实体销毁概率
        /// </summary>
        public void UpdateMemoryEntityDespawnProbability(float probability)
        {
            memoryEntityDespawnProbability = Mathf.Clamp01(probability);
        }

        /// <summary>
        /// 更新实体浮动速度
        /// </summary>
        public void UpdateEntityFloatSpeed(float speed)
        {
            entityFloatSpeed = Mathf.Max(0.1f, speed);
        }

        /// <summary>
        /// 检查是否可以与实体互动
        /// </summary>
        public bool CanInteractWithEntities()
        {
            return canInteractWithEntities;
        }

        /// <summary>
        /// 更新交互权限
        /// </summary>
        public void UpdateInteractionPermission(bool canInteract)
        {
            canInteractWithEntities = canInteract;
            Debug.Log($"[梦境场景] 交互权限: {(canInteract ? "允许" : "禁止")}");
        }

        /// <summary>
        /// 获取当前活跃实体数量
        /// </summary>
        public int GetActiveEntityCount()
        {
            int count = 0;
            foreach (var entity in memoryEntities)
            {
                if (entity != null && entity.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 重置所有效果
        /// </summary>
        public void Reset()
        {
            DeactivateDreamScene();
            UpdateDreamSceneOpacity(0f);
            UpdateMemoryEntitySpawnProbability(0.3f);
            UpdateMemoryEntityDespawnProbability(0.2f);
            UpdateEntityFloatSpeed(1.0f);
            UpdateInteractionPermission(false);
        }
    }
}
#endif
