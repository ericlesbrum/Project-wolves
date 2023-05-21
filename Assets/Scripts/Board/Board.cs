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
    [SerializeField] Message message;
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
            SetTurnEndClient(clientId);
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
    public void TestClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsServer)
            confirmButton.interactable = true;
        if (IsOwner) return;
        confirmButton.interactable = true;
    }
    [ClientRpc]
    public void AvatarButtonClientRpc(bool canToggle, ClientRpcParams clientRpcParams = default)
    {
        SetAvatarButtonsInteractivity(canToggle);
        confirmButton.interactable = canToggle ? true : false;
        string[] _tempPlayerPlayed = GameManager.Instance.playerPlayed.Value.ToString().Split('|');
        for (int i = 0; i < _tempPlayerPlayed.Length; i++)
        {
            if (_tempPlayerPlayed[i].Contains($"Eliminated - {i}"))
            {
                avatars[i].button.interactable = false;
            }
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
            role.text = $"{player._name} - {player.role}";
        }
        if (IsOwner) return;
        role.text = $"{player._name} - {player.role}";
    }
    [ClientRpc]
    public void SetTurnOnClientRpc(string turn, ClientRpcParams clientRpcParams = default)
    {
        this.turn.text = turn;
    }
    [ClientRpc]
    public void UpdatePlayerPlayedStatusClientRpc(string oldStatus, string newStatus, ClientRpcParams clientRpcParams = default)
    {
        if (!IsServer) return;
        string _tempString = GameManager.Instance.playerPlayed.Value;
        GameManager.Instance.playerPlayed.Value = _tempString.Replace(oldStatus, newStatus);
    }
    [ClientRpc]
    public void RevealPlayerClientRpc(string title, string description, bool endGame, ClientRpcParams clientRpcParams = default)
    {
        message.SetMsg(title, description, endGame);
        message.gameObject.SetActive(true);
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
            voteCountToEliminate[player] = 0;
            voteCountToInspec[player] = 0;
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
            PlayerCharacter playerToEliminate = voteCountToEliminate
                .FirstOrDefault(x => x.Value == maxEliminateVotes)
                .Key;
            playerToEliminate.alive = false;
            UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {playerToEliminate._id}", $"Eliminated - {playerToEliminate._id}");
        }
        CheckWinCondition(maxInspectVotes > 0, voteCountToInspect, maxInspectVotes);
    }
    private void SendMessageScreen(string title, string description, bool endGame)
    {
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            RevealPlayerClientRpc(title, description, endGame, GameManager.Instance.ReturnClientRpcParams((ulong)i));
    }
    private void CheckWinCondition(bool votes, Dictionary<PlayerCharacter, int> voteCountToInspect, int maxInspectVotes)
    {
        int werewolfCount = 0;
        int villagerCount = 0;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            PlayerCharacter player = client.PlayerObject.GetComponent<PlayerCharacter>();
            if (player.alive)
            {
                if (player.role == RoleType.Werewolf)
                {
                    werewolfCount++;
                }
                else
                {
                    villagerCount++;
                }
            }
        }

        if (werewolfCount >= villagerCount)
        {
            SendMessageScreen("Os lobisomens venceram", "Não há escapatória!", true);
        }
        else if (werewolfCount == 0)
        {
            SendMessageScreen("Os aldeões venceram", "Não existem mais lobisomens entre nós!", true);
        }
        else
        {
            if (votes)
            {
                PlayerCharacter playerToInspect = voteCountToInspect.FirstOrDefault(x => x.Value == maxInspectVotes).Key;
                if (playerToInspect.role == RoleType.Werewolf)
                {
                    playerToInspect.reveal = true;
                    SendMessageScreen("O lobisomen foi encontrado", $"O jogador {playerToInspect._name} é o lobisomen!", false);
                }
                else
                {
                    SendMessageScreen("O lobisomen não foi encontrado", $"O jogador {playerToInspect._name} não é o lobisomen!", false);
                }
            }
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
    private void SetTurnEndClient(ulong clientId)
    {
        if (GameManager.Instance.AllIsPlayed())
        {
            SetTurnEndClientRpc();
        }
        else
        {
            TestClientRpc(GameManager.Instance.ReturnClientRpcParams(clientId));
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