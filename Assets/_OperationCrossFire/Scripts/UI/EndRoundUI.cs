using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndRoundUI : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] GameObject _endRoundPanel;
    [SerializeField] TextMeshProUGUI _resultText;
    [SerializeField] TextMeshProUGUI _finalScoreText;
    [SerializeField] Button _restartButton;
    [SerializeField] Button _quitButton;

    bool _panelVisible;
    bool _restartRequested;

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

        if (_resultText == null || _finalScoreText == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires ResultText and FinalScoreText references.", this);
        }

        if (_restartButton == null || _quitButton == null)
        {
            Debug.LogError($"{nameof(EndRoundUI)} requires Restart and Quit Button references. Add Unity UI Button components on RestartControl and QuitControl.", this);
        }

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

    void HandleRoundEnded(RoundEndReason reason)
    {
        if (_resultText != null)
        {
            _resultText.text = FormatResult(reason);
        }

        if (_finalScoreText != null && _roundManager != null)
        {
            _finalScoreText.text = $"SCORE {_roundManager.Score:D3}";
        }

        if (_endRoundPanel != null)
        {
            _endRoundPanel.SetActive(true);
        }

        _panelVisible = true;
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

        if (_endRoundPanel != null && _endRoundPanel.activeSelf)
        {
            _endRoundPanel.SetActive(false);
        }
    }
}
