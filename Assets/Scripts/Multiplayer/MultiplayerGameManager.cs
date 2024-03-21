using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Vivox;
using Unity.Services.Vivox.AudioTaps;
using UnityEngine;
using UnityEngine.Audio;

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

    /// <summary>
    /// Le gameobject qui contient les taps (Participant tap, Audio tap, etc)
    /// </summary>
    public GameObject TapHolder;

    /// <summary>
    /// Les ids des joueurs selon Netcode pr gameobject
    /// </summary>
    private ulong[] playersIds;

    /// <summary>
    /// Les ids des joueurs pr l'authentification (Utilisé pr vivox)
    /// </summary>
    private Dictionary<string,VivoxParticipant> authServicePlayerIds; 

    /// <summary>
    /// Les gameobjects des joueurs
    /// </summary>
    private GameObject[] players;

    //Les audio mixers pr les voix
    private AudioMixer mainMixer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        mainMixer = Resources.Load<AudioMixer>("Audio/Main");
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    /// <summary>
    /// Quand le gameManager a bien load
    /// </summary>
    public override void OnNetworkSpawn()
    {
        SendPlayerInfoServerRpc(OwnerClientId, AuthenticationService.Instance.PlayerId);
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
    /// Set les données du joueur en solo
    /// </summary>
    /// <param name="id">L'id netcode for gameobjects</param>
    /// <param name="authId">L'id authentification</param>
    public void SetDataSolo(ulong id, string authId)
    {
        playersIds[0] = id;
        authServicePlayerIds.Add(authId,null);
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
        nbConnectedPlayers++;
    }

    /// <summary>
    /// Send game info to all clients
    /// </summary>
    /// <param name="nbMaxPlayers">Le nb de joueurs</param>
    /// <param name="allIds">Les id de tt les joueurs</param>
    [ClientRpc]
    private void SendGameInfoClientRpc(int nbMaxPlayers, ulong[] allIds, NetworkStringArray playerIds)
    {
        nbTotalPlayers = nbMaxPlayers;
        playersIds = allIds;
        players = new GameObject[nbMaxPlayers];
        foreach (string playerId in playerIds.Array)
        {
            authServicePlayerIds.Add(playerId, null);
        }
        int cpt = 0;
        foreach (ulong id in allIds)
        {
            foreach (GameObject playerTemp in GameObject.FindGameObjectsWithTag("Temp"))
            {
                if (playerTemp.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    playerTemp.tag = "Player";
                    playerTemp.name = "Player" + id;
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
    /// Envoie les infos du joueur courant au serveur
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendPlayerInfoServerRpc(ulong ownerId, string authId)
    {
        AddAuthPlayerId(ownerId, authId);
    }

    /// <summary>
    /// Ajoute l'id du joueur authentifié
    /// </summary>
    /// <param name="playerId">L'id netcode</param>
    /// <param name="authServiceId">L'id unity auth</param>
    private void AddAuthPlayerId(ulong playerId, string authServiceId)
    {
        Debug.Log("AddAuthPlayerId : " + playerId+ " authService : "+authServiceId);
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            authServicePlayerIds.Add(authServiceId, null);
        }
        if(nbConnectedPlayers == nbTotalPlayers)
        {
            gameCanStart = true;
            NetworkStringArray array = new()
            {
                Array = authServicePlayerIds.Keys.ToArray()
            };

            SendGameInfoClientRpc(nbTotalPlayers, playersIds, array);
        }
    }

    /// <summary>
    /// Ajoute le participant tap d'un joueur au dictionnaire des participants
    /// </summary>
    /// <param name="authId">L'id du joueur connecté</param>
    /// <param name="vivox">Le participant associé au vivox particpant</param>
    public void AddPlayerVivoxInfo(string authId, VivoxParticipant vivox)
    {
        authServicePlayerIds[authId] = vivox;
    }

    #region Death

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
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            //On veut remplacer l'audio source de son particpant tap par celle avec le evil mixer group
            authServicePlayerIds[deadPlayerAuthId].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
        }
    }

    #endregion

    #region Respawn

    /// <summary>
    /// Sync le respawn d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur ressucité</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncRespawnServerRpc(NetworkObjectReference obj,ulong playerId) //Appelé par le serveur
    {
        Destroy((GameObject)obj);
        SyncResClientRpc(playerId, GetIdsSaufJoueurs(playerId));
    }

    /// <summary>
    /// Envoie à tous les autres joueurs l'id du joueur ressucité
    /// </summary>
    /// <param name="resPlayerId">L'id du player ressucité</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncResClientRpc(ulong resPlayerId, ClientRpcParams clientRpcParams)
    {
        HandleResurrection(resPlayerId);
    }

    /// <summary>
    /// Cette fonction permet de gérer la résurrection d'un joueur qui n'est pas le joueur qui revient de parmi les morts
    /// </summary>
    /// <param name="id">L'id du joueur ressucité</param>
    private void HandleResurrection(ulong id)
    {
        int playerIndex = Array.IndexOf(playersIds, id);
        if (playerIndex != -1)
        {
            players[playerIndex].GetComponent<MonPlayerController>().HandleRespawn();
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            //On veut remplacer l'audio source de son particpant tap par celle avec main mixer group
            authServicePlayerIds[deadPlayerAuthId].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("Master")[0];
        }
    }

    #endregion

    #region SyncRagdoll

    /// <summary>
    /// Sync l'etat de la ragdoll d'un joueur aux autres
    /// </summary>
    /// <param name="playerId">L'id du joueur à ragdoll</param>
    /// <param name="ragdollActive">L'etat de la ragdoll</param>
    public void SyncRagdoll(ulong playerId, bool ragdollActive)
    {
        SyncRagdollClientRpc(playerId, ragdollActive, GetIdsSaufJoueurs(playerId));
    }

    /// <summary>
    /// Envoie à tous les autres joueurs l'id du joueur à ragdoll ou non
    /// </summary>
    /// <param name="playerId">L'id du joueur à ragdoll</param>
    /// <param name="ragdollActive">L'etat de la ragdoll</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncRagdollClientRpc(ulong playerId, bool ragdollActive, ClientRpcParams clientRpcParams)
    {
        HandleRagdoll(playerId, ragdollActive);
    }

    /// <summary>
    /// Handle la ragdoll d'un joueur sur les autres
    /// </summary>
    /// <param name="playerId">L'id du joueur à ragdoll</param>
    /// <param name="ragdollActive">Si on active ou desactive la ragdoll</param>
    private void HandleRagdoll(ulong playerId, bool ragdollActive)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            if(ragdollActive)
            {
                players[playerIndex].GetComponent<MonPlayerController>().EnableRagdoll();
            }
            else
            {
                players[playerIndex].GetComponent<MonPlayerController>().DisableRagdoll();
            }
            
        }
    }

    #endregion

    #region ClientRpcParams

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

    #endregion
}
