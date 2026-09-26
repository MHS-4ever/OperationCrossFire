using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] ShipHealth _shipHealth;
    [SerializeField] BoostAbility _boostAbility;
    [SerializeField] ShieldAbility _shieldAbility;
    [SerializeField] TextMeshProUGUI _hullText;
    [SerializeField] Image _hullPoint1;
    [SerializeField] Image _hullPoint2;
    [SerializeField] Image _hullPoint3;
    [SerializeField] TextMeshProUGUI _scoreText;
    [SerializeField] TextMeshProUGUI _timerText;
    [SerializeField] TextMeshProUGUI _phaseText;
    [SerializeField] Image _player1BoostFill;
    [SerializeField] Image _player1ShieldFill;
    [SerializeField] Image _player2BoostFill;
    [SerializeField] Image _player2ShieldFill;
    [SerializeField] GameObject _fluxBanner;
    [SerializeField] TextMeshProUGUI _fluxText;
    [SerializeField] float _reversalBannerSeconds = 1.25f;

    int _displayedTimerSeconds = int.MinValue;
    int _displayedScore = int.MinValue;
    int _displayedHull = int.MinValue;
    int _displayedStartingHull = int.MinValue;
    RoundPhase _displayedPhase = (RoundPhase)int.MinValue;
    float _hideReversalAt;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_shipHealth == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires a {nameof(ShipHealth)} reference.", this);
        }

        if (_boostAbility == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires a {nameof(BoostAbility)} reference.", this);
        }

        if (_shieldAbility == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires a {nameof(ShieldAbility)} reference.", this);
        }

        if (_hullText == null || _scoreText == null || _timerText == null || _phaseText == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires Hull, Score, Timer, and Phase text references.", this);
        }

        if (_hullPoint1 == null || _hullPoint2 == null || _hullPoint3 == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires HullPoint1–3 Image references.", this);
        }

        if (_player1BoostFill == null || _player1ShieldFill == null
            || _player2BoostFill == null || _player2ShieldFill == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires all four cooldown-fill Image references.", this);
        }

        if (_fluxBanner == null || _fluxText == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires FluxBanner and FluxText references.", this);
        }

        if (_reversalBannerSeconds < 0f)
        {
            Debug.LogError($"{nameof(HUDController)} reversal banner duration cannot be negative.", this);
        }

        HideFluxBanner();
    }

    void OnEnable()
    {
        if (_roundManager != null)
        {
            _roundManager.FluxWarning += HandleFluxWarning;
            _roundManager.FluxStarting += HandleFluxStarting;
            _roundManager.PhaseChanged += HandlePhaseChanged;
            _roundManager.RoundEnded += HandleRoundEnded;
            _roundManager.ScoreChanged += HandleScoreChanged;
        }

        if (_shipHealth != null)
        {
            _shipHealth.HullChanged += HandleHullChanged;
        }

        RefreshStaticDisplays();
        RefreshCooldownFills();
    }

    void OnDisable()
    {
        if (_roundManager != null)
        {
            _roundManager.FluxWarning -= HandleFluxWarning;
            _roundManager.FluxStarting -= HandleFluxStarting;
            _roundManager.PhaseChanged -= HandlePhaseChanged;
            _roundManager.RoundEnded -= HandleRoundEnded;
            _roundManager.ScoreChanged -= HandleScoreChanged;
        }

        if (_shipHealth != null)
        {
            _shipHealth.HullChanged -= HandleHullChanged;
        }
    }

    void Start()
    {
        RefreshStaticDisplays();
        RefreshCooldownFills();
    }

    void Update()
    {
        RefreshTimer();
        RefreshCooldownFills();

        if (_hideReversalAt > 0f && Time.time >= _hideReversalAt)
        {
            _hideReversalAt = 0f;
            HideFluxBanner();
        }
    }

    void RefreshStaticDisplays()
    {
        if (_roundManager != null)
        {
            ApplyScore(_roundManager.Score);
            ApplyPhase(_roundManager.CurrentPhase);
        }

        RefreshTimer();

        if (_shipHealth != null)
        {
            ApplyHull(_shipHealth.CurrentHull, _shipHealth.StartingHull);
        }
    }

    void RefreshTimer()
    {
        if (_timerText == null || _roundManager == null)
        {
            return;
        }

        int seconds = Mathf.Max(0, Mathf.CeilToInt(_roundManager.RemainingTime));
        if (seconds == _displayedTimerSeconds)
        {
            return;
        }

        _displayedTimerSeconds = seconds;
        int minutes = seconds / 60;
        int remainder = seconds % 60;
        _timerText.text = $"{minutes:00}:{remainder:00}";
    }

    void HandleScoreChanged(int score)
    {
        ApplyScore(score);
    }

    void ApplyScore(int score)
    {
        if (_scoreText == null || score == _displayedScore)
        {
            return;
        }

        _displayedScore = score;
        _scoreText.text = $"SCORE {score:D3}";
    }

    void HandleHullChanged(int hull)
    {
        if (_shipHealth == null)
        {
            return;
        }

        ApplyHull(hull, _shipHealth.StartingHull);
    }

    void ApplyHull(int hull, int startingHull)
    {
        if (hull == _displayedHull && startingHull == _displayedStartingHull)
        {
            return;
        }

        _displayedHull = hull;
        _displayedStartingHull = startingHull;

        if (_hullText != null)
        {
            _hullText.text = $"HULL {hull}/{startingHull}";
        }

        SetHullPoint(_hullPoint1, hull >= 1);
        SetHullPoint(_hullPoint2, hull >= 2);
        SetHullPoint(_hullPoint3, hull >= 3);
    }

    static void SetHullPoint(Image point, bool visible)
    {
        if (point == null)
        {
            return;
        }

        if (point.enabled != visible)
        {
            point.enabled = visible;
        }
    }

    void HandlePhaseChanged(RoundPhase phase)
    {
        ApplyPhase(phase);
    }

    void ApplyPhase(RoundPhase phase)
    {
        if (_phaseText == null || phase == _displayedPhase)
        {
            return;
        }

        _displayedPhase = phase;
        _phaseText.text = phase.ToString();
    }

    void RefreshCooldownFills()
    {
        float boostFill = ResolveFill(_boostAbility);
        float shieldFill = ResolveFill(_shieldAbility);
        SetFill(_player1BoostFill, boostFill);
        SetFill(_player2BoostFill, boostFill);
        SetFill(_player1ShieldFill, shieldFill);
        SetFill(_player2ShieldFill, shieldFill);
    }

    static float ResolveFill(BoostAbility ability)
    {
        if (ability == null)
        {
            return 1f;
        }

        return ComputeFill(ability.CooldownRemainingSeconds, ability.CooldownDurationSeconds);
    }

    static float ResolveFill(ShieldAbility ability)
    {
        if (ability == null)
        {
            return 1f;
        }

        return ComputeFill(ability.CooldownRemainingSeconds, ability.CooldownDurationSeconds);
    }

    static float ComputeFill(float remaining, float duration)
    {
        if (duration <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01(1f - (remaining / duration));
    }

    static void SetFill(Image image, float fill)
    {
        if (image == null || Mathf.Approximately(image.fillAmount, fill))
        {
            return;
        }

        image.fillAmount = fill;
    }

    void HandleFluxWarning(int countdown)
    {
        _hideReversalAt = 0f;
        ShowFluxBanner($"QUANTUM FLUX IN {countdown}...");
    }

    void HandleFluxStarting()
    {
        ShowFluxBanner("QUANTUM FLUX — ROLES REVERSED");
        _hideReversalAt = Time.time + Mathf.Max(0f, _reversalBannerSeconds);
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _hideReversalAt = 0f;
        HideFluxBanner();
    }

    void ShowFluxBanner(string message)
    {
        if (_fluxText != null)
        {
            _fluxText.text = message;
        }

        if (_fluxBanner != null && !_fluxBanner.activeSelf)
        {
            _fluxBanner.SetActive(true);
        }
    }

    void HideFluxBanner()
    {
        if (_fluxBanner != null && _fluxBanner.activeSelf)
        {
            _fluxBanner.SetActive(false);
        }
    }
}
