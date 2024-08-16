using UnityEngine;

/// <summary>
/// Generation de donjon avec des salles et des corridors qui les relient type ENTER THE GUNGEON
/// https://www.youtube.com/watch?v=NpS5v_Tg4Bw
/// Donne des idées ptet pas ce que je veux mais faut voir
/// </summary>
public class GenEtaAbre : GenerationEtage
{
    /// <summary>
    /// Nombre de salles par étage
    /// </summary>
    [SerializeField]
    private int nbRooms;

    /// <summary>
    /// Nombre de tentatives pour placer une salle
    /// </summary>
    [SerializeField]
    private int nbAttempts;

    public override void GenerateEtage()
    {
        throw new System.NotImplementedException();
    }

    public override void GenerateItems()
    {
        throw new System.NotImplementedException();
    }

    public override void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs, string pathToPieces, string pathToObjets, string pathToPotions, string pathToChests, string pathToPieges)
    {
        throw new System.NotImplementedException();
    }

    public override void ChargeHolders(Transform holderRooms, Transform holderHallways, Transform holderStairs)
    {
        throw new System.NotImplementedException();
    }
}
