#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace TimeStamp
{
    /// <summary>
    /// 音频元数据管理器
    /// 用于管理音频文件的元数据、版本控制和资产导入
    /// </summary>
    public class AudioMetadataManager : MonoBehaviour
    {
        public static AudioMetadataManager Instance { get; private set; }

        [Header("元数据存储")]
        [SerializeField] private string metadataFilePath = "Assets/Audio/metadata.json";

        private Dictionary<string, AudioMetadata> metadataDatabase = new Dictionary<string, AudioMetadata>();
        private Dictionary<string, string> versionHistory = new Dictionary<string, List<string>>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadMetadataDatabase();
        }

        #region 元数据管理

        /// <summary>
        /// 加载元数据数据库
        /// </summary>
        private void LoadMetadataDatabase()
        {
            if (File.Exists(metadataFilePath))
            {
                try
                {
                    string json = File.ReadAllText(metadataFilePath);
                    var loaded = JsonUtility.FromJson<MetadataDatabaseWrapper>(json);
                    metadataDatabase = loaded.metadatas;
                    Debug.Log($"[元数据管理器] 已加载 {metadataDatabase.Count} 条元数据");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[元数据管理器] 加载元数据失败: {e.Message}");
                }
            }
        }

        /// <summary>
        /// 保存元数据数据库
        /// </summary>
        private void SaveMetadataDatabase()
        {
            try
            {
                var wrapper = new MetadataDatabaseWrapper { metadatas = metadataDatabase };
                string json = JsonUtility.ToJson(wrapper, true);
                File.WriteAllText(metadataFilePath, json);
                Debug.Log($"[元数据管理器] 已保存 {metadataDatabase.Count} 条元数据");
            }
            catch (Exception e)
            {
                Debug.LogError($"[元数据管理器] 保存元数据失败: {e.Message}");
            }
        }

        /// <summary>
        /// 添加元数据
        /// </summary>
        public bool AddMetadata(AudioMetadata metadata)
        {
            if (metadata == null || string.IsNullOrEmpty(metadata.ResourceName))
                return false;

            string key = metadata.ResourceName.ToLower();

            if (metadataDatabase.ContainsKey(key))
            {
                // 更新现有元数据
                metadataDatabase[key] = metadata;
                Debug.Log($"[元数据管理器] 已更新元数据: {metadata.ResourceName}");
            }
            else
            {
                // 添加新元数据
                metadataDatabase[key] = metadata;
                Debug.Log($"[元数据管理器] 已添加元数据: {metadata.ResourceName}");
            }

            SaveMetadataDatabase();
            return true;
        }

        /// <summary>
        /// 获取元数据
        /// </summary>
        public AudioMetadata GetMetadata(string resourceName)
        {
            if (metadataDatabase.ContainsKey(resourceName.ToLower()))
            {
                return metadataDatabase[resourceName.ToLower()];
            }
            return null;
        }

        /// <summary>
        /// 检查元数据是否存在
        /// </summary>
        public bool HasMetadata(string resourceName)
        {
            return metadataDatabase.ContainsKey(resourceName.ToLower());
        }

        /// <summary>
        /// 删除元数据
        /// </summary>
        public bool RemoveMetadata(string resourceName)
        {
            if (metadataDatabase.ContainsKey(resourceName.ToLower()))
            {
                metadataDatabase.Remove(resourceName.ToLower());
                SaveMetadataDatabase();
                Debug.Log($"[元数据管理器] 已删除元数据: {resourceName}");
                return true;
            }
            return false;
        }

        #endregion

        #region 版本控制

        /// <summary>
        /// 记录版本历史
        /// </summary>
        public void RecordVersion(string resourceName, string versionNote)
        {
            string key = resourceName.ToLower();

            if (!versionHistory.ContainsKey(key))
            {
                versionHistory[key] = new List<string>();
            }

            versionHistory[key].Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {versionNote}");
            Debug.Log($"[元数据管理器] 已记录版本历史: {resourceName}");
        }

        /// <summary>
        /// 获取版本历史
        /// </summary>
        public List<string> GetVersionHistory(string resourceName)
        {
            if (versionHistory.ContainsKey(resourceName.ToLower()))
            {
                return versionHistory[resourceName.ToLower()];
            }
            return new List<string>();
        }

        /// <summary>
        /// 获取所有版本历史
        /// </summary>
        public Dictionary<string, List<string>> GetAllVersionHistory()
        {
            return versionHistory;
        }

        #endregion

        #region 查询和统计

        /// <summary>
        /// 获取所有元数据
        /// </summary>
        public List<AudioMetadata> GetAllMetadata()
        {
            return new List<AudioMetadata>(metadataDatabase.Values);
        }

        /// <summary>
        /// 获取指定类别的元数据
        /// </summary>
        public List<AudioMetadata> GetMetadataByCategory(string category)
        {
            List<AudioMetadata> result = new List<AudioMetadata>();
            foreach (var kvp in metadataDatabase)
            {
                if (kvp.Value.Category == category)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// 获取指定状态的元数据
        /// </summary>
        public List<AudioMetadata> GetMetadataByStatus(string status)
        {
            List<AudioMetadata> result = new List<AudioMetadata>();
            foreach (var kvp in metadataDatabase)
            {
                if (kvp.Value.Status == status)
                    result.Add(kvp.Value);
            }
            return result;
        }

        /// <summary>
        /// 统计各类别数量
        /// </summary>
        public Dictionary<string, int> GetCategoryStatistics()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (var kvp in metadataDatabase)
            {
                string category = kvp.Value.Category;
                if (!stats.ContainsKey(category))
                    stats[category] = 0;
                stats[category]++;
            }

            return stats;
        }

        /// <summary>
        /// 统计各状态数量
        /// </summary>
        public Dictionary<string, int> GetStatusStatistics()
        {
            Dictionary<string, int> stats = new Dictionary<string, int>();

            foreach (var kvp in metadataDatabase)
            {
                string status = kvp.Value.Status;
                if (!stats.ContainsKey(status))
                    stats[status] = 0;
                stats[status]++;
            }

            return stats;
        }

        /// <summary>
        /// 获取待完成数量
        /// </summary>
        public int GetPendingCount()
        {
            int count = 0;
            foreach (var kvp in metadataDatabase)
            {
                if (kvp.Value.Status == "Pending")
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 获取已完成数量
        /// </summary>
        public int GetCompletedCount()
        {
            int count = 0;
            foreach (var kvp in metadataDatabase)
            {
                if (kvp.Value.Status == "Completed")
                    count++;
            }
            return count;
        }

        #endregion

        #region 导出和导入

        /// <summary>
        /// 导出元数据为CSV
        /// </summary>
        public void ExportToCSV(string outputPath)
        {
            StringBuilder csv = new StringBuilder();
            csv.AppendLine("ResourceName,Category,Description,Status,CreatedDate,LastUpdated,Duration,SampleRate,BitDepth,Channels,Tags");

            foreach (var kvp in metadataDatabase)
            {
                AudioMetadata metadata = kvp.Value;
                csv.AppendLine($"{metadata.ResourceName},{metadata.Category},{metadata.Description},{metadata.Status},{metadata.CreatedDate},{metadata.LastUpdated},{metadata.Duration},{metadata.SampleRate},{metadata.BitDepth},{metadata.Channels},{metadata.Tags}");
            }

            File.WriteAllText(outputPath, csv.ToString());
            Debug.Log($"[元数据管理器] 已导出元数据到: {outputPath}");
        }

        /// <summary>
        /// 导出元数据为JSON
        /// </summary>
        public void ExportToJSON(string outputPath)
        {
            var wrapper = new MetadataDatabaseWrapper { metadatas = metadataDatabase };
            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(outputPath, json);
            Debug.Log($"[元数据管理器] 已导出元数据到: {outputPath}");
        }

        /// <summary>
        /// 从CSV导入元数据
        /// </summary>
        public int ImportFromCSV(string filePath)
        {
            int count = 0;
            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    string resourceName = parts[0].Trim();
                    string category = parts[1].Trim();
                    string status = parts[3].Trim();
                    string createdDate = parts[4].Trim();
                    string lastUpdated = parts[5].Trim();

                    metadataDatabase[resourceName.ToLower()] = new AudioMetadata
                    {
                        ResourceName = resourceName,
                        Category = category,
                        Status = status,
                        CreatedDate = createdDate,
                        LastUpdated = lastUpdated,
                        Duration = parts.Length > 6 ? parts[6].Trim() : "",
                        SampleRate = parts.Length > 7 ? parts[7].Trim() : "",
                        BitDepth = parts.Length > 8 ? parts[8].Trim() : "",
                        Channels = parts.Length > 9 ? parts[9].Trim() : "",
                        Tags = parts.Length > 10 ? parts[10].Trim() : ""
                    };
                    count++;
                }
            }

            SaveMetadataDatabase();
            Debug.Log($"[元数据管理器] 已导入 {count} 条元数据");
            return count;
        }

        #endregion

        #region 资产导入辅助

        /// <summary>
        /// 生成Unity导入设置JSON
        /// </summary>
        public string GenerateUnityImportSettings(AudioMetadata metadata)
        {
            StringBuilder settings = new StringBuilder();
            settings.AppendLine("{");
            settings.AppendLine("  \"guid\": \"" + Guid.NewGuid().ToString() + "\",");
            settings.AppendLine("  \"name\": \"" + metadata.ResourceName + "\",");
            settings.AppendLine("  \"settings\": {");
            settings.AppendLine("    \"sourceType\": 0,");
            settings.AppendLine("    \"importFormat\": 0,");
            settings.AppendLine("    \"audioType\": 0,");
            settings.AppendLine("    \"sampleRateSetting\": 0,");
            settings.AppendLine("    \"clipMode\": 0");
            settings.AppendLine("  }");
            settings.AppendLine("}");

            return settings.ToString();
        }

        /// <summary>
        /// 生成资源导入路径
        /// </summary>
        public string GenerateResourceImportPath(string resourceName, string category)
        {
            string fileName = resourceName.Replace(" ", "_");
            string folderPath = GetCategoryFolderPath(category);
            return $"{folderPath}/{fileName}";
        }

        /// <summary>
        /// 获取类别文件夹路径
        /// </summary>
        private string GetCategoryFolderPath(string category)
        {
            switch (category.ToLower())
            {
                case "insomnia":
                    return "Assets/Audio/Insomnia";
                case "scene":
                case "bgm":
                    return "Assets/Audio/BGM";
                case "emotional":
                    return "Assets/Audio/Emotional";
                case "special":
                    return "Assets/Audio/Special";
                case "sfx":
                    return "Assets/Audio/SFX";
                case "voice":
                    return "Assets/Audio/Voices";
                default:
                    return "Assets/Audio";
            }
        }

        #endregion
    }

    /// <summary>
    /// 元数据数据库包装器
    /// </summary>
    [Serializable]
    private class MetadataDatabaseWrapper
    {
        public Dictionary<string, AudioMetadata> metadatas;
    }

    /// <summary>
    /// 音频元数据
    /// </summary>
    [Serializable]
    public class AudioMetadata
    {
        public string ResourceName;
        public string Category;
        public string Description;
        public string Status; // Pending, InProgress, Completed, Rejected
        public string CreatedDate;
        public string LastUpdated;
        public string Duration;
        public string SampleRate;
        public string BitDepth;
        public string Channels;
        public string Tags;
    }
}
#endif
