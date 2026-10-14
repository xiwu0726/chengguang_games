using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Systems
{
    /// <summary>
    /// 随机奖励系统 - 管理随机奖励的发放
    /// </summary>
    public class RogueLikeRewardSystem : MonoBehaviour
    {
        /// <summary>
        /// 发放奖励
        /// </summary>
        public void GrantReward(List<RogueLikeEffect> rewards)
        {
            foreach (var reward in rewards)
            {
                GrantReward(reward);
            }
        }

        /// <summary>
        /// 发放单个奖励
        /// </summary>
        private void GrantReward(RogueLikeEffect reward)
        {
            switch (reward.effectType)
            {
                case RogueLikeEffect.EffectType.MemoryShardUnlock:
                    MemoryShardSystem.Instance?.CollectMemoryShard(reward.effectValue.ToString());
                    break;
                case RogueLikeEffect.EffectType.TimeShardUnlock:
                    // 时间碎片解锁逻辑
                    break;
                // 其他奖励类型...
            }
        }

        /// <summary>
        /// 随机生成奖励列表
        /// </summary>
        public List<RogueLikeEffect> GenerateRandomRewards(int count)
        {
            List<RogueLikeEffect> rewards = new List<RogueLikeEffect>();

            for (int i = 0; i < count; i++)
            {
                rewards.Add(GenerateRandomReward());
            }

            return rewards;
        }

        /// <summary>
        /// 生成随机奖励
        /// </summary>
        private RogueLikeEffect GenerateRandomReward()
        {
            return new RogueLikeEffect
            {
                effectType = (RogueLikeEffect.EffectType)Random.Range(0, 10),
                effectValue = Random.Range(1, 100)
            };
        }
    }
}
