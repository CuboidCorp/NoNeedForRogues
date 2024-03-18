using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Vivox.AudioTaps;
using UnityEngine;

/// <summary>
/// Gère le mode multijoueur
/// </summary>
public class MultiplayerGameManager : NetworkBehaviour
{
    public bool soloMode = false;

    public bool gameCanStart = false;

    public static MultiplayerGameManager Instance;

    private static int nbTotalPlayers = 0;

    public static int nbConnectedPlayers = 0;

    private ulong[] playersIds;

    private ulong hostId;

    private GameObject[] players;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    /// <summary>
    /// Set le nombre de joueurs dans le lobby qui vont jouer
    /// </summary>
    /// <param name="nb">Le nombre de joueur</param>
    public void SetNbPlayersLobby(int nb)
    {
        nbTotalPlayers = nb;
        playersIds = new ulong[nb];
    }

    /// <summary>
    /// Active tous les audioTaps de la scène
    /// </summary>
    public void ActiveAudioTaps()
    {
        GameObject[] audioTaps = GameObject.FindGameObjectsWithTag("AudioTap");

        foreach (GameObject tap in audioTaps)
        {
            tap.GetComponent<VivoxAudioTap>().enabled = true;
        }
    }

    /// <summary>
    /// When a player is connected
    /// </summary>
    /// <param name="id">Player id</param>
    private void OnClientConnected(ulong id)
    {
        if (!IsHost)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            return;
        }
        playersIds[nbConnectedPlayers] = id;
        if (nbConnectedPlayers == 0)
        {
            hostId = id;
        }
        nbConnectedPlayers++;
        Debug.Log("Player connected : " + id);
        Debug.Log(nbConnectedPlayers + "/" + nbTotalPlayers);
        if (nbConnectedPlayers == nbTotalPlayers)
        {
            gameCanStart = true;
            SendGameInfoClientRpc(nbTotalPlayers, playersIds);
        }

    }

    /// <summary>
    /// Send game info to all clients
    /// </summary>
    /// <param name="nbMaxPlayers">Le nb de joueurs</param>
    /// <param name="allIds">Les id de tt les joueurs</param>
    [ClientRpc]
    private void SendGameInfoClientRpc(int nbMaxPlayers, ulong[] allIds)
    {
        nbTotalPlayers = nbMaxPlayers;
        Debug.Log("nbMaxPlayers" + nbMaxPlayers);
        playersIds = allIds;
        Debug.Log(allIds.Length);
        players = new GameObject[nbMaxPlayers];
        int cpt = 0;
        foreach (ulong id in allIds)
        {
            foreach (GameObject playerTemp in GameObject.FindGameObjectsWithTag("Temp"))
            {
                if (playerTemp.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    playerTemp.tag = "Player";
                    playerTemp.name = "Player" + (id + 1);
                    players[cpt] = playerTemp;
                    cpt++;
                }
            }
        }

    }

    /// <summary>
    /// When a player is disconnected
    /// </summary>
    /// <param name="id">Player id </param>
    private void OnClientDisconnected(ulong id)
    {
        nbConnectedPlayers--;
        if (nbConnectedPlayers < nbTotalPlayers)
        {
            gameCanStart = false;
        }
    }

    /// <summary>
    /// Test qd on parle 
    /// TODO : Pr utiliser faudrait le mettre dans une boucle update avec affichage d'une icone qd on parle
    /// </summary>
    public void TestSpeech()
    {
        Debug.Log("Speech");
    }

    /// <summary>
    /// Sync la mort d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur mort</param>
    public void SyncDeath(ulong playerId) //Appelé par le serveur
    {
        SyncDeathClientRpc(playerId, GetIdsSaufJoueurs(playerId));
    }

    /// <summary>
    /// Envoie à tous les autres joueurs l'id du joueur mort
    /// </summary>
    /// <param name="deadPlayerId">L'id du player mort</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncDeathClientRpc(ulong deadPlayerId, ClientRpcParams clientRpcParams)
    {
        HandleDeath(deadPlayerId);
    }

    /// <summary>
    /// Cette fonction permet de gérer la mort d'un joueur qui n'est pas le joueur qui meurt
    /// </summary>
    /// <param name="id">L'id du joueur mort</param>
    public void HandleDeath(ulong id)
    {
        int playerIndex = Array.IndexOf(playersIds, id);
        if (playerIndex != -1)
        {
            players[playerIndex].GetComponent<MonPlayerController>().HandleDeath();
        }
    }

    /// <summary>
    /// Renvoie les client rpc params pour envoyer une id à tous les autres joueurs
    /// </summary>
    /// <returns>La client rpc params avec les bonnes info</returns>
    private ClientRpcParams GetIdAutresJoueurs()
    {
        ulong[] otherPlayerIds = playersIds.Where(id => id != OwnerClientId).ToArray();

        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = otherPlayerIds
            }
        };
    }

    /// <summary>
    /// Renvoie les client rpc params pour envoyer une id à tous les autres joueurs sauf un
    /// </summary>
    /// <param name="playerExclu">L'id du player exclu</param>
    /// <returns>La client rpc params avec les bonnes info</returns>
    private ClientRpcParams GetIdsSaufJoueurs(ulong playerExclu)
    {
        ulong[] otherPlayerIds = playersIds.Where(id => id != playerExclu).ToArray();

        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = otherPlayerIds
            }
        };
    }
}
