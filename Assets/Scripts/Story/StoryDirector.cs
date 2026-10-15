using System;
using System.Collections;
using System.Collections.Generic;
using Shiguang.Story;
using UnityEngine;

namespace TimeOfFragments.Story
{
    /// <summary>
    /// 剧情总控: 拿剧本库 + 当前进度, 按行播放, 处理选择支、演出、存档与结局。
    /// 界面全部交给 VNView, 这里只做逻辑。
    /// </summary>
    public class StoryDirector : MonoBehaviour
    {
        public const string PrefSpeedKey = "tof_text_speed";

        public static StoryDirector Instance;

        public VNView View;
        public StoryLibrary Library { get; private set; }
        public GameState State = new GameState();

        AudioSource bgmSource;
        AudioSource sfxSource;
        Coroutine lineRoutine;
        StoryScene current;
        int lineIndex;
        bool auto;
        bool skip;
        bool awaitingChoice;
        bool fastForwardRequested;
        bool saveMode;
        TextSpeedSetting speed = TextSpeedSetting.ById(1);
        string currentBgm = "";

        /// <summary>结局路由标记：场景顶层 next 等于它时按 endings.json 解析结局场景。</summary>
        const string EndingRouteToken = "@ending";

        /// <summary>topAffection 并列时的优先顺序，与 story/cast.json 的 characters[].name 顺序保持一致。</summary>
        static readonly string[] EndingCastOrder = { "林晓", "艾莉丝", "雷恩", "小雅", "时间领主", "老周", "维拉" };

        // 数据驱动结局表（语义与网页版 player-template.html 一致）；null = 表缺失/非法，回退旧五结局文案。
        EndingTable endingTable;
        bool endingRouteTaken;

        void Awake()
        {
            Instance = this;
        }

        public void Initialize(VNView view)
        {
            View = view;
            int savedSpeed = PlayerPrefs.GetInt(PrefSpeedKey, 1);
            speed = TextSpeedSetting.ById(savedSpeed);

            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = 0.55f;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            Library = new StoryLibrary();
            Library.Load();
            LoadEndingTable();

            View.OnAdvance = Advance;
            View.OnToggleAuto = ToggleAuto;
            View.OnToggleSkip = ToggleSkip;
            View.OnOpenSave = delegate { OpenSavePanel(true); };
            View.OnOpenLoad = delegate { OpenSavePanel(false); };
            View.OnOpenSettings = OpenSettings;
            View.OnBackToTitle = BackToTitle;
            View.OnNewGame = NewGame;
            View.OnContinueGame = ContinueGame;
            View.OnSlotChosen = OnSlotChosen;
            View.OnQuit = Quit;

            View.SetAutoState(false);
            View.SetSkipState(false);
            View.SetTitleSaveInfo(TitleSaveInfo());
            View.SetInsomniaTier(State.InsomniaTier);

            if (Library.SceneCount == 0)
            {
                View.Toast("没有找到剧本。剧本要放在 Assets/Resources/Story/ 下。", 5f);
                Debug.LogError("[Story] Resources/Story 下没有剧本 JSON。");
            }
            else
            {
                Debug.Log(string.Format("[Story] 剧本载入完成: {0} 个场景, 共 {1} 字。", Library.SceneCount, Library.TotalCjkChars));
            }
        }

        string TitleSaveInfo()
        {
            GameState autoSave = SaveSystem.Read(0);
            string head = Library == null
                ? ""
                : string.Format("剧本: {0} 个场景 · {1} 字　　", Library.SceneCount, Library.TotalCjkChars);
            if (autoSave == null) return head + "（还没有存档）";
            return head + "上次进度: " + autoSave.chapterLabel + "　" + autoSave.savedAt;
        }

        // ==================== 标题页 ====================

        public void NewGame()
        {
            State = new GameState();
            endingRouteTaken = false;
            View.SetEndingVisible(false);
            View.SetTitleVisible(false);
            View.SetInsomniaTier(0);
            JumpTo(Library.StartSceneId, 0);
        }

        public void ContinueGame()
        {
            GameState s = SaveSystem.Read(1);
            if (s == null) s = SaveSystem.Read(0);
            if (s == null)
            {
                View.Toast("还没有存档, 先开始游戏吧。");
                return;
            }
            State = s;
            endingRouteTaken = false;
            View.SetEndingVisible(false);
            View.SetTitleVisible(false);
            View.SetInsomniaTier(State.InsomniaTier);
            JumpTo(State.sceneId, State.lineIndex);
        }

