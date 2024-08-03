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
    [SerializeField]
    int nbEtages;

    public int currentEtage = 1;
    public int maxEtage = 5;

    private int maxEtageReached = 0;

    [SerializeField]
    private float cellSize = 1;
    [SerializeField]
    private int seed;
    [SerializeField]
    private int cellDistanceBetweenEtages;

    [SerializeField]
    private TypeEtage typeEtage;

    [SerializeField]
    private Vector2Int minTailleEtage;
    [SerializeField]
    private Vector2Int maxTailleEtage;

    [SerializeField] private int nbStairs = 1;

    [Header("Prefabs")]
    [SerializeField]
    private string pathToRooms;

    [SerializeField]
    private string pathToHallways; //Pour la bonne prefab on peut faire avec bitmask ou en generant des prefabs pour chaque cas TODO : Voir lequel on prend

    [SerializeField]
    private string pathToStairs;

    private GenerationEtage genEtage;

    public static GenerationDonjon instance;

    private int[] seeds;

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
        genEtage.Initialize(new Vector2Int(Random.Range(minTailleEtage.x, maxTailleEtage.x), Random.Range(minTailleEtage.y, maxTailleEtage.y)), nbStairs, cellSize);
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
