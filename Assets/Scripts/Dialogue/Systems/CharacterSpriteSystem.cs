using UnityEngine;
using UnityEngine.UI;

namespace TimeOfFragments.Dialogue.Systems
{
    /// <summary>
    /// 角色立绘显示系统 - 管理角色立绘的显示、隐藏和动画
    /// </summary>
    public class CharacterSpriteSystem : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image characterSprite;
        [SerializeField] private RectTransform characterRectTransform;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInSpeed = 0.5f;
        [SerializeField] private float fadeOutSpeed = 0.5f;
        [SerializeField] private float slideInSpeed = 0.5f;
        [SerializeField] private float slideOutSpeed = 0.5f;
        [SerializeField] private bool useAnimation = true;

        [Header("Position Settings")]
        [SerializeField] private Vector2 defaultPosition = new Vector2(0, 0);
        [SerializeField] private Vector2 leftPosition = new Vector2(-300f, 0);
        [SerializeField] private Vector2 rightPosition = new Vector2(300f, 0);
        [SerializeField] private Vector2 centerPosition = new Vector2(0, 0);

        [Header("Scale Settings")]
        [SerializeField] private float defaultScale = 1f;
        [SerializeField] private float zoomInScale = 1.1f;
        [SerializeField] private float zoomOutScale = 0.9f;

        private Sprite currentSprite = null;
        private CharacterPosition position = CharacterPosition.Center;
        private float currentScale = 1f;
        private float currentAlpha = 1f;
        private bool isAnimating = false;

        // 事件
        public event System.Action<Sprite, CharacterPosition> OnSpriteChanged;
        public event System.Action OnCharacterHidden;

        private enum CharacterPosition
        {
            Left,
            Center,
            Right
        }

        private void Awake()
        {
            if (characterSprite == null)
                Debug.LogError("CharacterSprite is not assigned!");

            if (characterRectTransform == null)
                characterRectTransform = characterSprite.GetComponent<RectTransform>();

            SetSprite(null);
            SetPosition(CharacterPosition.Center);
            SetScale(defaultScale);
            SetAlpha(0f);
        }

        /// <summary>
        /// 设置角色立绘
        /// </summary>
        public void SetSprite(Sprite sprite)
        {
            if (currentSprite == sprite) return;

            currentSprite = sprite;
            characterSprite.sprite = sprite;

            if (sprite != null)
            {
                // 自动调整比例
                float aspectRatio = (float)sprite.textureRect.width / sprite.textureRect.height;
                characterRectTransform.sizeDelta = new Vector2(400 * aspectRatio, 400);
            }
            else
            {
                characterRectTransform.sizeDelta = new Vector2(0, 0);
            }

            OnSpriteChanged?.Invoke(sprite, position);
        }

        /// <summary>
        /// 设置角色位置
        /// </summary>
        public void SetPosition(CharacterPosition position)
        {
            if (this.position == position) return;

            this.position = position;
            Vector2 targetPosition;

            switch (position)
            {
                case CharacterPosition.Left:
                    targetPosition = leftPosition;
                    break;
                case CharacterPosition.Right:
                    targetPosition = rightPosition;
                    break;
                case CharacterPosition.Center:
                default:
                    targetPosition = centerPosition;
                    break;
            }

            if (useAnimation)
            {
                StartCoroutine(SlideAnimation(targetPosition, slideInSpeed));
            }
            else
            {
                characterRectTransform.anchoredPosition = targetPosition;
            }
        }

        /// <summary>
        /// 设置角色缩放
        /// </summary>
        public void SetScale(float scale)
        {
            if (Mathf.Approximately(currentScale, scale)) return;

            currentScale = scale;
            characterRectTransform.localScale = Vector3.one * scale;
        }

        /// <summary>
        /// 设置透明度
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (Mathf.Approximately(currentAlpha, alpha)) return;

            currentAlpha = Mathf.Clamp01(alpha);
            Color color = characterSprite.color;
            color.a = currentAlpha;
            characterSprite.color = color;
        }

        /// <summary>
        /// 显示角色
        /// </summary>
        public void Show()
        {
            if (currentSprite == null) return;

            if (useAnimation)
            {
                StartCoroutine(FadeAnimation(1f, fadeInSpeed));
            }
            else
            {
                SetAlpha(1f);
            }
        }

        /// <summary>
        /// 隐藏角色
        /// </summary>
        public void Hide()
        {
            if (currentSprite == null) return;

            if (useAnimation)
            {
                StartCoroutine(FadeAnimation(0f, fadeOutSpeed, () => OnCharacterHidden?.Invoke()));
            }
            else
            {
                SetAlpha(0f);
            }
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        public void ZoomIn()
        {
            if (useAnimation)
            {
                StartCoroutine(ScaleAnimation(zoomInScale, 0.3f));
            }
            else
            {
                SetScale(zoomInScale);
            }
        }

        /// <summary>
        /// 缩小动画
        /// </summary>
        public void ZoomOut()
        {
            if (useAnimation)
            {
                StartCoroutine(ScaleAnimation(zoomOutScale, 0.3f));
            }
            else
            {
                SetScale(zoomOutScale);
            }
        }

        /// <summary>
        /// 淡入动画
        /// </summary>
        private System.Collections.IEnumerator FadeAnimation(float targetAlpha, float duration, System.Action onComplete = null)
        {
            isAnimating = true;
            float startAlpha = currentAlpha;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                Color color = characterSprite.color;
                color.a = currentAlpha;
                characterSprite.color = color;
                yield return null;
            }

            currentAlpha = targetAlpha;
            Color finalColor = characterSprite.color;
            finalColor.a = currentAlpha;
            characterSprite.color = finalColor;

            isAnimating = false;
            onComplete?.Invoke();
        }

        /// <summary>
        /// 滑动动画
        /// </summary>
        private System.Collections.IEnumerator SlideAnimation(Vector2 targetPosition, float duration)
        {
            isAnimating = true;
            Vector2 startPosition = characterRectTransform.anchoredPosition;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                characterRectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            characterRectTransform.anchoredPosition = targetPosition;
            isAnimating = false;
        }

        /// <summary>
        /// 缩放动画
        /// </summary>
        private System.Collections.IEnumerator ScaleAnimation(float targetScale, float duration)
        {
            isAnimating = true;
            float startScale = currentScale;
            float elapsedTime = 0f;

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / duration);
                currentScale = Mathf.Lerp(startScale, targetScale, t);
                characterRectTransform.localScale = Vector3.one * currentScale;
                yield return null;
            }

            currentScale = targetScale;
            characterRectTransform.localScale = Vector3.one * currentScale;
            isAnimating = false;
        }

        /// <summary>
        /// 检查是否正在动画
        /// </summary>
        public bool IsAnimating()
        {
            return isAnimating;
        }

        /// <summary>
        /// 获取当前角色立绘
        /// </summary>
        public Sprite GetCurrentSprite()
        {
            return currentSprite;
        }

        /// <summary>
        /// 获取当前位置
        /// </summary>
        public CharacterPosition GetCurrentPosition()
        {
            return position;
        }

        /// <summary>
        /// 获取当前缩放
        /// </summary>
        public float GetCurrentScale()
        {
            return currentScale;
        }

        /// <summary>
        /// 获取当前透明度
        /// </summary>
        public float GetCurrentAlpha()
        {
            return currentAlpha;
        }
    }
}
