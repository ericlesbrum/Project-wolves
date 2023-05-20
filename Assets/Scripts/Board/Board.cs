using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Linq;
using Unity.Services.Lobbies.Models;
using System;

public class Board : NetworkBehaviour
{
    public List<Avatar> avatars;
    [SerializeField] Transform parent;
    [SerializeField] GameObject avatar;
    [SerializeField] GameObject countDownScreen;
    [SerializeField] Button confirmButton;
    [SerializeField] TextMeshProUGUI CountDownText, role, turn, description;
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
            if (!_player.alive)
            {
                UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"Eliminated - {clientId}");
                AvatarButtonClientRpc(false, GameManager.Instance.ReturnClientRpcParams(clientId));
            }
            else
            {
                if (GameManager.Instance.turn.Value == 1)
                {
                    if (_player.role == RoleType.Werewolf || _player.role == RoleType.Seer)
                    {
                        UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
                        AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
                    }
                    else
                    {
                        UpdatePlayerPlayedStatusClientRpc($"NotPlayed - {clientId}", $"CanPlayed - {clientId}");
                        AvatarButtonClientRpc(false, GameManager.Instance.ReturnClientRpcParams(clientId));
                    }
                }
                else
                {
                    if (_player.reveal)
                    {
                        UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId} Reveal - {clientId}");
                        
                    }
                    else
                        UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {clientId}", $"NotPlayed - {clientId}");
                    AvatarButtonClientRpc(true, GameManager.Instance.ReturnClientRpcParams(clientId));
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
    public void AvatarButtonClientRpc(bool canToggle, ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
        {
            SetAvatarButtonsInteractivity(canToggle);
            UpdatePlayerAvatarsClient();
        }
        else
        {
            if (IsOwner) return;
            SetAvatarButtonsInteractivity(canToggle);
            UpdatePlayerAvatarsClient();
        }
        confirmButton.interactable = canToggle ? true : false;
        
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
        if (!IsServer)
        {
            return;
        }
        string _tempString = GameManager.Instance.playerPlayed.Value;
        GameManager.Instance.playerPlayed.Value = _tempString.Replace(oldStatus, newStatus);
    }

    private void UpdatePlayerAvatarsClient()
    {
        string[] _tempPlayerPlayed = GameManager.Instance.playerPlayed.Value.ToString().Split('|');
        for (int i = 0; i < _tempPlayerPlayed.Length; i++)
        {
            string playerStatus = _tempPlayerPlayed[i];

            bool isEliminated = playerStatus.Contains($"Eliminated - {i}");
            bool isRevealed = playerStatus.Contains($"Reveal - {i}");

            avatars[i].button.interactable = !isEliminated;

            if (isRevealed)
            {
                avatars[i]._name.text = "Werewolf";
                avatars[i].background.color = Color.red;
            }
        }
    }
    private void Gameplay()
    {
        GameStarted();
        if (IsServer)
        {
            string _tempTurnString = GameManager.Instance.turn.Value == 0 ? "Morning" : "Night";
            if (GameManager.Instance.AllIsPlayed())
            {
                int turnValue = GameManager.Instance.turn.Value;
                GameManager.Instance.turn.Value = turnValue == 1 ? 0 : 1;
                ChoosePlayer(_tempTurnString);
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
                {
                    NetworkManager.Singleton.ConnectedClients[(ulong)i].PlayerObject.GetComponent<PlayerCharacter>().choice = 100;
                }
            }
            NetworkManager.Singleton.ConnectedClientsList.ToList().ForEach(item =>
            {
                SetTurnOnClientRpc(_tempTurnString, GameManager.Instance.ReturnClientRpcParams(item.ClientId));
            });
        }
        confirmButton.interactable = true;
        TurnSetPlayerServerRpc();
    }
    private void ChoosePlayer(string turn)
    {
        List<NetworkClient> players = NetworkManager.Singleton.ConnectedClientsList.ToList();
        Dictionary<PlayerCharacter, int> voteCountToEliminate = new Dictionary<PlayerCharacter, int>();
        Dictionary<PlayerCharacter, int> voteCountToInspec = new Dictionary<PlayerCharacter, int>();
        foreach (NetworkClient client in players)
        {
            PlayerCharacter player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (player.alive)
            {
                voteCountToEliminate[player] = 0;
                voteCountToInspec[player] = 0;
            }
        }
        foreach (NetworkClient client in players)
        {
            PlayerCharacter player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (player.alive)
            {
                PlayerCharacter votedPlayer;
                if (turn.Equals("Night"))
                {
                    switch (player.role)
                    {
                        case RoleType.Werewolf:
                            votedPlayer = ReturnPlayer(player.choice);
                            if (votedPlayer != null)
                                voteCountToEliminate[votedPlayer]++;
                            break;
                        case RoleType.Seer:
                            votedPlayer = ReturnPlayer(player.choice);
                            if (votedPlayer != null)
                                voteCountToInspec[votedPlayer]++;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    votedPlayer = ReturnPlayer(player.choice);
                    if (votedPlayer != null)
                        voteCountToEliminate[votedPlayer]++;
                }
            }
        }
        ProcessVote(voteCountToEliminate, voteCountToInspec);
    }
    private void ProcessVote(Dictionary<PlayerCharacter, int> voteCountToEliminate, Dictionary<PlayerCharacter, int> voteCountToInspect)
    {
        int maxEliminateVotes = voteCountToEliminate.Max(x => x.Value);
        int maxInspectVotes = voteCountToInspect.Max(x => x.Value);

        if (maxEliminateVotes > 0)
        {
            PlayerCharacter playerToEliminate = voteCountToEliminate.FirstOrDefault(x => x.Value == maxEliminateVotes).Key;
            playerToEliminate.alive = false;
        }

        if (maxInspectVotes > 0)
        {
            PlayerCharacter playerToInspect = voteCountToInspect.FirstOrDefault(x => x.Value == maxInspectVotes).Key;
            if (playerToInspect.role == RoleType.Werewolf)
                playerToInspect.reveal = true;
        }
    }
    private PlayerCharacter ReturnPlayer(ulong clientId)
    {
        List<NetworkClient> players = NetworkManager.Singleton.ConnectedClientsList.ToList();
        return players[(int)clientId].PlayerObject.GetComponent<PlayerCharacter>();
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
    private void SetTurnEndClient()
    {
        if (GameManager.Instance.AllIsPlayed())
        {
            SetTurnEndClientRpc();
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
        Gameplay();
        countDownScreen.SetActive(false);
    }
}