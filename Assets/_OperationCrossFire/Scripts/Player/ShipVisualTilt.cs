using UnityEngine;

public class ShipVisualTilt : MonoBehaviour
{
    const float AimEpsilon = 0.01f;

    [SerializeField] Transform _shipVisual;
    [SerializeField] Transform _firePoint;
    [SerializeField] Transform _reticle;
    [SerializeField] RoundManager _roundManager;
    [SerializeField] ShipController _shipController;
    [SerializeField] float _maxAimTiltDegrees = 15f;
    [SerializeField] float _maxMoveBankDegrees = 6f;
    [SerializeField] float _maxCombinedTiltDegrees = 21f;
    [SerializeField] float _responseSpeed = 8f;

    Quaternion _neutralLocalRotation;
    float _currentTiltDegrees;

    void Awake()
    {
        if (_shipVisual == null)
        {
            Debug.LogError($"{nameof(ShipVisualTilt)} requires the ShipVisual child Transform.", this);
        }

        if (_firePoint == null)
        {
            Debug.LogError($"{nameof(ShipVisualTilt)} requires a FirePoint Transform.", this);
        }

        if (_reticle == null)
        {
            Debug.LogError($"{nameof(ShipVisualTilt)} requires the world Reticle Transform.", this);
        }

        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(ShipVisualTilt)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_shipController == null)
        {
            Debug.LogError($"{nameof(ShipVisualTilt)} requires a {nameof(ShipController)} reference.", this);
        }

        if (_responseSpeed < 0f)
        {
            Debug.LogError($"{nameof(ShipVisualTilt)} response speed cannot be negative.", this);
        }

        if (_shipVisual != null)
        {
            _neutralLocalRotation = _shipVisual.localRotation;
        }
    }

    void LateUpdate()
    {
        if (_shipVisual == null)
        {
            return;
        }

        float target = 0f;
        if (_roundManager != null && _roundManager.IsRunning)
        {
            target = Mathf.Clamp(ResolveAimTilt() + ResolveMoveBank(), -_maxCombinedTiltDegrees, _maxCombinedTiltDegrees);
        }

        if (_responseSpeed <= 0f)
        {
            _currentTiltDegrees = target;
        }
        else
        {
            float blend = 1f - Mathf.Exp(-_responseSpeed * Time.deltaTime);
            _currentTiltDegrees = Mathf.Lerp(_currentTiltDegrees, target, blend);
        }

        _shipVisual.localRotation = _neutralLocalRotation * Quaternion.Euler(0f, 0f, _currentTiltDegrees);
    }

    float ResolveAimTilt()
    {
        if (_firePoint == null || _reticle == null)
        {
            return 0f;
        }

        Vector2 toReticle = (Vector2)_reticle.position - (Vector2)_firePoint.position;
        if (toReticle.y <= AimEpsilon)
        {
            return 0f;
        }

        float degreesFromUp = Mathf.Atan2(toReticle.x, toReticle.y) * Mathf.Rad2Deg;
        return Mathf.Clamp(-degreesFromUp, -_maxAimTiltDegrees, _maxAimTiltDegrees);
    }

    float ResolveMoveBank()
    {
        if (_shipController == null)
        {
            return 0f;
        }

        return Mathf.Clamp(-_shipController.HorizontalInput * _maxMoveBankDegrees, -_maxMoveBankDegrees, _maxMoveBankDegrees);
    }
}
