using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// Gère et stocke les stats de jeu de tous les joueurs
/// </summary>
public class StatsManager : NetworkBehaviour
{
    public static StatsManager Instance;

    public PlayerStats localPlayerStats;
    public ulong localPlayerId;

    public Dictionary<ulong, PlayerStats> allStatsHolder;

    public NetworkVariable<int> totalGold = new NetworkVariable<int>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            allStatsHolder = new Dictionary<ulong, PlayerStats>();
            totalGold.Value = 0;
        }
        totalGold.OnValueChanged += OnGoldValueChanged;
    }

    /// <summary>
    /// Initialise ou reset les stats pour le joueur local
    /// </summary>
    /// <param name="playerId">Id du joueur</param>
    public void InitializeGame(ulong playerId)
    {
        localPlayerStats = new PlayerStats();
        localPlayerId = playerId;
    }
    
    private void OnGoldValueChanged(int previous, int current)
    {
        //TODO : Afficher une petite icone avec du texte vert/rouge en fonction de la différence sur le compteur de gold total
        PlayerUIManager.Instance.SetGoldText(current);
    }

    #region Adding stats

    public void AddGold(int nbGold)
    {
        localPlayerStats.nbGoldCollected += nbGold;
    }

    public void AddJump()
    {
        localPlayerStats.nbJumps++;
    }

    public void AddSpellCast()
    {
        localPlayerStats.spellsCasted++;
    }

    public void AddDamageTaken(float damageTaken)
    {
        localPlayerStats.damageTaken += damageTaken;
    }

    public void AddHealAmount(float healAmount)
    {
        localPlayerStats.healTaken += healAmount;
    }

    public void AddPotionDrank()
    {
        localPlayerStats.nbPotionDrank++;
    }

    #endregion

    /// <summary>
    /// Envoie les stats du joueur local au serveur
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SendStatsServerRpc(PlayerStats stats, ulong playerId)
    {
        allStatsHolder.Add(playerId, stats);
    }
}
