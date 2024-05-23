using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    [HideInInspector]
    public BoundsInt bounds;


    /// <summary>
    /// Vérifie si la salle est en collision avec une pseudo-salle
    /// </summary>
    /// <param name="boundsInt">Les coordonnées fictive de la pseudo salle</param>
    /// <returns>True si les deux salles sont en collision, false sinon</returns>
    public bool IsIntersectingWith(BoundsInt boundsInt)
    {
        return !((bounds.position.x >= (boundsInt.position.x + boundsInt.size.x)) || ((bounds.position.x + bounds.size.x) <= boundsInt.position.x)
            || (bounds.position.y >= (boundsInt.position.y + boundsInt.size.y)) || ((bounds.position.y + bounds.size.y) <= boundsInt.position.y)
            || (bounds.position.z >= (boundsInt.position.z + boundsInt.size.z)) || ((bounds.position.z + bounds.size.z) <= boundsInt.position.z));
    }

}