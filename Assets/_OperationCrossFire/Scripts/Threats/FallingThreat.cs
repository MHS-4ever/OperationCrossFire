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
    SpriteRenderer _visual;
    Color _authoredColor = Color.white;
    float _hitFlashUntil;
    float _baseDownwardSpeed;
    float _nextFireTime;
    int _currentHealth;
    bool _isInUse;
    bool _useOffscreenFallback;
    float _offscreenFallbackY;
    float _colliderHalfHeight;

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

        _visual = GetComponentInChildren<SpriteRenderer>(true);
        if (_visual != null)
        {
            _authoredColor = _visual.color;
        }

        CacheColliderHalfHeight();
    }

    void FixedUpdate()
    {
        if (!_isInUse || _rigidbody == null)
        {
            return;
        }

        float speed = CurrentDownwardSpeed();
        Vector2 nextPosition = _rigidbody.position + Vector2.down * (speed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(nextPosition);

        if (_kind == ThreatKind.Enemy)
        {
            TryFireIfDue();
        }

        TryOffscreenFallbackCleanup(nextPosition.y);
    }

    void LateUpdate()
    {
        if (_hitFlashUntil <= 0f || _visual == null)
        {
            return;
        }

        if (Time.time >= _hitFlashUntil)
        {
            RestoreAuthoredColor();
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
        RestoreAuthoredColor();
        gameObject.SetActive(true);
        return true;
    }

    internal void SetOffscreenFallback(float worldY)
    {
        _useOffscreenFallback = true;
        _offscreenFallbackY = worldY;
    }

    internal void ClearOffscreenFallback()
    {
        _useOffscreenFallback = false;
        _offscreenFallbackY = 0f;
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

    public void PlayHitFlash()
    {
        if (!_isInUse || _visual == null)
        {
            return;
        }

        _visual.color = Color.Lerp(_authoredColor, Color.white, 0.65f);
        _hitFlashUntil = Time.time + 0.08f;
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
        _hitFlashUntil = 0f;
        _currentHealth = _startingHealth;
        ClearOffscreenFallback();
        RestoreAuthoredColor();

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

    void CacheColliderHalfHeight()
    {
        if (_collider == null)
        {
            return;
        }

        float scaleY = Mathf.Abs(transform.lossyScale.y);
        if (scaleY <= 0.0001f)
        {
            scaleY = 1f;
        }

        if (_collider is CircleCollider2D circle)
        {
            _colliderHalfHeight = circle.radius * scaleY;
            return;
        }

        if (_collider is BoxCollider2D box)
        {
            _colliderHalfHeight = box.size.y * 0.5f * scaleY;
            return;
        }

        _colliderHalfHeight = _collider.bounds.extents.y;
    }

    void TryOffscreenFallbackCleanup(float worldY)
    {
        if (!_useOffscreenFallback)
        {
            return;
        }

        if (worldY + _colliderHalfHeight >= _offscreenFallbackY)
        {
            return;
        }

        ReturnToPool();
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

    void RestoreAuthoredColor()
    {
        _hitFlashUntil = 0f;
        if (_visual != null)
        {
            _visual.color = _authoredColor;
        }
    }
}
