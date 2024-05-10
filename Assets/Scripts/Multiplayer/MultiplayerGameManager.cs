using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.Services.Vivox;
using Unity.Services.Vivox.AudioTaps;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

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

    private PlayerState[] playersStates;

    /// <summary>
    /// Les ids des joueurs pr l'authentification (Utilisé pr vivox)
    /// </summary>
    private Dictionary<string, VivoxParticipant> authServicePlayerIds;

    /// <summary>
    /// Les gameobjects des joueurs
    /// </summary>
    private GameObject[] players;

    private string[] playerNames;

    //Les audio mixers pr les voix
    private AudioMixer mainMixer;

    #region Prefabs
    private GameObject copyCamPrefab;
    private GameObject grabZonePrefab;
    #endregion

    private void Awake()
    {
        Instance = this;
        mainMixer = Resources.Load<AudioMixer>("Audio/Main");
        copyCamPrefab = Resources.Load<GameObject>("Perso/CopyCam");
        grabZonePrefab = Resources.Load<GameObject>("Perso/GrabZone");
        authServicePlayerIds = new Dictionary<string, VivoxParticipant>();
    }

    private void Start()
    {
        if (!soloMode) //Car le start de LobbyManager est appelé avant celui de MultiplayerGameManager
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    /// <summary>
    /// Set le nombre de joueurs dans le lobby qui vont jouer
    /// </summary>
    /// <param name="nb">Le nombre de joueur</param>
    public void SetNbPlayersLobby(int nb)
    {
        nbTotalPlayers = nb;
        playersIds = new ulong[nb];
        playerNames = new string[nb];
    }

    /// <summary>
    /// Set les données du joueur en solo
    /// </summary>
    public void SetDataSolo()
    {
        playersIds = new ulong[1];
        playersStates = new PlayerState[1];
        playersStates[0] = PlayerState.Alive;
        players = new GameObject[1];
        playerNames = new string[1];
        gameCanStart = true;
    }

    /// <summary>
    /// Active tous les audioTaps de la scène
    /// </summary>
    public void ActiveAudioTaps()
    {
        GameObject[] audioTaps = GameObject.FindGameObjectsWithTag("AudioTap");

        foreach (GameObject tap in audioTaps)
        {
            if (soloMode)
            {
                tap.GetComponent<VivoxAudioTap>().ChannelName = VivoxVoiceConnexion.echoChannelName;
            }
            else
            {
                tap.GetComponent<VivoxAudioTap>().ChannelName = VivoxVoiceConnexion.channelName;
            }
        }
    }

    /// <summary>
    /// Quand un joueur autre se connecte
    /// </summary>
    /// <param name="id">Player id</param>
    public void OnClientConnected(ulong id)
    {
        if (!IsHost) //Le reste du comportement est donc uniquement géré par le serveur
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            return;
        }
        playersIds[nbConnectedPlayers] = id;

        if (soloMode)
        {
            players[0] = GameObject.FindWithTag("Player");
            playerNames[0] = "SOLO";
            SpawnGrabZone(id);
        }

    }

    private void Update()
    {
        if (NetworkManager.Singleton.ShutdownInProgress) //TODO est prok aussi quand on se deconnecte tt court
        {
            Cursor.lockState = CursorLockMode.None;
            NetworkManager.Singleton.Shutdown();
            //GameObject error = new("ErrorHandler");
            //error.AddComponent<ErrorHandler>();
            //error.GetComponent<ErrorHandler>().message = "T'as crash ou l'hote s'est tiré";
            SceneManager.LoadSceneAsync("MenuPrincipal");
        }
    }

    /// <summary>
    /// Send game info to all clients
    /// </summary>
    /// <param name="nbMaxPlayers">Le nb de joueurs</param>
    /// <param name="allIds">Les id de tt les joueurs</param>
    [ClientRpc]
    private void SendGameInfoClientRpc(int nbMaxPlayers, ulong[] allIds, NetworkStringArray allNames)
    {
        Debug.Log("Ouais on a un array");
        nbTotalPlayers = nbMaxPlayers;
        playersIds = allIds;
        players = new GameObject[nbMaxPlayers];
        playerNames = allNames.Array;
        playersStates = new PlayerState[nbMaxPlayers];
        for (int i = 0; i < nbMaxPlayers; i++)
        {
            playersStates[i] = PlayerState.Alive;
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

    #region Utilitaires

    /// <summary>
    /// Permet de changer le parent d'un networkObject en passant par le serveur
    /// </summary>
    /// <param name="pere">Le nouveau père du network objet (Doit être un network object)</param>
    /// <param name="fils">Le network object a reparenter</param>
    [ServerRpc(RequireOwnership = false)]
    public void ChangeParentServerRpc(NetworkObjectReference pere, NetworkObjectReference fils)
    {
        ((GameObject)fils).transform.parent = ((GameObject)pere).transform;
        ((GameObject)fils).transform.localPosition = Vector3.zero;
    }

    /// <summary>
    /// Permet d'enlever un parent d'un networkObject en passant par le serveur
    /// </summary>
    /// <param name="fils">L'objet dont on veut enlever le parent</param>
    [ServerRpc(RequireOwnership = false)]
    public void RemoveParentServerRpc(NetworkObjectReference fils)
    {
        ((GameObject)fils).transform.parent = null;
    }

    /// <summary>
    /// Renvoie le gameobject du joueur correspondant à l'id netcode
    /// </summary>
    /// <param name="playerId">L'id netcode recherché</param>
    /// <returns>Le gameObject du joueur ou null si erreur</returns>
    public GameObject GetPlayerById(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            return players[playerIndex];
        }
        return null;
    }
    #endregion

    /// <summary>
    /// When a player is disconnected
    /// </summary>
    /// <param name="id">Player id </param>
    public void OnClientDisconnected(ulong id) //TODO : Handle la vrai deconnection genre message de deconnection
    {
        if (NetworkManager.Singleton.LocalClientId == id) //Si on s'est fait deconnecter
        {
            Cursor.lockState = CursorLockMode.None;
            MonPlayerController.instanceLocale.gameObject.GetComponent<PickUpController>().DropObject();
            NetworkManager.Singleton.Shutdown();
            GameObject error = new("ErrorHandler");
            error.AddComponent<ErrorHandler>();
            error.GetComponent<ErrorHandler>().message = "Vous avez été déconnecté";
            SceneManager.LoadSceneAsync("MenuPrincipal");
        }
        nbConnectedPlayers--;
        if (nbConnectedPlayers < nbTotalPlayers)
        {
            gameCanStart = false;
        }
    }

    /// <summary>
    /// Envoie les infos du joueur courant au serveur
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendPlayerInfoServerRpc(ulong ownerId, string authId, string playerName)
    {
        AddAuthPlayerId(ownerId, authId);
        AddPlayerName(ownerId, playerName);
        nbConnectedPlayers++;
        if (nbConnectedPlayers == nbTotalPlayers)
        {
            GameSetup();
        }
    }

    /// <summary>
    /// Setup du jeu une fois que tous les joueurs sont connectés
    /// </summary>
    private void GameSetup()
    {
        gameCanStart = true;
        foreach (string valeur in playerNames)
        {
            Debug.Log("Nom : " + valeur);
        }
        NetworkStringArray stringArray = new()
        {
            Array = playerNames
        };
        SendGameInfoClientRpc(nbTotalPlayers, playersIds, stringArray);
        foreach (ulong playerId in playersIds)
        {
            SpawnGrabZone(playerId);
        }
    }

    /// <summary>
    /// Ajoute l'id du joueur authentifié
    /// </summary>
    /// <param name="playerId">L'id netcode</param>
    /// <param name="authServiceId">L'id unity auth</param>
    private void AddAuthPlayerId(ulong playerId, string authServiceId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            authServicePlayerIds.Add(authServiceId, null);
        }
    }
    /// <summary>
    /// Ajoute un player name au serveur
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="playerName"></param>
    private void AddPlayerName(ulong playerId, string playerName)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            playerNames[playerIndex] = playerName;
        }
    }

    #region Vivox Utils

    /// <summary>
    /// Ajoute le participant tap d'un joueur au dictionnaire des participants
    /// Et le connecte au joueur concerné
    /// </summary>
    /// <param name="authId">L'id du joueur connecté</param>
    /// <param name="vivox">Le participant associé au vivox particpant</param>
    public void AddPlayerVivoxInfo(string authId, VivoxParticipant vivox)
    {
        authServicePlayerIds[authId] = vivox;

        //On regarde l'index dans les keys du dictionnaire pour savoir quel joueur est concerné
        int playerIndex = Array.IndexOf(authServicePlayerIds.Keys.ToArray(), authId);

        if (playerIndex != -1)
        {
            AudioSource playerTap = authServicePlayerIds[authId].ParticipantTapAudioSource;
            switch (playersStates[playerIndex])
            {
                case PlayerState.Dead:
                    //On veut remplacer l'audio source de son particpant tap par celle avec le evil mixer group
                    playerTap.outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
                    break;
                case PlayerState.Alive:
                    //On veut remplacer l'audio source de son particpant tap par celle avec main mixer group
                    playerTap.outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
                    break;
            }
        }
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant à l'id authentification (Ne marche que si le joueur est vivant)
    /// </summary>
    /// <param name="authId"><L'id du joueur dont on veux le transform/param>
    /// <returns>Le transform du joueur ou null en cas d'erreur</returns>
    public Transform GetPlayerTransformFromAuthId(string authId)
    {
        int playerIndex = Array.IndexOf(authServicePlayerIds.Keys.ToArray(), authId);
        if (playerIndex != -1)
        {
            if (playersStates[playerIndex] == PlayerState.Alive)
            {
                return players[playerIndex].transform;
            }
            else
            {
                return GetGhostTransformFromPlayerId(playersIds[playerIndex]);
            }
        }
        return null;
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant à l'id netcode (Ne marche que si le joueur est mort et que son ghost a spawn)
    /// </summary>
    /// <param name="playerId">Le player id</param>
    /// <returns>Le transform recherché</returns>
    public Transform GetGhostTransformFromPlayerId(ulong playerId)
    {
        return GameObject.Find("GhostPlayer" + playerId).transform;
    }

    /// <summary>
    /// Recupère la participant tap d'un joueur et le met sur son phantome
    /// </summary>
    /// <param name="playerId"></param>
    public void MovePlayerTapToGhost(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            authServicePlayerIds[deadPlayerAuthId].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetGhostTransformFromPlayerId(playerId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].ParticipantTapAudioSource);

            //On veut remplacer l'audio source de son particpant tap par celle avec le evil mixer group
            authServicePlayerIds[deadPlayerAuthId].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
        }
    }

    /// <summary>
    /// Permet de parametrer l'audio source d'un participant tap pr avoir le son 3d dans la bonne distance
    /// </summary>
    /// <param name="audioSource">L'audio source du participant tap</param>
    public void AddParamToParticipantAudioSource(AudioSource audioSource)
    {
        audioSource.maxDistance = VivoxVoiceConnexion.maxDistance;
        audioSource.minDistance = VivoxVoiceConnexion.minAudibleDistance;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
        audioSource.spatialBlend = 1;
    }

    #endregion

    #region Death

    /// <summary>
    /// Sync la mort d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur mort</param>
    public void SyncDeath(ulong playerId) //Appelé par le serveur
    {
        SyncDeathClientRpc(playerId, SendRpcToPlayersExcept(playerId));
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
            playersStates[playerIndex] = PlayerState.Dead;
        }
    }

    #endregion

    #region Respawn

    /// <summary>
    /// Sync le respawn d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur ressucité</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncRespawnServerRpc(NetworkObjectReference obj, ulong playerId) //Appelé par le serveur
    {
        Destroy((GameObject)obj);
        SyncResClientRpc(playerId, SendRpcToPlayersExcept(playerId));
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
            playersStates[playerIndex] = PlayerState.Alive;
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            //On veut remplacer l'audio source de son particpant tap par celle avec main mixer group
            authServicePlayerIds[deadPlayerAuthId].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetPlayerTransformFromAuthId(deadPlayerAuthId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].ParticipantTapAudioSource);
            authServicePlayerIds[deadPlayerAuthId].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
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
        SyncRagdollClientRpc(playerId, ragdollActive, SendRpcToPlayersExcept(playerId));
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
            if (ragdollActive)
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
    private ClientRpcParams SendRpcToOtherPlayers()
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
    private ClientRpcParams SendRpcToPlayersExcept(ulong playerExclu)
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

    /// <summary>
    /// Renvoie les client rpc params pour envoyer une rpc à un joueur
    /// </summary>
    /// <param name="player">Le joueur a qui envoyer la rpc</param>
    /// <returns>La client RPC params avec les bonnes infos</returns>
    private ClientRpcParams SendRpcToPlayer(ulong player)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { player }
            }
        };
    }

    #endregion

    #region Spells

    /// <summary>
    /// Permet de spawn un objet quelconque pr tous les joueurs
    /// </summary>
    /// <param name="obj">L'objet a spawn</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonLightballServerRpc(Vector3 pos, float intensity, float time)
    {
        Debug.Log("Summon light ball");
        GameObject lightBall = Instantiate(Resources.Load<GameObject>("Sorts/LightBall"), pos, Quaternion.identity);
        lightBall.GetComponent<Light>().intensity = intensity;
        lightBall.AddComponent<Temporary>().StartCoroutine(nameof(Temporary.DestroyIn), time);
        lightBall.GetComponent<NetworkObject>().Spawn();
    }

    #endregion

    #region GrabZone
    /// <summary>
    /// ServerRpc pr spawn la grab zone
    /// </summary>
    /// <param name="ownerId">L'id de l'owner de la grabzone</param>
    private void SpawnGrabZone(ulong ownerId)
    {
        GameObject copyCam = Instantiate(copyCamPrefab, new Vector3(0, 0, 0), Quaternion.identity);
        copyCam.name = "CopyCam" + ownerId;
        copyCam.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
        GameObject grabZone = Instantiate(grabZonePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        grabZone.name = "GrabZone" + ownerId;
        grabZone.GetComponent<NetworkObject>().SpawnWithOwnership(ownerId);
        grabZone.transform.parent = copyCam.transform;
        grabZone.transform.localPosition = new Vector3(0, 0, 1.5f);

        GameObject player = GetPlayerById(ownerId);
        copyCam.transform.parent = player.transform;
        copyCam.transform.localPosition = new Vector3(0, 1.6f, -.1f);
        ChangeParentClientRpc(copyCam, SendRpcToPlayer(ownerId));
    }

    [ClientRpc]
    private void ChangeParentClientRpc(NetworkObjectReference networkRef, ClientRpcParams clientRpcParams)
    {
        GameObject copyCam = (GameObject)networkRef;

        ulong ownerId = copyCam.GetComponent<NetworkObject>().OwnerClientId;
        copyCam.name = "CopyCam" + ownerId;
        copyCam.transform.localPosition = new Vector3(0, 1.6f, -.1f);
        copyCam.transform.GetChild(0).localPosition = new Vector3(0, 0, 1.5f);
        copyCam.transform.GetChild(0).gameObject.name = "GrabZone" + ownerId;
        GameObject player = GetPlayerById(ownerId);
        //On met la grab zone dans le playerController
        player.GetComponent<MonPlayerController>().copyCam = copyCam;
        player.GetComponent<PickUpController>().holdArea = copyCam.transform.GetChild(0);
    }
    #endregion

    /// <summary>
    /// Les états possibles d'un joueur (Notamment pr les voice taps)
    /// </summary>
    private enum PlayerState
    {
        Alive,
        Dead
    }
}
