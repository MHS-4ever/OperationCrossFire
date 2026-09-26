using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] PlayerProjectile _playerProjectilePrefab;
    [SerializeField] Transform _poolRoot;
    [SerializeField] int _playerProjectilePrewarmCount = 32;

    PlayerProjectile[] _playerProjectiles;
    bool _configurationValid;
    bool _loggedPlayerProjectileExhaustion;

    void Awake()
    {
        _configurationValid = ValidateAndPrewarm();
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

        if (!_loggedPlayerProjectileExhaustion)
        {
            _loggedPlayerProjectileExhaustion = true;
            Debug.LogWarning($"{nameof(PoolManager)} player projectile pool exhausted (capacity {_playerProjectilePrewarmCount}).", this);
        }

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

    void HandleRoundEnded(RoundEndReason reason)
    {
        ReturnAllPlayerProjectiles();
    }

    public void ReturnAllPlayerProjectiles()
    {
        if (_playerProjectiles == null)
        {
            return;
        }

        for (int i = 0; i < _playerProjectiles.Length; i++)
        {
            PlayerProjectile projectile = _playerProjectiles[i];
            if (projectile != null && projectile.IsInUse)
            {
                projectile.PrepareForPool();
            }
        }
    }

    bool ValidateAndPrewarm()
    {
        if (_playerProjectilePrefab == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a {nameof(PlayerProjectile)} prefab reference.", this);
            return false;
        }

        if (_poolRoot == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a PoolRoot transform reference.", this);
            return false;
        }

        if (_playerProjectilePrewarmCount <= 0)
        {
            Debug.LogError($"{nameof(PoolManager)} player projectile prewarm count must be greater than zero.", this);
            return false;
        }

        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(PoolManager)} requires a {nameof(RoundManager)} reference.", this);
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
}
