using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class BoardClient : NetworkBehaviour
{
    [SerializeField] Board board;
    [ClientRpc]
    public void AvatarButtonClientRpc(bool canToggle, ClientRpcParams clientRpcParams = default)
    {
        board.SetAvatarButtonsInteractivity(canToggle);
        board.confirmButton.interactable = canToggle ? true : false;
        string[] _tempPlayerPlayed = GameManager.Instance.playerPlayed.Value.ToString().Split('|');
        for (int i = 0; i < _tempPlayerPlayed.Length; i++)
        {
            if (_tempPlayerPlayed[i].Contains($"Eliminated - {i}"))
            {
                board.avatars[i].button.interactable = false;
            }
        }
    }
    [ClientRpc]
    public void SetTurnEndClientRpc(ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine(board.CountDown());
        board.avatars.ForEach(avatar => avatar.SetImageVisibility(false));
    }
    [ClientRpc]
    public void SetAvatarClientRpc(string playerName, ulong id)
    {
        GameObject _tempAvatar = Instantiate(board.avatar, board.parent);
        _tempAvatar.GetComponent<Avatar>().SetAvatar(playerName, id);
        if (!board.avatars.Contains(_tempAvatar.GetComponent<Avatar>()))
            board.avatars.Add(_tempAvatar.GetComponent<Avatar>());
    }
    [ClientRpc]
    public void SetRolesClientRpc(PlayerCharacter player, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
        {
            board.role.text = $"{player._name} - {player.role}";
        }
        if (IsOwner) return;
        board.role.text = $"{player._name} - {player.role}";
    }
    [ClientRpc]
    public void SetTurnOnClientRpc(string turn, ClientRpcParams clientRpcParams = default)
    {
        board.turn.text = turn;
    }
    [ClientRpc]
    public void UpdatePlayerPlayedStatusClientRpc(string oldStatus, string newStatus, ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer) return;
        string _tempString = GameManager.Instance.playerPlayed.Value;
        GameManager.Instance.playerPlayed.Value = _tempString.Replace(oldStatus, newStatus);
    }
}
