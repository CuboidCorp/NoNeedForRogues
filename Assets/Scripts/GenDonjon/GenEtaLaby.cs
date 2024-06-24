using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Donc un labyrinthe avec nbStairs entrées et nbStairs sorties
/// </summary>
public class GenEtaLaby : GenerationEtage
{

    [Flags]
    public enum WallState
    {
        DOWN = 1,
        UP = 2,
        LEFT = 4,
        RIGHT = 8,
        VISITED = 16
    }

    private struct Neighbour
    {
        public Vector2Int Pos;
        public WallState SharedWall;
    }

    private GameObject[] couloirs;
    private GameObject[] stairs;

    private List<Vector2Int> deadEnds;

    private WallState[,] etage;

    public override void GenerateEtage()
    {
        InitEtage();
        GenerationTheorique();
        RenderingLabyrinthe();
    }

    public override void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs)
    {
        couloirs = Resources.LoadAll<GameObject>(pathToHallways);
        stairs = Resources.LoadAll<GameObject>(pathToStairs);
    }

    public override void GenerateItems()
    {
        foreach (Vector2Int deadEnd in deadEnds)
        {
            Debug.DrawRay(new Vector3(deadEnd.x, 0, deadEnd.y) * cellSize, Vector3.up, Color.yellow, 100f);
        }
    }

    private void InitEtage()
    {
        etage = new WallState[tailleEtage.x, tailleEtage.y];
        for (int i = 0; i < tailleEtage.x; i++)
        {
            for (int j = 0; j < tailleEtage.y; j++)
            {
                etage[i, j] = WallState.LEFT | WallState.RIGHT | WallState.UP | WallState.DOWN;
            }
        }
    }

    private void GenerationTheorique()
    {
        deadEnds = new List<Vector2Int>();
        bool aEuDesVoisins = true;
        Stack<Vector2Int> stack = new();
        Vector2Int currentCell = new(UnityEngine.Random.Range(0, tailleEtage.x), UnityEngine.Random.Range(0, tailleEtage.y));
        etage[currentCell.x, currentCell.y] |= WallState.VISITED;
        stack.Push(currentCell);

        while (stack.Count > 0)
        {
            Vector2Int posActuelle = stack.Pop();
            Neighbour[] voisins = GetUnvisitedNeighbor(posActuelle);
            if (voisins.Length > 0)
            {
                aEuDesVoisins = true;
                stack.Push(posActuelle);

                int randIndex = UnityEngine.Random.Range(0, voisins.Length);
                Neighbour randomNeighbour = voisins[randIndex];
                Vector2Int posNeighbour = randomNeighbour.Pos;

                etage[posActuelle.x, posActuelle.y] &= ~randomNeighbour.SharedWall;
                etage[posNeighbour.x, posNeighbour.y] &= ~GetOppositeWall(randomNeighbour.SharedWall);
                etage[posNeighbour.x, posNeighbour.y] |= WallState.VISITED;

                stack.Push(posNeighbour);
            }
            else if (aEuDesVoisins)
            {
                aEuDesVoisins = false;
                deadEnds.Add(posActuelle);
            }
        }
    }

    private void RenderingLabyrinthe()
    {
        for (int i = 0; i < tailleEtage.x; i++)
        {
            for (int j = 0; j < tailleEtage.y; j++)
            {
                GameObject go = Instantiate(couloirs[GetIndexCouloir(new Vector2Int(i, j))], new Vector3(i, 0, j) * cellSize, Quaternion.identity);
                GameObject goToRemove = go.transform.GetChild(0).gameObject;
                foreach (Transform child in go.transform)
                {
                    if (child.name == "Ceiling_SquareLarge")
                    {
                        goToRemove = child.gameObject;
                    }
                }
                Destroy(goToRemove);
            }
        }
    }

    private WallState GetOppositeWall(WallState wall)
    {
        return wall switch
        {
            WallState.LEFT => WallState.RIGHT,
            WallState.RIGHT => WallState.LEFT,
            WallState.UP => WallState.DOWN,
            WallState.DOWN => WallState.UP,
            _ => WallState.LEFT,
        };
    }

    private Neighbour[] GetUnvisitedNeighbor(Vector2Int pos)
    {
        List<Neighbour> unvisited = new();

        if (pos.x > 0 && !etage[pos.x - 1, pos.y].HasFlag(WallState.VISITED))
        {
            unvisited.Add(new Neighbour { Pos = new Vector2Int(pos.x - 1, pos.y), SharedWall = WallState.LEFT });
        }
        if (pos.x < tailleEtage.x - 1 && !etage[pos.x + 1, pos.y].HasFlag(WallState.VISITED))
        {
            unvisited.Add(new Neighbour { Pos = new Vector2Int(pos.x + 1, pos.y), SharedWall = WallState.RIGHT });
        }
        if (pos.y > 0 && !etage[pos.x, pos.y - 1].HasFlag(WallState.VISITED))
        {
            unvisited.Add(new Neighbour { Pos = new Vector2Int(pos.x, pos.y - 1), SharedWall = WallState.DOWN });
        }
        if (pos.y < tailleEtage.y - 1 && !etage[pos.x, pos.y + 1].HasFlag(WallState.VISITED))
        {
            unvisited.Add(new Neighbour { Pos = new Vector2Int(pos.x, pos.y + 1), SharedWall = WallState.UP });
        }
        return unvisited.ToArray();
    }

    /// <summary>
    /// Renvoie l'index d'un couloir en fonction de son voisin dans la grille
    /// </summary>
    /// <returns>L'index du couloir dans la liste des couloirs</returns>
    private int GetIndexCouloir(Vector2Int position)
    {
        int allFlagsMask = (int)(WallState.DOWN | WallState.UP | WallState.LEFT | WallState.RIGHT | WallState.VISITED);
        return ~(int)etage[position.x, position.y] & allFlagsMask;
    }
}
