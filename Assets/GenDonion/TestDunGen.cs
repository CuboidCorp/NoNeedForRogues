using UnityEngine;
using static UnityEditor.FilePathAttribute;

public class TestDunGen : MonoBehaviour
{
    [SerializeField]
    private GameObject cubePrefab;
    // Start is called before the first frame update
    void Start()
    {
        Vector3Int location = new(0, 0, 0);
        Vector3Int roomSize = Vector3Int.FloorToInt(cubePrefab.transform.localScale);

        BoundsInt roomBounds = new(location, roomSize);
        Instantiate(cubePrefab, roomBounds.center, Quaternion.identity);
        foreach (Vector3Int vector3Int in roomBounds.allPositionsWithin)
        {
            //Debug.Log(vector3Int);
            Debug.DrawLine(vector3Int, vector3Int + Vector3.up, Color.red, 1000);
        }
    }
}
