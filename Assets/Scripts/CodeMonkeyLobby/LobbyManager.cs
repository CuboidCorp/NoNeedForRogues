using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private GameObject lobbyWindow;

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_START_GAME = "StartGame";


    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyGameModeChanged;
    public event EventHandler<EventArgs> OnGameStarted;

    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }


    public enum GameMode
    {
        Coop,
        NYI
    }

    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName;
    private int nbPlayers;

    private GameObject audioManager;

    private void Awake()
    {
        Instance = this;
        audioManager = Resources.Load<GameObject>("AudioManager");

    }

    /// <summary>
    /// Au start on recupère le nom du joueur
    /// </summary>
    private void Start()
    {
        //On recupère l'objet DataHolder
        DataHolder dataHolder = FindObjectOfType<DataHolder>();
        if (dataHolder != null)
        {
            //On récupère le nom du joueur
            playerName = dataHolder.PlayerInfo.playerName;
        }
        else
        {
            //Pas de dataHolder -> Mode solo 
            //--> Debug only en théorie
            SoloMode();
        }
        playerNameText.text = playerName;
    }

    /// <summary>
    /// Lance le mode solo
    /// </summary>
    private void SoloMode()
    {
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 7777);
        MultiplayerGameManager.Instance.soloMode = true;
        MultiplayerGameManager.Instance.SetDataSolo();
        Instantiate(audioManager);
        OnGameStarted?.Invoke(this, EventArgs.Empty);
        NetworkManager.Singleton.OnClientConnectedCallback += MultiplayerGameManager.Instance.OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += MultiplayerGameManager.Instance.OnClientDisconnected;
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += MultiplayerGameManager.Instance.OnSceneLoadComplete;
        Destroy(lobbyWindow);
        Destroy(gameObject);
    }

    private void Update()
    {
#if !UNITY_EDITOR
        HandleRefreshLobbyList();
#endif
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    #region Handlers Refresh Data

    /// <summary>
    /// Permet de refresh la liste des lobby que les 5s, si on est connecté
    /// </summary>
    private void HandleRefreshLobbyList()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f)
            {
                float refreshLobbyListTimerMax = 5f;
                refreshLobbyListTimer = refreshLobbyListTimerMax;

                RefreshLobbyList();
            }
        }
    }

    /// <summary>
    /// Permet de gérer le heartbeat du lobby toutes les 15s, en tant que host
    /// </summary>
    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    /// <summary>
    /// Permet de gèrer la mise à jour des données du lobby toutes les 1s si on a rejoint un lobby
    /// </summary>
    private async void HandleLobbyPolling()
    {
        if (joinedLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });

                    joinedLobby = null;
                }
                else if (joinedLobby.Data[KEY_START_GAME].Value != "0")
                {
                    if (!IsLobbyHost())
                    {
                        //On setup avant de rejoindre le relay
                        MultiplayerGameManager.Instance.SetNbPlayersLobby(joinedLobby.MaxPlayers, GetAllPlayerNames());
                        RelayManager.Instance.JoinRelay(joinedLobby.Data[KEY_START_GAME].Value);
                    }

                    joinedLobby = null;
                    OnGameStarted.Invoke(this, EventArgs.Empty); //RECUP le nombre 
                }
            }
        }
    }

    #endregion

    #region Utils

    /// <summary>
    /// Retourne le lobby actuel
    /// </summary>
    /// <returns>Le lobby que l'on a rejoint</returns>
    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }

    /// <summary>
    /// Retourne si le joueur est l'hôte du lobby
    /// </summary>
    /// <returns>True si on est l'hote du lobby, false si il n'y a pas de lobby ou si on n'en est pas l'hote</returns>
    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    /// <summary>
    /// Retourne si le joueur est dans un lobby (Pr verif si on se fait kick ou que le lobby est supprimé)
    /// </summary>
    /// <returns>True si le joueur est bien dans le lobby false sinon</returns>
    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    // This player is in this lobby
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Cree un objet Player avec les données du joueur
    /// </summary>
    /// <returns>L'objet player correspondant au joueur actuel</returns>
    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
        });
    }

    private string[] GetAllPlayerNames()
    {
        string[] playerNames = new string[joinedLobby.MaxPlayers];
        for (int i = 0; i < joinedLobby.MaxPlayers; i++)
        {
            playerNames[i] = joinedLobby.Players[i].Data[KEY_PLAYER_NAME].Value;
        }
        return playerNames;
    }
    #endregion

    /// <summary>
    /// Permet de changer le mode de jeu du lobby
    /// </summary>
    public void ChangeGameMode()
    {
        if (IsLobbyHost())
        {
            GameMode gameMode =
                Enum.Parse<GameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            gameMode = gameMode switch
            {
                GameMode.NYI => GameMode.Coop,
                _ => GameMode.NYI,
            };
            UpdateLobbyGameMode(gameMode);
        }
    }

    /// <summary>
    /// Crée un lobby avec les paramètres donnés
    /// </summary>
    /// <param name="lobbyName">Le nom du lobby</param>
    /// <param name="maxPlayers">Le nombre maximum de joueur</param>
    /// <param name="isPrivate">Si le lobby est privé ou non</param>
    /// <param name="gameMode">Le mode de jeu du lobby</param>
    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, GameMode gameMode)
    {
        Player player = GetPlayer();

        nbPlayers = maxPlayers;

        CreateLobbyOptions options = new()
        {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member,"0") }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    /// <summary>
    /// Rafraichit la liste des lobbies disponibles
    /// </summary>
    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new()
            {
                Count = 25,

                // Filter for open lobbies only
                Filters = new List<QueryFilter> {
                new(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
                },

                // Order by newest lobbies first
                Order = new List<QueryOrder> {
                new(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse lobbyListQueryResponse = await Lobbies.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Permet de rejoindre un lobby en utilisant un code
    /// </summary>
    /// <param name="lobbyCode">Le code du lobby a rejoindre</param>
    public async void JoinLobbyByCode(string lobbyCode)
    {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
        {
            Player = player
        });

        joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    /// <summary>
    /// Permet de rejoindre un lobby spécifique
    /// </summary>
    /// <param name="lobby">Le lobby à rejoindre en question</param>
    public async void JoinLobby(Lobby lobby)
    {
        Player player = GetPlayer();

        joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    /// <summary>
    /// Permet de mettre à jour le nom du joueur
    /// </summary>
    /// <param name="playerName">Le noveau nom du joueur</param>
    public async void UpdatePlayerName(string playerName)
    {
        this.playerName = playerName;

        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new()
                {
                    Data = new Dictionary<string, PlayerDataObject>() {
                    {
                        KEY_PLAYER_NAME, new PlayerDataObject(
                            visibility: PlayerDataObject.VisibilityOptions.Public,
                            value: playerName)
                    }
                }
                };

                string playerId = AuthenticationService.Instance.PlayerId;

                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                joinedLobby = lobby;

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    /// <summary>
    /// Permet de rejoindre un lobby aléatoire
    /// </summary>
    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Permet de quitter le lobby actuel
    /// </summary>
    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    /// <summary>
    /// Permet de kick un joueur du lobby
    /// </summary>
    /// <param name="playerId">L'id du joueur à kick</param>
    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    /// <summary>
    /// Permet de mettre à jour le mode de jeu du lobby
    /// </summary>
    /// <param name="gameMode">Le nouveau mode de jeu du lobby</param>
    public async void UpdateLobbyGameMode(GameMode gameMode)
    {
        try
        {
            Debug.Log("UpdateLobbyGameMode " + gameMode);

            Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;

            OnLobbyGameModeChanged?.Invoke(this, new LobbyEventArgs { lobby = joinedLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    /// <summary>
    /// Start le jeu
    /// </summary>
    public async void StartGame()
    {
        if (IsLobbyHost())
        {
            try
            {
                LobbyUI.Instance.enabled = false;

                if (nbPlayers == 1)
                {
                    SoloMode();
                    return;
                }
                MultiplayerGameManager.Instance.SetNbPlayersLobby(joinedLobby.MaxPlayers, GetAllPlayerNames());

                string relayCode = await RelayManager.Instance.CreateRelay(nbPlayers);
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += MultiplayerGameManager.Instance.OnSceneLoadComplete;
                Lobby lobby = await Lobbies.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                    }
                });

                joinedLobby = lobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }


}