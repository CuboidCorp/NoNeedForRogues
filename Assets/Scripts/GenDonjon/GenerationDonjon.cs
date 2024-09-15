using UnityEngine;
using Donnees;
using Unity.Netcode;

public class GenerationDonjon : NetworkBehaviour
{
    #region Params Donjon
    [Header("Params Donjon")]
    /// <summary>
    /// Le nombre d'etage du donjon
    /// </summary>
    private int maxEtage = 5;

    /// <summary>
    /// Dernier etage atteint, permet de ne pas regenerer des items quand on revient dans un etage déjà atteint
    /// </summary>
    private int maxEtageReached = 0;

    [SerializeField]
    private float cellSize = 1;

    #endregion

    #region Params Etage
    [Header("Params Etage")]
    [SerializeField]
    private TypeEtage typeEtage;

    [SerializeField]
    private Vector2Int minTailleEtage;
    [SerializeField]
    private Vector2Int maxTailleEtage;

    private int nbStairs = 1;

    /// <summary>
    /// Nombre de chaudrons d'alchimie
    /// </summary>
    private int nbCauldrons = 1;

    private int baseDifficulty = 1;
    private int difficultyScaling = 1;
    private int currentDifficulty;
    #endregion

    #region Info Deplacement

    /// <summary>
    /// Etage actuel va de 1 a maxEtage
    /// </summary>
    private int currentEtage = 1;


    [SerializeField]
    private int seed;

    #endregion

    #region PrefabsPaths
    [Header("Prefabs")]
    [SerializeField]
    private string pathToRooms;

    [SerializeField]
    private string pathToHallways;

    [SerializeField]
    private string pathToStairs;

    [SerializeField]
    private string pathToPieces;

    [SerializeField]
    private string pathToObjets;

    [SerializeField]
    private string pathToPotions;

    [SerializeField]
    private string pathToChests;

    [SerializeField]
    private string pathToPieges;

    [SerializeField]
    private string pathToTrickshots;

    #endregion

    #region Holders

    [Header("Transform holders")]
    private Transform holderRooms;

    private Transform holderHallways;

    private Transform holderStairs;

    private Transform holderItems;

    private Transform holderTraps;

    private Transform holderTriggers;

    private Transform holderTrickshots;


    #endregion

    public static GenerationDonjon instance;
    private GenerationEtage genEtage;

    void Awake()
    {
        instance = this;
    }

    private void Start()
    {

        OnSceneLoaded();
    }

    /// <summary>
    /// Appelé quand la scene est chargée
    /// </summary>
    private void OnSceneLoaded()
    {
        GameObject holder = GameObject.Find("Dungeon");
        holderStairs = holder.transform.GetChild(0);
        holderHallways = holder.transform.GetChild(1);
        holderRooms = holder.transform.GetChild(2);
        holderItems = holder.transform.GetChild(3);
        holderTraps = holder.transform.GetChild(4);
        holderTriggers = holder.transform.GetChild(5);
        holderTrickshots = holder.transform.GetChild(6);

        if (maxEtageReached < currentEtage) //Si c'est un nouvel etage
        {
            if (maxEtageReached != 0)
            {
                RandomizeSeed();
            }
            if (MultiplayerGameManager.Instance.IsServer)
            {
                MultiplayerGameManager.Instance.seeds[currentEtage - 1] = seed;
                MultiplayerGameManager.Instance.conf.maxEtageReached = currentEtage;
                SendGenerationClientRpc(MultiplayerGameManager.Instance.conf, seed, true);
            }

        }
        else
        {
            if (MultiplayerGameManager.Instance.IsServer)
            {
                SendGenerationClientRpc(MultiplayerGameManager.Instance.conf, MultiplayerGameManager.Instance.seeds[currentEtage - 1], false);
            }
        }

        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            Destroy(cam);
        }

        if (MultiplayerGameManager.Instance.IsServer)
        {
            MultiplayerGameManager.Instance.SpawnPlayers();
        }
    }

    [ClientRpc]
    private void SendGenerationClientRpc(ConfigDonjon conf, int seed, bool isNewEtage)
    {
        this.seed = seed;
        Random.InitState(seed);
        Configure(conf);
        currentDifficulty = baseDifficulty + (currentEtage - 1) * difficultyScaling;
        Generate(isNewEtage);
    }

    /// <summary>
    /// Despawn tous les items de l'étage
    /// </summary>
    public void DespawnItems()
    {
        genEtage.DespawnObjects();
    }

    /// <summary>
    /// Configure le dojon en se basant sur une config de donjon
    /// Et sur les infos stockés dans le multiplayerGameManager
    /// </summary>
    /// <param name="conf">La config de donjon qui parametre le donjon</param>
    private void Configure(ConfigDonjon conf)
    {
        Debug.Log(conf);
        maxEtage = conf.nbEtages;
        seed = MultiplayerGameManager.Instance.conf.currentSeed;
        minTailleEtage = conf.minTailleEtage;
        maxTailleEtage = conf.maxTailleEtage;
        nbStairs = conf.nbStairs;
        nbCauldrons = conf.nbChaudrons;
        typeEtage = conf.typeEtage;
        baseDifficulty = conf.baseDiff;
        difficultyScaling = conf.diffScaling;
        currentEtage = MultiplayerGameManager.Instance.conf.currentEtage;
        maxEtageReached = MultiplayerGameManager.Instance.conf.maxEtageReached;
        Random.InitState(seed);
    }

    /// <summary>
    /// Genere un etage, si il est nouveau et qu'on est sur le serveur on genere les items
    /// </summary>
    /// <param name="isNewEtage">Si l'étage est nouveau ou non</param>
    public void Generate(bool isNewEtage)
    {
        Debug.Log("Generation");
        switch (typeEtage)
        {
            case TypeEtage.Labyrinthe:
                genEtage = GetComponent<GenEtaLaby>();
                break;
            case TypeEtage.Salles:
                genEtage = GetComponent<GenEtaSalles>();
                break;
            case TypeEtage.Arbre:
                genEtage = GetComponent<GenEtaAbre>();
                break;
        }
        genEtage.Initialize(new Vector2Int(Random.Range(minTailleEtage.x, maxTailleEtage.x), Random.Range(minTailleEtage.y, maxTailleEtage.y)), nbStairs, cellSize, currentDifficulty, MultiplayerGameManager.Instance.IsServer, nbCauldrons);
        genEtage.ChargePrefabs(pathToRooms, pathToHallways, pathToStairs, pathToPieces, pathToObjets, pathToPotions, pathToChests, pathToPieges, pathToTrickshots);
        genEtage.ChargeHolders(holderRooms, holderHallways, holderStairs, holderItems, holderTraps, holderTriggers, holderTrickshots);
        genEtage.GenerateEtage();
        genEtage.GeneratePieges();
        if (isNewEtage && MultiplayerGameManager.Instance.IsServer)
        {
            genEtage.GenerateItems();
        }

    }

    /// <summary>
    /// Randomize le seed
    /// </summary>
    public void RandomizeSeed()
    {
        seed = Random.Range(0, 1000000);
        MultiplayerGameManager.Instance.conf.currentSeed = seed;
    }

    public int GetSeed()
    {
        return seed;
    }
}