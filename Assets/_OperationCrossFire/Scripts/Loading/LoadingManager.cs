using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    [SerializeField] Image _loaderFill;
    [SerializeField] string _gameSceneName = "Game";
    [SerializeField] float _minimumDisplaySeconds = 3f;

    bool _loadFailed;
    float _displayStartedAt;

    void Awake()
    {
        if (_loaderFill == null)
        {
            Debug.LogError($"{nameof(LoadingManager)} requires the Loader_fill Image.", this);
        }

        if (string.IsNullOrWhiteSpace(_gameSceneName))
        {
            Debug.LogError($"{nameof(LoadingManager)} requires a Game scene name.", this);
        }

        SetFill(0f);
    }

    void Start()
    {
        _displayStartedAt = Time.unscaledTime;
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        SetFill(0f);
        yield return null;

        if (_loadFailed || string.IsNullOrWhiteSpace(_gameSceneName))
        {
            yield break;
        }

        if (!IsConfiguredGameSceneInBuildProfile())
        {
            FailLoad();
            yield break;
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(_gameSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            FailLoad();
            yield break;
        }

        operation.allowSceneActivation = false;

        float minimumSeconds = Mathf.Max(0f, _minimumDisplaySeconds);
        while (!IsReadyToActivate(operation, minimumSeconds))
        {
            SetFill(CurrentDisplayFill(operation, minimumSeconds));
            yield return null;
        }

        SetFill(1f);
        operation.allowSceneActivation = true;
    }

    bool IsReadyToActivate(AsyncOperation operation, float minimumSeconds)
    {
        bool loadReady = operation.progress >= 0.9f;
        bool minimumElapsed = Time.unscaledTime - _displayStartedAt >= minimumSeconds;
        return loadReady && minimumElapsed;
    }

    float CurrentDisplayFill(AsyncOperation operation, float minimumSeconds)
    {
        float loadT = Mathf.Clamp01(operation.progress / 0.9f);
        float timeT = minimumSeconds <= 0f
            ? 1f
            : Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((Time.unscaledTime - _displayStartedAt) / minimumSeconds));
        return Mathf.Min(loadT, timeT) * 0.9f;
    }

    bool IsConfiguredGameSceneInBuildProfile()
    {
        string configured = _gameSceneName.Trim();
        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
            {
                continue;
            }

            if (string.Equals(path, configured, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.Equals(fileName, configured, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    void FailLoad()
    {
        if (_loadFailed)
        {
            return;
        }

        _loadFailed = true;
        Debug.LogError(
            $"{nameof(LoadingManager)} cannot load '{_gameSceneName}'. Put Loading first and Game second in the Build Profile.",
            this);
    }

    void SetFill(float amount)
    {
        if (_loaderFill != null)
        {
            _loaderFill.fillAmount = amount;
        }
    }
}
