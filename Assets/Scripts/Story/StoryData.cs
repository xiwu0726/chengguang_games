using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Story
{
    /// <summary>
    /// 一个场景 = 一个 JSON 文件。字段与 story/SCHEMA.md §2 一一对应。
    /// 用 JsonUtility 反序列化, 因此全部字段必须是 public 且可序列化。
    /// </summary>
    [Serializable]
    public class StoryScene
    {
        public string sceneId = "";
        public string chapter = "";
        public string title = "";
        public string background = "";
        public string bgm = "";
        public string next = "";
        public StoryLine[] lines = new StoryLine[0];

        public int LineCount { get { return lines == null ? 0 : lines.Length; } }
    }

    /// <summary>内容行, 见 SCHEMA §3 / §4。type: narration|dialogue|choice|cg|fx</summary>
    [Serializable]
    public class StoryLine
    {
        public string type = "narration";
        public string text = "";
        public string speaker = "";
        public string sprite = "";
        public string background = "";
        public string bgm = "";
        public string sfx = "";
        public string fx = "";
        public string prompt = "";
        public StoryOption[] options = new StoryOption[0];

        public bool IsDialogue { get { return type == "dialogue"; } }
        public bool IsNarration { get { return type == "narration"; } }
        public bool IsChoice { get { return type == "choice"; } }
        public bool IsCg { get { return type == "cg"; } }
        public bool IsFx { get { return type == "fx"; } }
    }

    /// <summary>选择项, 见 SCHEMA §3.3。affection/insomnia 为整数, 无变化写 0。</summary>
    [Serializable]
    public class StoryOption
    {
        public string text = "";
        public string affectionWho = "";
        public int affection = 0;
        public int insomnia = 0;
        public string flag = "";
        public string next = "";
    }

    /// <summary>好感度条目(JsonUtility 不支持字典, 所以用列表)。</summary>
    [Serializable]
    public class AffectionEntry
    {
        public string who = "";
        public int value = 0;

        public AffectionEntry() { }

        public AffectionEntry(string who, int value)
        {
            this.who = who;
            this.value = value;
        }
    }

    /// <summary>
    /// 可存档的进度。失眠值 insomnia 0-100; flags 记录剧情标记; memoryShards 记录
    /// 已解锁的记忆碎片; playSeconds 累计游玩时长。
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int saveVersion = 1;
        public string sceneId = "";
        public int lineIndex = 0;
        public int insomnia = 0;
        public List<string> flags = new List<string>();
        public List<string> memoryShards = new List<string>();
        public List<AffectionEntry> affection = new List<AffectionEntry>();
        public float playSeconds = 0f;
        public string savedAt = "";
        public string chapterLabel = "";

        /// <summary>@ending 路由命中的结局场景 id（与网页版 state.endingId 同口径），未路由时为空。</summary>
        public string endingId = "";

        /// <summary>命中的结局规则下标；-1 = 未路由或走 fallback（与网页版 resolveEnding 的 index 一致）。</summary>
        public int endingRule = -1;

        public const int InsomniaMin = 0;
        public const int InsomniaMax = 100;

        public int GetAffection(string who)
        {
            if (string.IsNullOrEmpty(who)) return 0;
            for (int i = 0; i < affection.Count; i++)
            {
                if (affection[i].who == who) return affection[i].value;
            }
            return 0;
        }

        public int AddAffection(string who, int delta)
        {
            if (string.IsNullOrEmpty(who) || delta == 0) return GetAffection(who);
            for (int i = 0; i < affection.Count; i++)
            {
                if (affection[i].who == who)
                {
                    affection[i].value += delta;
                    return affection[i].value;
                }
            }
            affection.Add(new AffectionEntry(who, delta));
            return delta;
        }

        public int AddInsomnia(int delta)
        {
            insomnia = Mathf.Clamp(insomnia + delta, InsomniaMin, InsomniaMax);
            return insomnia;
        }

        public bool HasFlag(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        }

        public void AddFlag(string flag)
        {
            if (!string.IsNullOrEmpty(flag) && !flags.Contains(flag)) flags.Add(flag);
        }

        public bool HasShard(string shard)
        {
            return !string.IsNullOrEmpty(shard) && memoryShards.Contains(shard);
        }

        public void AddShard(string shard)
        {
            if (!string.IsNullOrEmpty(shard) && !memoryShards.Contains(shard)) memoryShards.Add(shard);
        }

        /// <summary>失眠值所在档位: 用于结局判定与视觉强度。</summary>
        public int InsomniaTier
        {
            get
            {
                if (insomnia < 25) return 0;
                if (insomnia < 55) return 1;
                if (insomnia < 80) return 2;
                return 3;
            }
        }

        public void Reset()
        {
            sceneId = "";
            lineIndex = 0;
            insomnia = 0;
            flags.Clear();
            memoryShards.Clear();
            affection.Clear();
            playSeconds = 0f;
            chapterLabel = "";
            endingId = "";
            endingRule = -1;
        }

        public string ToJson(bool pretty)
        {
            return JsonUtility.ToJson(this, pretty);
        }

        public static GameState FromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                GameState s = JsonUtility.FromJson<GameState>(json);
                if (s == null) return null;
                if (s.flags == null) s.flags = new List<string>();
                if (s.memoryShards == null) s.memoryShards = new List<string>();
                if (s.affection == null) s.affection = new List<AffectionEntry>();
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Story] 存档解析失败: " + e.Message);
                return null;
            }
        }
    }

    /// <summary>存档槽位读写。写到 Application.persistentDataPath 下的 saves/ 目录。</summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;
        public const int AutoSlot = 0; // 0 = 自动存档, 1-3 = 手动槽

        public static string SavePath(int slot)
        {
            string dir = System.IO.Path.Combine(Application.persistentDataPath, "saves");
            return System.IO.Path.Combine(dir, "slot" + slot + ".json");
        }

        public static bool Has(int slot)
        {
            return System.IO.File.Exists(SavePath(slot));
        }

        public static bool Write(int slot, GameState state)
        {
            try
            {
                string path = SavePath(slot);
                string dir = System.IO.Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);
                state.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                System.IO.File.WriteAllText(path, state.ToJson(true), new System.Text.UTF8Encoding(false));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Story] 存档写入失败: " + e.Message);
                return false;
            }
        }

        public static GameState Read(int slot)
        {
            try
            {
                string path = SavePath(slot);
                if (!System.IO.File.Exists(path)) return null;
                return GameState.FromJson(System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8));
            }
            catch (Exception e)
            {
                Debug.LogError("[Story] 存档读取失败: " + e.Message);
                return null;
            }
        }

        public static void Delete(int slot)
        {
            try
            {
                string path = SavePath(slot);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Story] 存档删除失败: " + e.Message);
            }
        }

        /// <summary>槽位摘要, 用于读档面板显示(没有存档返回 null)。</summary>
        public static string Describe(int slot)
        {
            GameState s = Read(slot);
            if (s == null) return null;
            return s.chapterLabel + "  " + s.savedAt;
        }
    }
}