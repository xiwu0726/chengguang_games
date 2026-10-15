#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using UnityEngine;

namespace TimeOfFragments.RogueLike.Systems
{
    /// <summary>
    /// 难度递增系统 - 管理游戏难度的递增
    /// </summary>
    public class RogueLikeDifficultySystem : MonoBehaviour
    {
        /// <summary>
        /// 难度等级
        /// </summary>
        public enum DifficultyLevel
        {
            Easy,
            Normal,
            Hard,
            VeryHard,
            Insane
        }

        /// <summary>
        /// 当前难度等级
        /// </summary>
        private DifficultyLevel currentDifficulty = DifficultyLevel.Normal;

        /// <summary>
        /// 难度递增间隔（场景数）
        /// </summary>
        private int difficultyIncreaseInterval = 5;

        /// <summary>
        /// 当前场景数
        /// </summary>
        private int currentSceneCount = 0;

        private void Awake()
        {
            // 初始化难度系统
            Debug.Log("RogueLikeDifficultySystem initialized.");
        }

        /// <summary>
        /// 增加难度
        /// </summary>
        public void IncreaseDifficulty()
        {
            currentSceneCount++;

            // 每隔一定场景增加一次难度
            if (currentSceneCount % difficultyIncreaseInterval == 0)
            {
                IncreaseDifficultyLevel();
            }
        }

        /// <summary>
        /// 增加难度等级
        /// </summary>
        private void IncreaseDifficultyLevel()
        {
            if (currentDifficulty < DifficultyLevel.Insanity)
            {
                currentDifficulty++;

                Debug.Log($"Difficulty increased to: {currentDifficulty}");
            }
        }

        /// <summary>
        /// 获取当前难度等级
        /// </summary>
        public DifficultyLevel GetCurrentDifficulty()
        {
            return currentDifficulty;
        }

        /// <summary>
        /// 获取难度描述
        /// </summary>
        public string GetDifficultyDescription()
        {
            switch (currentDifficulty)
            {
                case DifficultyLevel.Easy: return "简单";
                case DifficultyLevel.Normal: return "正常";
                case DifficultyLevel.Hard: return "困难";
                case DifficultyLevel.VeryHard: return "极难";
                case DifficultyLevel.Insanity: return "疯狂";
                default: return "未知";
            }
        }

        /// <summary>
        /// 重置难度
        /// </summary>
        public void ResetDifficulty()
        {
            currentDifficulty = DifficultyLevel.Normal;
            currentSceneCount = 0;

            Debug.Log("Difficulty reset to Normal.");
        }
    }
}
#endif
