using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;
//Ecrit par November
public class Generator3D : MonoBehaviour
{
    enum CellType
    {
        None,
        Room,
        Hallway,
        Stairs
    }

    enum DungeonType
    {
        Type0, //DEBUG ONLY
        Type1,
    }

    [SerializeField]
    int seed;

    [SerializeField]
    Vector3Int size;
    [SerializeField]
    int roomCount;
    [SerializeField]
    DungeonType ty;
    [SerializeField]
    private Transform RoomHolder;
    [SerializeField]
    private Transform HallwayStairsHolder;

    [Header("Temp")]
    [SerializeField]
    GameObject tempPrefab;
    [SerializeField]
    Material tempStairMaterial;
    [SerializeField]
    Material tempHallwayMaterial;

    #region Prefabs

    private GameObject[] normalRooms;
    private GameObject[] treasureRooms;
    private GameObject[] puzzleRooms;

    #endregion

    Random random;
    Grid3D<CellType> grid;
    List<RoomInfo> rooms;
    Delaunay3D delaunay;
    HashSet<Prim.Edge> selectedEdges;

    void Start()
    {
        random = new Random(seed);
        grid = new Grid3D<CellType>(size, Vector3Int.zero);
        rooms = new List<RoomInfo>();

        switch (ty)
        {
            case DungeonType.Type0:
                normalRooms = Resources.LoadAll<GameObject>("Donjon/Type0/Normal");
                treasureRooms = Resources.LoadAll<GameObject>("Donjon/Type0/Treasure");
                puzzleRooms = Resources.LoadAll<GameObject>("Donjon/Type0/Puzzle");
                break;
            case DungeonType.Type1:
                throw new System.NotImplementedException("Pas fait encore mon gars"); //TODO Faire le type 1
                //normalRooms = Resources.LoadAll<GameObject>("Rooms/Type1/Normal");
                //treasureRooms = Resources.LoadAll<GameObject>("Rooms/Type1/Treasure");
                //puzzleRooms = Resources.LoadAll<GameObject>("Rooms/Type1/Puzzle");
                //break;
        }


        PlaceRooms();
        Triangulate();
        //DebugDelaunay();
        CreateHallways();
        //DebugHallways();
        PathfindHallways();
        //DebugGrid();
    }

    void PlaceRooms()
    {
        int placedRooms = 0;
        int maxAttempts = 1000; // Nombre maximum d'essais pour placer toutes les salles

        while (placedRooms < roomCount && maxAttempts > 0)
        {
            // Sélection du type de salle aléatoire
            int roomType = random.Next(0, 3);
            GameObject futureRoom = roomType switch
            {
                1 => treasureRooms[random.Next(0, treasureRooms.Length)], // Treasure
                2 => puzzleRooms[random.Next(0, puzzleRooms.Length)], // Puzzle
                _ => normalRooms[random.Next(0, normalRooms.Length)] // Normal
            };

            // Taille de la salle
            Vector3Int roomSize = Vector3Int.RoundToInt(futureRoom.transform.localScale);

            if (roomSize.x > size.x || roomSize.y > size.y || roomSize.z > size.z)
            {
                //Debug.LogWarning("La salle est trop grande pour la taille de la grille.");
                maxAttempts--;
                continue;
            }

            // Génération de la position en fonction de la taille de la salle
            Vector3Int location = new(
                random.Next(0, size.x - roomSize.x),
                random.Next(0, size.y - roomSize.y),
                random.Next(0, size.z - roomSize.z)
            );

            BoundsInt roomBounds = new(location, roomSize);
            BoundsInt roomBuffer = new(location + new Vector3Int(-1, 0, -1), roomSize + new Vector3Int(2, 0, 2));

            if (IsPositionValid(roomBounds, roomBuffer))
            {
                RoomInfo infoFutRoom = new() { bounds = roomBounds };
                rooms.Add(infoFutRoom);
                PlaceRoom(roomBounds.center, futureRoom);
                int cptTaille = 0;
                foreach (Vector3Int pos in roomBounds.allPositionsWithin)
                {
                    cptTaille++;
                    grid[pos] = CellType.Room;
                }
                Debug.Log("Taille de la salle : " + cptTaille);

                placedRooms++;
            }

            maxAttempts--;
        }

        if (maxAttempts <= 0)
        {
            Debug.LogWarning("Placement des salles arrêté après avoir atteint le nombre maximum d'essais.");
        }
    }

