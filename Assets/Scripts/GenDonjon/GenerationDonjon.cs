using UnityEngine;
using Donnees;
using Unity.Netcode;

public class GenerationDonjon : NetworkBehaviour
{

    private int nbPlayersGenFinished = 0;

    #region Params Donjon
    [Header("Params Donjon")]

    [SerializeField]
    private float cellSize = 1;

    [SerializeField]
    private Vector3 loadingPos;
    #endregion

    #region Params Etage
    [Header("Params Etage")]
    private TypeEtage typeEtage;

    private Vector2Int minTailleEtage;
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
        GameObject holder = GameObject.Find("Dungeon");
        holderStairs = holder.transform.GetChild(0);
        holderHallways = holder.transform.GetChild(1);
        holderRooms = holder.transform.GetChild(2);
        holderItems = holder.transform.GetChild(3);
        holderTraps = holder.transform.GetChild(4);
        holderTriggers = holder.transform.GetChild(5);
        holderTrickshots = holder.transform.GetChild(6);
    }

    private void Start()
    {
        MonPlayerController.instanceLocale.transform.position = loadingPos;
        GameObject cam = GameObject.Find("Main Camera");
        if (cam != null)
        {
            Destroy(cam);
        }
    }

    /// <summary>
    /// Commence la generation sur le serveur
    /// </summary>
    public void StartGenerationServer()
    {
        nbPlayersGenFinished = 0;
        if (MultiplayerGameManager.Instance.conf.maxEtageReached < MultiplayerGameManager.Instance.conf.currentEtage) //Si c'est un nouvel etage
        {
            Debug.Log("Nouvel etage : " + MultiplayerGameManager.Instance.conf.currentEtage);
            if (MultiplayerGameManager.Instance.conf.maxEtageReached > 0)
            {
                Debug.Log("Randomize seed");
                RandomizeSeed();
            }
            MultiplayerGameManager.Instance.seeds[MultiplayerGameManager.Instance.conf.currentEtage - 1] = MultiplayerGameManager.Instance.conf.currentSeed;
            MultiplayerGameManager.Instance.conf.maxEtageReached = MultiplayerGameManager.Instance.conf.currentEtage;
            Debug.Log(MultiplayerGameManager.Instance.conf);
            SendGenerationClientRpc(MultiplayerGameManager.Instance.conf, true);
        }
        else
        {
            Debug.Log("Etage deja atteint");
            MultiplayerGameManager.Instance.conf.currentSeed = MultiplayerGameManager.Instance.seeds[MultiplayerGameManager.Instance.conf.currentEtage - 1];
            SendGenerationClientRpc(MultiplayerGameManager.Instance.conf, false);
        }
    }

    [ClientRpc]
    private void SendGenerationClientRpc(ConfigDonjon conf, bool isNewEtage)
    {
        Configure(conf);
        currentDifficulty = baseDifficulty + (currentEtage - 1) * difficultyScaling;
        Generate(isNewEtage);
        SendEndGenerationServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendEndGenerationServerRpc()
    {
        nbPlayersGenFinished++;
        if (nbPlayersGenFinished == MultiplayerGameManager.nbConnectedPlayers)
        {
            Debug.Log("Fin de la generation");
            MultiplayerGameManager.Instance.SpawnPlayers();
            GameObject portail = Instantiate(Resources.Load<GameObject>("Objets/Portail"), new Vector3(0, 22.4f, 0), Quaternion.identity);
            portail.GetComponent<NetworkObject>().Spawn();
        }
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
        seed = conf.currentSeed;
        minTailleEtage = conf.minTailleEtage;
        maxTailleEtage = conf.maxTailleEtage;
        nbStairs = conf.nbStairs;
        nbCauldrons = conf.nbChaudrons;
        typeEtage = conf.typeEtage;
        baseDifficulty = conf.baseDiff;
        difficultyScaling = conf.diffScaling;
        currentEtage = conf.currentEtage;
        Random.InitState(seed);
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
        seed = Random.Range(0, 1000000);
        MultiplayerGameManager.Instance.conf.currentSeed = seed;
        Debug.Log("New seed : " + seed);
    }

    public int GetSeed()
    {
        return seed;
    }
}