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
    public int maxEtage = 5;

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

    public int nbStairs = 1;

    /// <summary>
    /// Nombre de chaudrons d'alchimie
    /// </summary>
    private int nbCauldrons = 1;

    public int baseDifficulty = 1;
    public int difficultyScaling = 1;
    private int currentDifficulty;
    #endregion

    #region Info Deplacement

    /// <summary>
    /// Etage actuel va de 1 a maxEtage
    /// </summary>
    public int currentEtage = 1;

    /// <summary>
    /// Liste des seeds des etages déjà visité
    /// </summary>
    private int[] seeds;

    [SerializeField]
    private NetworkVariable<int> seed = new();

    private int tempSeed;

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
        if (instance != null)
        {
            instance.OnSceneLoaded();
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            seed.OnValueChanged += OnSeedValueChanged;
            DontDestroyOnLoad(this);
        }
    }

    private void Start()
    {

        OnSceneLoaded();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            seed.Value = tempSeed;
        }
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

        currentDifficulty = baseDifficulty + (currentEtage - 1) * difficultyScaling;

        if (maxEtageReached < currentEtage) //Si c'est un nouvel etage
        {
            if (maxEtageReached == 0)
            {
                //Premier etage
                Configure(MultiplayerGameManager.Instance.conf);
            }
            else
            {
                RandomizeSeed();
            }

            seeds[currentEtage - 1] = seed.Value;
            maxEtageReached = currentEtage;
            Generate(true);
        }
        else
        {
            if (MultiplayerGameManager.Instance.IsServer)
            {
                seed.Value = seeds[currentEtage - 1];
            }
            Generate(false);
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

    /// <summary>
    /// Quand la valeur du seed change on reseed le random pour toujours avoir les mêmes niveaux   
    /// </summary>
    private void OnSeedValueChanged(int previous, int current)
    {
        Random.InitState(current);
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
    /// </summary>
    /// <param name="conf">La config de donjon qui parametre le donjon</param>
    private void Configure(ConfigDonjon conf)
    {
        maxEtage = conf.nbEtages;
        tempSeed = conf.seed;
        minTailleEtage = conf.minTailleEtage;
        maxTailleEtage = conf.maxTailleEtage;
        nbStairs = conf.nbStairs;
        nbCauldrons = conf.nbChaudrons;
        typeEtage = conf.typeEtage;
        baseDifficulty = conf.baseDiff;
        difficultyScaling = conf.diffScaling;
        currentEtage = 1;
        maxEtageReached = 0;
        seeds = new int[maxEtage];
        Random.InitState(tempSeed);
    }

    /// <summary>
    /// Genere un etage, si il est nouveau et qu'on est sur le serveur on genere les items
    /// </summary>
    /// <param name="isNewEtage">Si l'étage est nouveau ou non</param>
    public void Generate(bool isNewEtage)
    {
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
        seed.Value = Random.Range(0, 1000000);
    }

    public int GetSeed()
    {
        return seed.Value;
    }
}