using UnityEngine;

public class GenerationDonjon : MonoBehaviour
{
    private enum TypeEtage
    {
        Labyrinthe,
        Salles,
        Arbre
    }

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

    private GenerationEtage genEtage;

    public static GenerationDonjon instance;

    

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
        }

        seeds ??= new int[maxEtage];

        instance = this;
        DontDestroyOnLoad(this);
    }

    // Start is called before the first frame update
    void Start()
    {
        currentDifficulty = baseDifficulty + (currentEtage-1) * difficultyScaling;
        if (maxEtageReached < currentEtage) //Si c'est un nouvel etage
        {

            RandomizeSeed();
            seeds[currentEtage] = seed;
            maxEtageReached = currentEtage;
            Generate(true);
        }
        else
        {
            seed = seeds[currentEtage];
            SetSeed();
            Generate(false);
        }

    }

    private void Generate(bool isNewEtage)
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
        genEtage.Initialize(new Vector2Int(Random.Range(minTailleEtage.x, maxTailleEtage.x), Random.Range(minTailleEtage.y, maxTailleEtage.y)), nbStairs, cellSize,baseDifficulty);
        genEtage.ChargePrefabs(pathToRooms, pathToHallways, pathToStairs);
        genEtage.GenerateEtage();
        if (isNewEtage)
        {
            genEtage.GenerateItems();
        }


    }

    public void RandomizeSeed()
    {
        seed = Random.Range(0, 1000000);
        SetSeed();
    }

    private void SetSeed()
    {
        Random.InitState(seed);
    }
}
