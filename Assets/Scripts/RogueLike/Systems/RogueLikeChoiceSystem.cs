#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Systems
{
    /// <summary>
    /// 随机选择系统 - 管理随机选择的影响
    /// </summary>
    public class RogueLikeChoiceSystem : MonoBehaviour
    {
        private void Awake()
        {
            // 初始化随机选择系统
            Debug.Log("RogueLikeChoiceSystem initialized.");
        }

        /// <summary>
        /// 应用选择效果
        /// </summary>
        public void ApplyChoiceEffects(List<RogueLikeEffect> effects)
        {
            foreach (var effect in effects)
            {
                ApplyEffect(effect);
            }
        }

        /// <summary>
        /// 应用单个效果
        /// </summary>
        private void ApplyEffect(RogueLikeEffect effect)
        {
            switch (effect.effectType)
            {
                case RogueLikeEffect.EffectType.InsomniaIncrease:
                    InsomniaController.Instance?.AddInsomniaValue(effect.effectValue);
                    break;
                case RogueLikeEffect.EffectType.InsomniaDecrease:
                    InsomniaController.Instance?.RemoveInsomniaValue(effect.effectValue);
                    break;
                case RogueLikeEffect.EffectType.MemoryShardUnlock:
                    MemoryShardSystem.Instance?.CollectMemoryShard(effect.effectValue.ToString());
                    break;
                // 其他效果类型...
            }
        }
    }
}
#endif
