using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndRoundUI : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] GameObject _endRoundPanel;
    [SerializeField] CanvasGroup _panelGroup;
    [SerializeField] TextMeshProUGUI _resultText;
    [SerializeField] TextMeshProUGUI _finalScoreText;
    [SerializeField] Button _restartButton;
    [SerializeField] Button _quitButton;
    [SerializeField] float _fadeSeconds = 0.3f;
    [SerializeField] float _defeatFlashSeconds = 0.12f;
    [SerializeField] Image _screenBlocker;

    bool _panelVisible;
    bool _restartRequested;
    bool _fading;
    bool _defeatFlash;
    float _phaseStart;
    float _phaseDuration;
    Image _panelImage;
    Color _panelBaseColor = Color.white;
    Vector3 _resultBaseScale = Vector3.one;
    float _resultPulseUntil;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_endRoundPanel == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires the EndRoundPanel GameObject.", this);
        }

        if (_panelGroup == null && _endRoundPanel != null)
        {
            _panelGroup = _endRoundPanel.GetComponent<CanvasGroup>();
        }

        if (_panelGroup == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires a CanvasGroup on EndRoundPanel. Add one in the Inspector.", this);
        }

        if (_resultText == null || _finalScoreText == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires ResultText and FinalScoreText references.", this);
        }

        if (_restartButton == null || _quitButton == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires Restart and Quit Button references. Add Unity UI Button components on RestartControl and QuitControl.", this);
        }

        if (_endRoundPanel != null)
        {
            _panelImage = _endRoundPanel.GetComponent<Image>();
            if (_panelImage != null)
            {
                _panelBaseColor = _panelImage.color;
            }
        }

        if (_resultText != null)
        {
            _resultBaseScale = _resultText.rectTransform.localScale;
        }

        if (_panelImage != null)
        {
            _panelImage.raycastTarget = true;
        }

        EnsureScreenBlocker();
        HidePanel();
    }

    void OnEnable()
    {
        if (_roundManager != null)
        {
            _roundManager.RoundEnded += HandleRoundEnded;
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.AddListener(HandleRestart);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.AddListener(HandleQuit);
        }
    }

    void OnDisable()
    {
        if (_roundManager != null)
        {
            _roundManager.RoundEnded -= HandleRoundEnded;
        }

        if (_restartButton != null)
        {
            _restartButton.onClick.RemoveListener(HandleRestart);
        }

        if (_quitButton != null)
        {
            _quitButton.onClick.RemoveListener(HandleQuit);
        }
    }

    void Update()
    {
        UpdateResultPulse();

        if (!_fading)
        {
            return;
        }

        float elapsed = Time.unscaledTime - _phaseStart;
        if (_defeatFlash)
        {
            if (elapsed >= _phaseDuration)
            {
                RestorePanelColor();
                BeginFadeIn();
            }

            return;
        }

        float t = _phaseDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / _phaseDuration);
        SetGroupAlpha(t);
        if (t < 1f)
        {
            return;
        }

        _fading = false;
        SetButtonsInteractable(true);
        _panelVisible = true;
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        if (_resultText != null)
        {
            _resultText.text = FormatResult(reason);
            _resultText.rectTransform.localScale = _resultBaseScale;
        }

        if (_finalScoreText != null && _roundManager != null)
        {
            _finalScoreText.text = $"SCORE {_roundManager.Score:D3}";
        }

        if (_endRoundPanel != null)
        {
            _endRoundPanel.SetActive(true);
        }

        if (_panelImage != null)
        {
            _panelImage.raycastTarget = true;
        }

        BeginBlockGameplayRaycasts();
        SetButtonsInteractable(false);
        _panelVisible = false;
        _restartRequested = false;

        if (reason == RoundEndReason.Victory)
        {
            _resultPulseUntil = Time.unscaledTime + 0.18f;
            BeginFadeIn();
            return;
        }

        BeginDefeatFlash();
    }

    void BeginDefeatFlash()
    {
        _defeatFlash = true;
        _fading = true;
        _phaseStart = Time.unscaledTime;
        _phaseDuration = _defeatFlashSeconds;
        SetGroupAlpha(1f);
        if (_panelImage != null)
        {
            _panelImage.color = new Color(0.45f, 0.08f, 0.08f, _panelBaseColor.a);
        }
    }

    void BeginFadeIn()
    {
        _defeatFlash = false;
        _fading = true;
        _phaseStart = Time.unscaledTime;
        _phaseDuration = Mathf.Max(0.01f, _fadeSeconds);
        SetGroupAlpha(0f);
        SetButtonsInteractable(false);
        BeginBlockGameplayRaycasts();
    }

    static string FormatResult(RoundEndReason reason)
    {
        switch (reason)
        {
            case RoundEndReason.Victory:
                return "VICTORY";
            case RoundEndReason.HullDepleted:
                return "DEFEAT — HULL DEPLETED";
            case RoundEndReason.BreachReached:
                return "DEFEAT — BREACH REACHED";
            default:
                return "ROUND ENDED";
        }
    }

    void HandleRestart()
    {
        if (!_panelVisible || _restartRequested)
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.buildIndex < 0)
        {
            Debug.LogError(
                $"{nameof(EndRoundUI)} cannot reload '{scene.name}'. Add the Game scene to the Build Profile scene list.",
                this);
            return;
        }

        _restartRequested = true;
        SceneManager.LoadScene(scene.buildIndex);
    }

    void HandleQuit()
    {
        if (!_panelVisible)
        {
            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void HidePanel()
    {
        _panelVisible = false;
        _restartRequested = false;
        _fading = false;
        _defeatFlash = false;
        RestorePanelColor();
        SetGroupAlpha(0f);
        SetButtonsInteractable(false);

        if (_panelGroup != null)
        {
            _panelGroup.blocksRaycasts = false;
            _panelGroup.interactable = false;
        }

        SetScreenBlockerActive(false);

        if (_resultText != null)
        {
            _resultText.rectTransform.localScale = _resultBaseScale;
        }

        if (_endRoundPanel != null && _endRoundPanel.activeSelf)
        {
            _endRoundPanel.SetActive(false);
        }
    }

    void SetGroupAlpha(float alpha)
    {
        if (_panelGroup != null)
        {
            _panelGroup.alpha = alpha;
        }
    }

    void BeginBlockGameplayRaycasts()
    {
        if (_panelGroup != null)
        {
            _panelGroup.blocksRaycasts = true;
            _panelGroup.interactable = false;
        }

        SetScreenBlockerActive(true);
    }

    void EnsureScreenBlocker()
    {
        if (_screenBlocker != null || _endRoundPanel == null)
        {
            return;
        }

        Transform parent = _endRoundPanel.transform.parent;
        if (parent == null)
        {
            return;
        }

        var blockerObject = new GameObject("EndRoundInputBlocker");
        blockerObject.layer = _endRoundPanel.layer;
        var rect = blockerObject.AddComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetSiblingIndex(_endRoundPanel.transform.GetSiblingIndex());

        _screenBlocker = blockerObject.AddComponent<Image>();
        _screenBlocker.color = new Color(0f, 0f, 0f, 0f);
        _screenBlocker.raycastTarget = true;
    }

    void SetScreenBlockerActive(bool active)
    {
        if (_screenBlocker != null)
        {
            _screenBlocker.raycastTarget = active;
            _screenBlocker.gameObject.SetActive(active);
        }
    }

    void SetButtonsInteractable(bool interactable)
    {
        if (_panelGroup != null)
        {
            _panelGroup.interactable = interactable;
            if (_endRoundPanel != null && _endRoundPanel.activeSelf)
            {
                _panelGroup.blocksRaycasts = true;
            }
        }

        if (_restartButton != null)
        {
            _restartButton.interactable = interactable;
        }

        if (_quitButton != null)
        {
            _quitButton.interactable = interactable;
        }
    }

    void RestorePanelColor()
    {
        if (_panelImage != null)
        {
            _panelImage.color = _panelBaseColor;
        }
    }

    void UpdateResultPulse()
    {
        if (_resultText == null || _resultPulseUntil <= 0f)
        {
            return;
        }

        float remaining = _resultPulseUntil - Time.unscaledTime;
        if (remaining <= 0f)
        {
            _resultPulseUntil = 0f;
            _resultText.rectTransform.localScale = _resultBaseScale;
            return;
        }

        float t = 1f - (remaining / 0.18f);
        float scale = Mathf.Lerp(1f, 1.12f, Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI));
        _resultText.rectTransform.localScale = _resultBaseScale * scale;
    }
}
