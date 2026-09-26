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

    bool _loadFailed;

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

        while (operation.progress < 0.9f)
        {
            SetFill(Mathf.Clamp01(operation.progress / 0.9f) * 0.9f);
            yield return null;
        }

        SetFill(0.9f);
        operation.allowSceneActivation = true;
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
