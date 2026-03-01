using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

namespace Artsystack.ArtsystackGui
{
    /// <summary>
    /// Quản lý màn hình loading
    /// </summary>
    public class LoadingScreenManager : MonoBehaviour
    {
        [Header("Loading Panel")]
        [SerializeField] private GameObject panel_Loading;

        [Header("Loading UI Elements")]
        [SerializeField] private Slider loading_Bar;
        [SerializeField] private TextMeshProUGUI text_Loading;
        [SerializeField] private TextMeshProUGUI text_Progress; // Hiển thị %

        [Header("Settings")]
        [SerializeField] private bool showPercentage = true;
        [SerializeField] private string loadingText = "Loading...";
        [SerializeField] private float minimumLoadTime = 1.0f; // Thời gian tối thiểu để tránh loading quá nhanh

        private static LoadingScreenManager instance;
        private float loadStartTime;
        private bool isLoading = false;

        public static LoadingScreenManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Tìm instance đã có trong scene
                    instance = FindObjectOfType<LoadingScreenManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            // Singleton pattern
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            
            // Ẩn panel loading khi khởi động
            if (panel_Loading != null)
                panel_Loading.SetActive(false);
        }

        /// <summary>
        /// Load scene với màn hình loading
        /// </summary>
        public void LoadScene(string sceneName)
        {
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        /// <summary>
        /// Load scene với màn hình loading (sử dụng scene build index)
        /// </summary>
        public void LoadScene(int sceneBuildIndex)
        {
            StartCoroutine(LoadSceneRoutine(sceneBuildIndex));
        }

        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isLoading = true;
            loadStartTime = Time.time;

            // Hiển thị panel loading
            if (panel_Loading != null)
                panel_Loading.SetActive(true);

            // Reset UI
            UpdateLoadingProgress(0f);
            UpdateLoadingText(loadingText);

            // Bắt đầu load scene
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

            // Cho phép activation ngay lập tức nếu muốn
            // asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                // Cập nhật progress (0.0 - 0.9)
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateLoadingProgress(progress);

                // Chờ scene load xong
                if (asyncLoad.progress >= 0.9f)
                {
                    // Đảm bảo thời gian loading tối thiểu
                    float elapsedTime = Time.time - loadStartTime;
                    if (elapsedTime < minimumLoadTime)
                    {
                        yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
                    }
                    
                    // Hoàn tất loading
                    UpdateLoadingProgress(1f);
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            isLoading = false;
        }

        private IEnumerator LoadSceneRoutine(int sceneBuildIndex)
        {
            isLoading = true;
            loadStartTime = Time.time;

            if (panel_Loading != null)
                panel_Loading.SetActive(true);

            UpdateLoadingProgress(0f);
            UpdateLoadingText(loadingText);

            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneBuildIndex);

            while (!asyncLoad.isDone)
            {
                float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                UpdateLoadingProgress(progress);

                if (asyncLoad.progress >= 0.9f)
                {
                    float elapsedTime = Time.time - loadStartTime;
                    if (elapsedTime < minimumLoadTime)
                    {
                        yield return new WaitForSeconds(minimumLoadTime - elapsedTime);
                    }
                    
                    UpdateLoadingProgress(1f);
                    asyncLoad.allowSceneActivation = true;
                }

                yield return null;
            }

            isLoading = false;
        }

        /// <summary>
        /// Cập nhật thanh loading
        /// </summary>
        private void UpdateLoadingProgress(float progress)
        {
            if (loading_Bar != null)
                loading_Bar.value = progress;

            if (showPercentage && text_Progress != null)
                text_Progress.text = $"{(int)(progress * 100)}%";
        }

        /// <summary>
        /// Cập nhật text loading
        /// </summary>
        private void UpdateLoadingText(string text)
        {
            if (text_Loading != null)
                text_Loading.text = text;
        }

        /// <summary>
        /// Hiển thị màn hình loading (có thể gọi thủ công)
        /// </summary>
        public void ShowLoading(string message = "Loading...")
        {
            if (panel_Loading != null)
            {
                panel_Loading.SetActive(true);
                UpdateLoadingText(message);
                UpdateLoadingProgress(0f);
            }
        }

        /// <summary>
        /// Ẩn màn hình loading
        /// </summary>
        public void HideLoading()
        {
            if (panel_Loading != null)
                panel_Loading.SetActive(false);
        }

        /// <summary>
        /// Cập nhật message loading
        /// </summary>
        public void SetLoadingMessage(string message)
        {
            UpdateLoadingText(message);
        }

        #region Public Properties

        public bool IsLoading => isLoading;

        public GameObject Panel_Loading => panel_Loading;

        #endregion
    }
}
