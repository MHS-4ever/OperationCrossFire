using UnityEngine;

public class ReticleController : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] Camera _gameplayCamera;
    [SerializeField] Transform _firePoint;

    [SerializeField] float _horizontalEdgeMargin = 0.4f;
    [SerializeField] float _verticalEdgeMargin = 0.4f;

    bool _configurationValid;

    void Awake()
    {
        _configurationValid = ValidateConfiguration(logErrors: true);
    }

    public void SetAimWorldPosition(PlayerId player, Vector2 worldPosition)
    {
        if (!_configurationValid)
        {
            return;
        }

        if (!CanAcceptAim(player))
        {
            return;
        }

        if (!TryGetAimBounds(out float minX, out float maxX, out float minY, out float maxY))
        {
            return;
        }

        float clampedX = Mathf.Clamp(worldPosition.x, minX, maxX);
        float clampedY = Mathf.Clamp(worldPosition.y, minY, maxY);
        transform.position = new Vector3(clampedX, clampedY, 0f);
    }

    bool CanAcceptAim(PlayerId player)
    {
        if (_roundManager == null || _roleManager == null)
        {
            return false;
        }

        if (!_roundManager.IsRunning)
        {
            return false;
        }

        return _roleManager.IsGunner(player);
    }

    bool ValidateConfiguration(bool logErrors)
    {
        bool valid = true;

        if (_roundManager == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} requires a {nameof(RoundManager)} reference.", this);
            }

            valid = false;
        }

        if (_roleManager == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} requires a {nameof(RoleManager)} reference.", this);
            }

            valid = false;
        }

        if (_gameplayCamera == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} requires a gameplay {nameof(Camera)} reference.", this);
            }

            valid = false;
        }
        else if (!_gameplayCamera.orthographic)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} gameplay camera must be orthographic.", this);
            }

            valid = false;
        }

        if (_firePoint == null)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} requires a FirePoint transform reference.", this);
            }

            valid = false;
        }

        if (_horizontalEdgeMargin < 0f || _verticalEdgeMargin < 0f)
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} aim margins cannot be negative.", this);
            }

            valid = false;
        }

        if (valid && !TryGetAimBounds(out _, out _, out float minY, out float maxY))
        {
            if (logErrors)
            {
                Debug.LogError($"{nameof(ReticleController)} vertical aim range is invalid (FirePoint vs camera top).", this);
            }

            valid = false;
        }

        return valid;
    }

    bool TryGetAimBounds(out float minX, out float maxX, out float minY, out float maxY)
    {
        minX = maxX = minY = maxY = 0f;

        if (_gameplayCamera == null || _firePoint == null)
        {
            return false;
        }

        float cameraHalfHeight = _gameplayCamera.orthographicSize;
        float cameraHalfWidth = cameraHalfHeight * _gameplayCamera.aspect;
        float cameraCenterX = _gameplayCamera.transform.position.x;
        float cameraCenterY = _gameplayCamera.transform.position.y;

        minX = cameraCenterX - cameraHalfWidth + _horizontalEdgeMargin;
        maxX = cameraCenterX + cameraHalfWidth - _horizontalEdgeMargin;

        minY = _firePoint.position.y + _verticalEdgeMargin;
        maxY = cameraCenterY + cameraHalfHeight - _verticalEdgeMargin;

        return minX <= maxX && minY <= maxY;
    }
}
