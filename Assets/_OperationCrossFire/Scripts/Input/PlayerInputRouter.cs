using UnityEngine;

public class PlayerInputRouter : MonoBehaviour
{
    const int PlayerCount = 2;
    const int SourceCount = 2;

    [SerializeField] RoundManager _roundManager;
    [SerializeField] RoleManager _roleManager;
    [SerializeField] ShipController _shipController;
    [SerializeField] BoostAbility _boostAbility;
    [SerializeField] ReticleController _reticleController;
    [SerializeField] WeaponController _weaponController;
    [SerializeField] ShieldAbility _shieldAbility;

    readonly bool[,] _leftHeld = new bool[PlayerCount, SourceCount];
    readonly bool[,] _rightHeld = new bool[PlayerCount, SourceCount];
    readonly bool[,] _fireHeld = new bool[PlayerCount, SourceCount];

    int _inputEpoch;

    public int InputEpoch => _inputEpoch;

    void Awake()
    {
        if (_roundManager == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(RoundManager)} reference.", this);
        }

        if (_roleManager == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(RoleManager)} reference.", this);
        }

        if (_shipController == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(ShipController)} reference.", this);
        }

        if (_boostAbility == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(BoostAbility)} reference.", this);
        }

        if (_reticleController == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(ReticleController)} reference.", this);
        }

        if (_weaponController == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(WeaponController)} reference.", this);
        }

        if (_shieldAbility == null)
        {
            Debug.LogError($"{nameof(PlayerInputRouter)} requires a {nameof(ShieldAbility)} reference.", this);
        }
    }

    void OnEnable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting += HandleFluxOrRoundStop;
        _roundManager.RoundEnded += HandleRoundEnded;
    }

    void OnDisable()
    {
        if (_roundManager == null)
        {
            return;
        }

        _roundManager.FluxStarting -= HandleFluxOrRoundStop;
        _roundManager.RoundEnded -= HandleRoundEnded;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            CancelAllInput();
        }
    }

    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            CancelAllInput();
        }
    }

    /// <summary>UI source. Touch HoldControls should use this overload.</summary>
    public void SetPilotLeft(PlayerId player, bool held)
    {
        SetPilotLeft(player, held, InputSource.Ui);
    }

    public void SetPilotLeft(PlayerId player, bool held, InputSource source)
    {
        SetPilotHold(player, isLeft: true, held, source);
    }

    /// <summary>UI source. Touch HoldControls should use this overload.</summary>
    public void SetPilotRight(PlayerId player, bool held)
    {
        SetPilotRight(player, held, InputSource.Ui);
    }

    public void SetPilotRight(PlayerId player, bool held, InputSource source)
    {
        SetPilotHold(player, isLeft: false, held, source);
    }

    public bool TryPilotBoost(PlayerId player)
    {
        if (!CanPilotCommand(player) || _boostAbility == null)
        {
            return false;
        }

        return _boostAbility.TryActivate(player);
    }

    public void SetGunnerAimWorld(PlayerId player, Vector2 worldPosition)
    {
        if (!CanGunnerCommand(player) || _reticleController == null)
        {
            return;
        }

        _reticleController.SetAimWorldPosition(player, worldPosition);
    }

    public void SetGunnerAimNormalized(PlayerId player, Vector2 normalizedPosition)
    {
        if (!CanGunnerCommand(player) || _reticleController == null)
        {
            return;
        }

        _reticleController.SetAimNormalized(player, normalizedPosition);
    }

    public bool AcceptsAimInput(PlayerId player)
    {
        return CanGunnerCommand(player);
    }

    /// <summary>UI source. Touch HoldControls should use this overload.</summary>
    public void SetGunnerFireHeld(PlayerId player, bool held)
    {
        SetGunnerFireHeld(player, held, InputSource.Ui);
    }

    public void SetGunnerFireHeld(PlayerId player, bool held, InputSource source)
    {
        if (_weaponController == null)
        {
            return;
        }

        int playerIndex = PlayerIndex(player);
        int sourceIndex = SourceIndex(source);

        if (!held)
        {
            _fireHeld[playerIndex, sourceIndex] = false;
            ApplyFire(player);
            return;
        }

        if (!CanGunnerCommand(player))
        {
            return;
        }

        _fireHeld[playerIndex, sourceIndex] = true;
        ApplyFire(player);
    }

    public bool TryGunnerShield(PlayerId player)
    {
        if (!CanGunnerCommand(player) || _shieldAbility == null)
        {
            return false;
        }

        return _shieldAbility.TryActivate(player);
    }

    public void CancelAllInput()
    {
        for (int player = 0; player < PlayerCount; player++)
        {
            for (int source = 0; source < SourceCount; source++)
            {
                _leftHeld[player, source] = false;
                _rightHeld[player, source] = false;
                _fireHeld[player, source] = false;
            }
        }

        _inputEpoch++;

        if (_shipController != null)
        {
            _shipController.ClearMovement();
        }

        if (_weaponController != null)
        {
            _weaponController.ClearFire();
        }
    }

    void SetPilotHold(PlayerId player, bool isLeft, bool held, InputSource source)
    {
        int playerIndex = PlayerIndex(player);
        int sourceIndex = SourceIndex(source);

        if (!held)
        {
            if (isLeft)
            {
                _leftHeld[playerIndex, sourceIndex] = false;
            }
            else
            {
                _rightHeld[playerIndex, sourceIndex] = false;
            }

            if (CanPilotCommand(player))
            {
                ApplyMovement(player);
            }

            return;
        }

        if (!CanPilotCommand(player))
        {
            return;
        }

        if (isLeft)
        {
            _leftHeld[playerIndex, sourceIndex] = true;
        }
        else
        {
            _rightHeld[playerIndex, sourceIndex] = true;
        }

        ApplyMovement(player);
    }

    void ApplyMovement(PlayerId player)
    {
        if (_shipController == null)
        {
            return;
        }

        int playerIndex = PlayerIndex(player);
        bool left = _leftHeld[playerIndex, SourceIndex(InputSource.Ui)]
            || _leftHeld[playerIndex, SourceIndex(InputSource.Editor)];
        bool right = _rightHeld[playerIndex, SourceIndex(InputSource.Ui)]
            || _rightHeld[playerIndex, SourceIndex(InputSource.Editor)];
        _shipController.SetMovement(player, left, right);
    }

    void ApplyFire(PlayerId player)
    {
        if (_weaponController == null)
        {
            return;
        }

        if (!CanGunnerCommand(player))
        {
            return;
        }

        int playerIndex = PlayerIndex(player);
        bool fire = _fireHeld[playerIndex, SourceIndex(InputSource.Ui)]
            || _fireHeld[playerIndex, SourceIndex(InputSource.Editor)];
        _weaponController.SetFireHeld(player, fire);
    }

    bool CanPilotCommand(PlayerId player)
    {
        return _roundManager != null
            && _roleManager != null
            && _roundManager.IsRunning
            && _roleManager.IsPilot(player);
    }

    bool CanGunnerCommand(PlayerId player)
    {
        return _roundManager != null
            && _roleManager != null
            && _roundManager.IsRunning
            && _roleManager.IsGunner(player);
    }

    static int PlayerIndex(PlayerId player)
    {
        return player == PlayerId.Player1 ? 0 : 1;
    }

    static int SourceIndex(InputSource source)
    {
        return source == InputSource.Ui ? 0 : 1;
    }

    void HandleFluxOrRoundStop()
    {
        CancelAllInput();
    }

    void HandleRoundEnded(RoundEndReason reason)
    {
        CancelAllInput();
    }
}
