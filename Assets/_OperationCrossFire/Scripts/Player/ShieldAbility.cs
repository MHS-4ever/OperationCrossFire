using UnityEngine;

public class ShieldAbility : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] GameObject _shieldVisual;
    [SerializeField] float _durationSeconds = 1.5f;
    [SerializeField] float _cooldownSeconds = 5f;

    float _activeEndTime;
    float _cooldownReadyAtTime;

    public bool IsActive => Time.time < _activeEndTime;

    public float CooldownRemainingSeconds =>
        Time.time >= _cooldownReadyAtTime ? 0f : _cooldownReadyAtTime - Time.time;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(ShieldAbility)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(ShieldAbility)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_shieldVisual == null)
        {
            Debug.LogError($"{nameof(ShieldAbility)} requires the Shield child GameObject.", this);
        }

        if (_durationSeconds <= 0f)
        {
            Debug.LogError($"{nameof(ShieldAbility)} duration must be greater than zero.", this);
        }

        if (_cooldownSeconds < 0f)
        {
            Debug.LogError($"{nameof(ShieldAbility)} cooldown cannot be negative.", this);
        }

        SetShieldVisualActive(false);
    }

    void OnEnable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting += HandleFluxStarting;
        _roundManager.RoundEnded += HandleRoundEnded;
    }

    void OnDisable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting -= HandleFluxStarting;
        _roundManager.RoundEnded -= HandleRoundEnded;
    }

    void Update()
    {
        if (!IsActive)
        {
            SetShieldVisualActive(false);
        }
    }

    public bool TryActivate(PlayerId player)
    {
        if (_roundManager == null || _roleManager == null || _shieldVisual == null)
        {
            return false;
        }

        if (_durationSeconds <= 0f || _cooldownSeconds < 0f)
        {
            return false;
        }

        if (!_roundManager.IsRunning)
        {
            return false;
        }

        if (!_roleManager.IsGunner(player))
        {
            return false;
        }

        if (IsActive)
        {
            return false;
        }

        if (Time.time < _cooldownReadyAtTime)
        {
            return false;
        }

        float now = Time.time;
        _activeEndTime = now + _durationSeconds;
        _cooldownReadyAtTime = now + _cooldownSeconds;
        SetShieldVisualActive(true);
        return true;
    }

    void HandleFluxStarting()
    {
        CancelActiveShield();
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        CancelActiveShield();
    }

    void CancelActiveShield()
    {
        _activeEndTime = 0f;
        SetShieldVisualActive(false);
    }

    void SetShieldVisualActive(bool active)
    {
        if (_shieldVisual != null && _shieldVisual.activeSelf != active)
        {
            _shieldVisual.SetActive(active);
        }
    }
}
