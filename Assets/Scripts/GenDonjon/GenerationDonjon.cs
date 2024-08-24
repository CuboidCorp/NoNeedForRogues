using UnityEngine;
using Donnees;

public class GenerationDonjon : MonoBehaviour
{

    [Header("Params Donjon")]
    /// <summary>
    /// Etage actuel va de 1 a maxEtage
    /// </summary>
    public int currentEtage = 1;
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
    [SerializeField]
    private int seed;

    private bool estServeur = false;

    [Header("Params Etage")]
    [SerializeField]
    private TypeEtage typeEtage;

    [SerializeField]
    private Vector2Int minTailleEtage;
    [SerializeField]
    private Vector2Int maxTailleEtage;

    public int nbStairs = 1;

    public int baseDifficulty = 1;
    public int difficultyScaling = 1;
    private int currentDifficulty;

    /// <summary>
    /// Liste des seeds des etages déjà visité
    /// </summary>
    private int[] seeds;

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

    [Header("Transform holders")]
    private Transform holderRooms;

    private Transform holderHallways;

    private Transform holderStairs;

    private Transform holderItems;

    private GenerationEtage genEtage;

    public static GenerationDonjon instance;

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
            DontDestroyOnLoad(this);
        }
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

        estServeur = MultiplayerGameManager.Instance.IsServer;

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

            seeds[currentEtage - 1] = seed;
            SetSeed();
            maxEtageReached = currentEtage;
            Generate(true, estServeur);
        }
        else
        {
            seed = seeds[currentEtage - 1];
            SetSeed();
            Generate(false, estServeur);
        }

        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            Destroy(cam);
        }

        if (estServeur)
        {
            MultiplayerGameManager.Instance.SpawnPlayers();
        }
    }


    private void Configure(ConfigDonjon conf)
    {
        maxEtage = conf.nbEtages;
        seed = conf.seed;
        minTailleEtage = conf.minTailleEtage;
        maxTailleEtage = conf.maxTailleEtage;
        nbStairs = conf.nbStairs;
        typeEtage = conf.typeEtage;
        baseDifficulty = conf.baseDiff;
        difficultyScaling = conf.diffScaling;
        currentEtage = 1;
        maxEtageReached = 0;
        seeds = new int[maxEtage];
    }

    public void Generate(bool isNewEtage, bool estServ)
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
        genEtage.Initialize(new Vector2Int(Random.Range(minTailleEtage.x, maxTailleEtage.x), Random.Range(minTailleEtage.y, maxTailleEtage.y)), nbStairs, cellSize, currentDifficulty, estServ);
        genEtage.ChargePrefabs(pathToRooms, pathToHallways, pathToStairs, pathToPieces, pathToObjets, pathToPotions, pathToChests, pathToPieges);
        genEtage.ChargeHolders(holderRooms, holderHallways, holderStairs, holderItems);
        genEtage.GenerateEtage();
        if (isNewEtage && estServ)
        {
            genEtage.GenerateItems();
        }
        genEtage.GeneratePieges();
    }

    public void RandomizeSeed()
    {
        seed = Random.Range(0, 1000000);
    }

    public void SetSeed()
    {
        Random.InitState(seed);
    }
}