using System;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    const float FirstFluxThresholdSeconds = 20f;
    const float SecondFluxThresholdSeconds = 40f;

    const float FirstFluxWarningStartSeconds = 17f;
    const float FirstFluxWarningMidSeconds = 18f;
    const float FirstFluxWarningEndSeconds = 19f;

    const float SecondFluxWarningStartSeconds = 37f;
    const float SecondFluxWarningMidSeconds = 38f;
    const float SecondFluxWarningEndSeconds = 39f;

    [SerializeField] float _roundDurationSeconds = 60f;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] bool _developmentDiagnostics;

    float _elapsedTime;
    RoundPhase _currentPhase = RoundPhase.Patrol;
    bool _isRunning;
    bool _roundEnded;
    RoundEndReason _endReason = RoundEndReason.None;
    int _score;

    bool _firstFluxTransitionApplied;
    bool _secondFluxTransitionApplied;

    bool _firstFluxWarning3Emitted;
    bool _firstFluxWarning2Emitted;
    bool _firstFluxWarning1Emitted;
    bool _secondFluxWarning3Emitted;
    bool _secondFluxWarning2Emitted;
    bool _secondFluxWarning1Emitted;

    public float ElapsedTime => _elapsedTime;
    public float RemainingTime => Mathf.Max(0f, _roundDurationSeconds - _elapsedTime);
    public RoundPhase CurrentPhase => _currentPhase;
    public bool IsRunning => _isRunning;
    public RoundEndReason EndReason => _endReason;
    public int Score => _score;

    public event Action<int> FluxWarning;
    public event Action FluxStarting;
    public event Action<RoundPhase> PhaseChanged;
    public event Action FluxCompleted;
    public event Action<RoundEndReason> RoundEnded;
    public event Action<int> ScoreChanged;

    void Awake()
    {
        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(RoundManager)} requires a {nameof(RoleManager)} reference assigned in the Inspector.", this);
        }

        if (_roundDurationSeconds <= 0f)
        {
            Debug.LogError($"{nameof(RoundManager)} round duration must be greater than zero.", this);
        }
    }

    void Start()
    {
        if (_roleManager == null || _roundDurationSeconds <= 0f)
        {
            return;
        }

        _elapsedTime = 0f;
        _currentPhase = RoundPhase.Patrol;
        _isRunning = true;
        _roundEnded = false;
        _endReason = RoundEndReason.None;
        _score = 0;
        ScoreChanged?.Invoke(_score);

        _firstFluxTransitionApplied = false;
        _secondFluxTransitionApplied = false;

        _firstFluxWarning3Emitted = false;
        _firstFluxWarning2Emitted = false;
        _firstFluxWarning1Emitted = false;
        _secondFluxWarning3Emitted = false;
        _secondFluxWarning2Emitted = false;
        _secondFluxWarning1Emitted = false;

        _roleManager.ResetRoles();
        PhaseChanged?.Invoke(_currentPhase);

        if (_developmentDiagnostics)
        {
            LogDiagnostic($"Round start — phase {_currentPhase}, roles P1={RoleLabel(PlayerId.Player1)}, P2={RoleLabel(PlayerId.Player2)}");
        }
    }

    void Update()
    {
        if (!_isRunning || _roundEnded || _roleManager == null)
        {
            return;
        }

        _elapsedTime += Time.deltaTime;
        ProcessMilestonesInChronologicalOrder();
    }

    public void ReportHullDepleted()
    {
        if (!_isRunning || _roundEnded)
        {
            return;
        }

        EndRound(RoundEndReason.HullDepleted);
    }

    public void ReportBreachReached()
    {
        if (!_isRunning || _roundEnded)
        {
            return;
        }

        EndRound(RoundEndReason.BreachReached);
    }

    public void AddScore(int points)
    {
        if (!_isRunning || _roundEnded || points <= 0)
        {
            return;
        }

        _score += points;
        ScoreChanged?.Invoke(_score);
    }

    void ProcessMilestonesInChronologicalOrder()
    {
        TryEmitWarning(ref _firstFluxWarning3Emitted, FirstFluxWarningStartSeconds, 3);
        if (!CanContinueProcessing())
        {
            return;
        }

        TryEmitWarning(ref _firstFluxWarning2Emitted, FirstFluxWarningMidSeconds, 2);
        if (!CanContinueProcessing())
        {
            return;
        }

        TryEmitWarning(ref _firstFluxWarning1Emitted, FirstFluxWarningEndSeconds, 1);
        if (!CanContinueProcessing())
        {
            return;
        }

        TryApplyFirstFluxTransition();
        if (!CanContinueProcessing())
        {
            return;
        }

        TryEmitWarning(ref _secondFluxWarning3Emitted, SecondFluxWarningStartSeconds, 3);
        if (!CanContinueProcessing())
        {
            return;
        }

        TryEmitWarning(ref _secondFluxWarning2Emitted, SecondFluxWarningMidSeconds, 2);
        if (!CanContinueProcessing())
        {
            return;
        }

        TryEmitWarning(ref _secondFluxWarning1Emitted, SecondFluxWarningEndSeconds, 1);
        if (!CanContinueProcessing())
        {
            return;
        }

        TryApplySecondFluxTransition();
        if (!CanContinueProcessing())
        {
            return;
        }

        if (_elapsedTime >= _roundDurationSeconds)
        {
            EndRound(RoundEndReason.Victory);
        }
    }

    bool CanContinueProcessing() => _isRunning && !_roundEnded;

    void TryEmitWarning(ref bool emitted, float atSeconds, int countdownValue)
    {
        if (emitted || _elapsedTime < atSeconds)
        {
            return;
        }

        emitted = true;
        FluxWarning?.Invoke(countdownValue);

        if (_developmentDiagnostics)
        {
            LogDiagnostic($"FluxWarning {countdownValue} at t={_elapsedTime:F2}s — phase {_currentPhase}");
        }
    }

    void TryApplyFirstFluxTransition()
    {
        if (_firstFluxTransitionApplied || _elapsedTime < FirstFluxThresholdSeconds)
        {
            return;
        }

        _firstFluxTransitionApplied = true;
        PerformFluxTransition(RoundPhase.Alert);
    }

    void TryApplySecondFluxTransition()
    {
        if (_secondFluxTransitionApplied || _elapsedTime < SecondFluxThresholdSeconds)
        {
            return;
        }

        _secondFluxTransitionApplied = true;
        PerformFluxTransition(RoundPhase.Critical);
    }

    void PerformFluxTransition(RoundPhase nextPhase)
    {
        if (_developmentDiagnostics)
        {
            LogDiagnostic($"FluxStarting → target phase {nextPhase} at t={_elapsedTime:F2}s");
        }

        FluxStarting?.Invoke();
        if (!CanContinueProcessing())
        {
            return;
        }

        _roleManager.SwapRoles();
        if (!CanContinueProcessing())
        {
            return;
        }

        _currentPhase = nextPhase;

        if (_developmentDiagnostics)
        {
            LogDiagnostic($"Roles after swap — P1={RoleLabel(PlayerId.Player1)}, P2={RoleLabel(PlayerId.Player2)}");
        }

        PhaseChanged?.Invoke(_currentPhase);
        if (!CanContinueProcessing())
        {
            return;
        }

        FluxCompleted?.Invoke();

        if (_developmentDiagnostics)
        {
            LogDiagnostic($"FluxCompleted — phase {_currentPhase} at t={_elapsedTime:F2}s");
        }
    }

    void EndRound(RoundEndReason reason)
    {
        if (_roundEnded)
        {
            return;
        }

        _roundEnded = true;
        _isRunning = false;
        _endReason = reason;
        _currentPhase = RoundPhase.Ended;

        PhaseChanged?.Invoke(_currentPhase);
        RoundEnded?.Invoke(_endReason);

        if (_developmentDiagnostics)
        {
            LogDiagnostic($"Round ended — {reason} at t={_elapsedTime:F2}s, phase {_currentPhase}");
        }
    }

    string RoleLabel(PlayerId player) => _roleManager.GetRole(player).ToString();

    void LogDiagnostic(string message)
    {
        Debug.Log($"[RoundManager] {message}", this);
    }
}
