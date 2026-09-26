using UnityEngine;
using UnityEngine.EventSystems;

public class AimAreaControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] PlayerInputRouter _router;
    [SerializeField] RectTransform _rectTransform;
    [SerializeField] PlayerId _player;

    bool _hasCapture;
    int _capturedPointerId;
    int _capturedEpoch;

    void Awake()
    {
        if (_router == null)
        {
            Debug.LogError($"{nameof(AimAreaControl)} requires a {nameof(PlayerInputRouter)} reference.", this);
        }

        if (_rectTransform == null)
        {
            Debug.LogError($"{nameof(AimAreaControl)} requires its own {nameof(RectTransform)} reference.", this);
        }
    }

    void OnDisable()
    {
        ClearCapture();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ClearCapture();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ClearCapture();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_router == null || _rectTransform == null)
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
        SendAim(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsCurrentCapture(eventData.pointerId))
        {
            DiscardStaleCapture();
            return;
        }

        SendAim(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        TryCompletePointer(eventData.pointerId);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        TryCompletePointer(eventData.pointerId);
    }

    void SendAim(PointerEventData eventData)
    {
        if (!TryGetNormalizedPoint(eventData, out Vector2 normalized))
        {
            return;
        }

        _router.SetGunnerAimNormalized(_player, normalized);
    }

    bool TryGetNormalizedPoint(PointerEventData eventData, out Vector2 normalized)
    {
        normalized = Vector2.zero;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = _rectTransform.rect;
        if (rect.width <= 0f || rect.height <= 0f)
        {
            return false;
        }

        float x = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
        float y = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);
        normalized = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
        return true;
    }

    void TryCompletePointer(int pointerId)
    {
        if (!_hasCapture)
        {
            return;
        }

        if (!IsEpochCurrent())
        {
            ClearCapture();
            return;
        }

        if (pointerId != _capturedPointerId)
        {
            return;
        }

        ClearCapture();
    }

    void DiscardStaleCapture()
    {
        if (_hasCapture && !IsEpochCurrent())
        {
            ClearCapture();
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

    void ClearCapture()
    {
        _hasCapture = false;
    }
}
