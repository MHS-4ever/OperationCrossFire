using System;
using UnityEngine;

public class ShipHealth : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] ShieldAbility _shieldAbility;
    [SerializeField] int _startingHull = 3;
    [SerializeField] float _invulnerabilitySeconds = 1f;

    int _currentHull;
    float _invulnerableUntil;
    float _lastHullLossFixedTime = -1f;
    bool _reportedHullDepleted;

    public int CurrentHull => _currentHull;
    public int StartingHull => _startingHull;
    public float InvulnerabilityRemainingSeconds =>
        Time.time >= _invulnerableUntil ? 0f : _invulnerableUntil - Time.time;

    public event Action<int> HullChanged;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(ShipHealth)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_shieldAbility == null)
        {
            Debug.LogError($"{nameof(ShipHealth)} requires a {nameof(ShieldAbility)} reference.", this);
        }

        if (_startingHull <= 0)
        {
            Debug.LogError($"{nameof(ShipHealth)} starting hull must be greater than zero.", this);
        }

        if (_invulnerabilitySeconds < 0f)
        {
            Debug.LogError($"{nameof(ShipHealth)} invulnerability cannot be negative.", this);
        }

        _currentHull = Mathf.Max(0, _startingHull);
        HullChanged?.Invoke(_currentHull);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null || _roundManager == null || !_roundManager.IsRunning)
        {
            return;
        }

        FallingThreat threat = other.GetComponentInParent<FallingThreat>();
        if (threat != null)
        {
            HandleThreatContact(threat);
            return;
        }

        EnemyProjectile enemyProjectile = other.GetComponentInParent<EnemyProjectile>();
        if (enemyProjectile != null)
        {
            HandleEnemyProjectileContact(enemyProjectile);
        }
    }

    void HandleThreatContact(FallingThreat threat)
    {
        if (!threat.IsInUse)
        {
            return;
        }

        if (threat.Kind == ThreatKind.Breach)
        {
            return;
        }

        TryApplyUnshieldedHullLoss();

        if (threat.IsInUse)
        {
            threat.ReturnToPool();
        }
    }

    void HandleEnemyProjectileContact(EnemyProjectile projectile)
    {
        if (!projectile.IsInUse)
        {
            return;
        }

        TryApplyUnshieldedHullLoss();

        if (projectile.IsInUse)
        {
            projectile.ReturnToPool();
        }
    }

    void TryApplyUnshieldedHullLoss()
    {
        if (_shieldAbility != null && _shieldAbility.IsActive)
        {
            return;
        }

        if (_currentHull <= 0 || _reportedHullDepleted)
        {
            return;
        }

        if (Time.time < _invulnerableUntil)
        {
            return;
        }

        if (Mathf.Approximately(Time.fixedTime, _lastHullLossFixedTime))
        {
            return;
        }

        _lastHullLossFixedTime = Time.fixedTime;
        _invulnerableUntil = Time.time + Mathf.Max(0f, _invulnerabilitySeconds);
        _currentHull = Mathf.Max(0, _currentHull - 1);
        HullChanged?.Invoke(_currentHull);

        if (_currentHull == 0 && !_reportedHullDepleted)
        {
            _reportedHullDepleted = true;
            _roundManager.ReportHullDepleted();
        }
    }
}
