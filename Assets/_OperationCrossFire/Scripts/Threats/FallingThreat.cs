using UnityEngine;

public enum ThreatKind
{
    Enemy,
    Debris,
    Breach
}

public class FallingThreat : MonoBehaviour
{
    [SerializeField] Rigidbody2D _rigidbody;
    [SerializeField] Collider2D _collider;
    [SerializeField] ThreatKind _kind = ThreatKind.Enemy;
    [SerializeField] int _startingHealth = 1;
    [SerializeField] int _scoreValue = 10;
    [SerializeField] float _enemyFireIntervalSeconds = 2.5f;
    [SerializeField] float _enemyFirstFireDelaySeconds = 1.25f;
    [SerializeField] float _enemyFireOffsetDown = 0.5f;

    PoolManager _ownerPool;
    RoundManager _roundManager;
    float _baseDownwardSpeed;
    float _nextFireTime;
    int _currentHealth;
    bool _isInUse;

    public ThreatKind Kind => _kind;
    public int CurrentHealth => _currentHealth;
    public int ScoreValue => _scoreValue;
    public bool IsInUse => _isInUse;

    void Awake()
    {
        if (_rigidbody == null)
        {
            Debug.LogError($"{nameof(FallingThreat)} requires a {nameof(Rigidbody2D)} reference.", this);
        }

        if (_collider == null)
        {
            Debug.LogError($"{nameof(FallingThreat)} requires a trigger {nameof(Collider2D)} reference.", this);
        }

        if (_startingHealth <= 0)
        {
            Debug.LogError($"{nameof(FallingThreat)} starting health must be greater than zero.", this);
        }

        if (_kind == ThreatKind.Enemy && _enemyFireIntervalSeconds <= 0f)
        {
            Debug.LogError($"{nameof(FallingThreat)} enemy fire interval must be greater than zero.", this);
        }
    }

    void FixedUpdate()
    {
        if (!_isInUse || _rigidbody == null)
        {
            return;
        }

        float speed = CurrentDownwardSpeed();
        _rigidbody.MovePosition(_rigidbody.position + Vector2.down * (speed * Time.fixedDeltaTime));

        if (_kind == ThreatKind.Enemy)
        {
            TryFireIfDue();
        }
    }

    internal void Initialize(PoolManager ownerPool, RoundManager roundManager)
    {
        _ownerPool = ownerPool;
        _roundManager = roundManager;
    }

    public bool TryLaunch(Vector2 worldPosition, float baseDownwardSpeed)
    {
        if (_isInUse || _ownerPool == null || _rigidbody == null || _collider == null)
        {
            return false;
        }

        if (_startingHealth <= 0 || baseDownwardSpeed <= 0f)
        {
            return false;
        }

        _baseDownwardSpeed = baseDownwardSpeed;
        _currentHealth = _startingHealth;
        _isInUse = true;
        _nextFireTime = _kind == ThreatKind.Enemy
            ? Time.time + _enemyFirstFireDelaySeconds
            : 0f;

        transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        transform.rotation = Quaternion.identity;
        _rigidbody.position = worldPosition;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _collider.enabled = true;
        gameObject.SetActive(true);
        return true;
    }

    public bool TakeDamage(int amount)
    {
        if (!_isInUse || amount <= 0)
        {
            return false;
        }

        _currentHealth -= amount;
        if (_currentHealth > 0)
        {
            return false;
        }

        ReturnToPool();
        return true;
    }

    public void ReturnToPool()
    {
        if (!_isInUse)
        {
            return;
        }

        if (_ownerPool == null)
        {
            PrepareForPool();
            return;
        }

        _ownerPool.ReleaseThreat(this);
    }

    internal void PrepareForPool()
    {
        _isInUse = false;
        _baseDownwardSpeed = 0f;
        _nextFireTime = 0f;
        _currentHealth = _startingHealth;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.position = Vector2.zero;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_collider != null)
        {
            _collider.enabled = false;
        }

        gameObject.SetActive(false);
    }

    float CurrentDownwardSpeed()
    {
        if (_roundManager == null)
        {
            return _baseDownwardSpeed;
        }

        RoundPhase phase = _roundManager.CurrentPhase;
        if (phase == RoundPhase.Alert || phase == RoundPhase.Critical)
        {
            return _baseDownwardSpeed * 1.25f;
        }

        return _baseDownwardSpeed;
    }

    void TryFireIfDue()
    {
        if (Time.time < _nextFireTime)
        {
            return;
        }

        _nextFireTime = Time.time + _enemyFireIntervalSeconds;

        if (_ownerPool == null || _rigidbody == null)
        {
            return;
        }

        if (!_ownerPool.TryAcquireEnemyProjectile(out EnemyProjectile projectile))
        {
            return;
        }

        Vector2 firePosition = _rigidbody.position + Vector2.down * _enemyFireOffsetDown;
        if (!projectile.TryLaunch(firePosition))
        {
            return;
        }
    }
}
