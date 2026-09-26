using UnityEngine;
using UnityEngine.EventSystems;

public class AimAreaControl : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    const float ReturnSeconds = 0.15f;

    [SerializeField] PlayerInputRouter _router;
    [SerializeField] RectTransform _rectTransform;
    [SerializeField] RectTransform _aimIcon;
    [SerializeField] PlayerId _player;

    bool _hasCapture;
    int _capturedPointerId;
    int _capturedEpoch;
    Vector2 _iconHome;
    bool _returning;
    float _returnStartTime;
    Vector2 _returnFrom;

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

        if (_aimIcon == null)
        {
            Debug.LogError($"{nameof(AimAreaControl)} requires the AimArea aim-icon RectTransform.", this);
        }
        else
        {
            _iconHome = _aimIcon.anchoredPosition;
        }
    }

    void OnEnable()
    {
        SnapIconHome();
    }

    void OnDisable()
    {
        ClearCapture(immediateIconReset: true);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            ClearCapture(immediateIconReset: true);
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            ClearCapture(immediateIconReset: true);
        }
    }

    void Update()
    {
        if (_hasCapture && !IsCaptureStillValid())
        {
            ClearCapture(immediateIconReset: true);
            return;
        }

        if (!_returning || _aimIcon == null)
        {
            return;
        }

        float t = ReturnSeconds <= 0f ? 1f : (Time.unscaledTime - _returnStartTime) / ReturnSeconds;
        if (t >= 1f)
        {
            SnapIconHome();
            return;
        }

        _aimIcon.anchoredPosition = Vector2.Lerp(_returnFrom, _iconHome, t);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_router == null || _rectTransform == null)
        {
            return;
        }

        DiscardStaleCapture();

        if (_hasCapture || !AcceptsAim())
        {
            return;
        }

        _hasCapture = true;
        _capturedPointerId = eventData.pointerId;
        _capturedEpoch = _router.InputEpoch;
        _returning = false;
        PlaceIcon(eventData);
        SendAim(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!IsCurrentCapture(eventData.pointerId) || !AcceptsAim())
        {
            if (_hasCapture && !IsCaptureStillValid())
            {
                ClearCapture(immediateIconReset: true);
            }
            else
            {
                DiscardStaleCapture();
            }

            return;
        }

        PlaceIcon(eventData);
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
        if (!AcceptsAim() || !TryGetNormalizedPoint(eventData, out Vector2 normalized))
        {
            return;
        }

        _router.SetGunnerAimNormalized(_player, normalized);
    }

    void PlaceIcon(PointerEventData eventData)
    {
        if (_aimIcon == null || _rectTransform == null)
        {
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 areaLocal))
        {
            return;
        }

        ApplyIconPivotInAreaLocal(ClampIconPivotInArea(areaLocal));
    }

    Vector2 ClampIconPivotInArea(Vector2 desiredPivotInArea)
    {
        GetIconExtentsInArea(out Vector2 offsetMin, out Vector2 offsetMax);
        Rect area = _rectTransform.rect;

        float minX = area.xMin - offsetMin.x;
        float maxX = area.xMax - offsetMax.x;
        float minY = area.yMin - offsetMin.y;
        float maxY = area.yMax - offsetMax.y;

        if (minX > maxX)
        {
            minX = maxX = area.center.x - (offsetMin.x + offsetMax.x) * 0.5f;
        }

        if (minY > maxY)
        {
            minY = maxY = area.center.y - (offsetMin.y + offsetMax.y) * 0.5f;
        }

        return new Vector2(
            Mathf.Clamp(desiredPivotInArea.x, minX, maxX),
            Mathf.Clamp(desiredPivotInArea.y, minY, maxY));
    }

    void GetIconExtentsInArea(out Vector2 offsetMin, out Vector2 offsetMax)
    {
        Rect iconRect = _aimIcon.rect;
        Vector2 pivotInArea = AreaLocalFromIconLocal(Vector2.zero);
        Vector2 bottomLeft = AreaLocalFromIconLocal(new Vector2(iconRect.xMin, iconRect.yMin));
        Vector2 bottomRight = AreaLocalFromIconLocal(new Vector2(iconRect.xMax, iconRect.yMin));
        Vector2 topLeft = AreaLocalFromIconLocal(new Vector2(iconRect.xMin, iconRect.yMax));
        Vector2 topRight = AreaLocalFromIconLocal(new Vector2(iconRect.xMax, iconRect.yMax));

        offsetMin = new Vector2(
            Min4(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x) - pivotInArea.x,
            Min4(bottomLeft.y, bottomRight.y, topLeft.y, topRight.y) - pivotInArea.y);
        offsetMax = new Vector2(
            Max4(bottomLeft.x, bottomRight.x, topLeft.x, topRight.x) - pivotInArea.x,
            Max4(bottomLeft.y, bottomRight.y, topLeft.y, topRight.y) - pivotInArea.y);
    }

    Vector2 AreaLocalFromIconLocal(Vector2 iconLocal)
    {
        return _rectTransform.InverseTransformPoint(_aimIcon.TransformPoint(iconLocal));
    }

    void ApplyIconPivotInAreaLocal(Vector2 areaLocal)
    {
        Vector3 world = _rectTransform.TransformPoint(areaLocal);
        RectTransform parent = _aimIcon.parent as RectTransform;
        if (parent == null)
        {
            _aimIcon.position = world;
            return;
        }

        Vector2 parentLocal = parent.InverseTransformPoint(world);
        _aimIcon.anchoredPosition = ParentLocalToAnchoredPosition(_aimIcon, parent, parentLocal);
    }

    static Vector2 ParentLocalToAnchoredPosition(RectTransform icon, RectTransform parent, Vector2 parentLocal)
    {
        Rect parentRect = parent.rect;
        Vector2 anchorMin = parentRect.min + Vector2.Scale(parentRect.size, icon.anchorMin);
        Vector2 anchorMax = parentRect.min + Vector2.Scale(parentRect.size, icon.anchorMax);
        Vector2 anchorReference = (anchorMin + anchorMax) * 0.5f;
        return parentLocal - anchorReference;
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

        if (!IsCaptureStillValid())
        {
            ClearCapture(immediateIconReset: true);
            return;
        }

        if (pointerId != _capturedPointerId)
        {
            return;
        }

        ClearCapture(immediateIconReset: false);
    }

    void DiscardStaleCapture()
    {
        if (_hasCapture && !IsCaptureStillValid())
        {
            ClearCapture(immediateIconReset: true);
        }
    }

    bool IsCurrentCapture(int pointerId)
    {
        return _hasCapture && IsCaptureStillValid() && pointerId == _capturedPointerId;
    }

    bool IsCaptureStillValid()
    {
        return IsEpochCurrent() && AcceptsAim();
    }

    bool AcceptsAim()
    {
        return _router != null && _router.AcceptsAimInput(_player);
    }

    bool IsEpochCurrent()
    {
        return _router != null && _capturedEpoch == _router.InputEpoch;
    }

    void ClearCapture(bool immediateIconReset)
    {
        _hasCapture = false;

        if (immediateIconReset)
        {
            SnapIconHome();
            return;
        }

        BeginIconReturn();
    }

    void BeginIconReturn()
    {
        if (_aimIcon == null)
        {
            return;
        }

        _returning = true;
        _returnStartTime = Time.unscaledTime;
        _returnFrom = _aimIcon.anchoredPosition;
    }

    void SnapIconHome()
    {
        _returning = false;
        if (_aimIcon != null)
        {
            _aimIcon.anchoredPosition = _iconHome;
        }
    }

    static float Min4(float a, float b, float c, float d)
    {
        return Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
    }

    static float Max4(float a, float b, float c, float d)
    {
        return Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));
    }
}
