using System;
using UnityEngine;
using UnityEngine.UI;

namespace TimeOfFragments.Story
{
    /// <summary>
    /// 纯代码搭 UI 的小工具集。整个游戏的界面都在运行时用这些方法建出来,
    /// 因此工程里不需要任何 prefab 或手写场景文件。
    /// 约定: 尺寸以 1920x1080 为基准(CanvasScaler 负责缩放)。
    /// </summary>
    public static class VNUiKit
    {
        public static readonly Color Ink = new Color(0.94f, 0.94f, 0.96f, 1f);
        public static readonly Color InkDim = new Color(0.72f, 0.74f, 0.80f, 1f);
        public static readonly Color Accent = new Color(0.45f, 0.78f, 0.95f, 1f);
        public static readonly Color PanelBg = new Color(0.04f, 0.05f, 0.08f, 0.82f);
        public static readonly Color ButtonBg = new Color(0.12f, 0.14f, 0.20f, 0.92f);
        public static readonly Color ButtonBgHi = new Color(0.24f, 0.34f, 0.46f, 0.98f);

        public static GameObject Node(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Rect(GameObject go)
        {
            return go.GetComponent<RectTransform>();
        }

        /// <summary>铺满父级。</summary>
        public static RectTransform Stretch(GameObject go, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            RectTransform rt = Rect(go);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>固定尺寸, 以父级中心为锚点。</summary>
        public static RectTransform Size(GameObject go, float w, float h, float x = 0f, float y = 0f)
        {
            RectTransform rt = Rect(go);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return rt;
        }

        public static RectTransform Anchor(GameObject go, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 pos)
        {
            RectTransform rt = Rect(go);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            return rt;
        }

        public static Image AddImage(GameObject go, Color color, bool raycast = false)
        {
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Solid(string name, Transform parent, Color color, bool raycast = false)
        {
            GameObject go = Node(name, parent);
            return AddImage(go, color, raycast);
        }

        public static Text Label(string name, Transform parent, string content, int size, TextAnchor align, Color color, bool bold = false)
        {
            GameObject go = Node(name, parent);
            Text t = go.AddComponent<Text>();
            t.font = FontProvider.Chinese;
            t.fontSize = size;
            t.text = content;
            t.alignment = align;
            t.color = color;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.35f;
            t.supportRichText = true;
            if (bold) t.fontStyle = FontStyle.Bold;
            return t;
        }

        /// <summary>一个可点击按钮(自带底板与文字)。</summary>
        public static Button TextButton(string name, Transform parent, string label, int fontSize, Action onClick, float w = 240f, float h = 72f)
        {
            GameObject go = Node(name, parent);
            Image bg = AddImage(go, ButtonBg, true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = bg;

            ColorBlock cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.25f, 1.25f, 1.32f, 1f);
            cb.pressedColor = new Color(0.8f, 0.86f, 1f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.4f);
            cb.fadeDuration = 0.08f;
            b.colors = cb;

            Text t = Label(name + "Text", go.transform, label, fontSize, TextAnchor.MiddleCenter, Ink);
            Stretch(t.gameObject, 12f, 4f, 12f, 4f);

            if (onClick != null) b.onClick.AddListener(new UnityEngine.Events.UnityAction(onClick));
            Size(go, w, h);
            return b;
        }

        /// <summary>透明全屏点击区(用于「点击任意处继续」)。</summary>
        public static Button ClickCatcher(string name, Transform parent, Action onClick)
        {
            GameObject go = Node(name, parent);
            Image img = AddImage(go, new Color(0f, 0f, 0f, 0f), true);
            Button b = go.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.None;
            Stretch(go);
            if (onClick != null) b.onClick.AddListener(new UnityEngine.Events.UnityAction(onClick));
            return b;
        }

        /// <summary>带标题与关闭按钮的模态面板。返回面板根节点与内容区。</summary>
        public static GameObject Modal(string name, Transform parent, string title, Action onClose, out RectTransform content)
        {
            GameObject root = Node(name, parent);
            Stretch(root);
            AddImage(root, new Color(0f, 0f, 0f, 0.62f), true);

            GameObject box = Node(name + "Box", root.transform);
            Size(box, 1100f, 720f);
            AddImage(box, PanelBg, true);
            GameObject topBar = VNUiKit.Node("BoxTopBar", box.transform);
            AddImage(topBar, new Color(0.16f, 0.20f, 0.28f, 1f));
            RectTransform bar = Rect(topBar);
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0f, 64f);
            bar.anchoredPosition = Vector2.zero;

            Text titleText = Label("ModalTitle", box.transform, title, 30, TextAnchor.MiddleLeft, Ink, true);
            Anchor(titleText.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(600f, 64f), new Vector2(28f, 0f));

            if (onClose != null)
            {
                Button close = TextButton("ModalClose", box.transform, "关闭", 26, onClose, 140f, 56f);
                Anchor(close.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(140f, 56f), new Vector2(-12f, -4f));
            }

            GameObject contentGo = Node(name + "Content", box.transform);
            content = Stretch(contentGo, 32f, 32f, 32f, 80f);
            return root;
        }
    }
}