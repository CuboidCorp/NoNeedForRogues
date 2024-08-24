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
/// G�re le mode multijoueur
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

    #region Donn�es Joueurs
    /// <summary>
    /// Les ids des joueurs selon Netcode pr gameobject
    /// </summary>
    private ulong[] playersIds;

    private PlayerState[] playersStates;

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

    public ConfigDonjon conf;

    #region Prefabs
    private GameObject copyCamPrefab;
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
        conf = new();
    }

    private void LoadPrefabs()
    {
        mainMixer = Resources.Load<AudioMixer>("Audio/Main");
        copyCamPrefab = Resources.Load<GameObject>("Perso/CopyCam");
        lightBall = Resources.Load<GameObject>("Sorts/LightBall");
        fireBall = Resources.Load<GameObject>("Sorts/FireBall");
        resurectio = Resources.Load<GameObject>("Sorts/ResurectioProjectile");
        healProj = Resources.Load<GameObject>("Sorts/HealProjectile");
        speedProj = Resources.Load<GameObject>("Sorts/AccelProjectile");
        fusrohdahProj = Resources.Load<GameObject>("Sorts/FusRoDahProjectile");
        explosionPrefab = Resources.Load<GameObject>("Sorts/Explosion");
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
    public void SetNbPlayersLobby(int nb, string[] playerNames) //TODO : L'envoyer a tt le monde
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
    /// Set les donn�es du joueur en solo
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

    #region Utilitaires

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
    /// Despawn un objet donn�e apr�s une certaine periode de temps
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
        GameObject[] playersTemp = GameObject.FindGameObjectsWithTag("Temp");
        if (nbConnectedPlayers < (int)id) //On recup les anciens joueurs et l'actuel
        {
            foreach (GameObject playerTemp in playersTemp)
            {
                if (!playersIds.Contains(playerTemp.GetComponent<NetworkObject>().OwnerClientId))
                {
                    //Si on l'a pas encore enregistr� on le fait
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
            error.GetComponent<ErrorHandler>().message = "Vous avez �t� d�connect�";
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
    /// Et le connecte au joueur concern�
    /// </summary>
    /// <param name="authId">L'id du joueur connect�</param>
    /// <param name="vivox">Le participant associ� au vivox particpant</param>
    /// <returns>L'index du joueur dans les arrays</returns>
    public int AddPlayerVivoxInfo(string authId, VivoxParticipant vivox)
    {
        int playerIndex = Array.IndexOf(playersAuthId, authId);
        participants[playerIndex] = vivox;
        return playerIndex;
    }

    /// <summary>
    /// Renvoie le transform du joueur correspondant � l'id authentification (Ne marche que si le joueur est vivant)
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
        participants[playerIndex].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
        GameObject tap = participants[playerIndex].CreateVivoxParticipantTap("Tap " + playerIndex);
        tap.transform.SetParent(GetGhostTransformFromPlayerId(playerId));
        tap.transform.localPosition = new Vector3(0, 1.6f, 0);
        AddParamToParticipantAudioSource(playerIndex);
    }

    /// <summary>
    /// Recup�re la participant tap d'un joueur et le met sur sa vache
    /// </summary>
    /// <param name="playerId">Id du joueur</param>
    public void MovePlayerTapToCow(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
        GameObject tap = participants[playerIndex].CreateVivoxParticipantTap("Tap " + playerIndex);
        tap.transform.SetParent(GetCowTransformFromPlayerId(playerId));
        tap.transform.localPosition = new Vector3(0, 1.6f, 0);
        AddParamToParticipantAudioSource(playerIndex);
    }

    public void MovePlayerTapToHuman(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].DestroyVivoxParticipantTap();//On enleve le tap pr le remettre apres
        GameObject tap = participants[playerIndex].CreateVivoxParticipantTap("Tap " + playerIndex);
        tap.transform.SetParent(GetPlayerTransformFromAuthId(playersAuthId[playerIndex]));
        tap.transform.localPosition = new Vector3(0, 1.6f, 0);
        AddParamToParticipantAudioSource(playerIndex);
    }

    /// <summary>
    /// Transforme le joueur en speedy(pr la voix)
    /// </summary>
    /// <param name="playerId">Le player concern�</param>
    public void SetSpeedyPlayerTap(ulong playerId)
    {
        int playerIndex = Array.IndexOf(playersIds, playerId);
        participants[playerIndex].ParticipantTapAudioSource.outputAudioMixerGroup = mainMixer.FindMatchingGroups("SpeedyVoice")[0];
    }

    /// <summary>
    /// Reset le player tap d'un joueur pour remettre la voix normale
    /// </summary>
    /// <param name="playerId">Le player concern�</param>
    public void ResetPlayerTap(ulong playerId)
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
            playersGo[playerIndex].GetComponent<MonPlayerController>().HandleDeath();
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
            playersGo[playerIndex].GetComponent<MonPlayerController>().HandleRespawn(); //Ca remet juste le tag a player
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
                playersGo[playerIndex].GetComponent<MonPlayerController>().EnableRagdoll(false);
            }
            else
            {
                playersGo[playerIndex].GetComponent<MonPlayerController>().DisableRagdoll(false);
            }

        }
    }

    /// <summary>
    /// Met le jouer du client en parametre en ragdoll pour un temps donn�
    /// </summary>
    /// <param name="time">Le temps pendant lequel le joueur est en ragdoll</param>
    /// <param name="client">Le client a ragdoll</param>
    [ClientRpc]
    public void SetRagdollTempClientRpc(float time, ClientRpcParams client)
    {
        StartCoroutine(MonPlayerController.instanceLocale.SetRagdollTemp(time));
    }

    #endregion

    #region ClientRpcParams

    /// <summary>
    /// Renvoie les client rpc params pour envoyer une id � tous les autres joueurs
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
            playersGo[playerIndex].GetComponent<MonPlayerController>().HandleRespawn();
            MovePlayerTapToHuman(id);
        }
    }

    /// <summary>
    /// Envoie les infos au joueur qu'il a re�u un speed boost
    /// </summary>
    /// <param name="duree">Dur�e du buff</param>
    /// <param name="clientRpcParams">L'id du client a sauvegard�</param>
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
    /// Synchronise la config du donjon entre l'hote et les autres clients
    /// </summary>
    /// <param name="conf">La nouvelle configuration du donjon</param>
    [ClientRpc]
    public void SyncConfigDonjonClientRpc(ConfigDonjon conf)
    {
        ConfigDonjonUI.Instance.SetConf(conf);
        this.conf = conf;
    }

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
                    player.transform.position = escaliers[i].GetComponent<Escalier>().spawnPoint.position;
                }
            }
        }
    }


    /// <summary>
    /// Envoie au serveur l'information que le joueur est pr�t
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
            //On start la coroutine sur tous les escaliers qui vont du meme cot� que les joueurs
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
            Debug.Log("Change level going " + (direction ? "up" : "down") + " : Current etage : " + GenerationDonjon.instance.currentEtage);
            playerRepartitionByStairs = new ulong[escaliersGo.Length][];
            for (int i = 0; i < escaliersGo.Length; i++)
            {
                playerRepartitionByStairs[i] = escaliersGo[i].GetComponent<Escalier>().GetPlayers();
            }
            //On v�rifie en fonction du g�n�ration donjon le current level
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

                if (GenerationDonjon.instance.currentEtage == 1) //On ne part pas
                {
                    TrollCowardPlayer();
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
        //On sauvegarde par escalier l� ou sont les gens
        DestroyStairs();
        NetworkManager.SceneManager.LoadScene("Donjon", LoadSceneMode.Single);

    }

    private void LeaveLobby()
    {
        AudioManager.instance.StopMusic();
        AudioManager.instance.SetMusic(AudioManager.Musique.DONJON);
        AudioManager.instance.ActivateMusic();

        playerRepartitionByStairs = new ulong[1][];
        playerRepartitionByStairs[0] = playersIds;
        //On recup les settings du donjon
        isInLobby = false;

        //On suppose que tous les clients font ces lignes
        MonPlayerController.instanceLocale.FullHeal();
        MonPlayerController.instanceLocale.FullMana();

        StatsManager.Instance.dateDebutGame = DateTime.Now;
        StatsManager.Instance.InitializeGame(MonPlayerController.instanceLocale.OwnerClientId);

    }

    private void TrollCowardPlayer()
    {
        Debug.Log("On ne peut pas fuir comme �a mec");
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

        for (int i = 0; i < playersAPunir.Count; i++)
        {
            GameObject playerGo = GetPlayerById(playersAPunir[i]);
            //Comment punir le joueur -> Ragdoll
            playerGo.GetComponent<Rigidbody>().AddExplosionForce(100, stairPlayerPos[i], 10, 1, ForceMode.Impulse);
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
    /// A cause d'un bug qui fait que les leaves des prefabs ont les memes globalobjectid, on supprime les anciens escaliers avant de quitter la scene pr qu'il soient bien gener�s
    /// </summary>
    private void DestroyStairs()
    {
        escaliersGo = null;
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
    /// V�rifie si les joueurs sont prets
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
    /// Verifie si tous les joueurs vont du meme cot�
    /// </summary>
    /// <returns>True si ils vont du meme cot�, false sinon</returns>
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
    /// Les �tats possibles d'un joueur (Notamment pr les voice taps)
    /// </summary>
    private enum PlayerState
    {
        Alive,
        Dead,
        Speedy
    }
}
