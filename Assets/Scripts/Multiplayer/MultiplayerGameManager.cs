using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
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

    public int countdownToNextLevel = 5;

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
    public Dictionary<string, (VivoxParticipant, GameObject)> authServicePlayerIds;

    /// <summary>
    /// Les gameobjects des joueurs
    /// </summary>
    private GameObject[] players;

    private string[] playerNames;

    /// <summary>
    /// Liste de si les joueurs sont prets ou non pour la suite
    /// </summary>
    private bool[] playersReady;

    /// <summary>
    /// Si les joueurs vont vers le haut ou non
    /// De base a null
    /// </summary>
    private bool[] playerGoingUp;

    private bool isInLobby = true;

    private ulong[][] playerRepartitionByStairs;

    private GameObject[] escaliersGo;

    //Les audio mixers pr les voix
    private AudioMixer mainMixer;

    private Coroutine changeLevelCoroutine;

    public ConfigDonjon conf;

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
    private GameObject explosionPrefab;
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        Instance = this;

        DontDestroyOnLoad(this);
        LoadPrefabs();
        authServicePlayerIds = new Dictionary<string, (VivoxParticipant, GameObject)>();
    }

    private void LoadPrefabs()
    {
        mainMixer = Resources.Load<AudioMixer>("Audio/Main");
        copyCamPrefab = Resources.Load<GameObject>("Perso/CopyCam");
        grabZonePrefab = Resources.Load<GameObject>("Perso/GrabZone");
        lightBall = Resources.Load<GameObject>("Sorts/LightBall");
        fireBall = Resources.Load<GameObject>("Sorts/FireBall");
        resurectio = Resources.Load<GameObject>("Sorts/ResurectioProjectile");
        healProj = Resources.Load<GameObject>("Sorts/HealProjectile");
        speedProj = Resources.Load<GameObject>("Sorts/SpeedProjectile");
        fusrohdahProj = Resources.Load<GameObject>("Sorts/FusRoDahProjectile");
        explosionPrefab = Resources.Load<GameObject>("Sorts/Explosion");
    }

    private void Start()
    {

        if (!soloMode) //Car le start de LobbyManager est appelé avant celui de MultiplayerGameManager
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
            //error.GetComponent<ErrorHandler>().message = "T'as crash ou l'hote s'est tiré";
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
    /// Set les données du joueur en solo
    /// </summary>
    public void SetDataSolo()
    {
        playersIds = new ulong[1];
        playersStates = new PlayerState[1];
        playersStates[0] = PlayerState.Alive;
        players = new GameObject[1];
        playerNames = new string[1];
        playersReady = new bool[1];
        playerGoingUp = new bool[1];
        playerNames[0] = "SOLO";
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
        nbTotalPlayers = nbMaxPlayers;
        playersIds = allIds;
        players = new GameObject[nbMaxPlayers];
        playerNames = allNames.Array;
        playersStates = new PlayerState[nbMaxPlayers];
        playersReady = new bool[nbMaxPlayers];
        playerGoingUp = new bool[nbMaxPlayers];
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
                    playerTemp.GetComponent<MonPlayerController>().playerUI.GetComponentInChildren<TMP_Text>().text = playerNames[Array.IndexOf(allIds, id)];
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

    /// <summary>
    /// Renvoie l'array de tous les go des joueurs
    /// </summary>
    /// <returns>Un array de gameobject</returns>
    public GameObject[] GetAllPlayersGo()
    {
        return players;
    }

    /// <summary>
    /// Despawn un objet donnée après une certaine periode de temps
    /// </summary>
    /// <param name="networkObj">Le network object a despawn</param>
    /// <param name="time">Dans combien de temps il faut le despawn</param>
    private static IEnumerator DespawnAfterTimer(NetworkObject networkObj, float time)
    {
        yield return new WaitForSeconds(time);
        networkObj.Despawn();
    }
    #endregion

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

            SpawnGrabZone(id);
        }

    }

    /// <summary>
    /// When a player is disconnected
    /// </summary>
    /// <param name="id">Player id </param>
    public void OnClientDisconnected(ulong id)
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
        if (nbConnectedPlayers < nbTotalPlayers) //En theorie dans le lobby il peut se reconnecter mais je crois pas
        {
            gameCanStart = false;
        }
        HandlePlayerDisconnection(id);
    }

    private void HandlePlayerDisconnection(ulong playerId)
    {
        //TODO : Despawn tt les objets et resize tous les array
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
    /// Et le connecte au joueur concerné
    /// </summary>
    /// <param name="authId">L'id du joueur connecté</param>
    /// <param name="vivox">Le participant associé au vivox particpant</param>
    public void AddPlayerVivoxInfo(string authId, VivoxParticipant vivox, GameObject talkIcon)
    {
        authServicePlayerIds[authId] = (vivox, talkIcon);

        //On regarde l'index dans les keys du dictionnaire pour savoir quel joueur est concerné
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
    /// Renvoie le go du joueur correspondant à l'id authentification (Ne marche que si le joueur est vivant)
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
    /// Renvoie le transform du joueur correspondant à l'id netcode (Ne marche que si le joueur est mort et que son ghost a spawn)
    /// </summary>
    /// <param name="playerId">Le player id</param>
    /// <returns>Le transform recherché</returns>
    public Transform GetGhostTransformFromPlayerId(ulong playerId)
    {
        return GameObject.Find("GhostPlayer" + playerId).transform;
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant à l'id netcode (Ne marche que si le joueur est en mode vache)
    /// </summary>
    /// <param name="playerId">Le player id</param>
    /// <returns>Le transform recherché</returns>
    public Transform GetCowTransformFromPlayerId(ulong playerId)
    {
        return GameObject.Find("Cow" + playerId).transform;
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
            authServicePlayerIds[deadPlayerAuthId].Item1.DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
            authServicePlayerIds[deadPlayerAuthId].Item1.CreateVivoxParticipantTap("Tap " + deadPlayerAuthId).transform.SetParent(GetGhostTransformFromPlayerId(playerId));
            AddParamToParticipantAudioSource(authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource);

            //On veut remplacer l'audio source de son particpant tap par celle avec le evil mixer group
            authServicePlayerIds[deadPlayerAuthId].Item1.ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
        }
    }

    /// <summary>
    /// Recupère la participant tap d'un joueur et le met sur sa vache
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
    /// <param name="playerId">Le player concerné</param>
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
    /// <param name="playerId">Le player concerné</param>
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
    /// Permet de sync l'etat de la ragdoll d'un joueur aux autres
    /// </summary>
    /// <param name="playerId">Le joueur qui change l'etat de sa ragdoll</param>
    /// <param name="ragdollActive">Si la ragdoll deviient active ou inactive</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncRagdollStateServerRpc(ulong playerId, bool ragdollActive)
    {
        SyncRagdoll(playerId, ragdollActive);
    }

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
    /// Summon une lightball pr tous les joueurs a une position donnée
    /// </summary>
    /// <param name="pos">La position de la boule</param>
    /// <param name="intensity">L'inténsité lumineuse de la boule</param>
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
    /// <param name="time">Durée du proj</param>
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
    /// <param name="buffDuration">Durée du buff</param>

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
        fusrohdah.GetComponent<Rigidbody>().velocity = dir * speed;
        fusrohdah.GetComponent<Fusrohdah>().SetupFusRohDah(expRange, expForce);
        fusrohdah.GetComponent<Fusrohdah>().StartCoroutine(nameof(Fusrohdah.DestroyIn), time);
        fusrohdah.GetComponent<NetworkObject>().Spawn();
    }

    [ServerRpc(RequireOwnership = false)]
    internal void SummonExplosionServerRpc(Vector3 pos, float expRange, float time)
    {
        GameObject explosion = Instantiate(explosionPrefab, pos, Quaternion.identity);
        explosion.transform.localScale = new Vector3(expRange, expRange, expRange);
        explosion.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(DespawnAfterTimer(explosion.GetComponent<NetworkObject>(), time));
    }

    /// <summary>
    /// Sync le respawn d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur ressucité</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncUncowServerRpc(NetworkObjectReference obj, ulong playerId) //Appelé par le serveur
    {
        Destroy((GameObject)obj);
        SyncUncowClientRpc(playerId, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Envoie à tous les autres joueurs l'id du joueur qui se detransforme
    /// </summary>
    /// <param name="resPlayerId">L'id du player ressucité</param>
    /// <param name="clientRpcParams">Les client rpcParams pr concerner tous les joueurs sauf le joueur mort</param>
    [ClientRpc]
    private void SyncUncowClientRpc(ulong resPlayerId, ClientRpcParams clientRpcParams)
    {
        HandleUncow(resPlayerId);
    }

    /// <summary>
    /// Cette fonction permet de gérer la résurrection d'un joueur qui n'est pas le joueur qui revient de parmi les morts
    /// </summary>
    /// <param name="id">L'id du joueur ressucité</param>
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

    #region Starting Game

    /// <summary>
    /// Spawne les joueurs dans leurs escaliers respectifs
    /// </summary>
    public void SpawnPlayers()
    {
        bool direction = playerGoingUp[0];
        GameObject[] escaliers;
        if (direction)
        {
            escaliers = GameObject.FindGameObjectsWithTag("DownStairs");
        }
        else
        {
            escaliers = GameObject.FindGameObjectsWithTag("UpStairs");
        }
        //On place les joueurs en fonction de leur ancien endroits
        for (int i = 0; i < escaliers.Length; i++)
        {
            if (i < playerRepartitionByStairs.Length) //Securite car quand on part du lobby y a qu'un escalier
            {
                foreach (ulong playerId in playerRepartitionByStairs[i])
                {
                    GameObject player = GetPlayerById(playerId);
                    player.GetComponent<Rigidbody>().AddExplosionForce(1000, escaliers[i].transform.position, 10, 0, ForceMode.Impulse);
                    player.transform.position = escaliers[i].GetComponent<Escalier>().spawnPoint.position;
                }
            }
        }
    }


    /// <summary>
    /// Envoie au serveur l'information que le joueur est prêt
    /// </summary>
    /// <param name="playerId">L'id du player qui est pret</param>
    /// <param name="isReady">Si le joueur est ready ou non</param>
    [ServerRpc(RequireOwnership = false)]
    public void SyncPlayerStateServerRpc(ulong playerId, bool isReady, bool isStairUp = false)
    {
        int index = Array.IndexOf(playersIds, playerId);
        if (index != -1)
        {
            playersReady[index] = isReady;
            if (!isReady)
            {
                playerGoingUp[index] = false;
                ResetCountDown();
                return;
            }

            playerGoingUp[index] = isStairUp;
            CheckGameCanStart();
        }
    }

    private void CheckGameCanStart()
    {
        if (PlayersAreReady() && PlayersAreGoingSameWay())
        {
            //On start la coroutine sur tous les escaliers qui vont du meme coté que les joueurs
            //A la fin de la coroutine on change la scene
            changeLevelCoroutine = StartCoroutine(StartMovingCountdown(countdownToNextLevel));
            bool direction = playerGoingUp[0];
            StartCountdownClientRpc(direction);
        }
    }

    /// <summary>
    /// Client rpc pour lire le countdown chez tous les clients
    /// </summary>
    /// <param name="direction">Direction des escaliers</param>
    [ClientRpc]
    private void StartCountdownClientRpc(bool direction)
    {
        if (direction)
        {
            escaliersGo = GameObject.FindGameObjectsWithTag("UpStairs");
        }
        else
        {
            escaliersGo = GameObject.FindGameObjectsWithTag("DownStairs");
        }
        foreach (GameObject esc in escaliersGo)
        {
            esc.GetComponent<Escalier>().StartCountdown(countdownToNextLevel);
        }
    }

    /// <summary>
    /// Reset le countdown
    /// </summary>
    private void ResetCountDown()
    {
        if (changeLevelCoroutine != null)
        {
            StopCoroutine(changeLevelCoroutine);
        }
        ResetCountdownClientRpc();
    }

    /// <summary>
    /// Reset le countdown pr tous les clients
    /// </summary>
    [ClientRpc]
    private void ResetCountdownClientRpc()
    {
        if (escaliersGo != null)
        {
            foreach (GameObject esc in escaliersGo)
            {
                esc.GetComponent<Escalier>().CancelCountdown();
            }
        }
    }

    private IEnumerator StartMovingCountdown(int nbSec)
    {
        yield return new WaitForSeconds(nbSec);
        ChangeLevel();
    }

    private void ChangeLevel()
    {
        bool direction = playerGoingUp[0];

        if (!isInLobby)
        {
            playerRepartitionByStairs = new ulong[escaliersGo.Length][];
            for (int i = 0; i < escaliersGo.Length; i++)
            {
                playerRepartitionByStairs[i] = escaliersGo[i].GetComponent<Escalier>().GetPlayers();
            }
            //On vérifie en fonction du génération donjon le current level
            if (direction == false) //On descend
            {
                GenerationDonjon.instance.currentEtage++;
                if (GenerationDonjon.instance.currentEtage > GenerationDonjon.instance.maxEtage)
                {
                    NetworkManager.SceneManager.LoadScene("EndScene", LoadSceneMode.Single);
                }
            }
            else
            {
                if (GenerationDonjon.instance.currentEtage == 1)
                {
                    Debug.Log("On ne peut pas fuir comme ça mec");
                    //On recup les players dans les stairs qui vont vers le haut
                    List<ulong> playersAPunir = new();
                    List<Vector3> stairPlayerPos = new();
                    GameObject[] upStairs = GameObject.FindGameObjectsWithTag("UpStairs");
                    foreach (GameObject stair in upStairs)
                    {
                        ulong[] players = stair.GetComponent<Escalier>().GetPlayers();
                        foreach (ulong player in players)
                        {
                            playersAPunir.Add(player);
                            stairPlayerPos.Add(stair.transform.position); //TODO : Ajouter la position de 
                        }
                    }

                    for(int i = 0 ;i<playersAPunir.Count ;i++)
                    {
                        GameObject playerGo = GetPlayerById(playersAPunir[i]);
                        //Comment punir le joueur -> Ragdoll
                        StartCoroutine(playerGo.GetComponent<MonPlayerController>().SetRagdollTemp(5));
                        AudioManager.instance.PlayOneShotClipServerRpc(playerGo.transform.position, SoundEffectOneShot.NUHUH);
                        Rigidbody[] ragdollElems = playerGo.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

                        foreach (Rigidbody ragdoll in ragdollElems)
                        {
                            ragdoll.AddExplosionForce(500, stairPlayerPos, 10);
                        }
                    }
                    return;
                }
                else
                {
                    GenerationDonjon.instance.currentEtage--;
                }

            }
        }
        else
        {
            LeaveLobby();
        }
        //On sauvegarde par escalier là ou sont les gens

        NetworkManager.SceneManager.LoadScene("Donjon", LoadSceneMode.Single);

    }

    private void LeaveLobby()
    {
        Debug.Log("Leaving lobby");
        playerRepartitionByStairs = new ulong[1][];
        playerRepartitionByStairs[0] = playersIds;
        //On recup les settings du donjon
        conf = ConfigDonjonUI.Instance.conf;
        isInLobby = false;

        //On suppose que tous les clients font ces lignes
        MonPlayerController.instanceLocale.FullHeal();
        MonPlayerController.instanceLocale.FullMana();

        StatsManager.Instance.dateDebutGame = DateTime.Now;
        StatsManager.Instance.InitializeGame(MonPlayerController.instanceLocale.OwnerClientId);

    }

    /// <summary>
    /// Vérifie si les joueurs sont prets
    /// </summary>
    /// <returns>True si ils sont tous prets, false sinon</returns>
    private bool PlayersAreReady()
    {
        foreach (bool state in playersReady)
        {
            if (state == false)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Verifie si tous les joueurs vont du meme coté
    /// </summary>
    /// <returns>True si ils vont du meme coté, false sinon</returns>
    private bool PlayersAreGoingSameWay()
    {
        bool dirInit = playerGoingUp[0];
        for (int i = 1; i < playerGoingUp.Length; i++)
        {
            if (playerGoingUp[i] != dirInit)
            {
                return false;
            }
        }
        return true;
    }

    #endregion

    /// <summary>
    /// Les états possibles d'un joueur (Notamment pr les voice taps)
    /// </summary>
    private enum PlayerState
    {
        Alive,
        Dead,
        Speedy
    }
}
