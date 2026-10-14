using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TimeStamp.Test
{
    /// <summary>
    /// API 429错误绕过示例
    /// 演示如何在Unity中自动处理429错误
    /// </summary>
    public class API429BypassExample : MonoBehaviour
    {
        [Header("API配置")]
        [SerializeField] private string apiUrl = "https://api.example.com/data";
        [SerializeField] private string apiKey = "your_api_key_here";

        [Header("UI元素")]
        [SerializeField] private Button getButton;
        [SerializeField] private Button postButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private TextMeshProUGUI attemptText;

        [Header("API绕过配置")]
        [SerializeField] private int maxRetries = 10;
        [SerializeField] private float initialDelay = 1.0f;
        [SerializeField] private float maxDelay = 60.0f;
        [SerializeField] private float backoffFactor = 2.0f;

        private API429BypassUnity apiBypass;

        private void Start()
        {
            // 初始化API绕过
            apiBypass = GetComponent<API429BypassUnity>();
            if (apiBypass == null)
            {
                apiBypass = gameObject.AddComponent<API429BypassUnity>();
            }

            // 设置配置
            apiBypass.SetConfig(maxRetries, initialDelay, maxDelay, backoffFactor);

            // 绑定按钮事件
            if (getButton != null)
            {
                getButton.onClick.AddListener(OnGetButtonClick);
            }

            if (postButton != null)
            {
                postButton.onClick.AddListener(OnPostButtonClick);
            }

            UpdateUI();
        }

        private void OnGetButtonClick()
        {
            StartCoroutine(ExecuteGetRequest());
        }

        private void OnPostButtonClick()
        {
            StartCoroutine(ExecutePostRequest());
        }

        /// <summary>
        /// 执行GET请求
        /// </summary>
        private IEnumerator ExecuteGetRequest()
        {
            UpdateStatus("正在发送GET请求...");

            using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return apiBypass.GetRequest(apiUrl, request, OnRequestSuccess, OnRequestError);
            }
        }

        /// <summary>
        /// 执行POST请求
        /// </summary>
        private IEnumerator ExecutePostRequest()
        {
            UpdateStatus("正在发送POST请求...");

            // 创建表单数据
            WWWForm formData = new WWWForm();
            formData.AddField("data", "test_data");
            formData.AddField("timestamp", System.DateTime.Now.ToString());

            using (UnityWebRequest request = UnityWebRequest.Post(apiUrl, formData))
            {
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                yield return apiBypass.PostRequest(apiUrl, formData, OnRequestSuccess, OnRequestError);
            }
        }

        /// <summary>
        /// 请求成功回调
        /// </summary>
        private void OnRequestSuccess(UnityWebRequest request)
        {
            string response = request.downloadHandler.text;
            UpdateResult($"成功! 状态码: {request.responseCode}\n{response}");
            UpdateStatus("请求完成");
        }

        /// <summary>
        /// 请求错误回调
        /// </summary>
        private void OnRequestError(UnityWebRequest request)
        {
            UpdateResult($"失败! 错误: {request.error}");
            UpdateStatus("请求失败");
        }

        /// <summary>
        /// 更新状态文本
        /// </summary>
        private void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }

            Debug.Log($"[API示例] 状态: {status}");
        }

        /// <summary>
        /// 更新结果文本
        /// </summary>
        private void UpdateResult(string result)
        {
            if (resultText != null)
            {
                resultText.text = result;
            }

            Debug.Log($"[API示例] 结果: {result}");
        }

        /// <summary>
        /// 更新UI
        /// </summary>
        private void UpdateUI()
        {
            if (attemptText != null)
            {
                attemptText.text = $"最大重试: {maxRetries}\n初始延迟: {initialDelay}秒\n退避因子: {backoffFactor}";
            }
        }

        private void OnDestroy()
        {
            // 清理事件
            if (getButton != null)
            {
                getButton.onClick.RemoveListener(OnGetButtonClick);
            }

            if (postButton != null)
            {
                postButton.onClick.RemoveListener(OnPostButtonClick);
            }
        }
    }
}
