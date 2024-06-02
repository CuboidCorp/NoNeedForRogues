using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class TestDunGen : MonoBehaviour
{
    [SerializeField]
    private GameObject hallwayPrefab;
    [SerializeField]
    private GameObject roomPrefab;
    // Start is called before the first frame update
    void Start()
    {
        Vector3 roomPos = new Vector3(88.8f, 7.2f, 132);
        Vector3 hallwayPos = new Vector3(79.2f, 7.2f, 132);
        //On check si les murs de chaques truc sont en colliusion avec les murs de l'autre
        GameObject ROOM = Instantiate(roomPrefab, roomPos, Quaternion.identity);
        GameObject Hallway = Instantiate(hallwayPrefab, hallwayPos, Quaternion.identity);
        ROOM.GetComponent<RoomInfo>().RemoveWall(Hallway);
    }
}
