using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeOfFragments.Story
{
    /// <summary>
    /// 视觉小说界面。整个界面在运行时用代码建出来(见 VNUiKit), 不依赖任何 prefab。
    /// 视图层只负责「显示」与「把用户操作转成回调」, 剧情推进逻辑全在 StoryDirector。
    /// </summary>
    public class VNView : MonoBehaviour
    {
        // ---- 交给 StoryDirector 挂的回调 ----
        public Action OnAdvance;
        public Action OnToggleAuto;
        public Action OnToggleSkip;
        public Action OnOpenSave;
        public Action OnOpenLoad;
        public Action OnOpenSettings;
        public Action OnBackToTitle;
        public Action OnNewGame;
        public Action OnContinueGame;
        public Action<int> OnSlotChosen;
        public Action OnQuit;

        Canvas canvas;
        RectTransform worldRoot;
        Image bgImage;
        Image cgImage;
        Image portraitImage;
        Image fadeOverlay;
        Image insomniaOverlay;
        RectTransform dialogueBox;
        GameObject namePlate;
        Text nameText;
        Text bodyText;
        Text hintText;
        Text chapterText;
        GameObject topBar;
        GameObject choiceRoot;
        GameObject titleScreen;
        Text titleSaveInfo;
        GameObject endingScreen;
        Text endingTitle;
        Text endingText;
        GameObject savePanel;
        GameObject settingsPanel;
        GameObject toastBox;
        Text toastText;
        GameObject debugPanel;
        Text debugText;
        RectTransform quickBar;
        readonly List<Button> choiceButtons = new List<Button>();

        Color autoIdle, skipIdle;
        Color autoOn = new Color(0.30f, 0.62f, 0.42f, 1f);
        Color skipOn = new Color(0.72f, 0.52f, 0.24f, 1f);
        bool typing;

        public bool IsTyping { get { return typing; } }

        // ==================== 构建 ====================

        public static VNView Create(Transform parent)
        {
            GameObject go = new GameObject("VNView");
            if (parent != null) go.transform.SetParent(parent, false);
            VNView view = go.AddComponent<VNView>();
            view.Build();
            return view;
        }

        void Build()
        {
            EnsureEventSystem();

            GameObject canvasGo = new GameObject("VNCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasGo.transform;

            // --- 世界层(背景/整屏CG/立绘): 震屏只晃这一层 ---
            worldRoot = VNUiKit.Stretch(VNUiKit.Node("World", root));
            bgImage = VNUiKit.AddImage(VNUiKit.Node("Background", worldRoot), Color.black);
            VNUiKit.Stretch(bgImage.gameObject);
            bgImage.preserveAspect = false;

            cgImage = VNUiKit.AddImage(VNUiKit.Node("CG", worldRoot), Color.white);
            VNUiKit.Stretch(cgImage.gameObject);
            cgImage.preserveAspect = true;
            cgImage.gameObject.SetActive(false);

            portraitImage = VNUiKit.AddImage(VNUiKit.Node("Portrait", worldRoot), Color.white);
            VNUiKit.Anchor(portraitImage.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(620f, 900f), new Vector2(-60f, 250f));
            portraitImage.preserveAspect = true;
            portraitImage.gameObject.SetActive(false);

            // --- 点击层: 在世界层之上、对话框之下(z 序), 对话框不挡射线所以点击能穿过来 ---
            VNUiKit.ClickCatcher("ClickCatcher", root, RaiseAdvance);

            // --- 失眠视觉层 ---
            insomniaOverlay = VNUiKit.AddImage(VNUiKit.Node("InsomniaOverlay", root), new Color(0.42f, 0.20f, 0.62f, 0f));
            VNUiKit.Stretch(insomniaOverlay.gameObject);

            // --- 顶栏 ---
            topBar = VNUiKit.Node("TopBar", root);
            VNUiKit.Stretch(topBar).offsetMax = new Vector2(0f, -68f);
            VNUiKit.Rect(topBar).anchorMin = new Vector2(0f, 1f);
            VNUiKit.Rect(topBar).anchorMax = new Vector2(1f, 1f);
            VNUiKit.Rect(topBar).pivot = new Vector2(0.5f, 1f);
            VNUiKit.Rect(topBar).sizeDelta = new Vector2(0f, 68f);
            VNUiKit.Rect(topBar).anchoredPosition = Vector2.zero;
            VNUiKit.AddImage(VNUiKit.Node("TopBarBg", topBar.transform), new Color(0f, 0f, 0f, 0.35f));
            VNUiKit.Stretch(topBar.transform.Find("TopBarBg").gameObject);

            chapterText = VNUiKit.Label("ChapterText", topBar.transform, "", 26, TextAnchor.MiddleLeft, VNUiKit.InkDim);
            VNUiKit.Anchor(chapterText.gameObject, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(900f, 48f), new Vector2(28f, 0f));

            quickBar = VNUiKit.Rect(VNUiKit.Node("QuickBar", topBar.transform));
            VNUiKit.Anchor(quickBar.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(700f, 52f), new Vector2(-16f, 0f));
            string[] labels = { "自动", "快进", "存档", "读档", "设置", "标题" };
            Action[] actions = { RaiseToggleAuto, RaiseToggleSkip, RaiseOpenSave, RaiseOpenLoad, RaiseOpenSettings, RaiseBackToTitle };
            for (int i = 0; i < labels.Length; i++)
            {
                Button b = VNUiKit.TextButton("Btn" + labels[i], quickBar, labels[i], 22, actions[i], 108f, 46f);
                VNUiKit.Anchor(b.gameObject, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(108f, 46f), new Vector2(-i * 114f, 0f));
                if (i == 0) autoIdle = ButtonIdleColor(b);
                if (i == 1) skipIdle = ButtonIdleColor(b);
            }

            // --- 对话框 ---
            GameObject boxGo = VNUiKit.Node("DialogueBox", root);
            dialogueBox = VNUiKit.Rect(boxGo);
            dialogueBox.anchorMin = new Vector2(0f, 0f);
            dialogueBox.anchorMax = new Vector2(1f, 0f);
            dialogueBox.pivot = new Vector2(0.5f, 0f);
            dialogueBox.sizeDelta = new Vector2(-80f, 330f);
            dialogueBox.anchoredPosition = new Vector2(0f, 40f);
            VNUiKit.AddImage(boxGo, VNUiKit.PanelBg);   // 不接收射线, 点击穿透到 ClickCatcher
            // 对话框顶边的高亮细线。原先写成 AddImage(Solid(...), color)：Solid 已经返回 Image，
            // 再套一层 AddImage 是类型错误（CS1503，编译不过），这里直接用 Solid 并拉成 2px 顶边条。
            Image boxLine = VNUiKit.Solid("BoxLine", boxGo.transform, new Color(0.45f, 0.78f, 0.95f, 0.55f));
            VNUiKit.Stretch(boxLine.gameObject, 0f, -2f, 0f, 0f);

            GameObject namePlateGo = VNUiKit.Node("NamePlate", boxGo.transform);
            namePlate = namePlateGo;
            VNUiKit.Anchor(namePlateGo, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(360f, 56f), new Vector2(36f, 12f));
            VNUiKit.AddImage(namePlateGo, new Color(0.10f, 0.20f, 0.30f, 0.95f));
            nameText = VNUiKit.Label("NameText", namePlateGo.transform, "", 28, TextAnchor.MiddleLeft, VNUiKit.Accent, true);
            VNUiKit.Stretch(nameText.gameObject, 18f, 0f, 18f, 0f);

            bodyText = VNUiKit.Label("BodyText", boxGo.transform, "", 32, TextAnchor.UpperLeft, VNUiKit.Ink);
            VNUiKit.Stretch(bodyText.gameObject, 44f, 56f, 44f, 70f);

            hintText = VNUiKit.Label("HintText", boxGo.transform, "空格 / 点击继续   Ctrl 快进   Esc 菜单", 20, TextAnchor.LowerRight, VNUiKit.InkDim);
            VNUiKit.Anchor(hintText.gameObject, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(560f, 36f), new Vector2(-24f, 12f));

            // --- 选择支 ---
            choiceRoot = VNUiKit.Node("Choices", root);
            VNUiKit.Stretch(choiceRoot);
            choiceRoot.SetActive(false);

            // --- 淡入淡出 / 结局 / 标题 ---
            fadeOverlay = VNUiKit.AddImage(VNUiKit.Node("Fade", root), new Color(0f, 0f, 0f, 1f));
            VNUiKit.Stretch(fadeOverlay.gameObject);

            BuildTitleScreen(root);
            BuildEndingScreen(root);
            BuildToast(root);
            BuildDebug(root);

            StartCoroutine(FadeRoutine(1f, 0f, 0.6f));
        }

        static Color ButtonIdleColor(Button b)
        {
            return b.colors.normalColor;
        }

        void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindObjectOfType<EventSystem>() != null) return;
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }

        void BuildTitleScreen(Transform root)
        {
            titleScreen = VNUiKit.Node("TitleScreen", root);
            VNUiKit.Stretch(titleScreen);

            Image bg = VNUiKit.AddImage(VNUiKit.Node("TitleBg", titleScreen.transform), Color.white, true);
            VNUiKit.Stretch(bg.gameObject);
            bg.sprite = ArtRegistry.Background("bg_title");
            bg.color = new Color(0.75f, 0.78f, 0.86f, 1f);

            // 标题页压暗层。同样去掉多余的 AddImage(Solid(...)) 嵌套，顺手不再靠 Find 找刚创建的节点。
            Image titleShade = VNUiKit.Solid("TitleShade", titleScreen.transform, new Color(0f, 0f, 0f, 0.45f));
            VNUiKit.Stretch(titleShade.gameObject);

            Text title = VNUiKit.Label("GameTitle", titleScreen.transform, "时光之痕", 104, TextAnchor.MiddleCenter, VNUiKit.Ink, true);
            VNUiKit.Anchor(title.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1200f, 150f), new Vector2(0f, 300f));

            Text sub = VNUiKit.Label("GameSub", titleScreen.transform, "Time Of Fragments", 32, TextAnchor.MiddleCenter, VNUiKit.InkDim);
            VNUiKit.Anchor(sub.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1200f, 60f), new Vector2(0f, 210f));

            Text tag = VNUiKit.Label("GameTag", titleScreen.transform, "关于时间、记忆与选择的故事", 24, TextAnchor.MiddleCenter, VNUiKit.InkDim);
            VNUiKit.Anchor(tag.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1200f, 60f), new Vector2(0f, 150f));

            string[] names = { "开始游戏", "继续游戏", "设置", "退出" };
            Action[] acts = { RaiseNewGame, RaiseContinueGame, RaiseOpenSettings, RaisQuit };
            for (int i = 0; i < names.Length; i++)
            {
                Button b = VNUiKit.TextButton("Title" + names[i], titleScreen.transform, names[i], 32, acts[i], 460f, 84f);
                VNUiKit.Anchor(b.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(460f, 84f), new Vector2(0f, -40f - i * 104f));
            }

            titleSaveInfo = VNUiKit.Label("TitleSaveInfo", titleScreen.transform, "", 22, TextAnchor.MiddleCenter, VNUiKit.InkDim);
            VNUiKit.Anchor(titleSaveInfo.gameObject, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(1400f, 40f), new Vector2(0f, 40f));

            SetTitleVisible(true);
        }

        void BuildEndingScreen(Transform root)
        {
            endingScreen = VNUiKit.Node("EndingScreen", root);
            VNUiKit.Stretch(endingScreen);
            VNUiKit.AddImage(endingScreen, new Color(0f, 0f, 0f, 0.9f), true);

            endingTitle = VNUiKit.Label("EndingTitle", endingScreen.transform, "", 64, TextAnchor.MiddleCenter, VNUiKit.Accent, true);
            VNUiKit.Anchor(endingTitle.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1500f, 100f), new Vector2(0f, 300f));

            endingText = VNUiKit.Label("EndingText", endingScreen.transform, "", 30, TextAnchor.UpperLeft, VNUiKit.Ink);
            VNUiKit.Anchor(endingText.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1400f, 520f), new Vector2(0f, 0f));

            Button back = VNUiKit.TextButton("EndingBack", endingScreen.transform, "回到标题", 30, RaiseBackToTitle, 320f, 78f);
            VNUiKit.Anchor(back.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(320f, 78f), new Vector2(0f, -360f));

            endingScreen.SetActive(false);
        }

        void BuildToast(Transform root)
        {
            toastBox = VNUiKit.Node("Toast", root);
            VNUiKit.Anchor(toastBox, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(760f, 64f), new Vector2(0f, 400f));
            VNUiKit.AddImage(toastBox, new Color(0f, 0f, 0f, 0.78f));
            toastText = VNUiKit.Label("ToastText", toastBox.transform, "", 26, TextAnchor.MiddleCenter, VNUiKit.Ink);
            VNUiKit.Stretch(toastText.gameObject, 20f, 0f, 20f, 0f);
            toastBox.SetActive(false);
        }

        void BuildDebug(Transform root)
        {
            debugPanel = VNUiKit.Node("DebugPanel", root);
            VNUiKit.Anchor(debugPanel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(760f, 400f), new Vector2(12f, -80f));
            VNUiKit.AddImage(debugPanel, new Color(0f, 0f, 0f, 0.72f));
            debugText = VNUiKit.Label("DebugText", debugPanel.transform, "", 20, TextAnchor.UpperLeft, new Color(0.65f, 1f, 0.75f, 1f));
            VNUiKit.Stretch(debugText.gameObject, 12f, 10f, 12f, 10f);
            debugPanel.SetActive(false);
        }

        // ==================== 显示 ====================

        public void SetChapter(string text)
        {
            if (chapterText != null) chapterText.text = text;
        }

        public void SetTitleVisible(bool visible)
        {
            if (titleScreen != null) titleScreen.SetActive(visible);
            if (dialogueBox != null) dialogueBox.gameObject.SetActive(!visible);
            if (topBar != null) topBar.SetActive(!visible);
            if (quickBar != null) quickBar.gameObject.SetActive(!visible);
        }

        public void SetTitleSaveInfo(string text)
        {
            if (titleSaveInfo != null) titleSaveInfo.text = text;
        }

        public void SetEndingVisible(bool visible)
        {
            if (endingScreen != null) endingScreen.SetActive(visible);
            if (dialogueBox != null) dialogueBox.gameObject.SetActive(!visible);
            if (topBar != null) topBar.SetActive(!visible);
        }

        public void ShowEnding(string title, string text)
        {
            if (endingTitle != null) endingTitle.text = title;
            if (endingText != null) endingText.text = text;
            SetEndingVisible(true);
        }

        public void SetBackground(string id)
        {
            Sprite s = ArtRegistry.Background(id);
            if (bgImage != null)
            {
                bgImage.sprite = s;
                bgImage.color = Color.white;
            }
        }

        public void SetCg(string id)
        {
            if (cgImage == null) return;
            if (string.IsNullOrEmpty(id)) { cgImage.gameObject.SetActive(false); return; }
            cgImage.sprite = ArtRegistry.Cg(id);
            cgImage.color = Color.white;
            cgImage.gameObject.SetActive(true);
        }

        public void SetPortrait(string id)
        {
            if (portraitImage == null) return;
            if (string.IsNullOrEmpty(id)) { portraitImage.gameObject.SetActive(false); return; }
            portraitImage.sprite = ArtRegistry.Portrait(id);
            portraitImage.color = Color.white;
            portraitImage.gameObject.SetActive(true);
        }

        public void SetSpeaker(string speaker)
        {
            bool has = !string.IsNullOrEmpty(speaker);
            if (namePlate != null) namePlate.SetActive(has);
            if (nameText != null) nameText.text = has ? speaker : "";
        }

        public void SetBody(string text)
        {
            if (bodyText != null) bodyText.text = text == null ? "" : text;
            typing = false;
        }

        /// <summary>逐字显示。fastForward 返回 true 时立刻全显(用于点击跳过打字)。</summary>
        public IEnumerator TypeBody(string text, float charDelay, Func<bool> fastForward)
        {
            if (bodyText == null) yield break;
            text = text == null ? "" : text;
            if (charDelay <= 0.0005f)
            {
                bodyText.text = text;
                yield break;
            }

            typing = true;
            int shown = 0;
            float acc = 0f;
            while (shown < text.Length)
            {
                if (fastForward != null && fastForward())
                {
                    break;
                }
                acc += Time.unscaledDeltaTime;
                bool progressed = false;
                while (acc >= charDelay && shown < text.Length)
                {
                    acc -= charDelay;
                    shown++;
                    progressed = true;
                }
                if (progressed) bodyText.text = text.Substring(0, shown);
                yield return null;
            }
            bodyText.text = text;
            typing = false;
        }

        public void CompleteTyping(string text)
        {
            SetBody(text);
        }

        public void ShowChoices(List<StoryOption> options, Action<StoryOption> onPick)
        {
            HideChoices();
            if (choiceRoot == null || options == null) return;
            choiceRoot.SetActive(true);

            int n = options.Count;
            float h = 92f;
            float gap = 22f;
            float totalH = n * h + (n - 1) * gap;
            float top = totalH * 0.5f - h * 0.5f;

            for (int i = 0; i < n; i++)
            {
                StoryOption opt = options[i];
                Button b = VNUiKit.TextButton("Choice" + i, choiceRoot.transform, "　" + opt.text, 28, null, 1180f, h);
                VNUiKit.Anchor(b.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(1180f, h), new Vector2(0f, 90f + top - i * (h + gap)));
                Image bg = b.targetGraphic as Image;
                if (bg != null) bg.color = new Color(0.10f, 0.13f, 0.19f, 0.94f);
                StoryOption captured = opt;
                b.onClick.AddListener(new UnityEngine.Events.UnityAction(delegate { if (onPick != null) onPick(captured); }));
                choiceButtons.Add(b);
            }
        }

        public void HideChoices()
        {
            for (int i = 0; i < choiceButtons.Count; i++)
            {
                if (choiceButtons[i] != null) UnityEngine.Object.Destroy(choiceButtons[i].gameObject);
            }
            choiceButtons.Clear();
            if (choiceRoot != null) choiceRoot.SetActive(false);
        }

        public void SetAutoState(bool on)
        {
            Color c = on ? autoOn : autoIdle;
            SetButtonColor("Btn自动", c);
        }

        public void SetSkipState(bool on)
        {
            Color c = on ? skipOn : skipIdle;
            SetButtonColor("Btn快进", c);
        }

        void SetButtonColor(string buttonName, Color color)
        {
            if (quickBar == null) return;
            Transform t = quickBar.Find(buttonName);
            if (t == null) return;
            Button b = t.GetComponent<Button>();
            if (b == null) return;
            ColorBlock cb = b.colors;
            cb.normalColor = color;
            cb.selectedColor = color;
            cb.highlightedColor = color * 1.15f;
            b.colors = cb;
            Image img = b.targetGraphic as Image;
            if (img != null) img.color = color;
        }

        public void SetInsomniaTier(int tier)
        {
            if (insomniaOverlay == null) return;
            float alpha = tier <= 0 ? 0f : tier == 1 ? 0.05f : tier == 2 ? 0.11f : 0.18f;
            insomniaOverlay.color = new Color(0.42f, 0.20f, 0.62f, alpha);
        }

        public void Toast(string message, float seconds = 2.2f)
        {
            StopCoroutine("ToastRoutine");
            StartCoroutine(ToastRoutine(message, seconds));
        }

        IEnumerator ToastRoutine(string message, float seconds)
        {
            if (toastBox == null) yield break;
            toastBox.SetActive(true);
            if (toastText != null) toastText.text = message;
            yield return new WaitForSecondsRealtime(seconds);
            toastBox.SetActive(false);
        }

        // ==================== 演出 ====================

        public IEnumerator FadeRoutine(float from, float to, float duration)
        {
            if (fadeOverlay == null) yield break;
            float t = 0f;
            Color c = fadeOverlay.color;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
                c.a = Mathf.Lerp(from, to, k);
                fadeOverlay.color = c;
                yield return null;
            }
            c.a = to;
            fadeOverlay.color = c;
        }

        public IEnumerator ShakeRoutine(float duration = 0.5f, float magnitude = 22f)
        {
            if (worldRoot == null) yield break;
            Vector2 origin = worldRoot.anchoredPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(t / duration);
                worldRoot.anchoredPosition = origin + new Vector2(UnityEngine.Random.Range(-magnitude, magnitude), UnityEngine.Random.Range(-magnitude, magnitude)) * damp;
                yield return null;
            }
            worldRoot.anchoredPosition = origin;
        }

        public IEnumerator FlashRoutine(float duration = 0.28f)
        {
            if (fadeOverlay == null) yield break;
            yield return FadeRoutine(fadeOverlay.color.a, 1f, duration * 0.35f);
            yield return FadeRoutine(1f, 0f, duration * 0.65f);
        }

        // ==================== 面板 ====================

        public void OpenSavePanel(bool saveMode)
        {
            ClosePanels();
            RectTransform content;
            GameObject panel = VNUiKit.Modal(saveMode ? "SavePanel" : "LoadPanel", canvas.transform,
                saveMode ? "存档" : "读档", ClosePanels, out content);
            savePanel = panel;
            panel.transform.SetAsLastSibling();
            PrepareModalInput(panel);

            string[] slotNames = { "自动存档", "存档 1", "存档 2", "存档 3" };
            for (int slot = 0; slot < 4; slot++)
            {
                int captured = slot;
                bool auto = slot == 0;
                string desc = SaveSystem.Describe(slot);
                string label = slotNames[slot] + "   " + (desc == null ? "（空）" : desc);
                Button b = VNUiKit.TextButton("Slot" + slot, content, label, 26, delegate
                {
                    if (OnSlotChosen != null) OnSlotChosen(captured);
                }, 1000f, 92f);
                VNUiKit.Anchor(b.gameObject, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1000f, 92f), new Vector2(0f, -30f - slot * 108f));
                if (!saveMode && desc == null)
                {
                    b.interactable = false;
                }
                if (auto && saveMode)
                {
                    b.interactable = false; // 自动存档由系统写, 不让玩家手点
                }
            }
        }

        public void OpenSettingsPanel(TextSpeedSetting speed, Action<TextSpeedSetting> onSpeed, Action onClose)
        {
            ClosePanels();
            RectTransform content;
            GameObject panel = VNUiKit.Modal("SettingsPanel", canvas.transform, "设置", delegate { ClosePanels(); if (onClose != null) onClose(); }, out content);
            settingsPanel = panel;
            panel.transform.SetAsLastSibling();
            PrepareModalInput(panel);

            Text speedLabel = VNUiKit.Label("SpeedLabel", content, "文字速度: " + speed.Label, 26, TextAnchor.MiddleLeft, VNUiKit.Ink);
            VNUiKit.Anchor(speedLabel.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(700f, 50f), new Vector2(10f, -20f));

            string[] names = { "慢", "中", "快", "瞬间" };
            for (int i = 0; i < names.Length; i++)
            {
                TextSpeedSetting opt = TextSpeedSetting.ById(i);
                Button b = VNUiKit.TextButton("Speed" + i, content, names[i], 24, delegate
                {
                    if (onSpeed != null) onSpeed(opt);
                    speedLabel.text = "文字速度: " + opt.Label;
                }, 200f, 64f);
                VNUiKit.Anchor(b.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(200f, 64f), new Vector2(10f + i * 216f, -84f));
            }

            Text tip = VNUiKit.Label("SettingsTip", content,
                "快捷键：空格/回车 继续　Ctrl 按住快进　Esc 开关本面板　F1 调试信息\n" +
                "存档位置：" + Application.persistentDataPath + "\\saves", 22, TextAnchor.UpperLeft, VNUiKit.InkDim);
            VNUiKit.Anchor(tip.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(1020f, 160f), new Vector2(10f, -180f));
        }

        void PrepareModalInput(GameObject panel)
        {
            // 模态面板在最上层且接收射线, 因此不会把点击漏给「点击继续」。
            if (panel == null) return;
        }

        public void ClosePanels()
        {
            if (savePanel != null) { UnityEngine.Object.Destroy(savePanel); savePanel = null; }
            if (settingsPanel != null) { UnityEngine.Object.Destroy(settingsPanel); settingsPanel = null; }
        }

        public bool AnyPanelOpen
        {
            get { return savePanel != null || settingsPanel != null; }
        }

        public void SetDebugVisible(bool visible)
        {
            if (debugPanel != null) debugPanel.SetActive(visible);
        }

        public void SetDebugText(string text)
        {
            if (debugText != null) debugText.text = text;
        }

        public bool DebugVisible
        {
            get { return debugPanel != null && debugPanel.activeSelf; }
        }

        // ==================== 输入 ====================

        void Update()
        {
            if (titleScreen != null && titleScreen.activeSelf) return;
            if (endingScreen != null && endingScreen.activeSelf) return;
            if (AnyPanelOpen) return;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)) RaiseAdvance();
            if (Input.GetKeyDown(KeyCode.Escape)) RaiseOpenSettings();
            if (Input.GetKeyDown(KeyCode.F1)) SetDebugVisible(!DebugVisible);
        }

        void RaiseAdvance() { if (OnAdvance != null) OnAdvance(); }
        void RaiseToggleAuto() { if (OnToggleAuto != null) OnToggleAuto(); }
        void RaiseToggleSkip() { if (OnToggleSkip != null) OnToggleSkip(); }
        void RaiseOpenSave() { if (OnOpenSave != null) OnOpenSave(); }
        void RaiseOpenLoad() { if (OnOpenLoad != null) OnOpenLoad(); }
        void RaiseOpenSettings() { if (OnOpenSettings != null) OnOpenSettings(); }
        void RaiseBackToTitle() { if (OnBackToTitle != null) OnBackToTitle(); }
        void RaiseNewGame() { if (OnNewGame != null) OnNewGame(); }
        void RaiseContinueGame() { if (OnContinueGame != null) OnContinueGame(); }
        void RaisQuit() { if (OnQuit != null) OnQuit(); }
    }

    /// <summary>文字速度档位。</summary>
    public struct TextSpeedSetting
    {
        public int id;
        public string Label;
        public float CharDelay;

        public static TextSpeedSetting ById(int id)
        {
            switch (id)
            {
                case 0: return new TextSpeedSetting { id = 0, Label = "慢", CharDelay = 0.055f };
                case 1: return new TextSpeedSetting { id = 1, Label = "中", CharDelay = 0.035f };
                case 2: return new TextSpeedSetting { id = 2, Label = "快", CharDelay = 0.018f };
                default: return new TextSpeedSetting { id = 3, Label = "瞬间", CharDelay = 0f };
            }
        }
    }
}