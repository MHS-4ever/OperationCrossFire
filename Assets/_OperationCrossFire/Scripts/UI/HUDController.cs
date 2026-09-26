using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] ShipHealth _shipHealth;
    [SerializeField] BoostAbility _boostAbility;
    [SerializeField] ShieldAbility _shieldAbility;
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
    RoundPhase _displayedPhase = (RoundPhase)int.MinValue;
    float _hideReversalAt;
    RectTransform _fluxBannerRect;
    Image _fluxBannerImage;
    Vector3 _fluxBannerBaseScale = Vector3.one;
    Color _fluxBannerBaseColor = Color.white;
    float _bannerPulseUntil;
    float _bannerFlashUntil;

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

        if (_scoreText == null || _timerText == null || _phaseText == null)
        {
            Debug.LogError($"{nameof(HUDController)} requires Score, Timer, and Phase text references.", this);
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

        CacheFluxBannerVisuals();
        HideFluxBanner();
    }

    void CacheFluxBannerVisuals()
    {
        if (_fluxBanner == null)
        {
            return;
        }

        _fluxBannerRect = _fluxBanner.transform as RectTransform;
        if (_fluxBannerRect != null)
        {
            _fluxBannerBaseScale = _fluxBannerRect.localScale;
        }

        _fluxBannerImage = _fluxBanner.GetComponent<Image>();
        if (_fluxBannerImage != null)
        {
            _fluxBannerBaseColor = _fluxBannerImage.color;
        }
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

        UpdateFluxBannerMotion();
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
            ApplyHull(_shipHealth.CurrentHull);
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
        ApplyHull(hull);
    }

    void ApplyHull(int hull)
    {
        if (hull == _displayedHull)
        {
            return;
        }

        _displayedHull = hull;

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
        RestoreFluxBannerColor();
        ShowFluxBanner($"QUANTUM FLUX IN {countdown}...");
        _bannerPulseUntil = Time.time + 0.18f;
    }

    void HandleFluxStarting()
    {
        ShowFluxBanner("QUANTUM FLUX — ROLES REVERSED");
        _hideReversalAt = Time.time + Mathf.Max(0f, _reversalBannerSeconds);
        _bannerPulseUntil = Time.time + 0.18f;
        if (_fluxBannerImage != null)
        {
            _fluxBannerImage.color = new Color(0.35f, 0.85f, 1f, _fluxBannerBaseColor.a);
            _bannerFlashUntil = Time.time + 0.18f;
        }
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
        _bannerPulseUntil = 0f;
        _bannerFlashUntil = 0f;
        RestoreFluxBannerScale();
        RestoreFluxBannerColor();

        if (_fluxBanner != null && _fluxBanner.activeSelf)
        {
            _fluxBanner.SetActive(false);
        }
    }

    void UpdateFluxBannerMotion()
    {
        if (_fluxBanner == null || !_fluxBanner.activeSelf)
        {
            return;
        }

        if (_bannerPulseUntil > 0f && _fluxBannerRect != null)
        {
            float remaining = _bannerPulseUntil - Time.time;
            if (remaining <= 0f)
            {
                _bannerPulseUntil = 0f;
                RestoreFluxBannerScale();
            }
            else
            {
                float t = 1f - (remaining / 0.18f);
                float scale = 1f + (0.08f * Mathf.Sin(t * Mathf.PI));
                _fluxBannerRect.localScale = _fluxBannerBaseScale * scale;
            }
        }

        if (_bannerFlashUntil > 0f && Time.time >= _bannerFlashUntil)
        {
            _bannerFlashUntil = 0f;
            RestoreFluxBannerColor();
        }
        else if (_bannerFlashUntil > 0f && _fluxBannerImage != null)
        {
            float t = 1f - ((_bannerFlashUntil - Time.time) / 0.18f);
            Color cyan = new Color(0.35f, 0.85f, 1f, _fluxBannerBaseColor.a);
            Color red = new Color(1f, 0.28f, 0.28f, _fluxBannerBaseColor.a);
            _fluxBannerImage.color = Color.Lerp(cyan, red, Mathf.Clamp01(t));
        }
    }

    void RestoreFluxBannerScale()
    {
        if (_fluxBannerRect != null)
        {
            _fluxBannerRect.localScale = _fluxBannerBaseScale;
        }
    }

    void RestoreFluxBannerColor()
    {
        if (_fluxBannerImage != null)
        {
            _fluxBannerImage.color = _fluxBannerBaseColor;
        }
    }
}
