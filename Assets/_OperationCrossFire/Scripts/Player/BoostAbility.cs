using UnityEngine;

public class BoostAbility : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] float _durationSeconds = 1f;
    [SerializeField] float _movementMultiplier = 1.75f;
    [SerializeField] float _cooldownSeconds = 4f;

    float _activeEndTime;
    float _cooldownReadyAtTime;

    public bool IsActive => Time.time < _activeEndTime;

    public float MovementMultiplier => IsActive ? _movementMultiplier : 1f;

    /// <summary>Seconds until activation is allowed again. Zero when ready.</summary>
    public float CooldownRemainingSeconds =>
        Time.time >= _cooldownReadyAtTime ? 0f : _cooldownReadyAtTime - Time.time;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(BoostAbility)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(BoostAbility)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_durationSeconds <= 0f)
        {
            Debug.LogError($"{nameof(BoostAbility)} duration must be greater than zero.", this);
        }

        if (_movementMultiplier <= 0f)
        {
            Debug.LogError($"{nameof(BoostAbility)} movement multiplier must be greater than zero.", this);
        }

        if (_cooldownSeconds < 0f)
        {
            Debug.LogError($"{nameof(BoostAbility)} cooldown cannot be negative.", this);
        }
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

    public bool TryActivate(PlayerId player)
    {
        if (_roundManager == null || _roleManager == null)
        {
            return false;
        }

        if (_durationSeconds <= 0f || _movementMultiplier <= 0f || _cooldownSeconds < 0f)
        {
            return false;
        }

        if (!_roundManager.IsRunning)
        {
            return false;
        }

        if (!_roleManager.IsPilot(player))
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
        return true;
    }

    void HandleFluxStarting()
    {
        CancelActiveBoost();
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        CancelActiveBoost();
    }

    void CancelActiveBoost()
    {
        _activeEndTime = 0f;
    }
}
