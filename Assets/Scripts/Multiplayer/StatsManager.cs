using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// G�re et stocke les stats de jeu de tous les joueurs
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
    
    /// <summary>
    /// Appel� quand la valeur de gold total est chang�
    /// Change l'ui du jeu pour afficher le nouveau gold
    /// </summary>
    /// <param name="previous">Ancienne valeur</param>
    /// <param name="current">Nouvelle valeur</param>
    private void OnGoldValueChanged(int previous, int current)
    {
        //TODO : Afficher une petite icone avec du texte vert/rouge en fonction de la diff�rence sur le compteur de gold total
        PlayerUIManager.Instance.SetGoldText(current);
    }

    #region Adding stats

    /// <summary>
    /// Ajoute de l'or au compteur commun et le stocke dans les stats du joueur local
    /// </summary>
    /// <param name="nbGold">Nombre de gold rammas�</param>
    public void AddGold(int nbGold)
    {
        localPlayerStats.nbGoldCollected += nbGold;
        totalGold.Value += nbGold;
    }

    /// <summary>
    /// Rajoute un jump aux stats du joueur local
    /// </summary>
    public void AddJump()
    {
        localPlayerStats.nbJumps++;
    }

    /// <summary>
    /// Rajoute un sort lanc� aux stats du joueur local
    /// </summary>
    public void AddSpellCast()
    {
        localPlayerStats.spellsCasted++;
    }

    /// <summary>
    /// Rajoute des degats pris aux stats du joueur local
    /// </summary>
    /// <param name="damageTaken">La quantit� de d�gats re�ue</param>
    public void AddDamageTaken(float damageTaken)
    {
        localPlayerStats.damageTaken += damageTaken;
    }

    /// <summary>
    /// Rajoute le nombre de pv soign�s aux stats du joueur local
    /// </summary>
    /// <param name="healAmount">Le nombre de pv soign�s</param>
    public void AddHealAmount(float healAmount)
    {
        localPlayerStats.healTaken += healAmount;
    }

    /// <summary>
    /// Rajoute une potion bue aux stats du joueur local
    /// </summary>
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
