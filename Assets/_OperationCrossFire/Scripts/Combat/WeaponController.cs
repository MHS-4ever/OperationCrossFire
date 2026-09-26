using UnityEngine;

public class WeaponController : MonoBehaviour
{
    const float DirectionEpsilonSqr = 0.0001f;

    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] PoolManager _poolManager;
    [SerializeField] Transform _firePoint;
    [SerializeField] Transform _reticle;
    [SerializeField] float _fireCooldownSeconds = 0.25f;

    bool _fireHeld;
    PlayerId _fireHeldPlayer;
    float _nextFireTime;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(WeaponController)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(WeaponController)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_poolManager == null)
        {
            Debug.LogError($"{nameof(WeaponController)} requires a {nameof(PoolManager)} reference.", this);
        }

        if (_firePoint == null)
        {
            Debug.LogError($"{nameof(WeaponController)} requires a FirePoint transform reference.", this);
        }

        if (_reticle == null)
        {
            Debug.LogError($"{nameof(WeaponController)} requires a Reticle transform reference.", this);
        }

        if (_fireCooldownSeconds <= 0f)
        {
            Debug.LogError($"{nameof(WeaponController)} fire cooldown must be greater than zero.", this);
        }
    }

    void OnEnable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting += HandleFluxOrRoundStop;
        _roundManager.RoundEnded += HandleRoundEnded;
    }

    void OnDisable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting -= HandleFluxOrRoundStop;
        _roundManager.RoundEnded -= HandleRoundEnded;
    }

    void Update()
    {
        if (!_fireHeld)
        {
            return;
        }

        if (!IsFireHoldValid())
        {
            ClearFire();
            return;
        }

        if (_fireCooldownSeconds <= 0f)
        {
            return;
        }

        if (Time.time < _nextFireTime)
        {
            return;
        }

        if (TryFireOneShot())
        {
            _nextFireTime = Time.time + _fireCooldownSeconds;
        }
    }

    public void SetFireHeld(PlayerId player, bool held)
    {
        if (!held)
        {
            ClearFire();
            return;
        }

        if (_roundManager == null || _roleManager == null)
        {
            ClearFire();
            return;
        }

        if (!_roundManager.IsRunning || !_roleManager.IsGunner(player))
        {
            ClearFire();
            return;
        }

        _fireHeld = true;
        _fireHeldPlayer = player;
    }

    public void ClearFire()
    {
        _fireHeld = false;
    }

    bool IsFireHoldValid()
    {
        if (_roundManager == null || _roleManager == null)
        {
            return false;
        }

        if (!_roundManager.IsRunning)
        {
            return false;
        }

        return _roleManager.IsGunner(_fireHeldPlayer);
    }

    bool TryFireOneShot()
    {
        if (_poolManager == null || _firePoint == null || _reticle == null)
        {
            return false;
        }

        Vector2 origin = _firePoint.position;
        Vector2 aim = _reticle.position;
        Vector2 direction = aim - origin;

        if (direction.sqrMagnitude < DirectionEpsilonSqr)
        {
            return false;
        }

        if (!_poolManager.TryAcquirePlayerProjectile(out PlayerProjectile projectile))
        {
            return false;
        }

        return projectile.TryLaunch(origin, direction);
    }

    void HandleFluxOrRoundStop()
    {
        ClearFire();
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        ClearFire();
    }
}
