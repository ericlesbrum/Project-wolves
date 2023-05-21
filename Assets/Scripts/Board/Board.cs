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
    public Button confirmButton;
    public GameObject avatar;
    public Transform parent;
    public GameObject countDownScreen;
    public Message message;
    public TextMeshProUGUI CountDownText, role, turn, description;
    public BoardServer boardServer;
    public BoardClient boardClient;
    IEnumerator Start()
    {
        LobbyManager lobby = FindObjectOfType<LobbyManager>();
        if (IsServer)
        {
            while (NetworkManager.Singleton.ConnectedClients.Count != lobby.joinnedLobby.Players.Count)
                yield return new WaitForSeconds(0.5f);
            for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            {
                boardClient.SetAvatarClientRpc(NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerCharacter>()._name, NetworkManager.Singleton.ConnectedClientsList[i].PlayerObject.GetComponent<PlayerCharacter>()._id);
                yield return new WaitForEndOfFrame();
            }
            GameManager.Instance.turn.Value = 1;
        }
        yield return new WaitForSeconds(1f);
        StartCoroutine("CountDown");
    }
    public void Confirm()
    {
        boardServer.ConfirmTurnServerRpc();
    }
    public void GameplayUpdate()
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
                boardClient.SetTurnOnClientRpc(_tempTurnString, GameManager.Instance.ReturnClientRpcParams(item.ClientId));
            });
        }
        confirmButton.interactable = true;
        boardServer.TurnSetPlayerServerRpc();
    }
    public void ChoosePlayer(string turn)
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
    public void ProcessVote(Dictionary<PlayerCharacter, int> voteCountToEliminate, Dictionary<PlayerCharacter, int> voteCountToInspect)
    {
        int maxEliminateVotes = voteCountToEliminate.Max(x => x.Value);
        int maxInspectVotes = voteCountToInspect.Max(x => x.Value);
        if (maxEliminateVotes > 0)
        {
            PlayerCharacter playerToEliminate = voteCountToEliminate
                .FirstOrDefault(x => x.Value == maxEliminateVotes)
                .Key;
            playerToEliminate.alive = false;
            boardClient.UpdatePlayerPlayedStatusClientRpc($"CanPlayed - {playerToEliminate._id}", $"Eliminated - {playerToEliminate._id}");
        }
        CheckWinCondition(maxInspectVotes > 0, voteCountToInspect, maxInspectVotes);
    }

    public void CheckWinCondition(bool votes, Dictionary<PlayerCharacter, int> voteCountToInspect, int maxInspectVotes)
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
    public PlayerCharacter ReturnPlayer(ulong clientId)
    {
        List<NetworkClient> players = NetworkManager.Singleton.ConnectedClientsList.ToList();
        return players[(int)clientId].PlayerObject.GetComponent<PlayerCharacter>();
    }
    public void GameStarted()
    {
        if (GameManager.Instance.gameStarted.Value)
            return;
        if (IsServer)
        {
            GameManager.Instance.AddRoles();
            NetworkManager.Singleton.ConnectedClientsList.ToList().ForEach(item =>
            {
                boardClient.SetRolesClientRpc(item.PlayerObject.GetComponent<PlayerCharacter>(), GameManager.Instance.ReturnClientRpcParams(item.ClientId));
            });

            string playerPlayed = string.Join("|", NetworkManager.Singleton.ConnectedClients.Select((client, index) => $"NotPlayed - {index}"));
            GameManager.Instance.playerPlayed.Value += playerPlayed;

            GameManager.Instance.gameStarted.Value = true;
        }
        ApplyListenerOnAvatarButtons();
    }
    public void SendMessageScreen(string title, string description, bool endGame)
    {
        for (int i = 0; i < NetworkManager.Singleton.ConnectedClientsList.Count; i++)
            RevealPlayerClientRpc(title, description, endGame, GameManager.Instance.ReturnClientRpcParams((ulong)i));
    }
    public void ApplyListenerOnAvatarButtons()
    {
        Enumerable.Range(0, avatars.Count)
            .Where(index => NetworkManager.Singleton.LocalClientId != (ulong)index)
            .ToList()
            .ForEach(index => avatars[index].button.onClick.AddListener(() => ToggleButton(index)));
        avatars[(int)NetworkManager.Singleton.LocalClientId].button.interactable = false;
    }
    public void ToggleButton(int indexClicked)
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
            boardServer.ToggleServerRpc((ulong)indexClicked);
        else
            boardServer.ToggleServerRpc(100);
    }
    public void SetAvatarButtonsInteractivity(bool canToggle)
    {
        avatars.ForEach(item => item.button.interactable = canToggle);
    }
    public void SetTurnEndClient()
    {
        if (GameManager.Instance.AllIsPlayed())
        {
            boardClient.SetTurnEndClientRpc();
        }
    }

    public IEnumerator CountDown()
    {
        int countDown = 5;
        countDownScreen.SetActive(true);
        do
        {
            CountDownText.text = countDown.ToString();
            yield return new WaitForSeconds(1f);
            countDown--;
        } while (countDown > 0);
        GameplayUpdate();
        countDownScreen.SetActive(false);
    }

    [ClientRpc]
    public void RevealPlayerClientRpc(string title, string description, bool endGame, ClientRpcParams clientRpcParams = default)
    {
        message.SetMsg(title, description, endGame);
        message.gameObject.SetActive(true);
    }
}