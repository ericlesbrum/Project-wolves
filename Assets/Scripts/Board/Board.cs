using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Board : NetworkBehaviour
{
    public List<Avatar> avatars;
    [SerializeField] Transform parent;
    [SerializeField] GameObject avatar;
    [SerializeField] GameObject countDownScreen;
    [SerializeField] Button confirmButton;
    [SerializeField] TextMeshProUGUI CountDownText, role;
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

    public void Confirm()
    {
        ConfirmTurnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void ConfirmTurnServerRpc(ServerRpcParams serverRpcParams = default)
    {
        string _tempString = GameManager.Instance.playerPlayed.Value;
        var clientId = serverRpcParams.Receive.SenderClientId;
        if (NetworkManager.ConnectedClients.ContainsKey(clientId))
        {
            var client = NetworkManager.ConnectedClients[clientId];
            PlayerCharacter _player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (_player.choice != 100)
            {
                _player.played = true;
                GameManager.Instance.playerPlayed.Value = _tempString.Replace($"NotPlayed - {clientId}", $"Played - {clientId}");
            }
            else
            {
                _player.played = false;
                GameManager.Instance.playerPlayed.Value = _tempString.Replace($"Played - {clientId}", $"NotPlayed - {clientId}");
            }
            SetTurnEndClient();
        }
    }

    public void SetTurnEndClient()
    {
        if (GameManager.Instance.AllIsPlayed())
        {
            SetTurnEndClientRpc();
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
    public void SetTurnEndClientRpc()
    {
        StartCoroutine("CountDown");
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
    public void SetRolesClientRpc(string role, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            this.role.text = role;
        if (IsOwner) return;
        this.role.text = role;
    }

    public void GameStarted()
    {
        if (IsServer && GameManager.Instance.gameStarted.Value)
            return;
        if (IsServer)
        {
            GameManager.Instance.AddRoles();
            foreach (var item in GameManager.Instance.playerList)
            {
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { item._id }
                    }
                };
                SetRolesClientRpc(item.role.ToString(), clientRpcParams);
            }
            GameManager.Instance.gameStarted.Value = true;
        }
        for (int i = 0; i < avatars.Count; i++)
        {
            int index = i;
            if (NetworkManager.Singleton.LocalClientId != (ulong)index)
                avatars[i].button.onClick.AddListener(() => ToggleButton(index));
            else
                avatars[i].button.interactable = false;
        }
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
}