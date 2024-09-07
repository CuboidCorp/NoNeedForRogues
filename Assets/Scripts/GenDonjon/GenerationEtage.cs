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

    public void Initialize(Vector2Int tailleEtage, int nbStairs, float cellSize, int difficulty, bool estServeur, int nbChaudrons)
    {
        this.tailleEtage = tailleEtage;
        this.nbStairs = nbStairs;
        this.cellSize = cellSize;
        this.difficulty = difficulty;
        estServ = estServeur;
        this.nbChaudrons = nbChaudrons;
    }

    protected Vector2Int tailleEtage;

    protected int nbStairs;

    protected float cellSize;

    protected int difficulty;

    protected bool estServ;

    protected int nbChaudrons;


    /// <summary>
    /// Génère l'étage 
    /// </summary>
    public abstract void GenerateEtage();

    /// <summary>
    /// Génère les items de l'étage
    /// </summary>
    public abstract void GenerateItems();

    /// <summary>
    /// Despawn les items et les pieges de l'étage
    /// </summary>
    public abstract void DespawnObjects();

    /// <summary>
    /// Genere les pieges de l'étage
    /// </summary>
    public abstract void GeneratePieges();

    /// <summary>
    /// Charge les prefabs pour les salles, les corridors et les escaliers
    /// </summary>
    /// <param name="pathToRooms">Le chemin dans les resources pr charger des salles</param>
    /// <param name="pathToHallways">Le chemin dans les resources pr charger des couloirs</param>
    /// <param name="pathToStairs">Le chemin dans les resources pr charger des escaliers</param>
    /// <param name="pathToPieces">Le chemin dans les resources pr charger les pieces/sac de pieces</param>
    /// <param name="pathToObjets">Le chemin dans les resources pr charger les objets trésor</param>
    /// <param name="pathToPotions">Le chemin dans les resources pr charger les potions</param>
    /// <param name="pathToChests">Le chemin dans les resources pr charger les coffres</param>
    /// <param name="pathToPieges">Le chemin dans les resources du dossier des pieges</param>
    /// <param name="pathToTrickshots">Le chemin dans les resources du dossier des trickshots</param>
    public abstract void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs, string pathToPieces, string pathToObjets, string pathToPotions, string pathToChests, string pathToPieges, string pathToTrickshots);

    /// <summary>
    /// Charge les holders pour les salles, les corridors et les escaliers
    /// </summary>
    /// <param name="holderRooms">Le transform qui contient tt les salles</param>
    /// <param name="holderHallways">Le transform qui contient tt les hallways</param>
    /// <param name="holderStairs">Le transform qui contien tt les stairs</param>
    /// <param name="holderItems">Le transform qui contient tt les items</param>
    /// <param name="holderTraps">Le transform qui contient tt les pieges</param>
    /// <param name="holderTrigger">Le transform qui contient tt les triggers</param>"
    /// <param name="holderTrickshot">Le transform qui contient tt lkes trickshots</param>
    public abstract void ChargeHolders(Transform holderRooms, Transform holderHallways, Transform holderStairs, Transform holderItems, Transform holderTraps, Transform holderTrigger, Transform holderTrickshot);
}
