using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BoardServer : NetworkBehaviour
{
    [SerializeField] Board board;
    private void Start()
    {
        board = GetComponent<Board>();
    }
    [ServerRpc(RequireOwnership = false)]
    public void TurnSetPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            PlayerCharacter _player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (!_player.alive)
            {
                board.boardClient.AvatarButtonClientRpc(false, GameManager.Instance.ReturnClientRpcParams(clientId));
            }
            else
            {
                if (GameManager.Instance.turn.Value == 1)
                {
                    if (_player.role == RoleType.Werewolf || _player.role == RoleType.Seer)
                    {
                        board.boardClient.UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
                        board.boardClient.AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                    }
                    else
                    {
                        board.boardClient.UpdatePlayerPlayedStatusClientRpc($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
                        board.boardClient.AvatarButtonClientRpc(false, GameManager.Instance.ReturnClientRpcParams(clientId));
                    }
                }
                else
                {
                    board.boardClient.UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
                    board.boardClient.AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConfirmTurnServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            PlayerCharacter _player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (_player.choice != 100)
            {
                board.boardClient.UpdatePlayerPlayedStatusClientRpc($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
            }
            else
            {
                board.boardClient.UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
            }
            board.SetTurnEndClient();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void ToggleServerRpc(ulong choice, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            PlayerCharacter _player = client.PlayerObject.GetComponent<PlayerCharacter>();
            _player.choice = choice;
        }
    }
}
