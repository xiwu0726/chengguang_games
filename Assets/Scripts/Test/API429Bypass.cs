using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace TimeStamp.Test
{
    /// <summary>
    /// Unity中API 429错误的自动绕过实现
    /// 支持自动重试、指数退避、请求限流
    /// </summary>
    public class API429BypassUnity : MonoBehaviour
    {
        [Header("配置")]
        [SerializeField] private int maxRetries = 10;
        [SerializeField] private float initialDelay = 1.0f;
        [SerializeField] private float maxDelay = 60.0f;
        [SerializeField] private float backoffFactor = 2.0f;

        [Header("调试")]
        [SerializeField] private bool showDebugLogs = true;

        private int currentAttempt = 0;

        /// <summary>
        /// 发送GET请求，自动处理429错误
        /// </summary>
        public IEnumerator GetRequest(string url, Action<UnityWebRequest> onSuccess, Action<UnityWebRequest> onError = null)
        {
            return MakeRequest(url, UnityWebRequest.Get(url), onSuccess, onError);
        }

        /// <summary>
        /// 发送POST请求，自动处理429错误
        /// </summary>
        public IEnumerator PostRequest(string url, WWWForm formData, Action<UnityWebRequest> onSuccess, Action<UnityWebRequest> onError = null)
        {
            return MakeRequest(url, UnityWebRequest.Post(url, formData), onSuccess, onError);
        }

        /// <summary>
        /// 发送JSON POST请求，自动处理429错误
        /// </summary>
        public IEnumerator PostJsonRequest(string url, string jsonData, Action<UnityWebRequest> onSuccess, Action<UnityWebRequest> onError = null)
        {
            byte[] postData = System.Text.Encoding.UTF8.GetBytes(jsonData);
            UnityWebRequest request = new UnityWebRequest(url, "POST");
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return MakeRequest(url, request, onSuccess, onError);
        }

        /// <summary>
        /// 执行请求，自动处理429错误
        /// </summary>
        private IEnumerator MakeRequest(string url, UnityWebRequest request, Action<UnityWebRequest> onSuccess, Action<UnityWebRequest> onError)
        {
            currentAttempt = 0;

            while (currentAttempt <= maxRetries)
            {
                if (showDebugLogs)
                {
                    Debug.Log($"[API] 发送请求 (尝试 {currentAttempt}/{maxRetries}): {url}");
                }

                yield return request.SendWebRequest();

                // 请求成功
                if (request.result == UnityWebRequest.Result.Success)
                {
                    if (showDebugLogs)
                    {
                        Debug.Log($"[API] 请求成功: {request.responseCode}");
                    }

                    onSuccess?.Invoke(request);
                    yield break;
                }

                // 请求失败
                if (showDebugLogs)
                {
                    Debug.LogWarning($"[API] 请求失败: {request.error}");
                }

                // 处理429错误
                if (request.responseCode == 429)
                {
                    currentAttempt++;

                    if (currentAttempt > maxRetries)
                    {
                        if (showDebugLogs)
                        {
                            Debug.LogError($"[API] 达到最大重试次数，放弃请求");
                        }

                        onError?.Invoke(request);
                        yield break;
                    }

                    // 计算延迟时间（指数退避 + 随机抖动）
                    float delay = CalculateBackoffDelay(currentAttempt - 1);
                    delay = AddJitter(delay);

                    // 检查Retry-After头
                    string retryAfter = request.GetResponseHeader("Retry-After");
                    if (!string.IsNullOrEmpty(retryAfter) && int.TryParse(retryAfter, out int retrySeconds))
                    {
                        delay = Mathf.Max(delay, retrySeconds);
                    }

                    if (showDebugLogs)
                    {
                        Debug.LogWarning($"[API] 429错误，等待 {delay:F2} 秒后重试... (尝试 {currentAttempt}/{maxRetries})");
                    }

                    yield return new WaitForSeconds(delay);
                    continue;
                }

                // 其他错误
                if (showDebugLogs)
                {
                    Debug.LogError($"[API] HTTP {request.responseCode}: {request.error}");
                }

                onError?.Invoke(request);
                yield break;
            }
        }

        /// <summary>
        /// 计算指数退避延迟
        /// </summary>
        private float CalculateBackoffDelay(int attempt)
        {
            float delay = initialDelay * Mathf.Pow(backoffFactor, attempt);
            return Mathf.Min(delay, maxDelay);
        }

        /// <summary>
        /// 添加随机抖动，避免雷鸣群效应
        /// </summary>
        private float AddJitter(float delay)
        {
            return delay * UnityEngine.Random.Range(0.5f, 1.5f);
        }

        /// <summary>
        /// 设置配置参数
        /// </summary>
        public void SetConfig(int maxRetries, float initialDelay, float maxDelay, float backoffFactor)
        {
            this.maxRetries = maxRetries;
            this.initialDelay = initialDelay;
            this.maxDelay = maxDelay;
            this.backoffFactor = backoffFactor;
        }

        /// <summary>
        /// 获取当前尝试次数
        /// </summary>
        public int GetCurrentAttempt()
        {
            return currentAttempt;
        }
    }
}
