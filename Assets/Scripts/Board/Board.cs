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
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                SetAvatarClientRpc(NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerCharacter>()._name, NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerCharacter>()._id);
                yield return new WaitForEndOfFrame();
            }
            GameManager.Instance.turn.Value = 1;
        }
        yield return new WaitForSeconds(1f);
        StartCoroutine("CountDown");
    }
    public void Confirm()
    {
        ConfirmTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TurnSetPlayerServerRpc(ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            PlayerCharacter _player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (GameManager.Instance.turn.Value == 1)
            {
                if (_player.role == RoleType.Werewolf || _player.role == RoleType.Seer)
                {
                    AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                    UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
                }
                else
                {
                    AvatarButtonClientRpc(false, GameManager.Instance.ReturnClientRpcParams(clientId));
                    UpdatePlayerPlayedStatusClientRpc($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
                }
            }
            else
            {
                AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
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
                UpdatePlayerPlayedStatusClientRpc($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
            }
            else
            {
                UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
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
    [ClientRpc]
    public void SetTurnOnClientRpc(string turn, ClientRpcParams clientRpcParams = default)
    {
        this.turn.text = turn;
    }
    [ClientRpc]
    private void UpdatePlayerPlayedStatusClientRpc(string oldStatus, string newStatus, ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer) return;
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
        Gameplay();
        countDownScreen.SetActive(false);
    }
    private void Gameplay()
    {
        GameStarted();
        if (IsServer)
        {
            if (GameManager.Instance.AllIsPlayed())
            {
                int turnValue = GameManager.Instance.turn.Value;
                GameManager.Instance.turn.Value = turnValue == 1 ? 0 : 1;
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
                {
                    NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.GetComponent<PlayerCharacter>().choice = 100;
                }
            }
            string _tempTurnString = GameManager.Instance.turn.Value == 0 ? "Morning" : "Night";
            NetworkManager.Singleton.ConnectedClientsList.ToList().ForEach(item =>
            {
                SetTurnOnClientRpc(_tempTurnString, GameManager.Instance.ReturnClientRpcParams(item.ClientId));
            });
        }
        TurnSetPlayerServerRpc();
    }
    private void GameStarted()
    {
        if (GameManager.Instance.gameStarted.Value)
            return;
        if (IsServer)
        {
            GameManager.Instance.AddRoles();
            NetworkManager.Singleton.ConnectedClientsList.ToList().ForEach(item =>
            {
                SetRolesClientRpc(item.PlayerObject.GetComponent<PlayerCharacter>(), GameManager.Instance.ReturnClientRpcParams(item.ClientId));
            });

            string playerPlayed = string.Join("|", NetworkManager.Singleton.ConnectedClients.Select((client, index) => $"NotPlayed - {index}"));
            GameManager.Instance.playerPlayed.Value += playerPlayed;

            GameManager.Instance.gameStarted.Value = true;
        }
        ApplyListenerOnAvatarButtons();
    }
    private void ApplyListenerOnAvatarButtons()
    {
        Enumerable.Range(0, avatars.Count)
            .Where(index => NetworkManager.Singleton.LocalClientId != (ulong)index)
            .ToList()
            .ForEach(index => avatars[index].button.onClick.AddListener(() => ToggleButton(index)));
        avatars[(int)NetworkManager.Singleton.LocalClientId].button.interactable = false;
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
    public void SetTurnEndClient()
    {
        if (GameManager.Instance.AllIsPlayed())
        {
            SetTurnEndClientRpc();
        }
    }
}