using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Unity.Services.Lobbies.Models;

public class Board : NetworkBehaviour
{
    public List<Avatar> avatars;
    [SerializeField] Transform parent;
    [SerializeField] GameObject avatar;
    [SerializeField] GameObject countDownScreen;
    [SerializeField] Button confirmButton;
    [SerializeField] TextMeshProUGUI CountDownText, role, turn;
    IEnumerator Start()
    {
        LobbyManager lobby = FindObjectOfType<LobbyManager>();
        if (IsServer)
        {
            while (NetworkManager.Singleton.ConnectedClients.Count != lobby.joinnedLobby.Players.Count)
                yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
            {
                SetAvatarClientRpc(GameManager.Instance.playerList[i]._name, GameManager.Instance.playerList[i]._id);
            }
        }
        StartCoroutine("CountDown");
    }

    public void GameStarted()
    {
        if (GameManager.Instance.gameStarted.Value)
            return;
        if (IsServer)
        {
            GameManager.Instance.AddRoles();
            GameManager.Instance.turn.Value = "Night";
            GameManager.Instance.playerList.ForEach(item =>
            {
                SetRolesClientRpc(item, GameManager.Instance.ReturnClientRpcParams(item._id));
            });

            for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
                GameManager.Instance.playerPlayed.Value += $"NotPlayed - {i}|";
            GameManager.Instance.gameStarted.Value = true;
        }
        Enumerable.Range(0, avatars.Count)
            .Where(index => NetworkManager.Singleton.LocalClientId != (ulong)index)
            .ToList()
            .ForEach(index => avatars[index].button.onClick.AddListener(() => ToggleButton(index)));
        avatars[(int)NetworkManager.Singleton.LocalClientId].button.interactable = false;
        TurnSetPlayerServerRpc();
    }
    public void Confirm()
    {
        ConfirmTurnServerRpc();
    }
    public void SetTurnEndClient()
    {
        if (GameManager.Instance.AllIsPlayed())
        {
            SetTurnEndClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void TurnSetPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;

        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            PlayerCharacter _player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (GameManager.Instance.turn.Value.ToString().Equals("Night"))
            {
                if (_player.role != RoleType.Villager)
                {
                    AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                    UpdatePlayerPlayedStatus($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
                }
                else
                {
                    AvatarButtonClientRpc(false, GameManager.Instance.ReturnClientRpcParams(clientId));
                    UpdatePlayerPlayedStatus($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
                }
            }
            else
            {
                AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                UpdatePlayerPlayedStatus($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
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
                UpdatePlayerPlayedStatus($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
            }
            else
            {
                UpdatePlayerPlayedStatus($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
            }
            SetTurnEndClient();
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

    [ClientRpc]
    public void AvatarButtonClientRpc(bool canToggle, ClientRpcParams clientRpcParams)
    {
        if (IsServer)
            SetAvatarButtonsInteractivity(canToggle);
        else
        {
            if (IsOwner) return;
            SetAvatarButtonsInteractivity(canToggle);
        }
    }
    [ClientRpc]
    public void SetTurnEndClientRpc(ClientRpcParams clientRpcParams = default)
    {
        StartCoroutine("CountDown");
        avatars.ForEach(avatar => avatar.SetImageVisibility(false));
    }
    [ClientRpc]
    public void SetAvatarClientRpc(string playerName, ulong id)
    {
        GameObject _tempAvatar = Instantiate(avatar, parent);
        _tempAvatar.GetComponent<Avatar>().SetAvatar(playerName, id);
        if (!avatars.Contains(_tempAvatar.GetComponent<Avatar>()))
            avatars.Add(_tempAvatar.GetComponent<Avatar>());
    }
    [ClientRpc]
    public void SetRolesClientRpc(PlayerCharacter player, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
        {
            role.text = player.role.ToString();
        }
        if (IsOwner) return;
        role.text = player.role.ToString();
    }

    private void UpdatePlayerPlayedStatus(string oldStatus, string newStatus)
    {
        string _tempString = GameManager.Instance.playerPlayed.Value;
        GameManager.Instance.playerPlayed.Value = _tempString.Replace(oldStatus, newStatus);
    }
    IEnumerator CountDown()
    {
        int countDown = 5;
        countDownScreen.SetActive(true);
        do
        {
            CountDownText.text = countDown.ToString();
            yield return new WaitForSeconds(1f);
            countDown--;
        } while (countDown > 0);
        GameStarted();
        countDownScreen.SetActive(false);
    }
    private void ToggleButton(int indexClicked)
    {
        for (int i = 0; i < avatars.Count; i++)
        {
            if (i != indexClicked)
            {
                avatars[i].SetImageVisibility(false);
            }
        }
        avatars[indexClicked].OnButtonClick();
        if (avatars[indexClicked].isVisible)
            ToggleServerRpc((ulong)indexClicked);
        else
            ToggleServerRpc(100);
    }
    private void SetAvatarButtonsInteractivity(bool canToggle)
    {
        avatars.ForEach(item => item.button.interactable = canToggle);
    }
}