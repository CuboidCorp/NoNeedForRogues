using System.Collections.Generic;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public Vector3Int roomSize;
    public List<GameObject> walls;
    public List<Vector3Int> hallwayPos;

    /// <summary>
    /// Enleve un mur qui est collide avec un couloir
    /// </summary>
    /// <param name="hallway">Le couloir en collision </param>
    public void RemoveWall(GameObject hallway)
    {
        List<GameObject> wallsToRemove = new List<GameObject>();
        foreach (GameObject wall in walls)
        {
            foreach (Transform child in hallway.transform)
            {
                if (child.name.Contains("Wall"))
                {
                    if (Vector3.Distance(wall.transform.position, child.position) < 1)
                    {
                        wallsToRemove.Add(wall);
                        break;
                    }
                }
            }
        }
        foreach (GameObject wall in wallsToRemove)
        {
            walls.Remove(wall);
            Destroy(wall);
        }
    }
}