using UnityEngine;

public class TouchControlPanel : MonoBehaviour
{
    [SerializeField] PlayerId _player;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] GameObject _pilotControls;
    [SerializeField] GameObject _gunnerControls;

    void Awake()
    {
        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(TouchControlPanel)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_pilotControls == null)
        {
            Debug.LogError($"{nameof(TouchControlPanel)} requires a PilotControls GameObject.", this);
        }

        if (_gunnerControls == null)
        {
            Debug.LogError($"{nameof(TouchControlPanel)} requires a GunnerControls GameObject.", this);
        }
    }

    void OnEnable()
    {
        if (_roleManager != null)
        {
            _roleManager.RolesChanged += HandleRolesChanged;
        }

        Refresh();
    }

    void OnDisable()
    {
        if (_roleManager != null)
        {
            _roleManager.RolesChanged -= HandleRolesChanged;
        }
    }

    void HandleRolesChanged(PlayerRole player1Role, PlayerRole player2Role)
    {
        Refresh();
    }

    void Refresh()
    {
        if (_roleManager == null)
        {
            return;
        }

        bool isPilot = _roleManager.IsPilot(_player);

        if (_pilotControls != null)
        {
            _pilotControls.SetActive(isPilot);
        }

        if (_gunnerControls != null)
        {
            _gunnerControls.SetActive(!isPilot);
        }
    }
}
