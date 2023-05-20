using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkVariable<bool> gameStarted;
    public NetworkVariable<int> maxLimitPlayers;
    public NetworkVariable<NetworkString> playerPlayed = new NetworkVariable<NetworkString>("");
    public NetworkVariable<int> turn = new NetworkVariable<int>(0);
    [SerializeField] GameObject gamePrefab;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            maxLimitPlayers.Value = 4;
            Instance = this;
        }
    }
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == maxLimitPlayers.Value)
            {
                SpawnGame();
            }
        };
    }
    public void AddRoles()
    {
        bool hasSeer;
        if (IsServer)
        {
            int countConnectedClientsList = NetworkManager.Singleton.ConnectedClientsList.Count;
            NetworkManager.Singleton.ConnectedClientsList[UnityEngine.Random.Range(0, countConnectedClientsList)].PlayerObject.GetComponent<PlayerCharacter>().role = RoleType.Werewolf;
            hasSeer = NetworkManager.Singleton.ConnectedClientsList.Any(item => item.PlayerObject.GetComponent<PlayerCharacter>().role == RoleType.Seer);

            while (!hasSeer)
            {
                int randomIndex = UnityEngine.Random.Range(0, countConnectedClientsList);
                if (NetworkManager.Singleton.ConnectedClientsList[randomIndex].PlayerObject.GetComponent<PlayerCharacter>().role == RoleType.None)
                    NetworkManager.Singleton.ConnectedClientsList[randomIndex].PlayerObject.GetComponent<PlayerCharacter>().role = RoleType.Seer;

                hasSeer = NetworkManager.Singleton.ConnectedClientsList.ToList().Any(pc => pc.PlayerObject.GetComponent<PlayerCharacter>().role == RoleType.Seer);
            }
            NetworkManager.Singleton.ConnectedClientsList.ToList().ForEach(player =>
            {
                if (player.PlayerObject.GetComponent<PlayerCharacter>().role == RoleType.None)
                    player.PlayerObject.GetComponent<PlayerCharacter>().role = RoleType.Villager;
                NetworkManager.Singleton.ConnectedClientsList[(int)player.PlayerObject.GetComponent<PlayerCharacter>()._id].PlayerObject.GetComponent<PlayerCharacter>().role = player.PlayerObject.GetComponent<PlayerCharacter>().role;
            });
        }
    }
    public bool AllIsPlayed()
    {
        return !playerPlayed.Value.ToString().Contains("NotPlayed");
    }

    public ClientRpcParams ReturnClientRpcParams(ulong id)
    {
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { id }
            }
        };
        return clientRpcParams;
    }

    void SpawnGame()
    {
        GameObject newGame = Instantiate(gamePrefab);
        newGame.GetComponent<NetworkObject>().Spawn();
    }
}
