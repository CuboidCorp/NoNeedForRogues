using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

    #region Données Joueurs
    /// <summary>
    /// Les ids des joueurs selon Netcode pr gameobject
    /// </summary>
    private ulong[] playersIds;

    [SerializeField] private PlayerState[] playersStates;

    /// <summary>
    /// Les gameobjects des joueurs
    /// </summary>
    private GameObject[] playersGo;

    private string[] playersNames;

    /// <summary>
    /// Les id Vivox des joueurs
    /// </summary>
    private string[] playersAuthId;

    /// <summary>
    /// Tous les vivox participants des joueurs
    /// </summary>
    private VivoxParticipant[] participants;

    #endregion

    #region Escaliers 
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

    /// <summary>
    /// Temporaire pr la repartition des joueurs dans les escaliers
    /// </summary>
    private ulong[][] playerRepartitionByStairs;

    /// <summary>
    /// Temporaire pr les escaliers dans la meme direction que les joueurs
    /// </summary>
    private GameObject[] escaliersGo;

    #endregion

    //Les audio mixers pr les voix
    private AudioMixer mainMixer;

    private Coroutine changeLevelCoroutine;

    #region Donjon
    [Header("Donjon")]
    public ConfigDonjon conf;

    /// <summary>
    /// Les seeds des etages
    /// </summary>
    public int[] seeds;
    #endregion

    #region Prefabs
    private GameObject alchimieZonePrefab;
    #endregion

    #region Sorts
    private GameObject lightBall;
    private GameObject fireBall;
    private GameObject resurectio;
    private GameObject healProj;
    private GameObject speedProj;
    private GameObject fusrohdahProj;
    private GameObject explosionPrefab;
    private GameObject zoneVentPrefab;
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
        conf = new();
    }

    /// <summary>
    /// Charge toutes les prefabs dont on a besoin
    /// </summary>
    private void LoadPrefabs()
    {
        mainMixer = Resources.Load<AudioMixer>("Audio/Main");
        lightBall = Resources.Load<GameObject>("Sorts/LightBall");
        fireBall = Resources.Load<GameObject>("Sorts/FireBall");
        resurectio = Resources.Load<GameObject>("Sorts/ResurectioProjectile");
        healProj = Resources.Load<GameObject>("Sorts/HealProjectile");
        speedProj = Resources.Load<GameObject>("Sorts/AccelProjectile");
        fusrohdahProj = Resources.Load<GameObject>("Sorts/FusRoDahProjectile");
        explosionPrefab = Resources.Load<GameObject>("Sorts/Explosion");
        alchimieZonePrefab = Resources.Load<GameObject>("Objets/AlchemyZone");
        zoneVentPrefab = Resources.Load<GameObject>("Sorts/ZoneVent");
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
        if (NetworkManager.Singleton.ShutdownInProgress) //Quand on se deconnecte / Kick ou crash
        {
            Cursor.lockState = CursorLockMode.None;
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadSceneAsync("MenuPrincipal");
        }
    }

    public void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        Debug.Log("Scene loaded : " + sceneName);
        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning("Client qui ont time out :");
            foreach (ulong client in clientsTimedOut)
            {
                Debug.LogWarning(client);
            }
        }

        if (sceneName == "Donjon")
        {
            StartCoroutine(TestAttente());
        }
    }

    /// <summary>
    /// Attends une seconde avant de lancer la generation du donjon
    /// Pour vraiment attendre que tout le monde soit bien connecté
    /// </summary>
    /// <returns></returns>
    private IEnumerator TestAttente()
    {
        yield return new WaitForSeconds(1);
        GenerationDonjon.instance.StartGenerationServer();
    }

    /// <summary>
    /// Set le nombre de joueurs dans le lobby qui vont jouer
    /// </summary>
    /// <param name="nb">Le nombre de joueur</param>
    public void SetNbPlayersLobby(int nb, string[] playerNames)
    {
        nbTotalPlayers = nb;
        playersIds = new ulong[nb];
        for (int i = 0; i < nb; i++)
        {
            playersIds[i] = ulong.MaxValue;
        }
        playersNames = playerNames;
        playersStates = new PlayerState[nb];
        playersGo = new GameObject[nb];
        playersAuthId = new string[nb];
        participants = new VivoxParticipant[nb];
        playersReady = new bool[nb];
        playerGoingUp = new bool[nb];
    }

    /// <summary>
    /// Set les données du joueur en solo
    /// </summary>
    public void SetDataSolo()
    {
        nbTotalPlayers = 1;
        playersIds = new ulong[1];
        playersStates = new PlayerState[1];
        playersStates[0] = PlayerState.Alive;
        playersGo = new GameObject[1];
        playersNames = new string[1];
        playersReady = new bool[1];
        playerGoingUp = new bool[1];
        playersNames[0] = "SOLO";
        playersAuthId = new string[1];
        participants = new VivoxParticipant[1];
    }

    /// <summary>
    /// Renvoie le nombre total de joueurs
    /// </summary>
    /// <returns>Renvoie le nombre de joueurs</returns>
    public int GetNbTotalPlayers()
    {
        return nbTotalPlayers;
    }

    #region Utilitaires

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
            return playersGo[playerIndex];
        }
        return null;
    }

    /// <summary>
    /// Renvoie l'array de tous les go des joueurs
    /// </summary>
    /// <returns>Un array de gameobject</returns>
    public GameObject[] GetAllPlayersGo()
    {
        return playersGo;
    }

    /// <summary>
    /// Despawn un objet donnée après une certaine periode de temps
    /// </summary>
    /// <param name="networkObj">Le network object a despawn</param>
    /// <param name="time">Dans combien de temps il faut le despawn</param>
    public IEnumerator DespawnAfterTimer(NetworkObjectReference networkObj, float time)
    {
        yield return new WaitForSeconds(time);
        DespawnObjServerRpc(networkObj);
    }

    /// <summary>
    /// Despawn un objet 
    /// </summary>
    /// <param name="obj">L'obj a despawn</param>
    [ServerRpc(RequireOwnership = false)]
    public void DespawnObjServerRpc(NetworkObjectReference obj)
    {
        ((GameObject)obj).GetComponent<NetworkObject>().Despawn(true);
    }

    /// <summary>
    /// Renvoie le nom du joueur correspondant à l'id netcode
    /// </summary>
    /// <param name="playerId">L'id du joueur dont on veut le nom</param>
    /// <returns>Le nom du joueur</returns>
    public string GetPlayerName(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            return playersNames[playerIndex];
        }
        return null;
    }

    #region ClientRpcParams

    /// <summary>
    /// Renvoie les client rpc params pour envoyer une id à tous les autres joueurs
    /// </summary>
    /// <returns>La client rpc params avec les bonnes info</returns>
    public ClientRpcParams SendRpcToOtherPlayers()
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
    public ClientRpcParams SendRpcToPlayersExcept(ulong playerExclu)
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
    public static ClientRpcParams SendRpcToPlayer(ulong player)
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


    #endregion

    #region Connexion / Deconnexion

    /// <summary>
    /// Quand un joueur autre se connecte
    /// </summary>
    /// <param name="id">Player id</param>
    public void OnClientConnected(ulong id)
    {
        GameObject[] playersTemp = GameObject.FindGameObjectsWithTag("Temp");
        if (nbConnectedPlayers < (int)id) //On recup les anciens joueurs et l'actuel
        {
            foreach (GameObject playerTemp in playersTemp)
            {
                if (!playersIds.Contains(playerTemp.GetComponent<NetworkObject>().OwnerClientId))
                {
                    //Si on l'a pas encore enregistré on le fait
                    ulong idTemp = playerTemp.GetComponent<NetworkObject>().OwnerClientId;
                    playersIds[nbConnectedPlayers] = idTemp;
                    playersStates[nbConnectedPlayers] = PlayerState.Alive;
                    playersGo[nbConnectedPlayers] = playerTemp;
                    playerTemp.tag = "Player";
                    playerTemp.name = "Player" + idTemp;
                    playerTemp.GetComponent<MonPlayerController>().playerUI.GetComponentInChildren<TextMeshProUGUI>().text = playersNames[nbConnectedPlayers];
                    nbConnectedPlayers++;
                }
            }
        }
        else
        {
            if (playersTemp.Length > 1)
            {
                Debug.LogWarning("Y a plusieurs persos la dedans ;("); //On est le client qui se connecte apres les autres
            }
            playersIds[nbConnectedPlayers] = id;
            playersStates[nbConnectedPlayers] = PlayerState.Alive;
            GameObject playerTemp = playersTemp[0];
            playerTemp.tag = "Player";
            playerTemp.name = "Player" + id;

            playersGo[nbConnectedPlayers] = playerTemp;
            playerTemp.GetComponent<MonPlayerController>().playerUI.GetComponentInChildren<TextMeshProUGUI>().text = playersNames[nbConnectedPlayers];
            nbConnectedPlayers++;
        }
        if (nbConnectedPlayers == nbTotalPlayers)
        {
            //Le jeu peut commencer
            gameCanStart = true;
            MonPlayerController.instanceLocale.JoinVivox();
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

    /// <summary>
    /// Gère la deconnexion d'un joueur
    /// </summary>
    /// <param name="playerId">L'id du joueur deconnecté</param>
    private void HandlePlayerDisconnection(ulong playerId)
    {
        //Est ce que il y a qqch a faire --> Voir lors des tests a plusieurs joueurs
    }

    #endregion

    #region Vivox Utils

    [ServerRpc(RequireOwnership = false)]
    public void SendAuthIdServerRpc(ulong playerId, string authId)
    {
        SendAuthIdClientRpc(playerId, authId);
    }

    [ClientRpc]
    private void SendAuthIdClientRpc(ulong playerId, string authId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        playersAuthId[playerIndex] = authId;
    }

    /// <summary>
    /// Ajoute le participant tap d'un joueur au dictionnaire des participants
    /// Et le connecte au joueur concerné
    /// </summary>
    /// <param name="authId">L'id du joueur connecté</param>
    /// <param name="vivox">Le participant associé au vivox particpant</param>
    /// <returns>L'index du joueur dans les arrays</returns>
    public int AddPlayerVivoxInfo(string authId, VivoxParticipant vivox)
    {
        int playerIndex = Array.IndexOf(playersAuthId, authId);
        participants[playerIndex] = vivox;
        return playerIndex;
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant à l'id authentification (Ne marche que si le joueur est vivant)
    /// </summary>
    /// <param name="authId"><L'id du joueur dont on veux le transform/param>
    /// <returns>Le transform du joueur ou null en cas d'erreur</returns>
    public Transform GetPlayerTransformFromAuthId(string authId)
    {
        int playerIndex = Array.IndexOf(playersAuthId, authId);
        if (playerIndex != -1)
        {
            if (playersStates[playerIndex] == PlayerState.Alive || playersStates[playerIndex] == PlayerState.Speedy)
            {
                return playersGo[playerIndex].transform;
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
        participants[playerIndex].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
        try //On met un try catch car si le joueur est trop loin il a pas de playerTap
        {
            GameObject tap = participants[playerIndex].CreateVivoxParticipantTap("Tap " + playerIndex);
            if (tap != null)
            {
                tap.transform.SetParent(GetGhostTransformFromPlayerId(playerId));
                tap.transform.localPosition = new Vector3(0, 1.6f, 0);
                AddParamToParticipantAudioSource(playerIndex);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Recupère la participant tap d'un joueur et le met sur sa vache
    /// </summary>
    /// <param name="playerId">Id du joueur</param>
    public void MovePlayerTapToCow(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
        try //On met un try catch car si le joueur est trop loin il a pas de playerTap
        {
            GameObject tap = participants[playerIndex].CreateVivoxParticipantTap("Tap " + playerIndex);
            if (tap != null)
            {
                tap.transform.SetParent(GetCowTransformFromPlayerId(playerId));
                tap.transform.localPosition = new Vector3(0, 1.6f, 0);
                AddParamToParticipantAudioSource(playerIndex);
            }

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public void MovePlayerTapToHuman(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
        try //On met un try catch car si le joueur est trop loin il a pas de playerTap
        {
            GameObject tap = participants[playerIndex].CreateVivoxParticipantTap("Tap " + playerIndex);
            if (tap != null)
            {
                tap.transform.SetParent(GetPlayerTransformFromAuthId(playersAuthId[playerIndex]));
                tap.transform.localPosition = new Vector3(0, 1.6f, 0);
                AddParamToParticipantAudioSource(playerIndex);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    /// <summary>
    /// Transforme le joueur en speedy(pr la voix)
    /// </summary>
    /// <param name="playerId">Le player concerné</param>
    [ServerRpc(RequireOwnership = false)]
    public void SetSpeedyPlayerTapServerRpc(ulong playerId)
    {
        SetSpeedyPlayerTapClientRpc(playerId, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Envoie a tous les clients sauf le client actuel l'info qu'il faut changer le tap
    /// </summary>
    /// <param name="cRpcParams">Les client rpc qui précisent tous les clients sauf celui qui a le boost</param>
    [ClientRpc]
    private void SetSpeedyPlayerTapClientRpc(ulong playerId, ClientRpcParams cRpcParams)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("SpeedyVoice")[0];
    }

    /// <summary>
    /// Demande au serv de dire aux clients de reset le tap d'un joueur pour remettre la voix normale
    /// </summary>
    /// <param name="playerId">Le joueur dont on doit reset le tap</param>
    [ServerRpc(RequireOwnership = false)]
    public void ResetPlayerTapServerRpc(ulong playerId)
    {
        ResetPlayerTapClientRpc(playerId, SendRpcToPlayersExcept(playerId));
    }

    /// <summary>
    /// Reset le player tap d'un joueur pour remettre la voix normale
    /// </summary>
    /// <param name="playerId">Le player concerné</param>
    [ClientRpc]
    private void ResetPlayerTapClientRpc(ulong playerId, ClientRpcParams cRpcParams)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
    }

    /// <summary>
    /// Permet de parametrer l'audio source d'un participant tap pr avoir le son 3d dans la bonne distance
    /// </summary>
    /// <param name="playerIndex">L'index du joueur dans les arrays</param>
    public void AddParamToParticipantAudioSource(int playerIndex)
    {
        AudioSource source = participants[playerIndex].ParticipantTapAudioSource;

        source.maxDistance = VivoxVoiceConnexion.maxDistance;
        source.minDistance = VivoxVoiceConnexion.minAudibleDistance;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.spatialBlend = 1;

        switch (playersStates[playerIndex])
        {
            case PlayerState.Dead:
                source.outputAudioMixerGroup = mainMixer.FindMatchingGroups("DeadVoice")[0];
                break;
            case PlayerState.Alive:
                source.outputAudioMixerGroup = mainMixer.FindMatchingGroups("NormalVoice")[0];
                break;
            case PlayerState.Speedy:
                source.outputAudioMixerGroup = mainMixer.FindMatchingGroups("SpeedyVoice")[0];
                break;
        }
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
            playersGo[playerIndex].tag = "Ragdoll";
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
            playersGo[playerIndex].tag = "Player";
            playersStates[playerIndex] = PlayerState.Alive;
            MovePlayerTapToHuman(id);
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
                playersGo[playerIndex].tag = "Ragdoll";
                playersGo[playerIndex].GetComponent<MonPlayerController>().EnableRagdoll(false);
            }
            else
            {
                playersGo[playerIndex].tag = "Player";
                playersGo[playerIndex].GetComponent<MonPlayerController>().DisableRagdoll(false);
            }

        }
    }

    /// <summary>
    /// Met le jouer du client en parametre en ragdoll pour un temps donné
    /// </summary>
    /// <param name="time">Le temps pendant lequel le joueur est en ragdoll</param>
    /// <param name="client">Le client a ragdoll</param>
    [ClientRpc]
    public void SetRagdollTempClientRpc(float time, ClientRpcParams client)
    {
        StartCoroutine(MonPlayerController.instanceLocale.SetRagdollTemp(time));
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

    /// <summary>
    /// Summon une explosion
    /// </summary>
    /// <param name="pos">Position de l'explosion</param>
    /// <param name="expRange">Range de l'explosion</param>
    /// <param name="time">Temps pendant lequel l'explosion est visible</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonExplosionServerRpc(Vector3 pos, float expRange, float time)
    {
        GameObject explosion = Instantiate(explosionPrefab, pos, Quaternion.identity);
        explosion.transform.localScale = new Vector3(expRange, expRange, expRange);
        explosion.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(DespawnAfterTimer(explosion, time));
    }

    /// <summary>
    /// Summon une zone de vent
    /// </summary>
    /// <param name="pos">Position de la zone de vent</param>
    /// <param name="dir">Direction de la zone de vent</param>
    /// <param name="force">Force de pousée de la zone</param>
    /// <param name="tailleCollider">Taille de la zone de vent</param>
    /// <param name="posCollider">Position de la zone de vent</param>
    /// <param name="time">Temps avant de despawn</param>
    [ServerRpc(RequireOwnership = false)]
    internal void SummonZoneVentServerRpc(Vector3 pos, Vector3 dir, float force, Vector3 tailleCollider, Vector3 posCollider, float time)
    {
        GameObject zoneVent = Instantiate(zoneVentPrefab, pos, Quaternion.LookRotation(dir));
        zoneVent.GetComponent<ZoneVent>().SetupZoneVent(force, posCollider, tailleCollider);
        zoneVent.GetComponent<NetworkObject>().Spawn();
        StartCoroutine(DespawnAfterTimer(zoneVent, time));
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
            playersGo[playerIndex].tag = "Player";
            MovePlayerTapToHuman(id);
        }
    }

    /// <summary>
    /// Envoie les infos au joueur qu'il a reçu un speed boost
    /// </summary>
    /// <param name="duree">Durée du buff</param>
    /// <param name="clientRpcParams">L'id du client a sauvegardé</param>
    [ClientRpc]
    public void SendSpeedBoostClientRpc(float duree, ClientRpcParams clientRpcParams)
    {
        MonPlayerController.instanceLocale.ReceiveSpeedBoost(duree);
    }

    #endregion

    #region Gestion Objets
    /// <summary>
    /// Demande au serv de dire a tt le monde de summon la copie d'un objet 
    /// </summary>
    /// <param name="obj">La reference au vrai objet</param>
    /// <param name="cheminObj">Chemin pr spawn l'objet</param>
    /// <param name="playerId">L'id du joueur qui grab l'objet</param>
    [ServerRpc(RequireOwnership = false)]
    public void SummonCopieObjetServerRpc(NetworkObjectReference obj, string cheminObj, ulong playerId)
    {
        SummonCopieObjetClientRpc(obj, cheminObj, playerId);
    }

    /// <summary>
    /// Summon sur un client la copie de l'objet
    /// </summary>
    /// <param name="obj">La reference au vrai objet</param>
    /// <param name="cheminObj">Le chemin de l'objet</param>
    /// <param name="playerId">L'id du joueur sur qui spawne</param>
    [ClientRpc]
    private void SummonCopieObjetClientRpc(NetworkObjectReference obj, string cheminObj, ulong playerId)
    {
        ((GameObject)obj).SetActive(false);
        int playerIndex = Array.IndexOf(playersIds, playerId);
        if (playerIndex != -1)
        {
            playersGo[playerIndex].GetComponentInChildren<PickUpController>().CreeCopie(cheminObj);
        }
    }

    /// <summary>
    /// Demande au serv la copie tenue par un player 
    /// </summary>
    /// <param name="obj">La reference au vrai objet</param>
    /// <param name="playerId">L'id du joueur dont on veut suppr la copie</param>
    [ServerRpc(RequireOwnership = false)]
    public void DestroyCopieServerRpc(NetworkObjectReference obj, ulong playerId)
    {
        DestroyCopieClientRpc(obj, playerId);
    }

    /// <summary>
    /// Detruit la copie tenue du joueur
    /// </summary>
    /// <param name="obj">La reference au vrai objet</param>
    /// <param name="playerId">L'id du joueur dont on veut suppr la copie</param>
    [ClientRpc]
    public void DestroyCopieClientRpc(NetworkObjectReference obj, ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        ((GameObject)obj).SetActive(true);
        if (playerIndex != -1)
        {
            playersGo[playerIndex].GetComponentInChildren<PickUpController>().SupprimerCopie();
        }
    }

    #endregion

    /// <summary>
    /// Demande au serv de summon une zone d'alchimie a la position donnée
    /// </summary>
    /// <param name="goName">Nom du alchemy pot qui veut sa zone</param>
    /// <param name="pos">Position de l'alchimie zone</param>
    [ServerRpc(RequireOwnership = false)]
    public void SummonAlchemyZoneServerRpc(string goName, Vector3 pos)
    {
        GameObject alchimieZone = Instantiate(alchimieZonePrefab, pos, Quaternion.identity);
        alchimieZone.transform.localScale = new Vector3(2, 2, 2);
        alchimieZone.GetComponent<NetworkObject>().Spawn();
        GameObject.Find(goName).GetComponent<AlchemyPot>().SetAlchemyZone(alchimieZone);
    }

    /// <summary>
    /// Demande au serv de summon une wind zone pr un ventilo a la pos donnée avec les params donnés
    /// </summary>
    /// <param name="goName">Nom du ventilo qui veut sa zone</param>
    /// <param name="pos">Position de la future windzone</param>
    [ServerRpc(RequireOwnership = false)]
    public void SummonVentiloWindZoneServerRpc(string goName, Vector3 pos, Vector3 dir, float force, Vector3 tailleCollider, Vector3 posCollider)
    {
        GameObject zoneVent = Instantiate(zoneVentPrefab, pos, Quaternion.Euler(dir));
        zoneVent.GetComponent<ZoneVent>().SetupZoneVent(force, posCollider, tailleCollider);
        zoneVent.GetComponent<NetworkObject>().Spawn();
        GameObject.Find(goName).GetComponent<Ventilo>().SetWindZone(zoneVent);
    }

    /// <summary>
    /// Synchronise la config du donjon entre l'hote et les autres clientsb 
    /// </summary>
    /// <param name="conf">La nouvelle configuration du donjon</param>
    [ClientRpc]
    public void SyncConfigDonjonClientRpc(ConfigDonjon conf)
    {
        if (ConfigDonjonUI.Instance != null)
        {
            ConfigDonjonUI.Instance.SetConf(conf);
        }
        this.conf = conf;
    }

    #region Starting Game

    /// <summary>
    /// Spawne les joueurs dans leurs escaliers respectifs
    /// </summary>
    public void SpawnPlayers()
    {
        bool direction = playerGoingUp[0];
        //On reset 
        playersReady = new bool[nbTotalPlayers];
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
                    SetSpawnPositionClientRpc(escaliers[i].GetComponent<Escalier>().spawnPoint.position, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerId } } });
                }
            }
        }
    }

    /// <summary>
    /// Set la position du joueur depuis le client car authorité client
    /// </summary>
    /// <param name="pos">Nouvelle pos du joeueur</param>
    /// <param name="clientRpcParams">Le client qu'il faut qu'on set</param>
    [ClientRpc]
    private void SetSpawnPositionClientRpc(Vector3 pos, ClientRpcParams clientRpcParams)
    {
        Debug.Log("Set spawn : " + pos);
        MonPlayerController.instanceLocale.transform.position = pos;
        MonPlayerController.instanceLocale.SetRespawnPoint(pos);
    }

    /// <summary>
    /// Set la position de spawn de tous les joueurs
    /// </summary>
    /// <param name="pos">Le nouveau spawn de tous les joueurs</param>
    public void SetSpawnAllPlayers(Vector3 pos)
    {
        foreach (ulong playerId in playersIds)
        {
            SetSpawnPositionClientRpc(pos, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playerId } } });
        }
    }

    /// <summary>
    /// Envoie au serveur l'information que le joueur est prêt
    /// </summary>
    /// <param name="playerId">L'id du player qui est pret</param>
    /// <param name="isReady">Si le joueur est ready ou non</param>
    public void SyncPlayerState(ulong playerId, bool isReady, bool isStairUp = false)
    {
        int index = Array.IndexOf(playersIds, playerId);
        if (index != -1)
        {
            playersReady[index] = isReady;
            if (!isReady)
            {
                ResetCountDown();
                return;
            }

            playerGoingUp[index] = isStairUp;
            CheckGameCanStart();
        }
    }

    /// <summary>
    /// Verifie si tous les joueurs sont prêts et vont dans le même sens
    /// </summary>
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

    /// <summary>
    /// Lance le compte a rebours pr changer le niveau
    /// </summary>
    /// <param name="nbSec">Nombre de secondes avant le changement de niveau</param>
    private IEnumerator StartMovingCountdown(int nbSec)
    {
        yield return new WaitForSeconds(nbSec);
        ChangeLevel();
    }

    /// <summary>
    /// Change le niveau vers le niveau suivant
    /// </summary>
    private void ChangeLevel()
    {
        bool direction = playerGoingUp[0];

        if (!isInLobby)
        {
            GenerationDonjon.instance.DespawnItems();
            Debug.Log("Change level going " + (direction ? "up" : "down") + " : Current etage : " + conf.currentEtage);
            playerRepartitionByStairs = new ulong[escaliersGo.Length][];
            for (int i = 0; i < escaliersGo.Length; i++)
            {
                playerRepartitionByStairs[i] = escaliersGo[i].GetComponent<Escalier>().GetPlayers();
            }
            //On vérifie en fonction du génération donjon le current level
            if (direction == false) //On descend
            {
                conf.currentEtage++;
                if (conf.currentEtage > conf.nbEtages)
                {
                    NetworkManager.SceneManager.LoadScene("EndScene", LoadSceneMode.Single);
                }
            }
            else
            {

                if (conf.currentEtage == 1) //On ne part pas
                {
                    TrollCowardPlayer();
                    return;
                }
                else
                {
                    conf.currentEtage--;
                }

            }
        }
        else
        {
            seeds = new int[conf.nbEtages];
            LeaveLobbyClientRpc();
        }
        //On sauvegarde par escalier là ou sont les gens
        DestroyStairs();
        NetworkManager.SceneManager.LoadScene("Donjon", LoadSceneMode.Single);

    }

    /// <summary>
    /// Prepare les clients a quitter le lobby
    /// </summary>
    [ClientRpc]
    private void LeaveLobbyClientRpc()
    {
        AudioManager.instance.StopMusic();
        AudioManager.instance.SetMusic(AudioManager.Musique.DONJON);
        AudioManager.instance.ActivateMusic();

        playerRepartitionByStairs = new ulong[1][];
        playerRepartitionByStairs[0] = playersIds;
        //On recup les settings du donjon
        isInLobby = false;

        MonPlayerController.instanceLocale.FullHeal();
        MonPlayerController.instanceLocale.FullMana();

        StatsManager.Instance.dateDebutGame = DateTime.Now;
        StatsManager.Instance.InitializeGame(MonPlayerController.instanceLocale.OwnerClientId);
    }

    /// <summary>
    /// Punit les joueurs qui étaient en train de fuir pr retourner au lobby
    /// </summary>
    private void TrollCowardPlayer()
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
                stairPlayerPos.Add(stair.transform.position);
            }
        }

        for (int i = 0; i < playersAPunir.Count; i++)
        {
            GameObject playerGo = GetPlayerById(playersAPunir[i]);
            //Comment punir le joueur -> Ragdoll
            playerGo.GetComponent<Rigidbody>().AddExplosionForce(100, stairPlayerPos[i], 10, 1, ForceMode.Impulse); //
            SetRagdollTempClientRpc(5, new ClientRpcParams() { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { playersAPunir[i] } } });
            AudioManager.instance.PlayOneShotClipServerRpc(playerGo.transform.position, AudioManager.SoundEffectOneShot.NUHUH);
            Rigidbody[] ragdollElems = playerGo.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

            foreach (Rigidbody ragdoll in ragdollElems)
            {
                ragdoll.AddExplosionForce(500, stairPlayerPos[i], 10, 1, ForceMode.Force);
            }
        }
    }


    /// <summary>
    /// A cause d'un bug qui fait que les leaves des prefabs ont les memes globalobjectid, on supprime les anciens escaliers avant de quitter la scene pr qu'il soient bien generés
    /// </summary>
    private void DestroyStairs()
    {
        ResetEscaliersGoClientRpc();
        GameObject[] escaliersUp = GameObject.FindGameObjectsWithTag("UpStairs");
        GameObject[] escaliersDown = GameObject.FindGameObjectsWithTag("DownStairs");
        foreach (GameObject escalier in escaliersUp)
        {
            escalier.GetComponent<NetworkObject>().Despawn();
        }
        foreach (GameObject escalier in escaliersDown)
        {
            escalier.GetComponent<NetworkObject>().Despawn();
        }
    }

    /// <summary>
    /// Reset les escaliers pour les clients
    /// </summary>
    [ClientRpc]
    private void ResetEscaliersGoClientRpc()
    {
        escaliersGo = null;
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

    #region Debug

    /// <summary>
    /// Demande au serveur de teleporter tous les players vers la position d'un joueur
    /// </summary>
    /// <param name="playerDest">L'id du player sur lequel on veut tp tt le monde</param>
    [ServerRpc(RequireOwnership = false)]
    public void TeleportAllServerRpc(ulong playerDest)
    {
        //Pr essayer on tp tt le monde sur le joueur actuel direct depûis le serveur
        int playerIndex = Array.IndexOf(playersIds, playerDest);
        TeleportAllClientRpc(playersGo[playerIndex].transform.position);

    }

    /// <summary>
    /// Teleporte tous les clients vers la position
    /// </summary>
    /// <param name="pos">Position ou on veut tous les clients</param>
    [ClientRpc]
    public void TeleportAllClientRpc(Vector3 pos)
    {
        MonPlayerController.instanceLocale.transform.position = pos;
    }

    /// <summary>
    /// Fait apparaitre un objet a ramasser a l'endroit donné
    /// </summary>
    /// <param name="position">L'endroit ou on veut spawn l'objet</param>
    [ServerRpc(RequireOwnership = false)]
    public void SummonTresorServerRpc(Vector3 position)
    {
        GameObject objet = Resources.Load<GameObject>("Donjon/Items/Treasures/GoldenIdol");
        GameObject instance = Instantiate(objet, position, Quaternion.identity);
        instance.GetComponent<TreasureObject>().value = 1;
        instance.GetComponent<NetworkObject>().Spawn();
    }

    #endregion

    /// <summary>
    /// Verifie si les joueurs connectés
    /// </summary>
    public void PrintConnectedClients()
    {
        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        foreach (var client in connectedClients)
        {
            Debug.Log("Client ID: " + client.ClientId);
        }
    }

    /// <summary>
    /// Dit a un joueur qu'il a perdu un item (Le nul)
    /// </summary>
    /// <param name="clientRpcParams">Les params pr le client qui a perdu son item</param>
    [ClientRpc]
    public void SendItemLostClientRpc(ClientRpcParams clientRpcParams)
    {
        StatsManager.Instance.AddItemLost();
    }

    [ClientRpc]
    public void SendStairLeaveDataClientRpc(NetworkObjectReference objRef, bool isUpStairs)
    {
        GameObject leave = (GameObject)objRef;
        leave.name = "Leave" + (isUpStairs ? "Up" : "Down") + leave.transform.position.x + "_" + leave.transform.position.z;
        leave.tag = isUpStairs ? "UpStairs" : "DownStairs";
    }

    [ServerRpc]
    public void TpSpawnServerRpc(ulong clientId)
    {
        TpSpawnClientRpc(SendRpcToPlayer(clientId));
    }

    [ClientRpc]
    private void TpSpawnClientRpc(ClientRpcParams cRpcParams)
    {
        MonPlayerController.instanceLocale.TpSpawn();
    }



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
