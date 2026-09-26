using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GunnerEditorInputAdapter : MonoBehaviour
{
    [SerializeField] RoleManager _roleManager;
    [SerializeField] RoundManager _roundManager;
    [SerializeField] Camera _gameplayCamera;
    [SerializeField] PlayerInputRouter _router;

    bool _awaitingMouseReleaseAfterFluxOrEnd;
    bool _uiOwnsMousePress;
    bool _worldMousePressActive;

#if UNITY_EDITOR
    PointerEventData _pointerEventData;
    readonly List<RaycastResult> _raycastResults = new List<RaycastResult>(8);
#endif

    void Awake()
    {
        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_gameplayCamera == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(Camera)} reference.", this);
        }

        if (_router == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(PlayerInputRouter)} reference.", this);
        }
    }

    void OnEnable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting += HandleFluxStarting;
        _roundManager.RoundEnded += HandleRoundEnded;
    }

    void OnDisable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting -= HandleFluxStarting;
        _roundManager.RoundEnded -= HandleRoundEnded;
    }

    void Update()
    {
#if UNITY_EDITOR
        PollEditorMouse();
#endif
    }

#if UNITY_EDITOR
    void PollEditorMouse()
    {
        if (_roleManager == null || _roundManager == null || _router == null || _gameplayCamera == null)
        {
            return;
        }

        if (!_roundManager.IsRunning)
        {
            return;
        }

        if (!TryGetCurrentGunner(out PlayerId gunner))
        {
            return;
        }

        TrackPressOwnership();

        if (_awaitingMouseReleaseAfterFluxOrEnd)
        {
            if (Input.GetMouseButton(0))
            {
                return;
            }

            _awaitingMouseReleaseAfterFluxOrEnd = false;
            _uiOwnsMousePress = false;
            _worldMousePressActive = false;
        }

        if (_uiOwnsMousePress)
        {
            _router.SetGunnerFireHeld(gunner, false, InputSource.Editor);
            return;
        }

        if (!_worldMousePressActive && IsInteractiveUiUnderMouse())
        {
            return;
        }

        if (TryGetMouseWorldOnGameplayPlane(out Vector2 worldPosition))
        {
            _router.SetGunnerAimWorld(gunner, worldPosition);
        }

        _router.SetGunnerFireHeld(gunner, _worldMousePressActive && Input.GetMouseButton(0), InputSource.Editor);
    }

    void TrackPressOwnership()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsInteractiveUiUnderMouse())
            {
                _uiOwnsMousePress = true;
                _worldMousePressActive = false;
            }
            else
            {
                _uiOwnsMousePress = false;
                _worldMousePressActive = true;
            }
        }

        if (!Input.GetMouseButton(0))
        {
            _uiOwnsMousePress = false;
            _worldMousePressActive = false;
        }
    }

    bool IsInteractiveUiUnderMouse()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return false;
        }

        if (_pointerEventData == null)
        {
            _pointerEventData = new PointerEventData(eventSystem);
        }
        else
        {
            _pointerEventData.Reset();
        }

        _pointerEventData.position = Input.mousePosition;
        _raycastResults.Clear();
        eventSystem.RaycastAll(_pointerEventData, _raycastResults);

        for (int i = 0; i < _raycastResults.Count; i++)
        {
            GameObject hit = _raycastResults[i].gameObject;
            if (hit == null)
            {
                continue;
            }

            if (hit.GetComponentInParent<HoldControl>() != null
                || hit.GetComponentInParent<AimAreaControl>() != null
                || hit.GetComponentInParent<TouchControlPanel>() != null)
            {
                return true;
            }
        }

        return false;
    }

    bool TryGetMouseWorldOnGameplayPlane(out Vector2 worldPosition)
    {
        worldPosition = Vector2.zero;

        if (_gameplayCamera == null)
        {
            return false;
        }

        float distanceToPlane = Mathf.Abs(_gameplayCamera.transform.position.z);
        Vector3 screen = Input.mousePosition;
        screen.z = distanceToPlane;
        Vector3 world = _gameplayCamera.ScreenToWorldPoint(screen);
        worldPosition = new Vector2(world.x, world.y);
        return true;
    }
#endif

    void HandleFluxStarting()
    {
        _awaitingMouseReleaseAfterFluxOrEnd = true;
        _uiOwnsMousePress = false;
        _worldMousePressActive = false;
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _awaitingMouseReleaseAfterFluxOrEnd = true;
        _uiOwnsMousePress = false;
        _worldMousePressActive = false;
    }

    bool TryGetCurrentGunner(out PlayerId gunner)
    {
        if (_roleManager.IsGunner(PlayerId.Player1))
        {
            gunner = PlayerId.Player1;
            return true;
        }

        if (_roleManager.IsGunner(PlayerId.Player2))
        {
            gunner = PlayerId.Player2;
            return true;
        }

        gunner = default;
        return false;
    }
}
