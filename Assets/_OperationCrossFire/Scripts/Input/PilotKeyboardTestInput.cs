using UnityEngine;

public class PilotKeyboardTestInput : MonoBehaviour
{
    [SerializeField] RoleManager _roleManager;
    [SerializeField] RoundManager _roundManager;
    [SerializeField] PlayerInputRouter _router;

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

        if (_router == null)
        {
            Debug.LogError($"{nameof(PilotKeyboardTestInput)} requires a {nameof(PlayerInputRouter)} reference.", this);
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
        if (_roleManager == null || _roundManager == null || _router == null)
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
            return;
        }

        bool leftHeld = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool rightHeld = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        _router.SetPilotLeft(pilot, leftHeld, InputSource.Editor);
        _router.SetPilotRight(pilot, rightHeld, InputSource.Editor);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _router.TryPilotBoost(pilot);
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
        _awaitingKeyReleaseAfterFluxOrEnd = true;
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
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
