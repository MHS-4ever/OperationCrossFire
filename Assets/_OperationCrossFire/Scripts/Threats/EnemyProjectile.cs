using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] Rigidbody2D _rigidbody;
    [SerializeField] BoxCollider2D _boxCollider;
    [SerializeField] float _travelSpeed = 5f;
    [SerializeField] float _maxLifetimeSeconds = 4f;

    PoolManager _ownerPool;
    RoundManager _roundManager;
    float _launchSpeed;
    float _despawnTime;
    bool _isInUse;

    public bool IsInUse => _isInUse;

    void Awake()
    {
        if (_rigidbody == null)
        {
            Debug.LogError($"{nameof(EnemyProjectile)} requires a {nameof(Rigidbody2D)} reference.", this);
        }

        if (_boxCollider == null)
        {
            Debug.LogError($"{nameof(EnemyProjectile)} requires a {nameof(BoxCollider2D)} reference.", this);
        }

        if (_travelSpeed <= 0f)
        {
            Debug.LogError($"{nameof(EnemyProjectile)} travel speed must be greater than zero.", this);
        }

        if (_maxLifetimeSeconds <= 0f)
        {
            Debug.LogError($"{nameof(EnemyProjectile)} max lifetime must be greater than zero.", this);
        }
    }

    void FixedUpdate()
    {
        if (!_isInUse || _rigidbody == null)
        {
            return;
        }

        if (Time.time >= _despawnTime)
        {
            ReturnToPool();
            return;
        }

        _rigidbody.MovePosition(_rigidbody.position + Vector2.down * (_launchSpeed * Time.fixedDeltaTime));
    }

    internal void Initialize(PoolManager ownerPool, RoundManager roundManager)
    {
        _ownerPool = ownerPool;
        _roundManager = roundManager;
    }

    public bool TryLaunch(Vector2 worldPosition)
    {
        if (_isInUse || _ownerPool == null || _rigidbody == null || _boxCollider == null)
        {
            return false;
        }

        if (_travelSpeed <= 0f || _maxLifetimeSeconds <= 0f)
        {
            return false;
        }

        _launchSpeed = ResolveLaunchSpeed();
        _isInUse = true;
        _despawnTime = Time.time + _maxLifetimeSeconds;

        transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        float angleDegrees = Mathf.Atan2(-1f, 0f) * Mathf.Rad2Deg - 90f;
        transform.rotation = Quaternion.Euler(0f, 0f, angleDegrees);

        _rigidbody.position = worldPosition;
        _rigidbody.linearVelocity = Vector2.zero;
        _rigidbody.angularVelocity = 0f;
        _boxCollider.enabled = true;
        gameObject.SetActive(true);
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

        _ownerPool.ReleaseEnemyProjectile(this);
    }

    internal void PrepareForPool()
    {
        _isInUse = false;
        _launchSpeed = 0f;
        _despawnTime = 0f;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector2.zero;
            _rigidbody.angularVelocity = 0f;
            _rigidbody.position = Vector2.zero;
        }

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        if (_boxCollider != null)
        {
            _boxCollider.enabled = false;
        }

        gameObject.SetActive(false);
    }

    float ResolveLaunchSpeed()
    {
        if (_roundManager != null && _roundManager.CurrentPhase == RoundPhase.Critical)
        {
            return _travelSpeed * 1.5f;
        }

        return _travelSpeed;
    }
}