    bool IsPositionValid(BoundsInt roomBounds, BoundsInt roomBuffer)
    {
        // Vérification des limites
        if (roomBounds.xMin < 0 || roomBounds.xMax > size.x
            || roomBounds.yMin < 0 || roomBounds.yMax > size.y
            || roomBounds.zMin < 0 || roomBounds.zMax > size.z)
        {
            return false;
        }

        // Vérification des collisions avec les salles existantes
        foreach (RoomInfo room in rooms)
        {
            if (room.IsIntersectingWith(roomBuffer))
            {
                return false;
            }
        }

        return true;
    }


    void Triangulate()
    {
        List<Vertex> vertices = new();

        foreach (RoomInfo room in rooms)
        {
            vertices.Add(new Vertex<RoomInfo>(room.bounds.center, room));
            //Debug.Log("DELAUNAY : pos1 " + room.bounds.position + " center : " + room.bounds.center);
        }

        delaunay = Delaunay3D.Triangulate(vertices);
    }

    void CreateHallways()
    {
        List<Prim.Edge> edges = new();

        foreach (Delaunay3D.Edge edge in delaunay.Edges)
        {
            edges.Add(new Prim.Edge(edge.U, edge.V));
        }

        List<Prim.Edge> minimumSpanningTree = Prim.MinimumSpanningTree(edges, edges[0].U);

        selectedEdges = new HashSet<Prim.Edge>(minimumSpanningTree);
        HashSet<Prim.Edge> remainingEdges = new(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (Prim.Edge edge in remainingEdges)
        {
            if (random.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
            }
        }
    }



    #region Debug
    void DebugHallways()
    {
        foreach (Prim.Edge edge in selectedEdges)
        {
            RoomInfo startRoom = (edge.U as Vertex<RoomInfo>).Item;
            RoomInfo endRoom = (edge.V as Vertex<RoomInfo>).Item;

            Vector3 startPos = startRoom.bounds.center;
            Vector3 endPos = endRoom.bounds.center;

            Debug.DrawLine(startPos, endPos, Color.blue, 100, false);
        }
    }

    void DebugDelaunay()
    {
        foreach (Delaunay3D.Edge edge in delaunay.Edges)
        {
            //Debug.Log("DELAUNAY : pos1 " + edge.U.Position + " pos2 : " + edge.V.Position);
            Debug.DrawLine(edge.U.Position, edge.V.Position, Color.red, 100, false);
        }
    }

    void DebugGrid()
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    Vector3Int pos = new(x, y, z);

                    switch (grid[pos])
                    {
                        case CellType.Room:
                            Debug.DrawLine(pos, pos + Vector3.up, Color.green, 100, false);
                            break;
                        case CellType.Hallway:
                            Debug.DrawLine(pos, pos + Vector3.up, Color.blue, 100, false);
                            break;
                        case CellType.Stairs:
                            Debug.DrawLine(pos, pos + Vector3.up, Color.yellow, 100, false);
                            break;
                    }
                }
            }
        }
    }
    #endregion


    void PathfindHallways()
    {
        DungeonPathfinder3D aStar = new(size);

        foreach (Prim.Edge edge in selectedEdges)
        {
            RoomInfo startRoom = (edge.U as Vertex<RoomInfo>).Item;
            RoomInfo endRoom = (edge.V as Vertex<RoomInfo>).Item;

            Vector3 startPosf = startRoom.bounds.center;
            Vector3 endPosf = endRoom.bounds.center;
            Vector3Int startPos = new((int)startPosf.x, (int)startPosf.y, (int)startPosf.z);
            Vector3Int endPos = new((int)endPosf.x, (int)endPosf.y, (int)endPosf.z);

            List<Vector3Int> path = aStar.FindPath(startPos, endPos, (DungeonPathfinder3D.Node a, DungeonPathfinder3D.Node b) =>
            {
                var pathCost = new DungeonPathfinder3D.PathCost();

                Vector3Int delta = b.Position - a.Position;

                if (delta.y == 0)
                {
                    //flat hallway
                    pathCost.cost = Vector3Int.Distance(b.Position, endPos);    //heuristic

                    if (grid[b.Position] == CellType.Stairs)
                    {
                        return pathCost;
                    }
                    else if (grid[b.Position] == CellType.Room)
                    {
                        pathCost.cost += 5;
                    }
                    else if (grid[b.Position] == CellType.None)
                    {
                        pathCost.cost += 1;
                    }

                    pathCost.traversable = true;
                }
                else
                {
                    //staircase
                    if ((grid[a.Position] != CellType.None && grid[a.Position] != CellType.Hallway)
                        || (grid[b.Position] != CellType.None && grid[b.Position] != CellType.Hallway)) return pathCost;

                    pathCost.cost = 100 + Vector3Int.Distance(b.Position, endPos);    //base cost + heuristic

                    int xDir = Mathf.Clamp(delta.x, -1, 1);
                    int zDir = Mathf.Clamp(delta.z, -1, 1);
                    Vector3Int verticalOffset = new(0, delta.y, 0);
                    Vector3Int horizontalOffset = new(xDir, 0, zDir);

                    if (!grid.InBounds(a.Position + verticalOffset)
                        || !grid.InBounds(a.Position + horizontalOffset)
                        || !grid.InBounds(a.Position + verticalOffset + horizontalOffset))
                    {
                        return pathCost;
                    }

                    if (grid[a.Position + horizontalOffset] != CellType.None
                        || grid[a.Position + horizontalOffset * 2] != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset] != CellType.None
                        || grid[a.Position + verticalOffset + horizontalOffset * 2] != CellType.None)
                    {
                        return pathCost;
                    }

                    pathCost.traversable = true;
                    pathCost.isStairs = true;
                }

                return pathCost;
            });

            if (path != null)
            {
                for (int i = 0; i < path.Count; i++)
                {
                    Vector3Int current = path[i];

                    if (grid[current] == CellType.None)
                    {
                        grid[current] = CellType.Hallway;
                    }

                    if (i > 0)
                    {
                        Vector3Int prev = path[i - 1];

                        Vector3Int delta = current - prev;

                        if (delta.y != 0)
                        {
                            int xDir = Mathf.Clamp(delta.x, -1, 1);
                            int zDir = Mathf.Clamp(delta.z, -1, 1);
                            Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                            Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

                            grid[prev + horizontalOffset] = CellType.Stairs;
                            grid[prev + horizontalOffset * 2] = CellType.Stairs;
                            grid[prev + verticalOffset + horizontalOffset] = CellType.Stairs;
                            grid[prev + verticalOffset + horizontalOffset * 2] = CellType.Stairs;

                            PlaceStairs(prev + horizontalOffset);
                            PlaceStairs(prev + horizontalOffset * 2);
                            PlaceStairs(prev + verticalOffset + horizontalOffset);
                            PlaceStairs(prev + verticalOffset + horizontalOffset * 2);
                        }

                        Debug.DrawLine(prev + new Vector3(0.5f, 0.5f, 0.5f), current + new Vector3(0.5f, 0.5f, 0.5f), Color.blue, 100, false);
                    }
                }

                foreach (Vector3Int pos in path)
                {
                    if (grid[pos] == CellType.Hallway)
                    {
                        PlaceHallway(pos);
                    }
                }
            }
        }
    }

    void PlaceCube(Vector3Int location, Vector3Int size, Material material)
    {
        GameObject go = Instantiate(tempPrefab, HallwayStairsHolder);
        go.transform.SetPositionAndRotation(location + new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity);
        go.GetComponent<Transform>().localScale = size;
        go.GetComponent<MeshRenderer>().material = material;
    }

    /// <summary>
    /// Place une salle à un endroit donné
    /// </summary>
    /// <param name="location">L'endroit ou placé la salle</param>
    /// <param name="roomToPlace">La salle a placer</param>
    void PlaceRoom(Vector3 location, GameObject roomToPlace)
    {
        //Placer la vraie salle
        GameObject go = Instantiate(roomToPlace, RoomHolder);
        go.transform.SetPositionAndRotation(location, Quaternion.identity);
    }

    void PlaceHallway(Vector3Int location)
    {
        PlaceCube(location, new Vector3Int(1, 1, 1), tempHallwayMaterial);
    }

    void PlaceStairs(Vector3Int location)
    {
        PlaceCube(location, new Vector3Int(1, 1, 1), tempStairMaterial);
    }
}
