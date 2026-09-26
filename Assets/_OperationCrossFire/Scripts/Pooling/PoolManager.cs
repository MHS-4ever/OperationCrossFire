using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] PlayerProjectile _playerProjectilePrefab;
    [SerializeField] Transform _poolRoot;
    [SerializeField] int _playerProjectilePrewarmCount = 32;
    [SerializeField] FallingThreat _enemyPrefab;
    [SerializeField] FallingThreat _debrisPrefab;
    [SerializeField] FallingThreat _breachPrefab;
    [SerializeField] EnemyProjectile _enemyProjectilePrefab;
    [SerializeField] int _enemyPrewarmCount = 24;
    [SerializeField] int _debrisPrewarmCount = 16;
    [SerializeField] int _breachPrewarmCount = 12;
    [SerializeField] int _enemyProjectilePrewarmCount = 48;

    PlayerProjectile[] _playerProjectiles;
    FallingThreat[] _enemies;
    FallingThreat[] _debris;
    FallingThreat[] _breaches;
    EnemyProjectile[] _enemyProjectiles;

    bool _configurationValid;
    bool _enemyPoolReady;
    bool _debrisPoolReady;
    bool _breachPoolReady;
    bool _enemyProjectilePoolReady;

    bool _loggedPlayerProjectileExhaustion;
    bool _loggedEnemyExhaustion;
    bool _loggedDebrisExhaustion;
    bool _loggedBreachExhaustion;
    bool _loggedEnemyProjectileExhaustion;

    void Awake()
    {
        _configurationValid = ValidateSharedReferences() && PrewarmPlayerProjectiles();
        _enemyPoolReady = PrewarmThreatPool(
            ThreatKind.Enemy,
            _enemyPrefab,
            _enemyPrewarmCount,
            nameof(_enemyPrefab),
            out _enemies);
        _debrisPoolReady = PrewarmThreatPool(
            ThreatKind.Debris,
            _debrisPrefab,
            _debrisPrewarmCount,
            nameof(_debrisPrefab),
            out _debris);
        _breachPoolReady = PrewarmThreatPool(
            ThreatKind.Breach,
            _breachPrefab,
            _breachPrewarmCount,
            nameof(_breachPrefab),
            out _breaches);
        _enemyProjectilePoolReady = PrewarmEnemyProjectiles();
    }

    void OnEnable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.RoundEnded += HandleRoundEnded;
    }

    void OnDisable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.RoundEnded -= HandleRoundEnded;
    }

    public bool TryAcquirePlayerProjectile(out PlayerProjectile projectile)
    {
        projectile = null;

        if (!_configurationValid || _playerProjectiles == null)
        {
            return false;
        }

        for (int i = 0; i < _playerProjectiles.Length; i++)
        {
            PlayerProjectile candidate = _playerProjectiles[i];
            if (candidate != null && !candidate.IsInUse)
            {
                projectile = candidate;
                return true;
            }
        }

        LogExhaustionOnce(ref _loggedPlayerProjectileExhaustion, "player projectile", _playerProjectilePrewarmCount);
        return false;
    }

    public bool TryAcquireThreat(ThreatKind kind, out FallingThreat threat)
    {
        threat = null;

        FallingThreat[] pool = GetThreatPool(kind, out bool ready, out bool logged, out int capacity, out string label);
        if (!ready || pool == null)
        {
            return false;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            FallingThreat candidate = pool[i];
            if (candidate != null && !candidate.IsInUse)
            {
                threat = candidate;
                return true;
            }
        }

        LogExhaustionOnce(ref logged, label, capacity);
        SetThreatExhaustionFlag(kind, logged);
        return false;
    }

    public bool TryAcquireEnemyProjectile(out EnemyProjectile projectile)
    {
        projectile = null;

        if (!_enemyProjectilePoolReady || _enemyProjectiles == null)
        {
            return false;
        }

        for (int i = 0; i < _enemyProjectiles.Length; i++)
        {
            EnemyProjectile candidate = _enemyProjectiles[i];
            if (candidate != null && !candidate.IsInUse)
            {
                projectile = candidate;
                return true;
            }
        }

        LogExhaustionOnce(ref _loggedEnemyProjectileExhaustion, "enemy projectile", _enemyProjectilePrewarmCount);
        return false;
    }

    internal void ReleasePlayerProjectile(PlayerProjectile projectile)
    {
        if (!_configurationValid || projectile == null || _playerProjectiles == null)
        {
            return;
        }

        for (int i = 0; i < _playerProjectiles.Length; i++)
        {
            if (_playerProjectiles[i] == projectile)
            {
                projectile.PrepareForPool();
                return;
            }
        }
    }

    internal void ReleaseThreat(FallingThreat threat)
    {
        if (threat == null)
        {
            return;
        }

        FallingThreat[] pool = GetThreatPool(threat.Kind, out bool ready, out _, out _, out _);
        if (!ready || pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            if (pool[i] == threat)
            {
                threat.PrepareForPool();
                return;
            }
        }
    }

    internal void ReleaseEnemyProjectile(EnemyProjectile projectile)
    {
        if (!_enemyProjectilePoolReady || projectile == null || _enemyProjectiles == null)
        {
            return;
        }

        for (int i = 0; i < _enemyProjectiles.Length; i++)
        {
            if (_enemyProjectiles[i] == projectile)
            {
                projectile.PrepareForPool();
                return;
            }
        }
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        ReturnAllPlayerProjectiles();
        ReturnAllThreats();
        ReturnAllEnemyProjectiles();
    }

    public void ReturnAllPlayerProjectiles()
    {
        ReturnAll(_playerProjectiles);
    }

    public void ReturnAllThreats()
    {
        ReturnAll(_enemies);
        ReturnAll(_debris);
        ReturnAll(_breaches);
    }

    public void ReturnAllEnemyProjectiles()
    {
        ReturnAll(_enemyProjectiles);
    }

    bool ValidateSharedReferences()
    {
        bool valid = true;

        if (_poolRoot == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a PoolRoot transform reference.", this);
            valid = false;
        }

        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a {nameof(RoundManager)} reference.", this);
            valid = false;
        }

        return valid;
    }

    bool PrewarmPlayerProjectiles()
    {
        if (_playerProjectilePrefab == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a {nameof(PlayerProjectile)} prefab reference.", this);
            return false;
        }

        if (_poolRoot == null)
        {
            return false;
        }

        if (_playerProjectilePrewarmCount <= 0)
        {
            Debug.LogError($"{nameof(PoolManager)} player projectile prewarm count must be greater than zero.", this);
            return false;
        }

        _playerProjectiles = new PlayerProjectile[_playerProjectilePrewarmCount];

        for (int i = 0; i < _playerProjectilePrewarmCount; i++)
        {
            PlayerProjectile instance = Instantiate(_playerProjectilePrefab, _poolRoot);
            instance.name = $"{_playerProjectilePrefab.name}_{i}";
            instance.Initialize(this);
            instance.PrepareForPool();
            _playerProjectiles[i] = instance;
        }

        return true;
    }

    bool PrewarmThreatPool(
        ThreatKind expectedKind,
        FallingThreat prefab,
        int prewarmCount,
        string prefabFieldName,
        out FallingThreat[] pool)
    {
        pool = null;

        if (prefab == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a {expectedKind} prefab reference ({prefabFieldName}).", this);
            return false;
        }

        if (_poolRoot == null || _roundManager == null)
        {
            return false;
        }

        if (prewarmCount <= 0)
        {
            Debug.LogError($"{nameof(PoolManager)} {expectedKind} prewarm count must be greater than zero.", this);
            return false;
        }

        if (prefab.Kind != expectedKind)
        {
            Debug.LogError(
                $"{nameof(PoolManager)} {expectedKind} prefab Kind is {prefab.Kind}; it must match the assigned pool.",
                this);
            return false;
        }

        pool = new FallingThreat[prewarmCount];

        for (int i = 0; i < prewarmCount; i++)
        {
            FallingThreat instance = Instantiate(prefab, _poolRoot);
            instance.name = $"{prefab.name}_{i}";
            instance.Initialize(this, _roundManager);
            instance.PrepareForPool();
            pool[i] = instance;
        }

        return true;
    }

    bool PrewarmEnemyProjectiles()
    {
        if (_enemyProjectilePrefab == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires an {nameof(EnemyProjectile)} prefab reference.", this);
            return false;
        }

        if (_poolRoot == null || _roundManager == null)
        {
            return false;
        }

        if (_enemyProjectilePrewarmCount <= 0)
        {
            Debug.LogError($"{nameof(PoolManager)} enemy projectile prewarm count must be greater than zero.", this);
            return false;
        }

        _enemyProjectiles = new EnemyProjectile[_enemyProjectilePrewarmCount];

        for (int i = 0; i < _enemyProjectilePrewarmCount; i++)
        {
            EnemyProjectile instance = Instantiate(_enemyProjectilePrefab, _poolRoot);
            instance.name = $"{_enemyProjectilePrefab.name}_{i}";
            instance.Initialize(this, _roundManager);
            instance.PrepareForPool();
            _enemyProjectiles[i] = instance;
        }

        return true;
    }

    FallingThreat[] GetThreatPool(
        ThreatKind kind,
        out bool ready,
        out bool logged,
        out int capacity,
        out string label)
    {
        switch (kind)
        {
            case ThreatKind.Enemy:
                ready = _enemyPoolReady;
                logged = _loggedEnemyExhaustion;
                capacity = _enemyPrewarmCount;
                label = "enemy";
                return _enemies;
            case ThreatKind.Debris:
                ready = _debrisPoolReady;
                logged = _loggedDebrisExhaustion;
                capacity = _debrisPrewarmCount;
                label = "debris";
                return _debris;
            case ThreatKind.Breach:
                ready = _breachPoolReady;
                logged = _loggedBreachExhaustion;
                capacity = _breachPrewarmCount;
                label = "breach";
                return _breaches;
            default:
                ready = false;
                logged = false;
                capacity = 0;
                label = "threat";
                return null;
        }
    }

    void SetThreatExhaustionFlag(ThreatKind kind, bool logged)
    {
        switch (kind)
        {
            case ThreatKind.Enemy:
                _loggedEnemyExhaustion = logged;
                break;
            case ThreatKind.Debris:
                _loggedDebrisExhaustion = logged;
                break;
            case ThreatKind.Breach:
                _loggedBreachExhaustion = logged;
                break;
        }
    }

    void LogExhaustionOnce(ref bool logged, string label, int capacity)
    {
        if (logged)
        {
            return;
        }

        logged = true;
        Debug.LogWarning($"{nameof(PoolManager)} {label} pool exhausted (capacity {capacity}).", this);
    }

    static void ReturnAll(PlayerProjectile[] pool)
    {
        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            PlayerProjectile projectile = pool[i];
            if (projectile != null && projectile.IsInUse)
            {
                projectile.PrepareForPool();
            }
        }
    }

    static void ReturnAll(FallingThreat[] pool)
    {
        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            FallingThreat threat = pool[i];
            if (threat != null && threat.IsInUse)
            {
                threat.PrepareForPool();
            }
        }
    }

    static void ReturnAll(EnemyProjectile[] pool)
    {
        if (pool == null)
        {
            return;
        }

        for (int i = 0; i < pool.Length; i++)
        {
            EnemyProjectile projectile = pool[i];
            if (projectile != null && projectile.IsInUse)
            {
                projectile.PrepareForPool();
            }
        }
    }
}