        public void BackToTitle()
        {
            if (lineRoutine != null) { StopCoroutine(lineRoutine); lineRoutine = null; }
            awaitingChoice = false;
            auto = false;
            skip = false;
            View.SetAutoState(false);
            View.SetSkipState(false);
            View.HideChoices();
            View.ClosePanels();
            View.SetEndingVisible(false);
            View.SetTitleVisible(true);
            View.SetTitleSaveInfo(TitleSaveInfo());
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ==================== 播放 ====================

        void JumpTo(string sceneId, int index)
        {
            StoryScene scene = Library.Get(sceneId);
            if (scene == null)
            {
                Debug.LogError("[Story] 找不到场景 " + sceneId + ", 回标题。");
                View.Toast("剧本里找不到场景 " + sceneId, 4f);
                BackToTitle();
                return;
            }

            current = scene;
            lineIndex = Mathf.Max(0, index);
            State.sceneId = scene.sceneId;
            State.lineIndex = lineIndex;
            State.chapterLabel = scene.chapter + " " + scene.title;

            if (!string.IsNullOrEmpty(scene.background)) View.SetBackground(scene.background);
            PlayBgm(scene.bgm);
            View.SetChapter(scene.chapter + "　·　" + scene.title);
            AutoSave();
            PlayCurrentLine();
        }

        void PlayCurrentLine()
        {
            if (current == null) return;

            if (lineIndex >= current.lines.Length)
            {
                EndScene();
                return;
            }

            StoryLine line = current.lines[lineIndex];
            State.lineIndex = lineIndex;
            awaitingChoice = false;
            fastForwardRequested = false;

            if (line.type != "cg") View.SetCg("");

            if (!string.IsNullOrEmpty(line.background) && line.type != "cg") View.SetBackground(line.background);
            if (!string.IsNullOrEmpty(line.bgm)) PlayBgm(line.bgm);
            if (!string.IsNullOrEmpty(line.sfx)) PlaySfx(line.sfx);

            if (line.IsChoice) { PlayChoice(line); return; }
            if (line.IsCg) { PlayCg(line); return; }
            if (line.IsFx) { PlayFx(line); return; }

            string speaker = line.IsDialogue ? line.speaker : "";
            View.SetSpeaker(speaker);
            View.SetPortrait(line.IsDialogue ? line.sprite : "");
            StartTyping(line.text);
        }

        void StartTyping(string text)
        {
            if (lineRoutine != null) StopCoroutine(lineRoutine);
            lineRoutine = StartCoroutine(TypeRoutine(text));
        }

        IEnumerator TypeRoutine(string text)
        {
            yield return View.TypeBody(text, skip ? 0f : speed.CharDelay, delegate { return fastForwardRequested || skip; });
            if (awaitingChoice) yield break;

            float wait = -1f;
            if (skip) wait = 0.04f;
            else if (auto) wait = Mathf.Max(1.1f, 0.7f + (text == null ? 0 : text.Length) * 0.055f);

            if (wait < 0f) yield break;

            float t = 0f;
            while (t < wait)
            {
                if (awaitingChoice) yield break;
                if (!skip && !auto) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (awaitingChoice) yield break;
            NextLine();
        }

        void NextLine()
        {
            fastForwardRequested = false;
            lineIndex++;
            PlayCurrentLine();
        }

        public void Advance()
        {
            if (View.AnyPanelOpen) return;
            if (awaitingChoice) return;
            if (View.IsTyping) { fastForwardRequested = true; View.CompleteTyping(CurrentLineText()); return; }
            NextLine();
        }

        string CurrentLineText()
        {
            if (current == null || lineIndex < 0 || lineIndex >= current.lines.Length) return "";
            return current.lines[lineIndex].text;
        }

        void PlayChoice(StoryLine line)
        {
            awaitingChoice = true;
            View.SetSpeaker("");
            View.SetPortrait("");
            View.SetBody(line.prompt);
            List<StoryOption> options = new List<StoryOption>();
            if (line.options != null)
            {
                for (int i = 0; i < line.options.Length; i++)
                {
                    if (line.options[i] != null) options.Add(line.options[i]);
                }
            }
            if (options.Count == 0)
            {
                Debug.LogWarning("[Story] " + current.sceneId + " 第 " + lineIndex + " 行是 choice 但没有 options, 跳过。");
                awaitingChoice = false;
                NextLine();
                return;
            }
            View.ShowChoices(options, OnOptionPicked);
        }

        void OnOptionPicked(StoryOption option)
        {
            View.HideChoices();
            awaitingChoice = false;

            if (option.affection != 0 && !string.IsNullOrEmpty(option.affectionWho))
            {
                State.AddAffection(option.affectionWho, option.affection);
            }
            if (option.insomnia != 0)
            {
                State.AddInsomnia(option.insomnia);
                View.SetInsomniaTier(State.InsomniaTier);
            }
            if (!string.IsNullOrEmpty(option.flag)) State.AddFlag(option.flag);
            AutoSave();

            if (!string.IsNullOrEmpty(option.next))
            {
                JumpTo(option.next, 0);
                return;
            }
            NextLine();
        }

        void PlayCg(StoryLine line)
        {
            View.SetSpeaker("");
            View.SetPortrait("");
            View.SetPortrait("");
            View.SetCg(line.background);
            StartTyping(line.text);
        }

        void PlayFx(StoryLine line)
        {
            switch (line.fx)
            {
                case "shake": StartCoroutine(View.ShakeRoutine(0.55f, 24f)); break;
                case "flash": StartCoroutine(View.FlashRoutine(0.3f)); break;
                case "insomnia":
                    View.SetInsomniaTier(Mathf.Max(1, State.InsomniaTier));
                    break;
                case "timeline": StartCoroutine(TimelineRoutine()); break;
                case "wait": StartCoroutine(WaitRoutine(0.5f)); break;
                default: Debug.LogWarning("[Story] 未知 fx: " + line.fx); break;
            }

            if (string.IsNullOrEmpty(line.text))
            {
                NextLine();
                return;
            }
            View.SetSpeaker("");
            View.SetPortrait("");
            StartTyping(line.text);
        }

        IEnumerator TimelineRoutine()
        {
            yield return View.FadeRoutine(0f, 0.75f, 0.22f);
            StartCoroutine(View.ShakeRoutine(0.8f, 28f));
            yield return new WaitForSecondsRealtime(0.12f);
            yield return View.FadeRoutine(0.75f, 0f, 0.5f);
        }

        IEnumerator WaitRoutine(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
        }

        // ==================== 自动 / 快进 ====================

        public void ToggleAuto()
        {
            auto = !auto;
            if (auto) skip = false;
            View.SetAutoState(auto);
            View.SetSkipState(skip);
            if (auto && !View.IsTyping && !awaitingChoice) StartCoroutine(AutoNudge());
        }

        IEnumerator AutoNudge()
        {
            yield return new WaitForSecondsRealtime(1.0f);
            if (auto && !awaitingChoice && !View.IsTyping) NextLine();
        }

        public void ToggleSkip()
        {
            skip = !skip;
            if (skip) auto = false;
            View.SetSkipState(skip);
            View.SetAutoState(auto);
            if (skip && !View.IsTyping && !awaitingChoice) NextLine();
        }

        // ==================== 存档 / 设置 ====================

        void AutoSave()
        {
            State.playSeconds = Mathf.Max(State.playSeconds, 0f);
            State.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            SaveSystem.Write(0, State);
        }

        void OpenSavePanel(bool save)
        {
            saveMode = save;
            View.OpenSavePanel(save);
        }

        void OnSlotChosen(int slot)
        {
            if (saveMode)
            {
                if (slot == 0) return;
                GameState snapshot = State;
                snapshot.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                SaveSystem.Write(slot, snapshot);
                View.ClosePanels();
                View.Toast("已存档到 " + SlotName(slot));
                View.SetTitleSaveInfo(TitleSaveInfo());
                return;
            }

            GameState loaded = SaveSystem.Read(slot);
            if (loaded == null)
            {
                View.Toast("这个存档位是空的。");
                return;
            }
            State = loaded;
            endingRouteTaken = false;
            View.ClosePanels();
            View.SetEndingVisible(false);
            View.SetTitleVisible(false);
            View.SetInsomniaTier(State.InsomniaTier);
            JumpTo(State.sceneId, State.lineIndex);
        }

        static string SlotName(int slot)
        {
            if (slot == 0) return "自动存档";
            return "存档 " + slot;
        }

        void OpenSettings()
        {
            View.OpenSettingsPanel(speed, delegate (TextSpeedSetting picked)
            {
                speed = picked;
                PlayerPrefs.SetInt(PrefSpeedKey, picked.id);
                PlayerPrefs.Save();
            }, null);
        }

        // ==================== 音频 ====================

        void PlayBgm(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (id == "stop")
            {
                if (bgmSource.isPlaying) bgmSource.Stop();
                currentBgm = "";
                return;
            }
            if (id == currentBgm && bgmSource.isPlaying) return;
            currentBgm = id;
            AudioClip clip = AudioRegistry.Bgm(id);
            if (clip == null)
            {
                bgmSource.Stop();
                return;
            }
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        void PlaySfx(string id)
        {
            AudioClip clip = AudioRegistry.Sfx(id);
            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        // ==================== 结局 ====================

        void LoadEndingTable()
        {
            TextAsset asset = Resources.Load<TextAsset>("StoryConfig/endings");
            if (asset == null)
            {
                endingTable = null;
                Debug.LogWarning("[Story] 未找到 StoryConfig/endings.json, 结局将回退旧五结局文案。");
                return;
            }
            try
            {
                endingTable = EndingResolver.ParseTable(asset.text);
                Debug.Log(string.Format("[Story] 结局表载入: {0} 条规则, fallback {1}。",
                    endingTable.RuleCount,
                    string.IsNullOrEmpty(endingTable.FallbackScene) ? "(无)" : endingTable.FallbackScene));
            }
            catch (EndingTableException e)
            {
                endingTable = null;
                Debug.LogError("[Story] 结局表非法, 回退旧五结局文案: " + e.Message);
            }
        }

        /// <summary>场景收尾路由: @ending → 按结局表解析; 否则走 next; 都没有则进结局画面。</summary>
        void EndScene()
        {
            string next = current == null ? "" : current.next;
            if (next == EndingRouteToken) { RouteToEnding(); return; }
            if (!string.IsNullOrEmpty(next) && Library.Has(next)) { JumpTo(next, 0); return; }
            ShowEnding(null);
        }

        /// <summary>
        /// 对应网页版 endScene 的 @ending 分支: 无表/无命中/已路由过 → 直接进结局画面;
        /// 否则记录命中结果并跳转结局场景（目标缺失时由 JumpTo 既有逻辑报错回标题）。
        /// </summary>
        void RouteToEnding()
        {
            if (endingTable == null) { ShowEnding(null); return; }
            EndingResolution pick = endingTable.Resolve(BuildEndingSnapshot());
            if (pick == null || endingRouteTaken) { ShowEnding(pick); return; }
            endingRouteTaken = true;
            State.endingId = pick.Scene;
            State.endingRule = pick.RuleIndex;
            JumpTo(pick.Scene, 0);
        }

        EndingSnapshot BuildEndingSnapshot()
        {
            Dictionary<string, int> affection = new Dictionary<string, int>();
            if (State.affection != null)
            {
                for (int i = 0; i < State.affection.Count; i++)
                {
                    AffectionEntry entry = State.affection[i];
                    if (entry != null && !string.IsNullOrEmpty(entry.who)) affection[entry.who] = entry.value;
                }
            }
            int shards = State.memoryShards == null ? 0 : State.memoryShards.Count;
            return new EndingSnapshot(State.flags, State.insomnia, shards, affection, EndingCastOrder);
        }

        void ShowEnding(EndingResolution pick)
        {
            string sceneId = pick != null ? pick.Scene : State.endingId;
            string ruleTitle = endingTable == null || string.IsNullOrEmpty(sceneId)
                ? "" : endingTable.RuleTitleFor(sceneId);
            EndingScreen legacy = LegacyEnding();
            string pickTitle = pick == null ? "" : pick.Title;
            string title = !string.IsNullOrEmpty(pickTitle) ? pickTitle
                : (!string.IsNullOrEmpty(ruleTitle) ? ruleTitle : legacy.Title);

            // 与网页版一致: 规则有标题时用结局场景最后一行作正文, 否则用旧结局文案。
            string lastText = LastLineText();
            string body = !string.IsNullOrEmpty(ruleTitle) && !string.IsNullOrEmpty(lastText)
                ? "「" + lastText + "」" : legacy.Body;

            body += "\n\n—— 本次游玩 ——\n时长: " + FormatTime(State.playSeconds) + "　失眠值: " + State.insomnia + " / 100";
            string affection = AffectionSummary();
            if (!string.IsNullOrEmpty(affection)) body += "\n好感: " + affection;
            if (State.flags.Count > 0) body += "\n标记: " + State.flags.Count + " 个";

            View.ShowEnding(title, body);
        }

        string LastLineText()
        {
            if (current == null || current.lines == null || current.lines.Length == 0) return "";
            StoryLine last = current.lines[current.lines.Length - 1];
            return last == null || last.text == null ? "" : last.text;
        }

        /// <summary>旧五结局文案: 仅在结局表缺失/非法、或无命中且无 fallback 时使用（对应网页版 legacyEnding）。</summary>
        EndingScreen LegacyEnding()
        {
            if (State.HasFlag("ending_miracle"))
            {
                return new EndingScreen("结局 · 奇迹", "所有碎片在同一瞬间安静下来。\n你没有回到过去, 也没有抹掉任何一道伤痕——你只是终于愿意带着它们继续往前走。\n时间没有原谅谁, 但它停止了继续索要。");
            }
            if (State.insomnia >= 80)
            {
                return new EndingScreen("结局 · 失眠", "你已经分不清自己是在醒来还是在睡去。\n那条链还在向前延伸, 只是再没有人替它念出下一个词。\n窗外的天亮了, 而你不知道这是第几个天亮。");
            }
            if (State.HasFlag("ending_hero"))
            {
                return new EndingScreen("结局 · 英雄", "你把最后一块碎片按回了它该在的位置。\n代价被记在了别人看不见的地方, 但这一次, 所有人都活着走出了那栋楼。");
            }
            if (State.HasFlag("ending_tragic"))
            {
                return new EndingScreen("结局 · 诀别", "你救下了能救的一切, 除了那个一直站在你身边的人。\n时间合拢的速度比你说出那句话更快。");
            }
            if (State.HasFlag("ending_open"))
            {
                return new EndingScreen("结局 · 未完成", "链断了。\n没有人知道断口那边是什么, 也没有人再提起那个名字。\n故事在这里停了下来, 像一句被刻意咽回去的话。");
            }
            return new EndingScreen("结局 · 归途", "碎片的答案不在过去, 而在你决定怎么过完接下来的每一天。\n你合上了那本借来的书, 把它放回原来的位置。");
        }

        struct EndingScreen
        {
            public string Title;
            public string Body;
            public EndingScreen(string title, string body) { Title = title; Body = body; }
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int m = total / 60;
            int s = total % 60;
            if (m >= 60) return (m / 60) + " 小时 " + (m % 60) + " 分";
            return m + " 分 " + s + " 秒";
        }

        string AffectionSummary()
        {
            if (State.affection == null || State.affection.Count == 0) return "";
            string s = "";
            for (int i = 0; i < State.affection.Count; i++)
            {
                if (i > 0) s += "、";
                s += State.affection[i].who + " " + State.affection[i].value;
            }
            return s;
        }

        // ==================== 每帧 ====================

        void Update()
        {
            bool playing = current != null && (View == null || (!View.AnyPanelOpen && !IsTitleVisible()));
            if (playing) State.playSeconds += Time.unscaledDeltaTime;

            if (View != null && View.DebugVisible) View.SetDebugText(BuildDebugText());
        }

        bool IsTitleVisible()
        {
            return current == null;
        }

        string BuildDebugText()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("F1 关闭调试信息");
            sb.AppendLine("场景: " + (current == null ? "(标题)" : current.sceneId + "　" + current.title));
            sb.AppendLine("行: " + lineIndex + " / " + (current == null ? 0 : current.lines.Length));
            sb.AppendLine("失眠: " + State.insomnia + " (档 " + State.InsomniaTier + ")　标记 " + State.flags.Count);
            sb.AppendLine("时长: " + FormatTime(State.playSeconds));
            sb.AppendLine("剧本: " + Library.SceneCount + " 场景 / " + Library.TotalCjkChars + " 字");
            sb.AppendLine("自动 " + auto + "　快进 " + skip + "　打字中 " + View.IsTyping);
            if (State.affection != null && State.affection.Count > 0)
            {
                sb.Append("好感: ");
                for (int i = 0; i < State.affection.Count; i++) sb.Append(State.affection[i].who + "=" + State.affection[i].value + " ");
                sb.AppendLine();
            }
            if (Library.Problems.Count > 0)
            {
                sb.AppendLine("剧本问题 " + Library.Problems.Count + ":");
                for (int i = 0; i < Library.Problems.Count && i < 8; i++) sb.AppendLine("  " + Library.Problems[i]);
            }
            if (ArtRegistry.MissingIds.Count > 0)
            {
                sb.AppendLine("缺美术 " + ArtRegistry.MissingIds.Count + " 个 (用占位图):");
                for (int i = 0; i < ArtRegistry.MissingIds.Count && i < 8; i++) sb.AppendLine("  " + ArtRegistry.MissingIds[i]);
            }
            return sb.ToString();
        }
    }
}