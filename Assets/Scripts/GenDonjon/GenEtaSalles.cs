using UnityEngine;

/// <summary>
/// https://www.youtube.com/watch?v=gHU5RQWbmWE
/// Sympa c'est la source d'inspi
/// </summary>
public class GenEtaSalles : GenerationEtage
{
    /// <summary>
    /// Nombre de salles par �tage
    /// </summary>
    [SerializeField]
    private int nbRooms;

    /// <summary>
    /// Nombre de tentatives pour placer une salle
    /// </summary>
    [SerializeField]
    private int nbAttempts;

    public override void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs)
    {
        throw new System.NotImplementedException();
    }

    public override void GenerateEtage()
    {
        throw new System.NotImplementedException();
    }

    public override void GenerateItems()
    {
        throw new System.NotImplementedException();
    }
}