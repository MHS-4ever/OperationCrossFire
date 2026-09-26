using System;
using UnityEngine;

public class RoleManager : MonoBehaviour
{
    public event Action<PlayerRole, PlayerRole> RolesChanged;

    PlayerRole _player1Role = PlayerRole.Pilot;
    PlayerRole _player2Role = PlayerRole.Gunner;

    public PlayerRole GetRole(PlayerId player)
    {
        return player switch
        {
            PlayerId.Player1 => _player1Role,
            PlayerId.Player2 => _player2Role,
            _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
        };
    }

    public bool IsPilot(PlayerId player) => GetRole(player) == PlayerRole.Pilot;

    public bool IsGunner(PlayerId player) => GetRole(player) == PlayerRole.Gunner;

    public void ResetRoles()
    {
        _player1Role = PlayerRole.Pilot;
        _player2Role = PlayerRole.Gunner;
        RolesChanged?.Invoke(_player1Role, _player2Role);
    }

    public void SwapRoles()
    {
        (_player1Role, _player2Role) = (_player2Role, _player1Role);
        RolesChanged?.Invoke(_player1Role, _player2Role);
    }
}
