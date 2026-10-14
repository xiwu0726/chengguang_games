using System;
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Story
{
    /// <summary>
    /// 剧本库: 启动时把 Assets/Resources/Story 下所有 JSON 读进内存并按 sceneId 建索引。
    /// 同时做链路校验(每个 next 都必须指向存在的场景), 把问题汇总起来而不是直接崩。
    /// </summary>
    public class StoryLibrary
    {
        public const string DefaultStartScene = "1-1";

        readonly Dictionary<string, StoryScene> scenes = new Dictionary<string, StoryScene>();
        readonly List<string> problems = new List<string>();

        public IReadOnlyList<string> Problems { get { return problems; } }
        public int SceneCount { get { return scenes.Count; } }
        public string StartSceneId { get; private set; }
        public int TotalCjkChars { get; private set; }

        public IEnumerable<StoryScene> AllScenes { get { return scenes.Values; } }

        public StoryScene Get(string sceneId)
        {
            if (string.IsNullOrEmpty(sceneId)) return null;
            StoryScene s;
            return scenes.TryGetValue(sceneId, out s) ? s : null;
        }

        public bool Has(string sceneId)
        {
            return !string.IsNullOrEmpty(sceneId) && scenes.ContainsKey(sceneId);
        }

        public void Load()
        {
            scenes.Clear();
            problems.Clear();
            TotalCjkChars = 0;

            TextAsset[] assets = Resources.LoadAll<TextAsset>("Story");
            if (assets == null || assets.Length == 0)
            {
                problems.Add("Resources/Story 下没有找到任何剧本 JSON。剧本必须放在 Assets/Resources/Story/ 里。");
                StartSceneId = DefaultStartScene;
                return;
            }

            for (int i = 0; i < assets.Length; i++)
            {
                TextAsset asset = assets[i];
                StoryScene scene = null;
                try
                {
                    scene = JsonUtility.FromJson<StoryScene>(asset.text);
                }
                catch (Exception e)
                {
                    problems.Add(asset.name + ": JSON 解析失败 - " + e.Message);
                    continue;
                }

                if (scene == null || string.IsNullOrEmpty(scene.sceneId))
                {
                    problems.Add(asset.name + ": 缺少 sceneId(是否为剧本文件?)");
                    continue;
                }
                if (scene.sceneId != asset.name)
                {
                    problems.Add(asset.name + ": sceneId(" + scene.sceneId + ") 与文件名不一致");
                }
                if (scenes.ContainsKey(scene.sceneId))
                {
                    problems.Add(scene.sceneId + ": sceneId 重复");
                    continue;
                }
                if (scene.lines == null || scene.lines.Length == 0)
                {
                    problems.Add(scene.sceneId + ": lines 为空");
                }

                scenes[scene.sceneId] = scene;
                TotalCjkChars += CountCjk(scene);
            }

            ValidateLinks();

            if (scenes.ContainsKey(DefaultStartScene)) StartSceneId = DefaultStartScene;
            else
            {
                StartSceneId = null;
                foreach (string key in scenes.Keys) { StartSceneId = key; break; }
                if (StartSceneId != null) problems.Add("找不到起始场景 " + DefaultStartScene + ", 退化为 " + StartSceneId);
            }

            if (problems.Count > 0)
            {
                Debug.LogWarning("[Story] 剧本问题 " + problems.Count + " 条:\n  " + string.Join("\n  ", problems.ToArray()));
            }
            Debug.Log("[Story] 载入 " + scenes.Count + " 个场景, 共 " + TotalCjkChars + " 个汉字。");
        }

        void ValidateLinks()
        {
            foreach (KeyValuePair<string, StoryScene> pair in scenes)
            {
                StoryScene scene = pair.Value;
                CheckLink(scene.sceneId, "next", scene.next);
                if (scene.lines == null) continue;
                for (int i = 0; i < scene.lines.Length; i++)
                {
                    StoryLine line = scene.lines[i];
                    if (line == null || line.options == null) continue;
                    for (int j = 0; j < line.options.Length; j++)
                    {
                        StoryOption opt = line.options[j];
                        if (opt == null) continue;
                        CheckLink(scene.sceneId + " 第 " + i + " 行的选项 " + j, "next", opt.next);
                        if (line.type == "choice" && string.IsNullOrEmpty(opt.next))
                        {
                            // 收敛型选择: next 为空表示继续本场景剩下的行, 这是合法的。
                        }
                    }
                }
            }
        }

        void CheckLink(string where, string field, string target)
        {
            if (string.IsNullOrEmpty(target)) return;
            if (!scenes.ContainsKey(target))
            {
                problems.Add(where + " 的 " + field + "(" + target + ") 指向不存在的场景");
            }
        }

        public static int CountCjk(StoryScene scene)
        {
            if (scene == null || scene.lines == null) return 0;
            int total = 0;
            for (int i = 0; i < scene.lines.Length; i++)
            {
                StoryLine line = scene.lines[i];
                if (line == null) continue;
                total += CountCjk(line.text);
                total += CountCjk(line.prompt);
                if (line.options == null) continue;
                for (int j = 0; j < line.options.Length; j++)
                {
                    if (line.options[j] != null) total += CountCjk(line.options[j].text);
                }
            }
            return total;
        }

        public static int CountCjk(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            int n = 0;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c >= '\u4e00' && c <= '\u9fff') n++;
            }
            return n;
        }
    }

    /// <summary>
    /// 中文字体。用系统字体动态渲染(动态字体按需栅格化, 不需要预先烘焙 CJK 字形集),
    /// 因此无需往工程里塞 TTF, 也不会出现 TMP 那种缺字方块。
    /// </summary>
    public static class FontProvider
    {
        static Font cached;

        public static Font Chinese
        {
            get
            {
                if (cached != null) return cached;

                string[] candidates = { "Microsoft YaHei UI", "Microsoft YaHei", "微软雅黑", "SimHei", "黑体", "SimSun", "宋体", "Noto Sans CJK SC", "Source Han Sans SC" };
                for (int i = 0; i < candidates.Length; i++)
                {
                    try
                    {
                        Font f = Font.CreateDynamicFontFromOSFont(candidates[i], 32);
                        if (f != null) { cached = f; return cached; }
                    }
                    catch (Exception) { }
                }

                try
                {
                    string[] installed = Font.GetOSInstalledFontNames();
                    if (installed != null && installed.Length > 0)
                    {
                        for (int i = 0; i < installed.Length; i++)
                        {
                            string name = installed[i];
                            if (string.IsNullOrEmpty(name)) continue;
                            if (name.IndexOf("YaHei", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                name.IndexOf("SimHei", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                name.IndexOf("SimSun", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                Font f = Font.CreateDynamicFontFromOSFont(name, 32);
                                if (f != null) { cached = f; return cached; }
                            }
                        }
                    }
                }
                catch (Exception) { }

                cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (cached == null) cached = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (cached == null) Debug.LogError("[Story] 找不到任何可用字体, 中文将无法显示。");
                return cached;
            }
        }
    }

    /// <summary>
    /// 美术资源按 id 取用。美术还没生产出来时**不报错**, 而是给一张带 id 色的占位图,
    /// 保证剧本可以立刻试玩(这也让「先写剧本、后补美术」成为可行流程)。
    /// 注意: 资源以 Texture2D 读入再在运行时包成 Sprite, 因此 PNG 不需要改导入设置。
    /// </summary>
    public static class ArtRegistry
    {
        public const string BgRoot = "Art/Bg/";
        public const string PortraitRoot = "Art/Portrait/";
        public const string CgRoot = "Art/Cg/";

        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        static readonly List<string> missing = new List<string>();

        public static IReadOnlyList<string> MissingIds { get { return missing; } }

        public static Sprite Background(string id) { return Fetch(BgRoot, id); }
        public static Sprite Portrait(string id) { return Fetch(PortraitRoot, id); }
        public static Sprite Cg(string id) { return Fetch(CgRoot, id); }

        public static Sprite Fetch(string root, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string key = root + id;
            Sprite cachedSprite;
            if (cache.TryGetValue(key, out cachedSprite)) return cachedSprite;

            Sprite sprite = Resources.Load<Sprite>(key);
            if (sprite == null)
            {
                Texture2D tex = Resources.Load<Texture2D>(key);
                if (tex != null)
                {
                    sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    sprite.name = id;
                }
            }
            if (sprite == null)
            {
                if (!missing.Contains(key)) missing.Add(key);
                sprite = Placeholder(id);
            }
            cache[key] = sprite;
            return sprite;
        }

        static Sprite Placeholder(string id)
        {
            const int w = 64, h = 64;
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color tint = ColorFromId(id);
            Color[] pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    bool border = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                    pixels[y * w + x] = border ? tint * 1.6f : tint;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            tex.name = "placeholder_" + id;
            Sprite s = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), 100f);
            s.name = "placeholder_" + id;
            return s;
        }

        static Color ColorFromId(string id)
        {
            int hash = 17;
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < id.Length; i++) hash = hash * 31 + id[i];
            }
            float r = 0.18f + ((hash & 0xFF) / 255f) * 0.22f;
            float g = 0.18f + (((hash >> 8) & 0xFF) / 255f) * 0.22f;
            float b = 0.24f + (((hash >> 16) & 0xFF) / 255f) * 0.26f;
            return new Color(r, g, b, 1f);
        }

        public static void LogMissingOnce()
        {
            if (missing.Count == 0) return;
            Debug.LogWarning("[Story] " + missing.Count + " 个美术资源尚未生产(已用占位图):\n  " + string.Join("\n  ", missing.ToArray()));
        }
    }

    /// <summary>音频按 id 取用; 资源缺失只是静音, 永不抛错。</summary>
    public static class AudioRegistry
    {
        public const string BgmRoot = "Audio/Bgm/";
        public const string SfxRoot = "Audio/Sfx/";

        static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

        public static AudioClip Bgm(string id) { return Fetch(BgmRoot, id); }
        public static AudioClip Sfx(string id) { return Fetch(SfxRoot, id); }

        public static AudioClip Fetch(string root, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            string key = root + id;
            AudioClip cachedClip;
            if (cache.TryGetValue(key, out cachedClip)) return cachedClip;
            AudioClip clip = Resources.Load<AudioClip>(key);
            cache[key] = clip;
            return clip;
        }
    }
}