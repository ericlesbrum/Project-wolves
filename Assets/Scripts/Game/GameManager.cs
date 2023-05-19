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
    public NetworkVariable<int> maxLimitPlayers;
    public NetworkVariable<NetworkString> playerPlayed = new NetworkVariable<NetworkString>("");
    public NetworkVariable<NetworkString> turn = new NetworkVariable<NetworkString>("");
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
                SpawnGame();
            }
        };
    }
    public void AddRoles()
    {
        bool hasSeer;
        if (IsServer)
        {
            playerList[UnityEngine.Random.Range(0, playerList.Count)].role = RoleType.Werewolf;
            hasSeer = playerList.Any(pc => pc.role == RoleType.Seer);

            while (!hasSeer)
            {
                int randomIndex = UnityEngine.Random.Range(0, playerList.Count);
                if (playerList[randomIndex].role == RoleType.None)
                    playerList[randomIndex].role = RoleType.Seer;

                hasSeer = playerList.Any(pc => pc.role == RoleType.Seer);
            }
            playerList.ForEach(player =>
            {
                if (player.role == RoleType.None)
                    player.role = RoleType.Villager;
                NetworkManager.Singleton.ConnectedClientsList[(int)player._id].PlayerObject.GetComponent<PlayerCharacter>().role = player.role;
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
