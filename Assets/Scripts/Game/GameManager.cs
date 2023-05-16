using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public bool gameStarted, changeTurn;
    public List<PlayerCharacter> characterList;
    [SerializeField] GameObject gamePrefab;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            if (IsHost && NetworkManager.Singleton.ConnectedClients.Count == 2)
            {
                SpawnGame();
            }
        };
    }
    public bool AllIsMarked()
    {
        return characterList.All(obj => obj.GetComponent<PlayerCharacter>().played);
    }
    void SpawnGame()
    {
        GameObject newGame = Instantiate(gamePrefab);
        newGame.GetComponent<NetworkObject>().Spawn();
    }
}
