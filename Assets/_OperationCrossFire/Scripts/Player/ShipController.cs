using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] Rigidbody2D _rigidbody;
    [SerializeField] BoxCollider2D _boxCollider;
    [SerializeField] Camera _gameplayCamera;
    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] BoostAbility _boostAbility;

    [SerializeField] float _normalHorizontalSpeed = 5f;
    [SerializeField] float _configuredMinCenterX = -8.6f;
    [SerializeField] float _configuredMaxCenterX = 8.6f;
    [SerializeField] float _viewportEdgeMargin = 0.2f;

    float _fixedWorldY;
    float _cachedColliderHalfWidth;
    int _horizontalInput;
    bool _configurationValid;

    public int HorizontalInput => _horizontalInput;

    void Awake()
    {
        _configurationValid = ValidateConfiguration(logErrors: true);

        if (_configurationValid && _rigidbody != null)
        {
            _fixedWorldY = _rigidbody.position.y;
        }

        CacheColliderHalfWidth();

        if (_configurationValid && !TryGetEffectiveHorizontalLimits(out float minX, out float maxX))
        {
            Debug.LogError(
                $"{nameof(ShipController)} effective horizontal limits are invalid (camera view too narrow or misconfigured).",
                this);
            _configurationValid = false;
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

    public void SetMovement(PlayerId player, bool leftHeld, bool rightHeld)
    {
        if (!CanAcceptMovement(player))
        {
            ClearMovement();
            return;
        }

        if (leftHeld && rightHeld)
        {
            _horizontalInput = 0;
            return;
        }

        if (leftHeld)
        {
            _horizontalInput = -1;
            return;
        }

        if (rightHeld)
        {
            _horizontalInput = 1;
            return;
        }

        _horizontalInput = 0;
    }

    public void ClearMovement()
    {
        _horizontalInput = 0;
    }

    void FixedUpdate()
    {
        if (!_configurationValid || _rigidbody == null)
        {
            return;
        }

        if (_roundManager == null || !_roundManager.IsRunning)
        {
            return;
        }

        if (_horizontalInput == 0)
        {
            return;
        }

        if (!TryGetEffectiveHorizontalLimits(out float minCenterX, out float maxCenterX))
        {
            return;
        }

        float speedMultiplier = _boostAbility.MovementMultiplier;
        float deltaX = _horizontalInput * _normalHorizontalSpeed * speedMultiplier * Time.fixedDeltaTime;

        Vector2 position = _rigidbody.position;
        position.x = Mathf.Clamp(position.x + deltaX, minCenterX, maxCenterX);
        position.y = _fixedWorldY;
        _rigidbody.MovePosition(position);
    }

    bool CanAcceptMovement(PlayerId player)
    {
        if (_roundManager == null || _roleManager == null)
        {
            return false;
        }

        if (!_roundManager.IsRunning)
        {
            return false;
        }

        return _roleManager.IsPilot(player);
    }

    void HandleFluxOrRoundStop()
    {
        ClearMovement();
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        ClearMovement();
    }

    bool ValidateConfiguration(bool logErrors)
    {
        bool valid = true;

        if (_rigidbody == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} requires a {nameof(Rigidbody2D)} reference.", this);
            }

            valid = false;
        }

        if (_boxCollider == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} requires a {nameof(BoxCollider2D)} reference.", this);
            }

            valid = false;
        }

        if (_gameplayCamera == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} requires a gameplay {nameof(Camera)} reference.", this);
            }

            valid = false;
        }
        else if (!_gameplayCamera.orthographic)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} gameplay camera must be orthographic.", this);
            }

            valid = false;
        }

        if (_roundManager == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} requires a {nameof(RoundManager)} reference.", this);
            }

            valid = false;
        }

        if (_roleManager == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} requires a {nameof(RoleManager)} reference.", this);
            }

            valid = false;
        }

        if (_boostAbility == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} requires a {nameof(BoostAbility)} reference.", this);
            }

            valid = false;
        }

        if (_normalHorizontalSpeed <= 0f)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} normal horizontal speed must be greater than zero.", this);
            }

            valid = false;
        }

        if (_configuredMinCenterX >= _configuredMaxCenterX)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} configured centre X limits are inverted or equal.", this);
            }

            valid = false;
        }

        if (_viewportEdgeMargin < 0f)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ShipController)} viewport edge margin cannot be negative.", this);
            }

            valid = false;
        }

        return valid;
    }

    void CacheColliderHalfWidth()
    {
        if (_boxCollider == null)
        {
            _cachedColliderHalfWidth = 0f;
            return;
        }

        Vector2 size = _boxCollider.size;
        Vector3 lossyScale = _boxCollider.transform.lossyScale;
        _cachedColliderHalfWidth = Mathf.Abs(size.x * lossyScale.x) * 0.5f;
    }

    bool TryGetEffectiveHorizontalLimits(out float minCenterX, out float maxCenterX)
    {
        minCenterX = _configuredMinCenterX;
        maxCenterX = _configuredMaxCenterX;

        if (_gameplayCamera == null)
        {
            return false;
        }

        float cameraHalfHeight = _gameplayCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * _gameplayCamera.aspect;
        float cameraCenterX = _gameplayCamera.transform.position.x;

        float inset = _cachedColliderHalfWidth + _viewportEdgeMargin;
        float cameraMinCenterX = cameraCenterX - cameraHalfWidth + inset;
        float cameraMaxCenterX = cameraCenterX + cameraHalfWidth - inset;

        minCenterX = Mathf.Max(_configuredMinCenterX, cameraMinCenterX);
        maxCenterX = Mathf.Min(_configuredMaxCenterX, cameraMaxCenterX);

        return minCenterX <= maxCenterX;
    }
}
