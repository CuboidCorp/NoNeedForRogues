using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class EndGameManager : MonoBehaviour
{
    [Header("Player Stats")]
    [SerializeField] private GameObject playerStatsCanvaPrefab;
    [SerializeField] private Transform playerStatsHolder;
    [SerializeField] private Transform[] playerStatsPosition;
    private Dictionary<ulong, List<string>> playerTitles;

    [Header("Score")]
    [SerializeField] private GameObject scoreCanva;
    [SerializeField] private GameObject detailScoreCanva;

    public static EndGameManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        //Tous les joueurs envoient leurs stats
        StatsManager.Instance.SendStatsServerRpc(StatsManager.Instance.localPlayerStats, StatsManager.Instance.localPlayerId);
        if (MultiplayerGameManager.Instance.IsServer)
        {
            MultiplayerGameManager.Instance.SetSpawnAllPlayers(transform.position);
        }
        AudioManager.instance.SetMusic(AudioManager.Musique.END);
    }

    #region Player Stats

    /// <summary>
    /// Calcule les titres de chaque joueur
    /// </summary>
    public void CalculTitres()
    {
        playerTitles = new Dictionary<ulong, List<string>>();
        int maxGold = -1;
        int maxJumps = -1;
        int maxSpells = -1;
        float maxDamage = -1;
        int maxPotions = -1;
        int maxDeaths = -1;
        int maxTrickshots = -1;
        int maxItemsLost = -1;
        ulong[] maxHolders = new ulong[8];
        foreach (KeyValuePair<ulong, PlayerStats> playStats in StatsManager.Instance.allStatsHolder)
        {
            PlayerStats stats = playStats.Value;
            if (stats.nbGoldCollected > maxGold)
            {
                maxGold = stats.nbGoldCollected;
                maxHolders[0] = playStats.Key;
            }
            if (stats.nbJumps > maxJumps)
            {
                maxJumps = stats.nbJumps;
                maxHolders[1] = playStats.Key;
            }
            if (stats.spellsCasted > maxSpells)
            {
                maxSpells = stats.spellsCasted;
                maxHolders[2] = playStats.Key;
            }
            if (stats.damageTaken > maxDamage)
            {
                maxDamage = stats.damageTaken;
                maxHolders[3] = playStats.Key;
            }
            if (stats.nbPotionDrank > maxPotions)
            {
                maxPotions = stats.nbPotionDrank;
                maxHolders[4] = playStats.Key;
            }
            if (stats.nbMorts > maxDeaths)
            {
                maxDeaths = stats.nbMorts;
                maxHolders[5] = playStats.Key;
            }
            if (stats.nbTrickshots > maxTrickshots)
            {
                maxTrickshots = stats.nbTrickshots;
                maxHolders[6] = playStats.Key;
            }
            if (stats.nbItemsLost > maxItemsLost)
            {
                maxItemsLost = stats.nbItemsLost;
                maxHolders[7] = playStats.Key;
            }
        }
        if (maxGold > 0)
        {
            playerTitles.Add(maxHolders[0], new List<string> { "L'avare" });
        }
        if (maxJumps > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[1]))
            {
                playerTitles[maxHolders[1]].Add("Le sauteur");
            }
            else
            {
                playerTitles.Add(maxHolders[1], new List<string> { "Le sauteur" });
            }
        }
        if (maxSpells > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[2]))
            {
                playerTitles[maxHolders[2]].Add("Le mage");
            }
            else
            {
                playerTitles.Add(maxHolders[2], new List<string> { "Le mage" });
            }
        }

        if (maxDamage > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[3]))
            {
                playerTitles[maxHolders[3]].Add("Le tank");
            }
            else
            {
                playerTitles.Add(maxHolders[3], new List<string> { "Le tank" });
            }
        }

        if (maxPotions > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[4]))
            {
                playerTitles[maxHolders[4]].Add("L'alcoolique");
            }
            else
            {
                playerTitles.Add(maxHolders[4], new List<string> { "L'alcoolique" });
            }
        }


        if (maxDeaths > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[5]))
            {
                playerTitles[maxHolders[5]].Add("Le suicidaire");
            }
            else
            {
                playerTitles.Add(maxHolders[5], new List<string> { "Le suicidaire" });
            }
        }


        if (maxTrickshots > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[6]))
            {
                playerTitles[maxHolders[6]].Add("Le trickshoteur");
            }
            else
            {
                playerTitles.Add(maxHolders[6], new List<string> { "Le trickshoteur" });
            }
        }

        if (maxItemsLost > 0)
        {
            if (playerTitles.ContainsKey(maxHolders[7]))
            {
                playerTitles[maxHolders[7]].Add("Le maladroit");
            }
            else
            {
                playerTitles.Add(maxHolders[7], new List<string> { "Le maladroit" });
            }
        }
    }

    /// <summary>
    /// Affiche les stats de tous les joueurs
    /// </summary>
    public void DisplayAllPlayerStats()
    {
        int cpt = 0;
        foreach (KeyValuePair<ulong, PlayerStats> playStats in StatsManager.Instance.allStatsHolder)
        {
            string playerName = MultiplayerGameManager.Instance.GetPlayerName(playStats.Key);
            Vector3 position = playerStatsPosition[cpt].position;
            Vector3 rotation = playerStatsPosition[cpt].rotation.eulerAngles;
            DisplayPlayerStats(playStats.Key, playStats.Value, playerName, position, rotation);
            cpt++;
        }
    }

    /// <summary>
    /// Affiche les stats d'un joueur à une position donnée
    /// </summary>
    /// <param name="playerId">L'id du joueur</param>
    /// <param name="stats">Les stats a afficher</param>
    /// <param name="playerName">Le nom du joueur</param>
    /// <param name="position">Position du truc</param>
    /// <param name="rotation">Rotation du truc</param>
    private void DisplayPlayerStats(ulong playerId, PlayerStats stats, string playerName, Vector3 position, Vector3 rotation)
    {
        GameObject playerStat = Instantiate(playerStatsCanvaPrefab, playerStatsHolder);
        playerStat.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
        playerStat.transform.GetChild(0).GetComponent<TMP_Text>().text = playerName;
        if (playerTitles.ContainsKey(playerId))
        {
            playerStat.transform.GetChild(1).GetComponent<TMP_Text>().text = playerTitles[playerId][UnityEngine.Random.Range(0, playerTitles[playerId].Count)];
        }
        else
        {
            playerStat.transform.GetChild(1).GetComponent<TMP_Text>().text = "le Naze";
        }

        playerStat.transform.GetChild(2).GetComponent<TMP_Text>().text = "Or collecté : " + stats.nbGoldCollected;
        playerStat.transform.GetChild(3).GetComponent<TMP_Text>().text = "Sauts : " + stats.nbJumps;
        playerStat.transform.GetChild(4).GetComponent<TMP_Text>().text = "Sorts lancés : " + stats.spellsCasted;
        playerStat.transform.GetChild(5).GetComponent<TMP_Text>().text = "Dégâts pris : " + stats.damageTaken;
        playerStat.transform.GetChild(6).GetComponent<TMP_Text>().text = "Potions bues : " + stats.nbPotionDrank;
        playerStat.transform.GetChild(7).GetComponent<TMP_Text>().text = "Morts : " + stats.nbMorts;
        playerStat.transform.GetChild(8).GetComponent<TMP_Text>().text = "Trickshots : " + stats.nbTrickshots;
        playerStat.transform.GetChild(9).GetComponent<TMP_Text>().text = "Fail : " + stats.nbItemsLost;
    }

    #endregion

    #region Score
    /// <summary>
    /// Calcule le score sur le serv et l'envoie à tous les clients
    /// </summary>
    public void CalculScore()
    {
        DateTime dateFin = DateTime.Now;
        detailScoreCanva.SetActive(true);
        //Donc pr calc le score on recup le temps d'exploration, l'or recolté, le nombre de morts
        int score;
        int totalGold = StatsManager.Instance.totalGold.Value;
        detailScoreCanva.transform.GetChild(1).GetComponent<TMP_Text>().text = "Or collecté : " + totalGold;
        int totalDeaths = 0;
        int tempsExploEnSecondes;
        int nbEtages = MultiplayerGameManager.Instance.conf.nbEtages;
        int dernierEtageAtteint = MultiplayerGameManager.Instance.conf.currentEtage;

        int penaliteGameOver = 1000 * (nbEtages - dernierEtageAtteint);

        //Calcul nbMorts 
        foreach (KeyValuePair<ulong, PlayerStats> playStats in StatsManager.Instance.allStatsHolder)
        {
            totalDeaths += playStats.Value.nbMorts;
        }
        detailScoreCanva.transform.GetChild(2).GetComponent<TMP_Text>().text = "Morts : " + totalDeaths;

        detailScoreCanva.transform.GetChild(3).GetComponent<TMP_Text>().text = "Debut : " + StatsManager.Instance.dateDebutGame.ToString("HH:mm:ss");
        detailScoreCanva.transform.GetChild(4).GetComponent<TMP_Text>().text = "Fin : " + dateFin.ToString("HH:mm:ss");

        //Temps d'exploration moyen par etage
        TimeSpan tempsTotal = dateFin - StatsManager.Instance.dateDebutGame;
        tempsExploEnSecondes = (int)tempsTotal.TotalSeconds;

        detailScoreCanva.transform.GetChild(5).GetComponent<TMP_Text>().text = "Temps d'explo : " + tempsExploEnSecondes + " s";

        int tempsMoyenParEtage = tempsExploEnSecondes / nbEtages;
        //Le temps moyen par etage est comparé au temps moyen normalisé en fonction de la taille min et max et du nombre d'escaliers
        Vector2Int tailleMoyenne = (MultiplayerGameManager.Instance.conf.maxTailleEtage + MultiplayerGameManager.Instance.conf.minTailleEtage) / 2;
        detailScoreCanva.transform.GetChild(6).GetComponent<TMP_Text>().text = "Taille moy : " + tailleMoyenne.x + " x " + tailleMoyenne.y;
        int aireMoyenne = tailleMoyenne.x * tailleMoyenne.y;
        detailScoreCanva.transform.GetChild(7).GetComponent<TMP_Text>().text = "Aire moy : " + aireMoyenne;
        int tempsMoyenNormalise = aireMoyenne - 15 * MultiplayerGameManager.Instance.conf.nbStairs;
        detailScoreCanva.transform.GetChild(8).GetComponent<TMP_Text>().text = "Temps moyen : " + tempsMoyenParEtage + " s";
        detailScoreCanva.transform.GetChild(9).GetComponent<TMP_Text>().text = "Temps normalisé : " + tempsMoyenNormalise + " s";
        int diffTemps = tempsMoyenParEtage - tempsMoyenNormalise;

        detailScoreCanva.transform.GetChild(10).GetComponent<TMP_Text>().text = "Pénalité gameover : " + penaliteGameOver;


        score = totalGold * 10 - totalDeaths * 5 - diffTemps * 5 - penaliteGameOver;
        MultiplayerGameManager.Instance.SyncScoreClientRpc(score, MultiplayerGameManager.Instance.conf.seed);
    }

    public void AfficherScore(int score, int seedInit)
    {
        scoreCanva.transform.GetChild(1).GetComponent<TMP_Text>().text = "Score : " + score;
        scoreCanva.transform.GetChild(2).GetComponent<TMP_Text>().text = "Seed initial : " + seedInit;

        if (score < 0)
        {
            scoreCanva.transform.GetChild(3).GetComponent<TMP_Text>().text = "Looser";
        }
        else
        {
            scoreCanva.transform.GetChild(3).GetComponent<TMP_Text>().text = "Peut mieux faire ...";
        }
    }
    #endregion
}
