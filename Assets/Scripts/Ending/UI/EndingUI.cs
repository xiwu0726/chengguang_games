using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeOfFragments.Ending.UI
{
    /// <summary>
    /// 结局UI系统 - 管理结局显示和选择
    /// </summary>
    public class EndingUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject endingPanel;
        [SerializeField] private Text endingTitle;
        [SerializeField] private Text endingDescription;
        [SerializeField] private Text endingDetail;
        [SerializeField] private Image endingCG;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button choiceButton;

        [Header("CG Collection")]
        [SerializeField] private List<Sprite> cgCollection;

        [Header("Settings")]
        [SerializeField] private float cgDisplayTime = 5f;
        [SerializeField] private float detailDisplayTime = 10f;

        private EndingData currentEnding;
        private float timer = 0f;

        // 事件
        public event System.Action OnEndingShown;
        public event System.Action OnContinueClicked;
        public event System.Action OnRetryClicked;
        public event System.Action OnNewGameClicked;
        public event System.Action OnChoiceClicked;

        private void Awake()
        {
            if (endingPanel == null)
                Debug.LogError("EndingPanel is not assigned!");

            if (endingTitle == null)
                Debug.LogError("EndingTitle is not assigned!");

            if (endingDescription == null)
                Debug.LogError("EndingDescription is not assigned!");

            if (endingDetail == null)
                Debug.LogError("EndingDetail is not assigned!");

            if (endingCG == null)
                Debug.LogError("EndingCG is not assigned!");

            if (continueButton == null)
                Debug.LogError("ContinueButton is not assigned!");

            if (retryButton == null)
                Debug.LogError("RetryButton is not assigned!");

            if (newGameButton == null)
                Debug.LogError("NewGameButton is not assigned!");

            if (choiceButton == null)
                Debug.LogError("ChoiceButton is not assigned!");

            endingPanel.SetActive(false);

            continueButton.onClick.AddListener(OnContinueButtonClicked);
            retryButton.onClick.AddListener(OnRetryButtonClicked);
            newGameButton.onClick.AddListener(OnNewGameButtonClicked);
            choiceButton.onClick.AddListener(OnChoiceButtonClicked);
        }

        private void Update()
        {
            if (endingPanel.activeSelf)
            {
                timer += Time.deltaTime;

                // 自动关闭CG显示
                if (timer >= cgDisplayTime && endingCG.sprite != null)
                {
                    endingCG.sprite = null;
                    timer = 0f;
                }

                // 自动关闭详细信息
                if (timer >= detailDisplayTime && endingDetail.text != "")
                {
                    endingDetail.text = "";
                }
            }
        }

        /// <summary>
        /// 显示结局
        /// </summary>
        public void ShowEnding(EndingData ending)
        {
            if (ending == null) return;

            currentEnding = ending;

            // 显示结局面板
            endingPanel.SetActive(true);

            // 显示结局信息
            endingTitle.text = ending.endingName;
            endingDescription.text = ending.description;
            endingDetail.text = ending.detail;

            // 显示结局评级
            string ratingText = GetRatingText(ending.rating);
            endingDescription.text += $"\n\n评级: {ratingText}";

            // 显示结局类型
            string typeText = GetEndTypeText(ending.endType);
            endingDescription.text += $"\n类型: {typeText}";

            // 显示CG
            if (ending.cgCollection != null && ending.cgCollection.Count > 0)
            {
                endingCG.sprite = ending.cgCollection[0];
                timer = 0f;
            }
            else
            {
                endingCG.sprite = null;
            }

            // 设置按钮状态
            SetupButtons(ending);

            Debug.Log($"Shown ending: {ending.endingName}");

            // 触发事件
            OnEndingShown?.Invoke();
        }

        /// <summary>
        /// 隐藏结局
        /// </summary>
        public void HideEnding()
        {
            endingPanel.SetActive(false);
            currentEnding = null;
        }

        /// <summary>
        /// 设置按钮状态
        /// </summary>
        private void SetupButtons(EndingData ending)
        {
            // 检查是否可重复
            bool canRepeat = ending.canRepeat && ending.isAchieved;

            // 根据结局类型设置按钮
            switch (ending.endType)
            {
                case EndType.Normal:
                    continueButton.gameObject.SetActive(true);
                    retryButton.gameObject.SetActive(false);
                    newGameButton.gameObject.SetActive(false);
                    choiceButton.gameObject.SetActive(false);
                    break;

                case EndType.Heroic:
                    continueButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(false);
                    newGameButton.gameObject.SetActive(true);
                    choiceButton.gameObject.SetActive(true);
                    break;

                case EndType.Tragic:
                    continueButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(true);
                    newGameButton.gameObject.SetActive(true);
                    choiceButton.gameObject.SetActive(true);
                    break;

                case EndType.Open:
                    continueButton.gameObject.SetActive(true);
                    retryButton.gameObject.SetActive(false);
                    newGameButton.gameObject.SetActive(true);
                    choiceButton.gameObject.SetActive(true);
                    break;

                case EndType.Miracle:
                    continueButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(false);
                    newGameButton.gameObject.SetActive(true);
                    choiceButton.gameObject.SetActive(true);
                    break;

                case EndType.Hidden:
                    continueButton.gameObject.SetActive(false);
                    retryButton.gameObject.SetActive(false);
                    newGameButton.gameObject.SetActive(false);
                    choiceButton.gameObject.SetActive(true);
                    break;
            }
        }

        /// <summary>
        /// 获取评级文本
        /// </summary>
        private string GetRatingText(EndingRating rating)
        {
            switch (rating)
            {
                case EndingRating.S: return "S (完美)";
                case EndingRating.A: return "A (优秀)";
                case EndingRating.B: return "B (良好)";
                case EndingRating.C: return "C (一般)";
                case EndingRating.D: return "D (差)";
                case EndingRating.F: return "F (失败)";
                default: return "B";
            }
        }

        /// <summary>
        /// 获取结局类型文本
        /// </summary>
        private string GetEndTypeText(EndType endType)
        {
            switch (endType)
            {
                case EndType.Normal: return "正常";
                case EndType.Heroic: return "英雄";
                case EndType.Tragic: return "悲剧";
                case EndType.Open: return "开放";
                case EndType.Miracle: return "奇迹";
                case EndType.Hidden: return "隐藏";
                default: return "未知";
            }
        }

        /// <summary>
        /// 继续按钮点击
        /// </summary>
        private void OnContinueButtonClicked()
        {
            OnContinueClicked?.Invoke();
        }

        /// <summary>
        /// 重玩按钮点击
        /// </summary>
        private void OnRetryButtonClicked()
        {
            OnRetryClicked?.Invoke();
        }

        /// <summary>
        /// 新游戏按钮点击
        /// </summary>
        private void OnNewGameButtonClicked()
        {
            OnNewGameClicked?.Invoke();
        }

        /// <summary>
        /// 选择按钮点击
        /// </summary>
        private void OnChoiceButtonClicked()
        {
            OnChoiceClicked?.Invoke();
        }

        /// <summary>
        /// 获取当前结局
        /// </summary>
        public EndingData GetCurrentEnding()
        {
            return currentEnding;
        }

        /// <summary>
        /// 检查是否显示结局
        /// </summary>
        public bool IsShowing()
        {
            return endingPanel.activeSelf;
        }

        /// <summary>
        /// 显示CG
        /// </summary>
        public void ShowCG(Sprite cg)
        {
            endingCG.sprite = cg;
            timer = 0f;
        }

        /// <summary>
        /// 清除CG
        /// </summary>
        public void ClearCG()
        {
            endingCG.sprite = null;
        }

        /// <summary>
        /// 设置CG显示时间
        /// </summary>
        public void SetCGDisplayTime(float time)
        {
            cgDisplayTime = time;
        }

        /// <summary>
        /// 获取CG集合
        /// </summary>
        public List<Sprite> GetCGCollection()
        {
            return cgCollection;
        }

        /// <summary>
        /// 设置CG集合
        /// </summary>
        public void SetCGCollection(List<Sprite> collection)
        {
            cgCollection = collection;
        }
    }
}
