using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using TMPro;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Networking.Transport.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System;

public class LobbyManager : MonoBehaviour
{
    public Lobby hostLobby, joinnedLobby;
    [SerializeField] GameObject introLobby, panelLobby, gameStartButton, alertJoinLobby, alertStartGame;
    [SerializeField] TMP_InputField playerName, lobbyCode;
    [SerializeField] TextMeshProUGUI playersList, lobbyCodeText, maxLimtedPlayerMsg;
    [SerializeField] GameObject canvas;
    bool startedGame;

    async void Start()
    {
        maxLimtedPlayerMsg.text = $"Número máximo de jogadores: {GameManager.Instance.maxLimitPlayers.Value}";
        await UnityServices.InitializeAsync();
    }

    async Task Authenticate()
    {
        if (AuthenticationService.Instance.IsSignedIn)
            return;
        AuthenticationService.Instance.ClearSessionToken();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateLobby()
    {
        await Authenticate();

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = GetPlayer(),
            Data = new Dictionary<string, DataObject>
            {
                {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, "0") }
            }
        };

        hostLobby = await Lobbies.Instance.CreateLobbyAsync("lobby", GameManager.Instance.maxLimitPlayers.Value, options);
        joinnedLobby = hostLobby;

        InvokeRepeating("SendLobbyHeartBeat", 7, 7);
        lobbyCodeText.text = joinnedLobby.LobbyCode;
        ShowPlayers();
        gameStartButton.SetActive(true);
        introLobby.SetActive(false);
        panelLobby.SetActive(true);
    }

    void CheckForUpdates()
    {
        if (joinnedLobby == null || startedGame)
        {
            return;
        }
        UpdateLobby();
        ShowPlayers();
        if (joinnedLobby.Data["StartGame"].Value != "0")
        {
            if (hostLobby == null)
            {
                JoinRelay(joinnedLobby.Data["StartGame"].Value);
            }
            startedGame = true;
        }

    }

    async public void JoinLobbyByCode()
    {
        await Authenticate();

        JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
        {
            Player = GetPlayer()
        };

        try
        {
            joinnedLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode.text, options);
            if (joinnedLobby == null)
                alertJoinLobby.SetActive(true);
            lobbyCodeText.text = joinnedLobby.LobbyCode;

            ShowPlayers();
            introLobby.SetActive(false);
            panelLobby.SetActive(true);
            InvokeRepeating("CheckForUpdates", 3, 3);
        }
        catch (ArgumentNullException ex)
        {
            alertJoinLobby.SetActive(true);
        }
        catch (AuthenticationException ex)
        {
            alertJoinLobby.SetActive(true);
        }
        catch (System.Exception ex)
        {
            alertJoinLobby.SetActive(true);
        }
    }

    async void SendLobbyHeartBeat()
    {
        if (hostLobby == null)
            return;
        await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);

        UpdateLobby();
        ShowPlayers();
    }
    void ShowPlayers()
    {
        string _temp;
        playersList.text = "";
        for (int i = 0; i < joinnedLobby.Players.Count; i++)
        {
            _temp = joinnedLobby.Players[i].Data["name"].Value == "Guest" ? (i + 1).ToString() : null;
            playersList.text += $"{i + 1} - {joinnedLobby.Players[i].Data["name"].Value} {_temp}\n";
        }
    }
    Player GetPlayer()
    {
        Player player = new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                {"name",new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public,playerName.text!=""?playerName.text:$"Guest")}
            }
        };
        return player;
    }
    async void UpdateLobby()
    {
        if (joinnedLobby == null)
            return;
        joinnedLobby = await LobbyService.Instance.GetLobbyAsync(joinnedLobby.Id);
    }
    async Task<string> CreateRelay()
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();

        return joinCode;
    }

    async void JoinRelay(string joinCode)
    {
        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartClient();
        panelLobby.SetActive(false);
    }

    public async void StartGame()
    {
        if (joinnedLobby.Players.Count < GameManager.Instance.maxLimitPlayers.Value)
        {
            alertStartGame.SetActive(true);
            return;
        }
        string relayCode = await CreateRelay();
        Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinnedLobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                {"StartGame", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            }
        });
        joinnedLobby = lobby;
        panelLobby.SetActive(false);
        canvas.SetActive(false);
    }
}