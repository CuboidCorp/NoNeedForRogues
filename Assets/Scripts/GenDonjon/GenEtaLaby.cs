using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Donc un labyrinthe avec nbStairs entrées et nbStairs sorties
/// </summary>
public class GenEtaLaby : GenerationEtage
{

    [Flags]
    public enum WallState
    {
        LEFT = 1,
        DOWN = 2,
        RIGHT = 4,
        UP = 8,
        VISITED = 16
    }

    private struct Neighbour
    {
        public Vector2Int Pos;
        public WallState SharedWall;
    }

    private GameObject[] couloirsPrefabs;
    private GameObject upStairPrefab;
    private GameObject downStairPrefab;

    private Transform stairHolder;
    private Transform hallwaysHolder;

    private List<Vector2Int> deadEnds;
    private Vector2Int[] stairsPos;



    private const float yCoordinateUpStairs = 0;
    private const float yCoordinateDownStairs = -1;

    private WallState[,] etage;

    #region Traps

    #endregion

    public override void GenerateEtage()
    {
        InitEtage();
        GenerationTheorique();
        GenerationEscaliers();
        RenderingLabyrinthe();
    }

    public override void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs)
    {
        couloirsPrefabs = Resources.LoadAll<GameObject>(pathToHallways);
        upStairPrefab = Resources.Load<GameObject>(pathToStairs + "UpStairs");
        downStairPrefab = Resources.Load<GameObject>(pathToStairs + "DownStairs");
    }

    public override void ChargeHolders(Transform holderRooms, Transform holderHallways, Transform holderStairs)
    {
        stairHolder = holderStairs;
        hallwaysHolder = holderHallways;
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

    private void GenerationEscaliers()
    {
        stairsPos = new Vector2Int[nbStairs * 2];
        //On genere les points d'entrée
        int cptStairs = 0;
        Vector2Int cellPos = new(0, 0);
        Vector2Int stairPos = new(0, 0);
        while (cptStairs < nbStairs)
        {
            int side = Random.Range(0, 4); //0 gauche 1 Haut 2 droite 3 bas
            switch (side)
            {
                case 0: //Angle a 90
                    stairPos.x = -1;
                    stairPos.y = Random.Range(0, tailleEtage.y);
                    cellPos = new Vector2Int(0, stairPos.y);
                    break;
                case 1: // Angle a 0
                    stairPos.x = Random.Range(0, tailleEtage.x);
                    stairPos.y = -1;
                    cellPos = new Vector2Int(stairPos.x, 0);
                    break;
                case 2: //Angle a 270
                    stairPos.x = tailleEtage.x;
                    stairPos.y = Random.Range(0, tailleEtage.y);
                    cellPos = new Vector2Int(tailleEtage.x - 1, stairPos.y);
                    break;
                case 3: //Angle a 180
                    stairPos.x = Random.Range(0, tailleEtage.x);
                    stairPos.y = tailleEtage.y;
                    cellPos = new Vector2Int(stairPos.x, tailleEtage.y - 1);
                    break;
            }

            if (IsStairPlaceable(stairPos))
            {
                GameObject stairs = Instantiate(upStairPrefab, new Vector3(stairPos.x, yCoordinateUpStairs, stairPos.y) * cellSize, Quaternion.identity);
                stairs.transform.parent = stairHolder;
                stairs.name = "SUp" + stairPos.x + "_" + stairPos.y;
                stairs.transform.eulerAngles = new Vector3(0, 90 - (side * 90), 0);

                stairsPos[cptStairs] = stairPos;
                cptStairs++;

                etage[cellPos.x, cellPos.y] ^= (WallState)(1 << side);


            }
        }

        Debug.Log("Down stairs");

        //Points de sorties
        while (cptStairs < nbStairs * 2)
        {
            int side = Random.Range(0, 4); //0 gauche 1 Haut 2 droite 3 bas
            switch (side)
            {
                case 0: //270
                    stairPos.x = -1;
                    stairPos.y = Random.Range(0, tailleEtage.y);
                    cellPos = new Vector2Int(0, stairPos.y);
                    break;
                case 1://180
                    stairPos.x = Random.Range(0, tailleEtage.x);
                    stairPos.y = -1;
                    cellPos = new Vector2Int(stairPos.x, 0);
                    break;
                case 2://90
                    stairPos.x = tailleEtage.x;
                    stairPos.y = Random.Range(0, tailleEtage.y);
                    cellPos = new Vector2Int(tailleEtage.x - 1, stairPos.y);
                    break;
                case 3://0
                    stairPos.x = Random.Range(0, tailleEtage.x);
                    stairPos.y = tailleEtage.y;
                    cellPos = new Vector2Int(stairPos.x, tailleEtage.y - 1);
                    break;
            }

            if (IsStairPlaceable(stairPos))
            {
                switch (side)
                {
                    case 0:
                        stairPos.x--;
                        break;
                    case 1:
                        stairPos.y--;
                        break;
                    case 2:
                        stairPos.x++;
                        break;
                    case 3:
                        stairPos.y++;
                        break;
                }
                GameObject stairs = Instantiate(downStairPrefab, new Vector3(stairPos.x, yCoordinateDownStairs, stairPos.y) * cellSize, Quaternion.identity);
                stairs.transform.parent = stairHolder;
                stairs.name = "SDown" + stairPos.x + "_" + stairPos.y;
                stairs.transform.eulerAngles = new Vector3(0, 270 - (side * 90), 0);
                stairsPos[cptStairs] = stairPos;
                cptStairs++;

                etage[cellPos.x, cellPos.y] ^= (WallState)(1 << side);

            }
        }
    }

    private bool IsStairPlaceable(Vector2Int pos)
    {
        foreach (Vector2Int vector in stairsPos)
        {
            if (pos == vector)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Décide du retrait des murs en se promenant dans le labyrinthe et en faisant machine arrière des qu'il rencontre une impasse
    /// </summary>
    private void GenerationTheorique()
    {
        deadEnds = new List<Vector2Int>();
        bool aEuDesVoisins = true;
        Stack<Vector2Int> stack = new();
        Vector2Int currentCell = new(Random.Range(0, tailleEtage.x), Random.Range(0, tailleEtage.y));
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

                int randIndex = Random.Range(0, voisins.Length);
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
                GameObject go = Instantiate(couloirsPrefabs[GetIndexCouloir(new Vector2Int(i, j))], new Vector3(i, 0, j) * cellSize, Quaternion.identity);
                go.transform.parent = hallwaysHolder;
                go.name = "Couloir" + GetIndexCouloir(new Vector2Int(i, j)) + "P" + i + "_" + j;
                GameObject goToRemove = go.transform.GetChild(0).gameObject;
                foreach (Transform child in go.transform)
                {
                    if (child.name == "Ceiling_SquareLarge") //TODO : Temporaire --> Pour voir l'intérieur du labyrinthe
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

    #region Generation Objets
    public override void GenerateItems()
    {
        foreach (Vector2Int deadEnd in deadEnds)
        {
            Debug.DrawRay(new Vector3(deadEnd.x, 0, deadEnd.y) * cellSize, Vector3.up, Color.yellow, 100f);
        }
    }

    #endregion

    #region Generation Pieges

    #endregion

}
