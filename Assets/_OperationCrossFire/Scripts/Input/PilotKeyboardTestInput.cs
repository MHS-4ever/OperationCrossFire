using UnityEngine;

public class PilotKeyboardTestInput : MonoBehaviour
{
    [SerializeField] RoleManager _roleManager;
    [SerializeField] RoundManager _roundManager;
    [SerializeField] ShipController _shipController;
    [SerializeField] BoostAbility _boostAbility;

    bool _awaitingKeyReleaseAfterFluxOrEnd;

    void Awake()
    {
        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(PilotKeyboardTestInput)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(PilotKeyboardTestInput)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_shipController == null)
        {
            Debug.LogError($"{nameof(PilotKeyboardTestInput)} requires a {nameof(ShipController)} reference.", this);
        }

        if (_boostAbility == null)
        {
            Debug.LogError($"{nameof(PilotKeyboardTestInput)} requires a {nameof(BoostAbility)} reference.", this);
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
        PollEditorKeyboard();
#endif
    }

#if UNITY_EDITOR
    void PollEditorKeyboard()
    {
        if (_roleManager == null || _roundManager == null || _shipController == null || _boostAbility == null)
        {
            return;
        }

        if (!_roundManager.IsRunning)
        {
            return;
        }

        if (_awaitingKeyReleaseAfterFluxOrEnd)
        {
            if (!AreAllPilotKeysReleased())
            {
                return;
            }

            _awaitingKeyReleaseAfterFluxOrEnd = false;
        }

        if (!TryGetCurrentPilot(out PlayerId pilot))
        {
            _shipController.ClearMovement();
            return;
        }

        bool leftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        _shipController.SetMovement(pilot, leftHeld, rightHeld);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _boostAbility.TryActivate(pilot);
        }
    }

    static bool AreAllPilotKeysReleased()
    {
        return !Input.GetKey(KeyCode.A)
            && !Input.GetKey(KeyCode.D)
            && !Input.GetKey(KeyCode.LeftArrow)
            && !Input.GetKey(KeyCode.RightArrow)
            && !Input.GetKey(KeyCode.Space);
    }
#endif

    void HandleFluxStarting()
    {
        _shipController?.ClearMovement();
        _awaitingKeyReleaseAfterFluxOrEnd = true;
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        _shipController?.ClearMovement();
        _awaitingKeyReleaseAfterFluxOrEnd = true;
    }

    bool TryGetCurrentPilot(out PlayerId pilot)
    {
        if (_roleManager.IsPilot(PlayerId.Player1))
        {
            pilot = PlayerId.Player1;
            return true;
        }

        if (_roleManager.IsPilot(PlayerId.Player2))
        {
            pilot = PlayerId.Player2;
            return true;
        }

        pilot = default;
        return false;
    }
}
