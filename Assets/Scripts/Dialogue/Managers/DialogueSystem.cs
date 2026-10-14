using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TimeOfFragments.Dialogue.Managers
{
    /// <summary>
    /// 对话文本系统 - 管理所有对话的显示和处理
    /// </summary>
    public class DialogueSystem : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Text dialogueText;
        [SerializeField] private Image characterSprite;
        [SerializeField] private Text characterName;
        [SerializeField] private GameObject choicePanel;
        [SerializeField] private Button continueButton;

        [Header("Settings")]
        [SerializeField] private float typingSpeed = 0.05f;
        [SerializeField] private float nameTextSpeed = 0.1f;
        [SerializeField] private bool autoContinue = false;

        private DialogueType currentDialogueType = DialogueType.Normal;
        private string currentText = "";
        private int currentIndex = 0;
        private List<DialogueLine> dialogueLines;
        private bool isTyping = false;
        private bool isComplete = false;
        private float timer = 0f;

        // 事件
        public event System.Action OnDialogueComplete;
        public event System.Action OnChoiceSelected;

        private void Awake()
        {
            if (dialogueText == null)
                Debug.LogError("DialogueText is not assigned!");

            if (characterSprite == null)
                Debug.LogError("CharacterSprite is not assigned!");

            if (characterName == null)
                Debug.LogError("CharacterName is not assigned!");

            if (choicePanel == null)
                Debug.LogError("ChoicePanel is not assigned!");

            if (continueButton == null)
                Debug.LogError("ContinueButton is not assigned!");

            choicePanel.SetActive(false);
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        private void Update()
        {
            if (isTyping && !autoContinue)
            {
                timer += Time.deltaTime;
                if (timer >= typingSpeed)
                {
                    timer = 0f;
                    currentIndex++;
                    UpdateTextDisplay();

                    if (currentIndex >= currentText.Length)
                    {
                        isTyping = false;
                        currentIndex = currentText.Length;
                        UpdateTextDisplay();
                        CheckIfComplete();
                    }
                }
            }
            else if (autoContinue && isTyping)
            {
                timer += Time.deltaTime;
                if (timer >= typingSpeed * 2) // 自动打字速度稍快
                {
                    timer = 0f;
                    currentIndex++;
                    UpdateTextDisplay();

                    if (currentIndex >= currentText.Length)
                    {
                        isTyping = false;
                        currentIndex = currentText.Length;
                        UpdateTextDisplay();
                        CheckIfComplete();
                    }
                }
            }
        }

        /// <summary>
        /// 开始对话
        /// </summary>
        public void StartDialogue(List<DialogueLine> lines, DialogueType type = DialogueType.Normal)
        {
            dialogueLines = lines;
            currentDialogueType = type;
            isComplete = false;
            isTyping = false;
            currentIndex = 0;

            // 根据对话类型设置UI
            switch (type)
            {
                case DialogueType.StreamOfConsciousness:
                    dialogueText.color = new Color(0.8f, 0.8f, 1f); // 浅蓝色
                    break;
                case DialogueType.Dream:
                    dialogueText.color = new Color(1f, 0.8f, 0.8f); // 浅红色
                    break;
                case DialogueType.Combat:
                    dialogueText.color = new Color(1f, 0.8f, 0.6f); // 橙色
                    break;
                default:
                    dialogueText.color = Color.white;
                    break;
            }

            // 显示第一个对话行
            ShowFirstLine();
        }

        /// <summary>
        /// 显示第一个对话行
        /// </summary>
        private void ShowFirstLine()
        {
            if (dialogueLines == null || dialogueLines.Count == 0)
            {
                Debug.LogError("Dialogue lines is empty!");
                EndDialogue();
                return;
            }

            DialogueLine firstLine = dialogueLines[0];
            characterName.text = firstLine.characterName;
            characterSprite.sprite = firstLine.characterSprite;

            if (firstLine.isChoice)
            {
                currentText = firstLine.text;
                isComplete = true;
                ShowChoices(firstLine.choices);
            }
            else
            {
                currentText = firstLine.text;
                currentIndex = 0;
                isComplete = false;
                isTyping = true;
                timer = 0f;
                UpdateTextDisplay();
            }
        }

        /// <summary>
        /// 更新文本显示
        /// </summary>
        private void UpdateTextDisplay()
        {
            if (currentText.Length > currentIndex)
            {
                dialogueText.text = currentText.Substring(0, currentIndex);
            }
            else
            {
                dialogueText.text = currentText;
            }
        }

        /// <summary>
        /// 检查是否完成
        /// </summary>
        private void CheckIfComplete()
        {
            if (!isComplete && !isTyping)
            {
                // 检查是否还有更多对话行
                if (dialogueLines.Count > 1)
                {
                    // 显示继续按钮
                    continueButton.gameObject.SetActive(true);
                }
                else
                {
                    // 对话结束
                    EndDialogue();
                }
            }
        }

        /// <summary>
        /// 继续按钮点击
        /// </summary>
        private void OnContinueButtonClicked()
        {
            continueButton.gameObject.SetActive(false);

            // 移除当前行，显示下一行
            dialogueLines.RemoveAt(0);

            if (dialogueLines.Count > 0)
            {
                ShowFirstLine();
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// 显示选择
        /// </summary>
        private void ShowChoices(List<Choice> choices)
        {
            choicePanel.SetActive(true);
            choicePanel.GetComponentInChildren<ChoiceSystem>()?.InitializeChoices(choices);
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        private void EndDialogue()
        {
            choicePanel.SetActive(false);
            dialogueText.text = "";
            characterName.text = "";
            characterSprite.sprite = null;
            dialogueLines = null;
            isComplete = true;
            isTyping = false;

            OnDialogueComplete?.Invoke();
        }

        /// <summary>
        /// 快速完成打字效果
        /// </summary>
        public void SkipTyping()
        {
            isTyping = false;
            currentIndex = currentText.Length;
            UpdateTextDisplay();
            CheckIfComplete();
        }

        /// <summary>
        /// 检查当前是否正在打字
        /// </summary>
        public bool IsTyping()
        {
            return isTyping;
        }

        /// <summary>
        /// 检查对话是否完成
        /// </summary>
        public bool IsComplete()
        {
            return isComplete;
        }
    }
}
