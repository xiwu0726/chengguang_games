using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Systems
{
    /// <summary>
    /// 随机挑战系统 - 管理随机触发的小挑战
    /// </summary>
    public class RogueLikeChallengeSystem : MonoBehaviour
    {
        /// <summary>
        /// 挑战类型
        /// </summary>
        public enum ChallengeType
        {
            SpeedChallenge,     // 速度挑战
            AccuracyChallenge,  // 精度挑战
            MemoryChallenge,    // 记忆挑战
            ChoiceChallenge,    // 选择挑战
            SurvivalChallenge   // 存活挑战
        }

        /// <summary>
        /// 挑战数据
        /// </summary>
        private List<RogueLikeChallenge> challenges = new List<RogueLikeChallenge>();

        private void Awake()
        {
            // 初始化挑战系统
            Debug.Log("RogueLikeChallengeSystem initialized.");
        }

        /// <summary>
        /// 随机触发挑战
        /// </summary>
        public void TriggerRandomChallenge()
        {
            if (challenges.Count == 0) return;

            // 随机选择一个挑战
            RogueLikeChallenge challenge = challenges[Random.Range(0, challenges.Count)];

            if (!challenge.isCompleted)
            {
                // 显示挑战
                ShowChallenge(challenge);

                Debug.Log($"Triggered challenge: {challenge.challengeName}");
            }
        }

        /// <summary>
        /// 完成挑战
        /// </summary>
        public void CompleteChallenge(string challengeId)
        {
            foreach (var challenge in challenges)
            {
                if (challenge.challengeId == challengeId)
                {
                    challenge.isCompleted = true;

                    // 奖励
                    GrantChallengeReward(challenge);

                    Debug.Log($"Completed challenge: {challenge.challengeName}");
                    break;
                }
            }
        }

        /// <summary>
        /// 显示挑战
        /// </summary>
        private void ShowChallenge(RogueLikeChallenge challenge)
        {
            // 在实际项目中，这里会显示挑战UI
            Debug.Log($"Showing challenge: {challenge.challengeName} - {challenge.description}");
        }

        /// <summary>
        /// 奖励挑战
        /// </summary>
        private void GrantChallengeReward(RogueLikeChallenge challenge)
        {
            // 在实际项目中，这里会发放奖励
            Debug.Log($"Granting challenge reward: {challenge.reward}");
        }

        /// <summary>
        /// 添加挑战
        /// </summary>
        public void AddChallenge(RogueLikeChallenge challenge)
        {
            if (!challenges.Contains(challenge))
            {
                challenges.Add(challenge);

                Debug.Log($"Added challenge: {challenge.challengeName}");
            }
        }

        /// <summary>
        /// 获取未完成的挑战
        /// </summary>
        public List<RogueLikeChallenge> GetActiveChallenges()
        {
            List<RogueLikeChallenge> active = new List<RogueLikeChallenge>();

            foreach (var challenge in challenges)
            {
                if (!challenge.isCompleted)
                {
                    active.Add(challenge);
                }
            }

            return active;
        }
    }

    /// <summary>
    /// 随机挑战数据
    /// </summary>
    [System.Serializable]
    public class RogueLikeChallenge
    {
        public string challengeId;
        public string challengeName;
        public string description;
        public ChallengeType challengeType;
        public string reward;
        public bool isCompleted = false;
        public System.Action onCompleted;
    }
}
