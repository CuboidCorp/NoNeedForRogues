using System.Collections.Generic;
using UnityEngine;

public class RoomInfo : MonoBehaviour
{
    public Vector3Int roomSize;
    public List<GameObject> walls;

    /// <summary>
    /// Enleve un mur qui est collide avec un couloir
    /// </summary>
    /// <param name="hallway">Le couloir en collision </param>
    public void RemoveWall(GameObject hallway)
    {
        foreach (GameObject wall in walls)
        {
            foreach (Transform child in hallway.transform)
            {
                if (child.name.Contains("Wall"))
                {
                    Debug.Log("Mur");
                    if (wall.GetComponent<Collider>().bounds.Intersects(child.GetComponent<Collider>().bounds))
                    {
                        Debug.Log("Wall removed");
                        walls.Remove(wall);
                        Destroy(wall);
                        break;
                    }
                }
            }
        }
    }
}