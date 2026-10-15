#if CG_LEGACY  // 这些子系统是早期生成的脚手架，运行时（Assets/Scripts/Story + Editor）完全不引用它们，
             // 且存在跨命名空间未 using 的历史编译错误；先整体屏蔽保证出包，需要时加 define 再修。
using System.Collections.Generic;
using UnityEngine;

namespace TimeOfFragments.RogueLike.Systems
{
    /// <summary>
    /// 隐藏要素系统 - 管理隐藏剧情和彩蛋
    /// </summary>
    public class RogueLikeHiddenElementSystem : MonoBehaviour
    {
        /// <summary>
        /// 隐藏要素列表
        /// </summary>
        private List<RogueLikeHiddenElement> hiddenElements = new List<RogueLikeHiddenElement>();

        private void Awake()
        {
            // 初始化隐藏要素系统
            Debug.Log("RogueLikeHiddenElementSystem initialized.");
        }

        /// <summary>
        /// 发现隐藏要素
        /// </summary>
        public void DiscoverHiddenElement(string elementId)
        {
            foreach (var element in hiddenElements)
            {
                if (element.elementId == elementId)
                {
                    element.isDiscovered = true;

                    Debug.Log($"Discovered hidden element: {element.elementName}");
                    break;
                }
            }
        }

        /// <summary>
        /// 检查隐藏要素是否已发现
        /// </summary>
        public bool IsHiddenElementDiscovered(string elementId)
        {
            foreach (var element in hiddenElements)
            {
                if (element.elementId == elementId)
                {
                    return element.isDiscovered;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有已发现的隐藏要素
        /// </summary>
        public List<RogueLikeHiddenElement> GetDiscoveredElements()
        {
            List<RogueLikeHiddenElement> discovered = new List<RogueLikeHiddenElement>();

            foreach (var element in hiddenElements)
            {
                if (element.isDiscovered)
                {
                    discovered.Add(element);
                }
            }

            return discovered;
        }

        /// <summary>
        /// 添加隐藏要素
        /// </summary>
        public void AddHiddenElement(RogueLikeHiddenElement element)
        {
            if (!hiddenElements.Contains(element))
            {
                hiddenElements.Add(element);

                Debug.Log($"Added hidden element: {element.elementName}");
            }
        }
    }

    /// <summary>
    /// 隐藏要素数据
    /// </summary>
    [System.Serializable]
    public class RogueLikeHiddenElement
    {
        public string elementId;
        public string elementName;
        public string description;
        public bool isDiscovered = false;
        public List<string> unlockConditions;
    }
}
#endif
