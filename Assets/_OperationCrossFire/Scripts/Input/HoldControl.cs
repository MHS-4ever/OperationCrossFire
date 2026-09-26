using UnityEngine;
using UnityEngine.EventSystems;

public enum HoldControlKind
{
    Left,
    Right,
    Boost,
    Fire,
    Shield
}

public class HoldControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] PlayerInputRouter _router;
    [SerializeField] PlayerId _player;
    [SerializeField] HoldControlKind _kind;

    bool _hasCapture;
    int _capturedPointerId;
    int _capturedEpoch;

    void Awake()
    {
        if (_router == null)
        {
            Debug.LogError($"{nameof(HoldControl)} requires a {nameof(PlayerInputRouter)} reference.", this);
        }
    }

    void OnDisable()
    {
        ReleaseCapture(sendRelease: true);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ReleaseCapture(sendRelease: true);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ReleaseCapture(sendRelease: true);
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_router == null)
        {
            return;
        }

        DiscardStaleCapture();

        if (_hasCapture)
        {
            return;
        }

        _hasCapture = true;
        _capturedPointerId = eventData.pointerId;
        _capturedEpoch = _router.InputEpoch;

        switch (_kind)
        {
            case HoldControlKind.Left:
                _router.SetPilotLeft(_player, true);
                break;
            case HoldControlKind.Right:
                _router.SetPilotRight(_player, true);
                break;
            case HoldControlKind.Fire:
                _router.SetGunnerFireHeld(_player, true);
                break;
            case HoldControlKind.Boost:
                _router.TryPilotBoost(_player);
                break;
            case HoldControlKind.Shield:
                _router.TryGunnerShield(_player);
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        TryCompletePointer(eventData.pointerId);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsCurrentCapture(eventData.pointerId))
        {
            return;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TryCompletePointer(eventData.pointerId);
    }

    void TryCompletePointer(int pointerId)
    {
        if (!_hasCapture)
        {
            return;
        }

        if (!IsEpochCurrent())
        {
            ReleaseCapture(sendRelease: false);
            return;
        }

        if (pointerId != _capturedPointerId)
        {
            return;
        }

        ReleaseCapture(sendRelease: true);
    }

    void DiscardStaleCapture()
    {
        if (_hasCapture && !IsEpochCurrent())
        {
            ReleaseCapture(sendRelease: false);
        }
    }

    bool IsCurrentCapture(int pointerId)
    {
        return _hasCapture && IsEpochCurrent() && pointerId == _capturedPointerId;
    }

    bool IsEpochCurrent()
    {
        return _router != null && _capturedEpoch == _router.InputEpoch;
    }

    void ReleaseCapture(bool sendRelease)
    {
        if (!_hasCapture)
        {
            return;
        }

        bool epochStillValid = _router != null && _capturedEpoch == _router.InputEpoch;
        _hasCapture = false;

        if (!sendRelease || !epochStillValid || _router == null)
        {
            return;
        }

        switch (_kind)
        {
            case HoldControlKind.Left:
                _router.SetPilotLeft(_player, false);
                break;
            case HoldControlKind.Right:
                _router.SetPilotRight(_player, false);
                break;
            case HoldControlKind.Fire:
                _router.SetGunnerFireHeld(_player, false);
                break;
        }
    }
}
