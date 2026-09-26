using UnityEngine;

public class PlayerProjectile : MonoBehaviour
{
    const float DirectionEpsilonSqr = 0.0001f;

    [SerializeField] Rigidbody2D _rigidbody;
    [SerializeField] BoxCollider2D _boxCollider;
    [SerializeField] float _travelSpeed = 12f;
    [SerializeField] float _maxLifetimeSeconds = 3f;

    PoolManager _ownerPool;
    Vector2 _direction;
    float _despawnTime;
    bool _isInUse;

    public bool IsInUse => _isInUse;

    void Awake()
    {
        if (_rigidbody == null)
        {
            Debug.LogError($"{nameof(PlayerProjectile)} requires a {nameof(Rigidbody2D)} reference.", this);
        }

        if (_boxCollider == null)
        {
            Debug.LogError($"{nameof(PlayerProjectile)} requires a {nameof(BoxCollider2D)} reference.", this);
        }

        if (_travelSpeed <= 0f)
        {
            Debug.LogError($"{nameof(PlayerProjectile)} travel speed must be greater than zero.", this);
        }

        if (_maxLifetimeSeconds <= 0f)
        {
            Debug.LogError($"{nameof(PlayerProjectile)} max lifetime must be greater than zero.", this);
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

        Vector2 delta = _direction * (_travelSpeed * Time.fixedDeltaTime);
        _rigidbody.MovePosition(_rigidbody.position + delta);
    }

    internal void Initialize(PoolManager ownerPool)
    {
        _ownerPool = ownerPool;
    }

    public bool TryLaunch(Vector2 worldPosition, Vector2 direction)
    {
        if (_ownerPool == null || _rigidbody == null || _boxCollider == null)
        {
            return false;
        }

        if (_travelSpeed <= 0f || _maxLifetimeSeconds <= 0f)
        {
            return false;
        }

        if (direction.sqrMagnitude < DirectionEpsilonSqr)
        {
            return false;
        }

        _direction = direction.normalized;
        _isInUse = true;
        _despawnTime = Time.time + _maxLifetimeSeconds;

        transform.position = new Vector3(worldPosition.x, worldPosition.y, 0f);
        float angleDegrees = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg - 90f;
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

        _ownerPool.ReleasePlayerProjectile(this);
    }

    internal void PrepareForPool()
    {
        _isInUse = false;
        _direction = Vector2.zero;
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
}
