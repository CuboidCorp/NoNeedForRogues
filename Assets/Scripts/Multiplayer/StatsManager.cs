/// <summary>
/// Gère et stocke les stats de jeu de tous les joueurs
/// </summary>
public class StatsManager : NetworkBehaviour
{
    public static StatsManager Instance;

    public PlayerStats localPlayerStats;
    public ulong localPlayerId;

    public Dictionary<ulong, PlayerStats> allStatsHolder;

    private void Awake()
    {
        Instance = this;
    }

    private void OnNetworkSpawn()
    {
        if(IsServer)
        {
            allStatsHolder = new Dictionary<ulong, PlayerStats> ();
        }
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
        localPlayerStats.spellCasts++;
    }

    public void AddDamageTaken(float damageTaken)
    {
        localPlayerStats.damageTaken += damageTaken;
    }

    public void AddHealAmount(float healAmount)
    {
        localPlayerStats.healAmount += healAmount;
    }
    
    public void AddDistanceMoved(float distanceMoved)
    {
        localPlayerStats.distanceMoved += distanceMoved;
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
