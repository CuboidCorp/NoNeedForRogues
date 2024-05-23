using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using Graphs;

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
        CreateHallways();
        PathfindHallways();
    }

    void PlaceRooms()
    {
        for (int i = 0; i < roomCount; i++) //TODO faire avec un while plutot
        {
            Vector3Int location = new(
                random.Next(0, size.x),
                random.Next(0, size.y),
                random.Next(0, size.z)
            );

            //On prend un type de salle au hasard
            //Donc on prend une salle au hasard de ce type
            //Et on la place si possible

            //Il faudra changer les poids de chaque salle une fois mais plus tard lol
            int roomType = random.Next(0, 3);
            GameObject futureRoom;
            RoomInfo infoFutRoom;
            switch (roomType)
            {
                case 1:
                    //Treasure
                    futureRoom = treasureRooms[random.Next(0, treasureRooms.Length)];
                    infoFutRoom = futureRoom.GetComponent<RoomInfo>();
                    break;
                case 2:
                    //Puzzle
                    futureRoom = puzzleRooms[random.Next(0, puzzleRooms.Length)];
                    infoFutRoom = futureRoom.GetComponent<RoomInfo>();
                    break;
                default:
                    //Normal
                    futureRoom = normalRooms[random.Next(0, normalRooms.Length)];
                    infoFutRoom = futureRoom.GetComponent<RoomInfo>();
                    break;
            }

            bool estPlacable = true;
            BoundsInt roomBounds = new(location, Vector3Int.RoundToInt(futureRoom.transform.localScale));
            infoFutRoom.bounds = roomBounds;
            BoundsInt roomBuffer = new(location + new Vector3Int(-1, 0, -1), Vector3Int.RoundToInt(futureRoom.transform.localScale) + new Vector3Int(2, 0, 2));

            foreach (RoomInfo room in rooms) //Pour que ce soit plus stylé on place les salles avec au minimun un certain espace entre elles
            {
                if (room.IsIntersectingWith(roomBuffer))
                {
                    estPlacable = false;
                    break;
                }
            }

            if (roomBounds.xMin < 0 || roomBounds.xMax >= size.x
                || roomBounds.yMin < 0 || roomBounds.yMax >= size.y
                || roomBounds.zMin < 0 || roomBounds.zMax >= size.z)
            {
                estPlacable = false;
            }

            if (estPlacable)
            {
                rooms.Add(infoFutRoom);
                PlaceRoom(roomBounds.position, futureRoom);

                foreach (Vector3Int pos in roomBounds.allPositionsWithin)
                {
                    grid[pos] = CellType.Room;
                }
            }
        }
    }

    void Triangulate()
    {
        List<Vertex> vertices = new();

        foreach (RoomInfo room in rooms)
        {
            vertices.Add(new Vertex<RoomInfo>((Vector3)room.bounds.position + ((Vector3)room.bounds.size) / 2, room));
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
        var remainingEdges = new HashSet<Prim.Edge>(edges);
        remainingEdges.ExceptWith(selectedEdges);

        foreach (Prim.Edge edge in remainingEdges)
        {
            if (random.NextDouble() < 0.125)
            {
                selectedEdges.Add(edge);
            }
        }
    }

    void PathfindHallways()
    {
        DungeonPathfinder3D aStar = new DungeonPathfinder3D(size);

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
                    Vector3Int verticalOffset = new Vector3Int(0, delta.y, 0);
                    Vector3Int horizontalOffset = new Vector3Int(xDir, 0, zDir);

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

                foreach (var pos in path)
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
        GameObject go = Instantiate(tempPrefab, location, Quaternion.identity);
        go.GetComponent<Transform>().localScale = size;
        go.GetComponent<MeshRenderer>().material = material;
    }

    /// <summary>
    /// Place une salle à un endroit donné
    /// </summary>
    /// <param name="location">L'endroit ou placé la salle</param>
    /// <param name="roomToPlace">La salle a placer</param>
    void PlaceRoom(Vector3Int location, GameObject roomToPlace)
    {
        //Placer la vraie salle
        Instantiate(roomToPlace, location, Quaternion.identity);

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
