#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Systems
{
    /// <summary>
    /// 多周目重玩性系统 - 管理多周目体验
    /// </summary>
    public class RogueLikeNewGamePlusSystem : MonoBehaviour
    {
        /// <summary>
        /// 周目编号
        /// </summary>
        private int currentRun = 1;

        /// <summary>
        /// 周目解锁内容
        /// </summary>
        private Dictionary<int, List<string>> unlockedContent = new Dictionary<int, List<string>>();

        /// <summary>
        /// 周目难度倍率
        /// </summary>
        private float difficultyMultiplier = 1f;

        private void Awake()
        {
            // 初始化多周目系统
            Debug.Log($"RogueLikeNewGamePlusSystem initialized. Current run: {currentRun}");
        }

        /// <summary>
        /// 进入新周目
        /// </summary>
        public void EnterNewRun()
        {
            currentRun++;

            // 增加难度倍率
            difficultyMultiplier += 0.2f;

            // 保存解锁内容
            SaveUnlockedContent();

            Debug.Log($"Entered new run: {currentRun}. Difficulty multiplier: {difficultyMultiplier}");
        }

        /// <summary>
        /// 获取当前周目
        /// </summary>
        public int GetCurrentRun()
        {
            return currentRun;
        }

        /// <summary>
        /// 获取难度倍率
        /// </summary>
        public float GetDifficultyMultiplier()
        {
            return difficultyMultiplier;
        }

        /// <summary>
        /// 解锁内容
        /// </summary>
        public void UnlockContent(string contentId)
        {
            if (!unlockedContent.ContainsKey(currentRun))
            {
                unlockedContent[currentRun] = new List<string>();
            }

            if (!unlockedContent[currentRun].Contains(contentId))
            {
                unlockedContent[currentRun].Add(contentId);

                Debug.Log($"Unlocked content '{contentId}' in run {currentRun}");
            }
        }

        /// <summary>
        /// 检查内容是否解锁
        /// </summary>
        public bool IsContentUnlocked(string contentId)
        {
            if (unlockedContent.ContainsKey(currentRun))
            {
                return unlockedContent[currentRun].Contains(contentId);
            }

            return false;
        }

        /// <summary>
        /// 保存解锁内容
        /// </summary>
        private void SaveUnlockedContent()
        {
            // 在实际项目中，这里会保存到文件
            // 例如：SaveManager.Instance.SaveNewGamePlusData(currentRun, unlockedContent[currentRun]);
            Debug.Log($"Saved unlocked content for run {currentRun}");
        }

        /// <summary>
        /// 加载解锁内容
        /// </summary>
        private void LoadUnlockedContent()
        {
            // 在实际项目中，这里会从文件加载
            Debug.Log("Loaded unlocked content.");
        }
    }
}
#endif
