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
/// G�re le mode multijoueur
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
    /// Les ids des joueurs pr l'authentification (Utilis� pr vivox)
    /// </summary>
    public Dictionary<string, (VivoxParticipant, GameObject)> authServicePlayerIds;

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

    #region Sorts
    private GameObject lightBall;
    private GameObject fireBall;
    private GameObject resurectio;
    private GameObject healProj;
    private GameObject speedProj;
    private GameObject fusrohdahProj;
    #endregion

    private void Awake()
    {
        Instance = this;
        mainMixer = Resources.Load<AudioMixer>("Audio/Main");
        copyCamPrefab = Resources.Load<GameObject>("Perso/CopyCam");
        grabZonePrefab = Resources.Load<GameObject>("Perso/GrabZone");
        authServicePlayerIds = new Dictionary<string, (VivoxParticipant, GameObject)>();
        lightBall = Resources.Load<GameObject>("Sorts/LightBall");
        fireBall = Resources.Load<GameObject>("Sorts/FireBall");
        resurectio = Resources.Load<GameObject>("Sorts/ResurectioProjectile");
        healProj = Resources.Load<GameObject>("Sorts/HealProjectile");
        speedProj = Resources.Load<GameObject>("Sorts/SpeedProjectile");
    }

    private void Start()
    {
        if (!soloMode) //Car le start de LobbyManager est appel� avant celui de MultiplayerGameManager
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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
            //error.GetComponent<ErrorHandler>().message = "T'as crash ou l'hote s'est tir�";
            SceneManager.LoadSceneAsync("MenuPrincipal");
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
    /// Set les donn�es du joueur en solo
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
    /// <param name="pere">Le nouveau p�re du network objet (Doit �tre un network object)</param>
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
    /// Renvoie le gameobject du joueur correspondant � l'id netcode
    /// </summary>
    /// <param name="playerId">L'id netcode recherch�</param>
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

    /// <summary>
    /// Renvoie l'array de tous les go des joueurs
    /// </summary>
    /// <returns>Un array de gameobject</returns>
    public GameObject[] GetAllPlayersGo()
    {
        return players;
    }
    #endregion

    /// <summary>
    /// Quand un joueur autre se connecte
    /// </summary>
    /// <param name="id">Player id</param>
    public void OnClientConnected(ulong id)
    {
        if (!IsHost) //Le reste du comportement est donc uniquement g�r� par le serveur
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
            error.GetComponent<ErrorHandler>().message = "Vous avez �t� d�connect�";
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
    /// Setup du jeu une fois que tous les joueurs sont connect�s
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
    /// Ajoute l'id du joueur authentifi�
    /// </summary>
    /// <param name="playerId">L'id netcode</param>
    /// <param name="authServiceId">L'id unity auth</param>
    private void AddAuthPlayerId(ulong playerId, string authServiceId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            authServicePlayerIds.Add(authServiceId, (null, null));
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
    /// Et le connecte au joueur concern�
    /// </summary>
    /// <param name="authId">L'id du joueur connect�</param>
    /// <param name="vivox">Le participant associ� au vivox particpant</param>
    public void AddPlayerVivoxInfo(string authId, VivoxParticipant vivox, GameObject talkIcon)
    {
        authServicePlayerIds[authId] = (vivox, talkIcon);

        //On regarde l'index dans les keys du dictionnaire pour savoir quel joueur est concern�
        int playerIndex = Array.IndexOf(authServicePlayerIds.Keys.ToArray(), authId);

        if (playerIndex != -1)
        {
            AudioSource playerTap = authServicePlayerIds[authId].Item1.ParticipantTapAudioSource;
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
                case PlayerState.Speedy:
                    playerTap.outputAudioMixerGroup = mainMixer.FindMatchingGroups("SpeedyVoice")[0];
                    break;
            }
        }
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant � l'id authentification (Ne marche que si le joueur est vivant)
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
    /// Renvoie le go du joueur correspondant � l'id authentification (Ne marche que si le joueur est vivant)
    /// </summary>
    /// <param name="authId">L'id du joueur dont on veux le go</param>
    /// <returns>Le gameObject du joueur ou null en cas d'erreur</returns>
    public GameObject GetPlayerGameObjectFromAuthId(string authId)
    {
        int playerIndex = Array.IndexOf(authServicePlayerIds.Keys.ToArray(), authId);
        if (playerIndex != -1)
        {
            if (playersStates[playerIndex] == PlayerState.Alive)
            {
                return players[playerIndex];
            }
            else
            {
                return GetGhostTransformFromPlayerId(playersIds[playerIndex]).gameObject;
            }
        }
        return null;
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant � l'id netcode (Ne marche que si le joueur est mort et que son ghost a spawn)
    /// </summary>
    /// <param name="playerId">Le player id</param>
    /// <returns>Le transform recherch�</returns>
    public Transform GetGhostTransformFromPlayerId(ulong playerId)
    {
        return GameObject.Find("GhostPlayer" + playerId).transform;
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant � l'id netcode (Ne marche que si le joueur est en mode vache)
    /// </summary>
    /// <param name="playerId">Le player id</param>
    /// <returns>Le transform recherch�</returns>
    public Transform GetCowTransformFromPlayerId(ulong playerId)
    {
        return GameObject.Find("Cow" + playerId).transform;
    }

    /// <summary>
    /// Recup�re la participant tap d'un joueur et le met sur son phantome
    /// </summary>
    /// <param name="playerId"></param>
    public void MovePlayerTapToGhost(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            authServicePlayerIds[deadPlayerAuthId].Item1.DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].Item1.CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetGhostTransformFromPlayerId(playerId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource);

            //On veut remplacer l'audio source de son particpant tap par celle avec le evil mixer group
            authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
        }
    }

    /// <summary>
    /// Recup�re la participant tap d'un joueur et le met sur sa vache
    /// </summary>
    /// <param name="playerId">Id du joueur</param>
    public void MovePlayerTapToCow(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            authServicePlayerIds[deadPlayerAuthId].Item1.DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].Item1.CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetCowTransformFromPlayerId(playerId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource);
        }
    }

    /// <summary>
    /// Transforme le joueur en speedy(pr la voix)
    /// </summary>
    /// <param name="playerId">Le player concern�</param>
    public void SetSpeedyPlayerTap(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            string playerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            AudioMixerGroup temp = mainMixer.FindMatchingGroups("SpeedyVoice")[0];
            authServicePlayerIds[playerAuthId].Item1.ParticipantTapAudioSource.outputAudioMixerGroup = temp;
        }
    }

    /// <summary>
    /// Reset le player tap d'un joueur pour remettre la voix normale
    /// </summary>
    /// <param name="playerId">Le player concern�</param>
    public void ResetPlayerTap(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            string playerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            authServicePlayerIds[playerAuthId].Item1.ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
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

    public void SetNormalChannelTap()
    {
        GameObject channelTap = GameObject.FindWithTag("ChannelTap");
        channelTap.GetComponent<AudioSource>().outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
    }

    public void SetDeadChannelTap()
    {
        GameObject channelTap = GameObject.FindWithTag("ChannelTap");
        channelTap.GetComponent<AudioSource>().outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
    }

    public void SetSpeedyChannelTap()
    {
        GameObject channelTap = GameObject.FindWithTag("ChannelTap");
        channelTap.GetComponent<AudioSource>().outputAudioMixerGroup = mainMixer.FindMatchingGroups("SpeedyVoice")[0];
    }

    /// <summary>
    /// Active tous les audioTaps de la sc�ne
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

    #endregion

    #region Death

    /// <summary>
    /// Sync la mort d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur mort</param>
    public void SyncDeath(ulong playerId) //Appel� par le serveur
    {
        SyncDeathClientRpc(playerId, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Envoie � tous les autres joueurs l'id du joueur mort
    /// </summary>
    /// <param name="deadPlayerId">L'id du player mort</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncDeathClientRpc(ulong deadPlayerId, ClientRpcParams clientRpcParams)
    {
        HandleDeath(deadPlayerId);
    }

    /// <summary>
    /// Cette fonction permet de g�rer la mort d'un joueur qui n'est pas le joueur qui meurt
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
    /// <param name="playerId">L'id du joueur ressucit�</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncRespawnServerRpc(NetworkObjectReference obj, ulong playerId) //Appel� par le serveur
    {
        Destroy((GameObject)obj);
        SyncResClientRpc(playerId, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Envoie � tous les autres joueurs l'id du joueur ressucit�
    /// </summary>
    /// <param name="resPlayerId">L'id du player ressucit�</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncResClientRpc(ulong resPlayerId, ClientRpcParams clientRpcParams)
    {
        HandleResurrection(resPlayerId);
    }

    /// <summary>
    /// Cette fonction permet de g�rer la r�surrection d'un joueur qui n'est pas le joueur qui revient de parmi les morts
    /// </summary>
    /// <param name="id">L'id du joueur ressucit�</param>
    private void HandleResurrection(ulong id)
    {
        int playerIndex = Array.IndexOf(playersIds, id);
        if (playerIndex != -1)
        {
            players[playerIndex].GetComponent<MonPlayerController>().HandleRespawn(); //Ca remet juste le tag a player
            playersStates[playerIndex] = PlayerState.Alive;
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            //On veut remplacer l'audio source de son particpant tap par celle avec main mixer group
            authServicePlayerIds[deadPlayerAuthId].Item1.DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].Item1.CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetPlayerTransformFromAuthId(deadPlayerAuthId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource);
            authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
        }
    }

    #endregion

    #region SyncRagdoll

    /// <summary>
    /// Sync l'etat de la ragdoll d'un joueur aux autres
    /// </summary>
    /// <param name="playerId">L'id du joueur � ragdoll</param>
    /// <param name="ragdollActive">L'etat de la ragdoll</param>
    public void SyncRagdoll(ulong playerId, bool ragdollActive)
    {
        SyncRagdollClientRpc(playerId, ragdollActive, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Envoie � tous les autres joueurs l'id du joueur � ragdoll ou non
    /// </summary>
    /// <param name="playerId">L'id du joueur � ragdoll</param>
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
    /// <param name="playerId">L'id du joueur � ragdoll</param>
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
    /// Renvoie les client rpc params pour envoyer une id � tous les autres joueurs
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
    /// Renvoie les client rpc params pour envoyer une id � tous les autres joueurs sauf un
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
    /// Renvoie les client rpc params pour envoyer une rpc � un joueur
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
    /// Summon une lightball pr tous les joueurs a une position donn�e
    /// </summary>
    /// <param name="pos">La position de la boule</param>
    /// <param name="intensity">L'int�nsit� lumineuse de la boule</param>
    /// <param name="time">Le temps pendant lequel la boule existera</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonLightballServerRpc(Vector3 pos, float intensity, float time)
    {
        Debug.Log("Summon light ball");
        GameObject lightBallGo = Instantiate(lightBall, pos, Quaternion.identity);
        lightBallGo.GetComponent<Light>().intensity = intensity;
        lightBallGo.AddComponent<Temporary>().StartCoroutine(nameof(Temporary.DestroyIn), time);
        lightBallGo.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Summon une fireball et l'envoie dans une direction 
    /// </summary>
    /// <param name="pos">Position de spawn de la boule de feu</param>
    /// <param name="dir">Direction de la boule de feu</param>
    /// <param name="speed">Vitesse de la boule de feu</param>
    /// <param name="expRange">Range of explosion</param>
    /// <param name="expForce">Force of the explosion</param>
    /// <param name="time">Temps avant de despawn le proj</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonFireBallServerRpc(Vector3 pos, Vector3 dir, float speed, float expRange, float expForce, float time)
    {
        Debug.Log("Summon fire ball");
        GameObject fireBallGo = Instantiate(fireBall, pos, Quaternion.identity);
        fireBallGo.GetComponent<Rigidbody>().velocity = dir * speed;
        fireBallGo.GetComponent<FireBall>().SetupFireBall(expRange, expForce);
        fireBallGo.GetComponent<FireBall>().StartCoroutine(nameof(FireBall.ExplodeIn), time);
        fireBallGo.GetComponent<NetworkObject>().Spawn();

    }

    /// <summary>
    /// Summon un projectile de resurection 
    /// </summary>
    /// <param name="pos">Endroit ou spawn les </param>
    /// <param name="dir">Direction du projectile</param>
    /// <param name="speed">Vitesse du projectile</param>
    /// <param name="time">Temps de vie du projectile</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonResurectioServerRpc(Vector3 pos, Vector3 dir, float speed, float time)
    {
        GameObject projResurrectio = Instantiate(resurectio, pos, Quaternion.LookRotation(dir));
        projResurrectio.transform.eulerAngles = new Vector3(0, 90, 90) + Quaternion.LookRotation(Vector3.forward, dir).eulerAngles;
        projResurrectio.GetComponent<Rigidbody>().velocity = dir * speed;
        projResurrectio.GetComponent<ResurrectionSpell>().StartCoroutine(nameof(ResurrectionSpell.DestroyIn), time);
        projResurrectio.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Summon un projectile de heal
    /// </summary>
    /// <param name="pos">Position de spawn</param>
    /// <param name="dir">Direction du proj</param>
    /// <param name="speed">Vitesse du proj</param>
    /// <param name="time">Dur�e du proj</param>
    /// <param name="healAmount">Amount a heal</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonHealProjServerRpc(Vector3 pos, Vector3 dir, float speed, float time, float healAmount)
    {
        GameObject projHeal = Instantiate(healProj, pos, Quaternion.LookRotation(dir));
        projHeal.GetComponent<Rigidbody>().velocity = dir * speed;
        projHeal.transform.eulerAngles += new Vector3(0, 0, 90);
        projHeal.GetComponent<HealProjectile>().SetHealAmount(healAmount);
        projHeal.GetComponent<HealProjectile>().StartCoroutine(nameof(HealProjectile.DestroyAfterTime), time);
        projHeal.GetComponent<NetworkObject>().Spawn();
    }
    /// <summary>
    /// Summo un projectile d'acceleration
    /// </summary>
    /// <param name="pos">Position de spawn du proj</param>
    /// <param name="dir">Direction du proj</param>
    /// <param name="speed">Vitesse du proj</param>
    /// <param name="time">Temps avant expiration</param>
    /// <param name="buffDuration">Dur�e du buff</param>

    [ServerRpc(RequireOwnership = false)]
    internal void SummonAccelProjServerRpc(Vector3 pos, Vector3 dir, float speed, float time, float buffDuration)
    {
        GameObject accelProj = Instantiate(speedProj, pos, Quaternion.LookRotation(dir));
        accelProj.transform.LookAt(transform.position + dir);
        accelProj.GetComponent<Rigidbody>().velocity = dir * speed;
        accelProj.GetComponent<AccelProjectile>().SetBuffDuration(buffDuration);
        accelProj.GetComponent<AccelProjectile>().StartCoroutine(nameof(AccelProjectile.DestroyAfterTime), time);
        accelProj.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Summon un projectile de fusrohdah
    /// </summary>
    /// <param name="pos">Position de spawn du proj</param>
    /// <param name="dir">Direction du proj</param>
    /// <param name="speed">Vitesse du proj</param>
    /// <param name="time">Temps avant expiration</param>
    /// <param name="expRange">Range of explosion</param>
    /// <param name="expForce">Force of the explosion</param>

    [ServerRpc(RequireOwnership = false)]
    internal void SummonFusrohdahServerRpc(Vector3 pos, Vector3 dir, float speed, float time, float expRange, float expForce)
    {
        GameObject fusrohdah = Instantiate(fusrohdahProj, pos, Quaternion.LookRotation(dir));
        fusrohdah.transform.LookAt(transform.position + dir); //Pour la rotation on ne se soucie que du y en vrai
        fusrohdah.GetComponent<Rigidbody>().velocity = dir * speed;
        fusrohdah.GetComponent<Fusrohdah>().SetupFusRohDah(expRange, expForce);
        fusrohdah.GetComponent<Fusrohdah>().StartCoroutine(nameof(Fusrohdah.DestroyIn), time);
        fusrohdah.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Sync le respawn d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur ressucit�</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncUncowServerRpc(NetworkObjectReference obj, ulong playerId) //Appel� par le serveur
    {
        Destroy((GameObject)obj);
        SyncUncowClientRpc(playerId, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Envoie � tous les autres joueurs l'id du joueur qui se detransforme
    /// </summary>
    /// <param name="resPlayerId">L'id du player ressucit�</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncUncowClientRpc(ulong resPlayerId, ClientRpcParams clientRpcParams)
    {
        HandleUncow(resPlayerId);
    }

    /// <summary>
    /// Cette fonction permet de g�rer la r�surrection d'un joueur qui n'est pas le joueur qui revient de parmi les morts
    /// </summary>
    /// <param name="id">L'id du joueur ressucit�</param>
    private void HandleUncow(ulong id)
    {
        int playerIndex = Array.IndexOf(playersIds, id);
        if (playerIndex != -1)
        {
            players[playerIndex].GetComponent<MonPlayerController>().HandleRespawn();
            string deadPlayerAuthId = authServicePlayerIds.Keys.ElementAt(playerIndex);
            //On veut remplacer l'audio source de son particpant tap par celle avec main mixer group
            authServicePlayerIds[deadPlayerAuthId].Item1.DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].Item1.CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetPlayerTransformFromAuthId(deadPlayerAuthId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource);
        }
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
    /// Les �tats possibles d'un joueur (Notamment pr les voice taps)
    /// </summary>
    private enum PlayerState
    {
        Alive,
        Dead,
        Speedy
    }
}
