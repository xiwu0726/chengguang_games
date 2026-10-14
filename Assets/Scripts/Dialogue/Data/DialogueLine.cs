using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.Dialogue.Data
{
    /// <summary>
    /// 对话行数据结构
    /// </summary>
    [System.Serializable]
    public class DialogueLine
    {
        /// <summary>
        /// 角色名称
        /// </summary>
        public string characterName;

        /// <summary>
        /// 角色立绘
        /// </summary>
        public Sprite characterSprite;

        /// <summary>
        /// 对话文本
        /// </summary>
        public string text;

        /// <summary>
        /// 是否是选择对话
        /// </summary>
        public bool isChoice;

        /// <summary>
        /// 选择选项列表
        /// </summary>
        public List<Choice> choices;

        /// <summary>
        /// 是否跳过打字效果
        /// </summary>
        public bool skipTyping;

        /// <summary>
        /// 对话类型
        /// </summary>
        public DialogueType dialogueType = DialogueType.Normal;

        /// <summary>
        /// 触发的对话事件
        /// </summary>
        public System.Action onDialogueComplete;

        /// <summary>
        /// 触发的选择事件
        /// </summary>
        public System.Action<Choice> onChoiceSelected;

        public DialogueLine()
        {
            characterName = "";
            characterSprite = null;
            text = "";
            isChoice = false;
            choices = new List<Choice>();
            skipTyping = false;
            dialogueType = DialogueType.Normal;
        }

        public DialogueLine(string name, Sprite sprite, string text)
        {
            characterName = name;
            characterSprite = sprite;
            this.text = text;
            isChoice = false;
            choices = new List<Choice>();
            skipTyping = false;
            dialogueType = DialogueType.Normal;
        }
    }

    /// <summary>
    /// 选择选项数据结构
    /// </summary>
    [System.Serializable]
    public class Choice
    {
        /// <summary>
        /// 选择文本
        /// </summary>
        public string text;

        /// <summary>
        /// 选择效果
        /// </summary>
        public ChoiceEffect effect;

        /// <summary>
        /// 是否已选择
        /// </summary>
        public bool isSelected;

        /// <summary>
        /// 触发的选择事件
        /// </summary>
        public System.Action onSelected;

        public Choice()
        {
            text = "";
            effect = new ChoiceEffect();
            isSelected = false;
        }

        public Choice(string text, ChoiceEffect effect)
        {
            this.text = text;
            this.effect = effect;
            isSelected = false;
        }
    }

    /// <summary>
    /// 选择效果
    /// </summary>
    [System.Serializable]
    public class ChoiceEffect
    {
        /// <summary>
        /// 是否改变好感度
        /// </summary>
        public bool changeAffection = false;

        /// <summary>
        /// 好感度变化值
        /// </summary>
        public int affectionChange = 0;

        /// <summary>
        /// 是否改变失眠值
        /// </summary>
        public bool changeInsomnia = false;

        /// <summary>
        /// 失眠值变化
        /// </summary>
        public float insomniaChange = 0f;

        /// <summary>
        /// 是否解锁记忆碎片
        /// </summary>
        public bool unlockMemoryShard = false;

        /// <summary>
        /// 解锁的记忆碎片ID
        /// </summary>
        public string memoryShardId = "";

        /// <summary>
        /// 是否改变时间线
        /// </summary>
        public bool changeTimeLine = false;

        /// <summary>
        /// 目标时间线
        /// </summary>
        public int targetTimeLine = 0;

        /// <summary>
        /// 是否触发剧情分支
        /// </summary>
        public bool triggerBranch = false;

        /// <summary>
        /// 触发的剧情分支ID
        /// </summary>
        public string branchId = "";

        /// <summary>
        /// 是否触发对话事件
        /// </summary>
        public bool triggerDialogueEvent = false;

        /// <summary>
        /// 触发的对话事件ID
        /// </summary>
        public string dialogueEventId = "";
    }
}
