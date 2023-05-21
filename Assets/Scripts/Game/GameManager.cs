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
            maxLimitPlayers.Value = 6;
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
        if (IsServer)
        {
            int werewolfCount = 0;
            int seerCount = 0;
            int maxWerewolves = (maxLimitPlayers.Value == 16) ? 3 : 2;


            List<int> availableIndices = new List<int>(maxLimitPlayers.Value);
            for (int i = 0; i < maxLimitPlayers.Value; i++)
            {
                availableIndices.Add(i);
            }

            Shuffle(availableIndices);

            for (int i = 0; i < maxLimitPlayers.Value; i++)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableIndices.Count);
                int playerIndex = availableIndices[randomIndex];
                if (werewolfCount < maxWerewolves)
                {
                    NetworkManager.Singleton.ConnectedClientsList[playerIndex].PlayerObject.GetComponent<PlayerCharacter>().role = RoleType.Werewolf;
                    werewolfCount++;
                }
                else if (seerCount < 2)
                {
                    NetworkManager.Singleton.ConnectedClientsList[playerIndex].PlayerObject.GetComponent<PlayerCharacter>().role = RoleType.Seer;
                    seerCount++;
                }
                else
                {
                    NetworkManager.Singleton.ConnectedClientsList[playerIndex].PlayerObject.GetComponent<PlayerCharacter>().role = RoleType.Villager;
                }
                availableIndices.RemoveAt(randomIndex);
            }
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

    private void SpawnGame()
    {
        GameObject newGame = Instantiate(gamePrefab);
        newGame.GetComponent<NetworkObject>().Spawn();
    }
    private void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
