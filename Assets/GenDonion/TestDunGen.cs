using System.Collections.Generic;
using System.Drawing;
using UnityEditor;
using UnityEngine;
using static DungeonPathfinder3D;

public class TestDunGen : MonoBehaviour
{
    [SerializeField]
    private GameObject hallwayPrefab;
    [SerializeField]
    private GameObject roomPrefab;
    // Start is called before the first frame update
    [SerializeField]
    Vector3Int size;

    Grid3D<CellType> grid;
    Vector3 cellSize = new(1, 1, 1);

    private Dictionary<int, GameObject> lookupHallwaysTable;
    enum CellType
    {
        None,
        Room,
        Hallway,
        Stairs
    }

}
