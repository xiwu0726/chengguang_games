#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TimeStamp
{
    /// <summary>
    /// 音频资源制作辅助工具
    /// 用于生成音频资源清单、检查命名规范、管理音频元数据
    /// </summary>
    public class AudioProductionHelper : MonoBehaviour
    {
        public static AudioProductionHelper Instance { get; private set; }

        [Header("音频资源清单")]
        [SerializeField] private TextAsset audioResourceList;
        [SerializeField] private TextAsset audioNamingConvention;

        [Header("输出路径")]
        [SerializeField] private string outputDirectory = "Assets/Audio";
        [SerializeField] private string projectName = "TimeStamp";

        private Dictionary<string, AudioResourceInfo> audioResources = new Dictionary<string, AudioResourceInfo>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAudioResources();
        }

        #region 音频资源管理

        /// <summary>
        /// 加载音频资源清单
        /// </summary>
        private void LoadAudioResources()
        {
            audioResources.Clear();

            // 加载标准资源清单
            if (audioResourceList != null)
            {
                string[] lines = audioResourceList.text.Split('\n');
                foreach (string line in lines)
                {
                    string trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                        continue;

                    string[] parts = trimmed.Split(',');
                    if (parts.Length >= 3)
                    {
                        string category = parts[0].Trim();
                        string name = parts[1].Trim();
                        string description = parts[2].Trim();

                        audioResources[name] = new AudioResourceInfo
                        {
                            Name = name,
                            Category = category,
                            Description = description,
                            Status = "Pending",
                            CreatedDate = DateTime.Now.ToString("yyyy-MM-dd")
                        };
                    }
                }
            }
        }

        /// <summary>
        /// 获取所有待制作的音频资源
        /// </summary>
        public List<AudioResourceInfo> GetPendingResources()
        {
            List<AudioResourceInfo> pending = new List<AudioResourceInfo>();
            foreach (var kvp in audioResources)
            {
                if (kvp.Value.Status == "Pending")
                    pending.Add(kvp.Value);
            }
            return pending;
        }

        /// <summary>
        /// 获取所有音频资源
        /// </summary>
        public List<AudioResourceInfo> GetAllResources()
        {
            return new List<AudioResourceInfo>(audioResources.Values);
        }

        /// <summary>
        /// 获取指定类别的音频资源
        /// </summary>
        public List<AudioResourceInfo> GetResourcesByCategory(string category)
        {
            List<AudioResourceInfo> resources = new List<AudioResourceInfo>();
            foreach (var kvp in audioResources)
            {
                if (kvp.Value.Category == category)
                    resources.Add(kvp.Value);
            }
            return resources;
        }

        /// <summary>
        /// 更新资源状态
        /// </summary>
        public void UpdateResourceStatus(string resourceName, string status)
        {
            if (audioResources.ContainsKey(resourceName))
            {
                audioResources[resourceName].Status = status;
                audioResources[resourceName].LastUpdated = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }

        #endregion

        #region 命名规范检查

        /// <summary>
        /// 检查命名规范
        /// </summary>
        public bool CheckNamingConvention(string resourceName, string category)
        {
            string pattern = GetNamingPattern(category);
            if (string.IsNullOrEmpty(pattern))
                return true; // 无命名规范

            return Regex.IsMatch(resourceName, pattern, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// 获取命名规范
        /// </summary>
        private string GetNamingPattern(string category)
        {
            switch (category)
            {
                case "Insomnia":
                    return @"^Insomnia_[A-Z][a-zA-Z]+_[A-Z][a-zA-Z]+$";
                case "Scene":
                    return @"^Scene_[A-Z][a-zA-Z]+$";
                case "Emotional":
                    return @"^Emotional_[A-Z][a-zA-Z]+$";
                case "Special":
                    return @"^Special_[A-Z][a-zA-Z]+$";
                case "SFX":
                    return @"^SFX_[A-Z][a-zA-Z]+$";
                case "Voice":
                    return @"^Voice_[A-Z][a-zA-Z]+_[A-Z][a-zA-Z]+$";
                default:
                    return null;
            }
        }

        /// <summary>
        /// 生成标准命名
        /// </summary>
        public string GenerateStandardName(string category, string name)
        {
            string prefix = category.ToLower();
            string suffix = name.ToUpper();
            return $"{prefix}_{suffix}";
        }

        /// <summary>
        /// 检查所有资源命名规范
        /// </summary>
        public Dictionary<string, bool> CheckAllNamingConventions()
        {
            Dictionary<string, bool> results = new Dictionary<string, bool>();

            foreach (var kvp in audioResources)
            {
                results[kvp.Key] = CheckNamingConvention(kvp.Key, kvp.Value.Category);
            }

            return results;
        }

        #endregion

        #region 路径生成

        /// <summary>
        /// 生成音频文件路径
        /// </summary>
        public string GetFilePath(string resourceName, string category)
        {
            string fileName = GenerateStandardName(category, resourceName);
            string folderPath = GetFolderPath(category);

            return $"{folderPath}/{fileName}.wav";
        }

        /// <summary>
        /// 获取类别文件夹路径
        /// </summary>
        private string GetFolderPath(string category)
        {
            switch (category)
            {
                case "Insomnia":
                    return $"{outputDirectory}/Insomnia";
                case "Scene":
                    return $"{outputDirectory}/BGM";
                case "Emotional":
                    return $"{outputDirectory}/Emotional";
                case "Special":
                    return $"{outputDirectory}/Special";
                case "SFX":
                    return $"{outputDirectory}/SFX";
                case "Voice":
                    return $"{outputDirectory}/Voices";
                default:
                    return outputDirectory;
            }
        }

        /// <summary>
        /// 生成Unity资源路径
        /// </summary>
        public string GetUnityResourcePath(string resourceName, string category)
        {
            string fileName = GenerateStandardName(category, resourceName);
            string folderPath = GetFolderPath(category);

            // 转换为Unity Resources路径
            string relativePath = folderPath.Replace("Assets/", "");
            return $"Assets/{relativePath}/{fileName}";
        }

        #endregion

        #region 音频元数据管理

        /// <summary>
        /// 创建音频元数据文件
        /// </summary>
        public void CreateMetadataFile(string resourceName, string category, AudioMetadata metadata)
        {
            string filePath = GetFilePath(resourceName, category);
            string json = JsonUtility.ToJson(metadata, true);
            File.WriteAllText(filePath + ".meta.json", json);

            Debug.Log($"[音频辅助工具] 已创建元数据文件: {filePath}.meta.json");
        }

        /// <summary>
        /// 读取音频元数据文件
        /// </summary>
        public AudioMetadata ReadMetadataFile(string resourceName, string category)
        {
            string filePath = GetFilePath(resourceName, category) + ".meta.json";
            if (!File.Exists(filePath))
                return null;

            string json = File.ReadAllText(filePath);
            return JsonUtility.FromJson<AudioMetadata>(json);
        }

        /// <summary>
        /// 生成资源清单报告
        /// </summary>
        public string GenerateResourceReport()
        {
            StringBuilder report = new StringBuilder();
            report.AppendLine($"# 《{projectName}》音频资源清单报告");
            report.AppendLine($"生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();

            // 按类别分组
            Dictionary<string, List<AudioResourceInfo>> grouped = new Dictionary<string, List<AudioResourceInfo>>();
            foreach (var kvp in audioResources)
            {
                if (!grouped.ContainsKey(kvp.Value.Category))
                    grouped[kvp.Value.Category] = new List<AudioResourceInfo>();

                grouped[kvp.Value.Category].Add(kvp.Value);
            }

            // 输出报告
            foreach (var kvp in grouped)
            {
                report.AppendLine($"## {kvp.Key}");
                report.AppendLine($"总计: {kvp.Value.Count} 个资源");
                report.AppendLine();

                foreach (var resource in kvp.Value)
                {
                    report.AppendLine($"- {resource.Name}: {resource.Description}");
                    report.AppendLine($"  状态: {resource.Status}");
                    report.AppendLine($"  创建日期: {resource.CreatedDate}");
                    report.AppendLine();
                }
            }

            // 统计
            report.AppendLine("## 统计");
            report.AppendLine($"总资源数: {audioResources.Count}");
            report.AppendLine($"待制作: {GetPendingResources().Count}");
            report.AppendLine($"已完成: {audioResources.Count - GetPendingResources().Count}");

            return report.ToString();
        }

        #endregion

        /// <summary>
        /// 导出资源清单到文件
        /// </summary>
        public void ExportResourceList()
        {
            string report = GenerateResourceReport();
            string outputPath = $"{outputDirectory}/ResourceReport_{DateTime.Now:yyyyMMdd}.md";
            File.WriteAllText(outputPath, report);

            Debug.Log($"[音频辅助工具] 已导出资源清单到: {outputPath}");
        }
    }

    /// <summary>
    /// 音频资源信息
    /// </summary>
    [Serializable]
    public class AudioResourceInfo
    {
        public string Name;
        public string Category;
        public string Description;
        public string Status;
        public string CreatedDate;
        public string LastUpdated;
    }

    /// <summary>
    /// 音频元数据
    /// </summary>
    [Serializable]
    public class AudioMetadata
    {
        public string ResourceName;
        public string Category;
        public string Creator;
        public string Description;
        public string Duration;
        public string SampleRate;
        public string BitDepth;
        public string Channels;
        public string Tags;
        public DateTime CreatedDate;
        public string Notes;
    }
}
#endif
