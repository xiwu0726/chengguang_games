#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeOfFragments.Dialogue.Systems
{
    /// <summary>
    /// 选择对话框系统 - 管理玩家选择的显示和处理
    /// </summary>
    public class ChoiceSystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private GameObject choiceButtonPrefab;
        [SerializeField] private Button closeButton;

        [Header("Settings")]
        [SerializeField] private float appearDelay = 0.2f;
        [SerializeField] private float disappearDelay = 0.1f;
        [SerializeField] private bool autoCloseOnChoice = true;

        private List<Choice> currentChoices = new List<Choice>();
        private Dictionary<int, Button> choiceButtons = new Dictionary<int, Button>();

        // 事件
        public event System.Action<int> OnChoiceSelected;

        private void Awake()
        {
            if (choiceContainer == null)
                Debug.LogError("ChoiceContainer is not assigned!");

            if (choiceButtonPrefab == null)
                Debug.LogError("ChoiceButtonPrefab is not assigned!");

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseChoices);

            gameObject.SetActive(false);
        }

        /// <summary>
        /// 初始化选择对话框
        /// </summary>
        public void InitializeChoices(List<Choice> choices)
        {
            currentChoices = choices;
            choiceButtons.Clear();

            // 清除旧的选择按钮
            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }

            // 创建新的选择按钮
            for (int i = 0; i < choices.Count; i++)
            {
                GameObject choiceObj = Instantiate(choiceButtonPrefab, choiceContainer);
                Button choiceButton = choiceObj.GetComponent<Button>();
                Text choiceText = choiceObj.GetComponentInChildren<Text>();

                if (choiceText != null)
                {
                    choiceText.text = choices[i].text;
                }

                choiceButton.onClick.AddListener(() => OnChoiceButtonClicked(i));
                choiceButtons[i] = choiceButton;
            }

            // 显示选择对话框
            ShowChoices();
        }

        /// <summary>
        /// 显示选择对话框
        /// </summary>
        public void ShowChoices()
        {
            gameObject.SetActive(true);
            StartCoroutine(AppearAnimation());
        }

        /// <summary>
        /// 隐藏选择对话框
        /// </summary>
        public void CloseChoices()
        {
            StartCoroutine(DisappearAnimation());
        }

        /// <summary>
        /// 选择按钮点击
        /// </summary>
        private void OnChoiceButtonClicked(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= currentChoices.Count) return;

            Choice choice = currentChoices[choiceIndex];

            // 标记选择为已选
            choice.isSelected = true;

            // 触发选择事件
            choice.onSelected?.Invoke();

            // 触发全局选择事件
            OnChoiceSelected?.Invoke(choiceIndex);

            // 播放选择音效
            AudioManager.Instance?.PlaySFX("Choice");

            // 关闭选择对话框
            if (autoCloseOnChoice)
            {
                CloseChoices();
            }
        }

        /// <summary>
        /// 出现动画
        /// </summary>
        private System.Collections.IEnumerator AppearAnimation()
        {
            // 等待一小段时间
            yield return new WaitForSeconds(appearDelay);

            // 显示所有按钮
            foreach (var button in choiceButtons.Values)
            {
                button.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// 消失动画
        /// </summary>
        private System.Collections.IEnumerator DisappearAnimation()
        {
            // 等待一小段时间
            yield return new WaitForSeconds(disappearDelay);

            // 隐藏所有按钮
            foreach (var button in choiceButtons.Values)
            {
                button.gameObject.SetActive(false);
            }

            // 等待按钮完全隐藏后，禁用整个系统
            yield return new WaitForSeconds(0.1f);
            gameObject.SetActive(false);
        }

        /// <summary>
        /// 获取当前选择列表
        /// </summary>
        public List<Choice> GetCurrentChoices()
        {
            return currentChoices;
        }

        /// <summary>
        /// 获取特定选择的文本
        /// </summary>
        public string GetChoiceText(int choiceIndex)
        {
            if (choiceIndex < 0 || choiceIndex >= currentChoices.Count) return "";
            return currentChoices[choiceIndex].text;
        }

        /// <summary>
        /// 检查是否正在显示选择
        /// </summary>
        public bool IsShowing()
        {
            return gameObject.activeSelf;
        }

        /// <summary>
        /// 清除所有选择
        /// </summary>
        public void ClearChoices()
        {
            currentChoices.Clear();
            choiceButtons.Clear();

            foreach (Transform child in choiceContainer)
            {
                Destroy(child.gameObject);
            }

            CloseChoices();
        }
    }
}
#endif
