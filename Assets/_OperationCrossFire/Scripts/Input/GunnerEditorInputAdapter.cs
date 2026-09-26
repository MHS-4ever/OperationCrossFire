using UnityEngine;

public class GunnerEditorInputAdapter : MonoBehaviour
{
    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] Camera _gameplayCamera;
    [SerializeField] ReticleController _reticleController;
    [SerializeField] WeaponController _weaponController;

    bool _awaitingMouseReleaseAfterFluxOrEnd;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_gameplayCamera == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(Camera)} reference.", this);
        }

        if (_reticleController == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(ReticleController)} reference.", this);
        }

        if (_weaponController == null)
        {
            Debug.LogError($"{nameof(GunnerEditorInputAdapter)} requires a {nameof(WeaponController)} reference.", this);
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
        if (_roleManager == null || _roundManager == null || _reticleController == null || _weaponController == null || _gameplayCamera == null)
        {
            return;
        }

        if (!_roundManager.IsRunning)
        {
            return;
        }

        if (!TryGetCurrentGunner(out PlayerId gunner))
        {
            _weaponController.ClearFire();
            return;
        }

        if (TryGetMouseWorldOnGameplayPlane(out Vector2 worldPosition))
        {
            _reticleController.SetAimWorldPosition(gunner, worldPosition);
        }

        if (_awaitingMouseReleaseAfterFluxOrEnd)
        {
            if (Input.GetMouseButton(0))
            {
                _weaponController.ClearFire();
                return;
            }

            _awaitingMouseReleaseAfterFluxOrEnd = false;
        }

        if (Input.GetMouseButton(0))
        {
            _weaponController.SetFireHeld(gunner, true);
        }
        else
        {
            _weaponController.SetFireHeld(gunner, false);
        }
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
        _weaponController?.ClearFire();
        _awaitingMouseReleaseAfterFluxOrEnd = true;
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _weaponController?.ClearFire();
        _awaitingMouseReleaseAfterFluxOrEnd = true;
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
