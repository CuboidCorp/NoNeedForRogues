using UnityEngine;

public abstract class GenerationEtage : MonoBehaviour
{
    public enum CellType
    {
        None,
        Room,
        Hallway,
        Stairs
    }

    public void Initialize(Vector2Int tailleEtage, int nbStairs, float cellSize, int difficulty, bool estServeur)
    {
        this.tailleEtage = tailleEtage;
        this.nbStairs = nbStairs;
        this.cellSize = cellSize;
        this.difficulty = difficulty;
        this.estServ = estServeur;
    }

    protected Vector2Int tailleEtage;

    protected int nbStairs;

    protected float cellSize;

    protected int difficulty;

    protected bool estServ;

    /// <summary>
    /// G�n�re l'�tage 
    /// </summary>
    public abstract void GenerateEtage();

    /// <summary>
    /// G�n�re les items de l'�tage
    /// </summary>
    public abstract void GenerateItems();

    /// <summary>
    /// Genere les pieges de l'�tage
    /// </summary>
    public abstract void GeneratePieges();

    /// <summary>
    /// Charge les prefabs pour les salles, les corridors et les escaliers
    /// </summary>
    /// <param name="pathToRooms">Le chemin dans les resources pr charger des salles</param>
    /// <param name="pathToHallways">Le chemin dans les resources pr charger des couloirs</param>
    /// <param name="pathToStairs">Le chemin dans les resources pr charger des escaliers</param>
    /// <param name="pathToPieces">Le chemin dans les resources pr charger les pieces/sac de pieces</param>
    /// <param name="pathToObjets">Le chemin dans les resources pr charger les objets tr�sor</param>
    /// <param name="pathToPotions">Le chemin dans les resources pr charger les potions</param>
    /// <param name="pathToChests">Le chemin dans les resources pr charger les coffres</param>
    /// <param name="pathToPieges">Le chemin dans les resources du dossier des pieges</param>
    public abstract void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs, string pathToPieces, string pathToObjets, string pathToPotions, string pathToChests, string pathToPieges);

    /// <summary>
    /// Charge les holders pour les salles, les corridors et les escaliers
    /// </summary>
    /// <param name="holderRooms">Le transform qui contient tt les salles</param>
    /// <param name="holderHallways">Le transform qui contient tt les hallways</param>
    /// <param name="holderStairs">Le transform qui contien tt les stairs</param>
    public abstract void ChargeHolders(Transform holderRooms, Transform holderHallways, Transform holderStairs, Transform holderItems);
}
