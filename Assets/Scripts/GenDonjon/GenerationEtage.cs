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

    public void Initialize(Vector2Int tailleEtage, int nbStairs, float cellSize)
    {
        this.tailleEtage = tailleEtage;
        this.nbStairs = nbStairs;
        this.cellSize = cellSize;
    }

    protected Vector2Int tailleEtage;

    protected int nbStairs;

    protected float cellSize;

    /// <summary>
    /// G�n�re l'�tage 
    /// </summary>
    public abstract void GenerateEtage();

    /// <summary>
    /// G�n�re les items de l'�tage
    /// </summary>
    public abstract void GenerateItems();

    /// <summary>
    /// Charge les prefabs pour les salles, les corridors et les escaliers
    /// </summary>
    /// <param name="pathToRooms">Le chemin dans les resources pr charger des salles</param>
    /// <param name="pathToHallways">Le chemin dans les resources pr charger des couloirs</param>
    /// <param name="pathToStairs">Le chemin dans les resources pr charger des escaliers</param>
    public abstract void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs);
}