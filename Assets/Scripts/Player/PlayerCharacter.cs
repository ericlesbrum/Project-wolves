using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
public class PlayerCharacter : NetworkBehaviour, INetworkSerializable
{
    public RoleType role;
    public ulong _id;
    public bool alive;
    public string _name;
    public ulong choice;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref _id);
        serializer.SerializeValue(ref role);
        serializer.SerializeValue(ref _name);
        serializer.SerializeValue(ref choice);
        serializer.SerializeValue(ref alive);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerServerRpc(ulong clientId)
    {
        LobbyManager lobby = FindObjectOfType<LobbyManager>();
        SetPlayerCharacter(clientId, lobby.joinnedLobby.Players[(int)clientId].Data["name"].Value);
    }

    public void SetPlayerCharacter(ulong _id, string _name, RoleType role = RoleType.None)
    {
        alive = true;
        this._id = _id;
        this.role = role;
        choice = 100;
        alive = true;
        this._name = _name != "Guest" ? _name : $"Guest {_id + 1}";
    }
    public override void OnNetworkSpawn()
    {
        SetPlayerServerRpc(GetComponent<NetworkObject>().OwnerClientId);
    }
}
