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

    private int currentEtage = 0;

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

    [Header("Prefabs")]
    [SerializeField]
    private string pathToRooms;

    [SerializeField]
    private string pathToHallways; //Pour la bonne prefab on peut faire avec bitmask ou en generant des prefabs pour chaque cas TODO : Voir lequel on prend

    [SerializeField]
    private string pathToStairs;

    private GenerationEtage genEtage;

    // Start is called before the first frame update
    void Start()
    {
        Random.InitState(seed);
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
        genEtage.Initialize(new Vector2Int(Random.Range(minTailleEtage.x, maxTailleEtage.x), Random.Range(minTailleEtage.y, maxTailleEtage.y)), 1, cellSize);
        genEtage.ChargePrefabs(pathToRooms, pathToHallways, pathToStairs);
        genEtage.GenerateEtage();
        genEtage.GenerateItems();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void RandomizeSeed()
    {
        seed = Random.Range(0, 1000000);
    }
}
