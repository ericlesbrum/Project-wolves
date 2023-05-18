using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public NetworkVariable<bool> gameStarted;
    public NetworkVariable<bool> changeTurn;
    public NetworkVariable<int> maxLimitPlayers;
    public NetworkVariable<NetworkString> playerPlayed = new NetworkVariable<NetworkString>("");
    public List<PlayerCharacter> playerList;
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
                for (int i = 0; i < NetworkManager.Singleton.ConnectedClients.Count; i++)
                    playerPlayed.Value += $"NotPlayed - {i}|";
                SpawnGame();
            }
        };
    }
    public void AddRoles()
    {
        bool hasWerewolf, hasSeer;
        if (IsServer)
        {
            playerList[UnityEngine.Random.Range(0, playerList.Count)].role = RoleType.Werewolf;
            do
            {
                hasWerewolf = playerList.Any(pc => pc.role == RoleType.Werewolf);
                hasSeer = playerList.Any(pc => pc.role == RoleType.Seer);
                int randomIndex = UnityEngine.Random.Range(0, playerList.Count);
                if (playerList[randomIndex].role != RoleType.Werewolf && playerList[randomIndex].role != RoleType.Werewolf)
                    playerList[randomIndex].role = RoleType.Seer;
            } while (hasWerewolf && hasSeer);
            foreach (var player in playerList)
            {
                if (player.role == RoleType.None)
                    player.role = RoleType.Villager;
            }
        }
    }
    public bool AllIsPlayed()
    {
        return !playerPlayed.Value.ToString().Contains("NotPlayed");
    }
    void SpawnGame()
    {
        GameObject newGame = Instantiate(gamePrefab);
        newGame.GetComponent<NetworkObject>().Spawn();
    }
}
