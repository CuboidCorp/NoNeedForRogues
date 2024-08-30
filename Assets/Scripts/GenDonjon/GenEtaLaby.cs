using System;
using System.Collections.Generic;
using Unity.Netcode;
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
    private GameObject leavePrefab;

    private Transform stairHolder;
    private Transform hallwaysHolder;
    private Transform itemHolder;

    private List<Vector2Int> deadEnds;
    private Vector2Int[] stairsPos;
    private Vector2Int[] trapsPos;

    private const float yCoordinateUpStairs = 0;
    private const float yCoordinateDownStairs = -1;

    private WallState[,] etage;

    #region Objets
    private GameObject[] prefabsPieces;
    private GameObject[] prefabsObjets;
    private GameObject[] prefabsPotions;
    private GameObject[] prefabsCoffres;

    private List<GameObject> objets;

    #endregion

    #region Traps
    private GameObject[] prefabsTraps;
    public enum Traps //TODO : A mettre dans l'ordre qu'ils sont dans le dossier des ressources
    {
        BOULDER_TRAP,
        FLOOR_TRAP,
        SPIKE_TRAP,
        BEAR_TRAP,
        CRUSH_TRAP,
        ARROW_TRAP,
        POISON_ARROW_TRAP,
        AXE_TRAP,
        SAW_TRAP,
        DOOR_TRAP,
        TOXIC_GAZ,
        SLEEP_GAZ
    }
    #endregion

    public override void GenerateEtage()
    {
        InitEtage();
        GenerationTheorique();
        GenerationEscaliers();
        RenderingLabyrinthe();
    }

    public override void ChargePrefabs(string pathToRooms, string pathToHallways, string pathToStairs, string pathToPieces, string pathToObjets, string pathToPotions, string pathToChests, string pathToPieges)
    {
        couloirsPrefabs = Resources.LoadAll<GameObject>(pathToHallways);
        upStairPrefab = Resources.Load<GameObject>(pathToStairs + "UpStairs");
        downStairPrefab = Resources.Load<GameObject>(pathToStairs + "DownStairs");
        leavePrefab = Resources.Load<GameObject>("Donjon/Leave");
        prefabsPieces = Resources.LoadAll<GameObject>(pathToPieces);
        prefabsObjets = Resources.LoadAll<GameObject>(pathToObjets);
        prefabsPotions = Resources.LoadAll<GameObject>(pathToPotions);
        prefabsCoffres = Resources.LoadAll<GameObject>(pathToChests);
        prefabsTraps = Resources.LoadAll<GameObject>(pathToPieges);
    }

    public override void ChargeHolders(Transform holderRooms, Transform holderHallways, Transform holderStairs, Transform holderItems)
    {
        stairHolder = holderStairs;
        hallwaysHolder = holderHallways;
        itemHolder = holderItems;
    }

    private void InitEtage()
    {
        objets = new List<GameObject>();
        etage = new WallState[tailleEtage.x, tailleEtage.y];
        for (int i = 0 ; i < tailleEtage.x ; i++)
        {
            for (int j = 0 ; j < tailleEtage.y ; j++)
            {
                etage[i, j] = WallState.LEFT | WallState.RIGHT | WallState.UP | WallState.DOWN;
            }
        }
    }

    /// <summary>
    /// Genere les escaliers 
    /// </summary>
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
                if (estServ)
                {
                    GameObject leave = Instantiate(leavePrefab, stairs.transform.GetChild(6).position, Quaternion.Euler(0, 180 - (side * 90), 0));
                    leave.tag = "UpStairs";
                    leave.name = "LeaveUP" + stairPos.x + "_" + stairPos.y;
                    leave.GetComponent<Escalier>().spawnPoint = stairs.transform.GetChild(5);
                    leave.GetComponent<Escalier>().isUpStairs = true;
                    leave.GetComponent<NetworkObject>().Spawn();
                }

                stairsPos[cptStairs] = stairPos;
                cptStairs++;

                etage[cellPos.x, cellPos.y] ^= (WallState)(1 << side);


            }
        }

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
                if (estServ)
                {
                    GameObject leave = Instantiate(leavePrefab, stairs.transform.GetChild(6).position, Quaternion.Euler(0, 180 - (side * 90), 0));
                    leave.name = "LeaveDown" + stairPos.x + "_" + stairPos.y;
                    leave.tag = "DownStairs";
                    leave.GetComponent<Escalier>().spawnPoint = stairs.transform.GetChild(5);
                    leave.GetComponent<Escalier>().isUpStairs = false;
                    leave.GetComponent<NetworkObject>().Spawn();
                }
                stairsPos[cptStairs] = stairPos;
                cptStairs++;

                etage[cellPos.x, cellPos.y] ^= (WallState)(1 << side);

            }
        }
    }

    /// <summary>
    /// Verifie si il y a deja un escalier placé
    /// </summary>
    /// <param name="pos">La position du possible escalier</param>
    /// <returns>True si placeable, false sinon</returns>
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
        for (int i = 0 ; i < tailleEtage.x ; i++)
        {
            for (int j = 0 ; j < tailleEtage.y ; j++)
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
            Vector3 position = new Vector3(deadEnd.x, 0, deadEnd.y) * cellSize + new Vector3(0, 0.6f, 0);
            int typeTresor = Random.Range(0, 4);
            int valeur = 10 + Random.Range(2 * difficulty, 4 * difficulty); //Valeur de l'or
            int force = Random.Range(5, Mathf.Clamp(5 + difficulty / 2, 5, 20));//Force de la potion ou du piege
            switch (typeTresor)
            {
                case 0: //Sac de piece
                    GeneratePieces(prefabsPieces[Random.Range(0, prefabsPieces.Length)], position, valeur);
                    break;
                case 1: //Objet précieux a ramasser
                    //TODO : Faudrait voir les cas ou y a des pieges en dessous des tresors
                    GenerateTresor(prefabsObjets[Random.Range(0, prefabsObjets.Length)], position, (int)(valeur * 1.2f));
                    break;
                case 2: //Potions
                    GeneratePotion(prefabsPotions[Random.Range(0, prefabsPotions.Length)], position, force);
                    break;
                case 3: //Coffres
                    GenerateChest(prefabsCoffres[Random.Range(0, prefabsCoffres.Length)], position, valeur, force, etage[deadEnd.x, deadEnd.y]);
                    break;
            }
            Debug.DrawRay(position, Vector3.up, Color.yellow, 100f);
        }
    }

    public override void DespawnItems()
    {
        foreach (GameObject obj in objets)
        {
            if (obj != null)
            {
                obj.GetComponent<NetworkObject>().Despawn(true);
            }
        }
    }

    /// <summary>
    /// Genere un objet pieces/sac de pieces a un endroit donné à une valeur donnée
    /// </summary>
    /// <param name="objet">Les pieces/sac de pieces a instantier</param>
    /// <param name="position">La position ou on veut mettre l'objet</param>
    /// <param name="valeur">La valeur des pièces</param>
    private void GeneratePieces(GameObject objet, Vector3 position, int valeur)
    {
        GameObject instance = Instantiate(objet, itemHolder);
        objets.Add(instance);
        instance.transform.position = position;
        instance.GetComponent<GoldObject>().value = valeur;
        instance.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Genere un objet tresor a ramasser et recolter a un endroit donné à une valeur donnée
    /// </summary>
    /// <param name="objet">Le trésor a spawn</param>
    /// <param name="position">La position de l'objet</param>
    /// <param name="valeur">La valeur de l'objet</param>
    private void GenerateTresor(GameObject objet, Vector3 position, int valeur)
    {
        GameObject instance = Instantiate(objet, itemHolder);
        objets.Add(instance);
        instance.transform.position = position;
        instance.GetComponent<TreasureObject>().value = valeur;
        instance.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Genere une potion a un endroit donnée avec une force donné
    /// </summary>
    /// <param name="objet">La prefab de la potion a spawn</param>
    /// <param name="position">La position de la potion</param>
    /// <param name="force">La force de la potion</param>
    private void GeneratePotion(GameObject objet, Vector3 position, int force)
    {
        GameObject instance = Instantiate(objet, itemHolder);
        objets.Add(instance);
        instance.transform.position = position;
        instance.GetComponent<PotionObject>().power = force;
        instance.GetComponent<PotionObject>().SetType(Random.Range(0, 3));
        instance.GetComponent<NetworkObject>().Spawn();
    }

    private void GenerateChest(GameObject chest, Vector3 position, int valeur, int force, WallState etatPos)
    {
        GameObject instance = Instantiate(chest, itemHolder);
        objets.Add(instance);
        Debug.Log(etatPos);
        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(0, etatPos * 90, 0); //TODO : Verifier au niveau des bits c'est quoi la bonne formule
        int typeCoffre = Random.Range(0, 2);
        Chest coffreScript = instance.GetComponent<Chest>();
        instance.GetComponent<NetworkObject>().Spawn();
        if (typeCoffre == 0)
        {
            //TRESOR
            //Summon un des objets d'au dessus avec plus de puissance
            int typeTresor = Random.Range(0, 3);
            switch (typeTresor)
            {
                case 0:
                    coffreScript.onOpen.AddListener(() => GeneratePieces(prefabsPieces[Random.Range(0, prefabsPieces.Length)], coffreScript.posObjetInterne.position, (int)(valeur * 1.5f)));
                    break;
                case 1:
                    coffreScript.onOpen.AddListener(() => GenerateTresor(prefabsObjets[Random.Range(0, prefabsObjets.Length)], coffreScript.posObjetInterne.position, (int)(valeur * 1.5f)));
                    break;
                case 2:
                    coffreScript.onOpen.AddListener(() => GeneratePotion(prefabsPotions[Random.Range(0, prefabsPotions.Length)], coffreScript.posObjetInterne.position, (int)(force * 1.5f)));
                    break;
            }
        }
        else
        {
            int typePiege = Random.Range(0, 6);
            switch (typePiege)
            {
                case 0: //Fleches normales
                    coffreScript.onOpen.AddListener(() => Debug.Log("Fleche"));
                    break;
                case 1: //Fleches empoisonnées
                    coffreScript.onOpen.AddListener(() => Debug.Log("FLECHES EMPOISONNEES"));
                    break;
                case 2: //Gaz poison
                    //Le son du gaz devrait être constant tant qu'il y a du gaz
                    coffreScript.onOpen.AddListener(() => SummonToxicGaz(coffreScript.posObjetInterne.position, 1, 0.5f, 5));
                    break;
                case 3: //Gaz dodo
                    //Le son du gaz devrait être constant tant qu'il y a du gaz
                    coffreScript.onOpen.AddListener(() => SummonSleepingGaz(coffreScript.posObjetInterne.position, 5, .5f, 5));
                    break;
                case 4: //Bombes
                    coffreScript.onOpen.AddListener(() => Debug.Log("IM BOUT TO BLOW"));
                    break;
                case 5: //Fake bombs
                    coffreScript.onOpen.AddListener(() => Debug.Log("TROLLED"));
                    break;

            }
        }
    }



    #endregion

    #region Generation Pieges
    public override void GeneratePieges()
    {
        int nbPieges = 10 + difficulty * 2;

        trapsPos = new Vector2Int[nbPieges];
        int nbPiegesPlaces = 0;



        //On genere les pieges en fonction de la diffculté --> Pr le moment pas de lien entre les items et tt
        //Donc n'importe quel pos mais pas dans les deadends pr certains

        while (nbPiegesPlaces < nbPieges)
        {
            Vector2Int posPiege = new(Random.Range(0, tailleEtage.x), Random.Range(0, tailleEtage.y));

            if (!IsTrapPosValid(posPiege))
            {
                continue;
            }

            //Nimporte ou : 
            //Sol qui s'ouvre
            //Piege a pique
            //Piege a scie 
            //Piege boulder
            //Piege a ours
            List<int> possibleTraps = { 0, 1, 2, 3, 4, 5 };

            WallState etatMurs = etage[posPiege.x, posPiege.y] & ~WallState.VISITED;

            if (etatMurs == 5 || etatMurs == 10)
            {
                //2 murs et 2 endrois ou passer
                //Piege hache
                //Mur compresseur
                possibleTraps.Add(6);
                possibleTraps.Add(7);
            }

            if (posPiege.x == tailleEtage.x - 1 || posPiege.x == 0 || posPiege.y == tailleEtage.y - 1 || posPiege.y == 0)
            {
                //Extremite
                //Fausse porte
                //Piege a fleches
                possibleTraps.Add(8);
                possibleTraps.Add(9);
            }

            int indexPiege = possibleTraps[Random.Range(0, possibleTraps.Count)];

            Debug.Log("Piege choisi : " + (Traps)indexPiege);
            //TODO : Instantiate le gameObject du piege dans la liste des pieges
            GameObject piege = Instantiate(prefabsTraps[indexPiege], posPiege, Quaternion.identity);
            //Si ça marche et qu'on a tt placé
            trapsPos[nbPiegesPlaces] = posPiege;
            nbPiegesPlaces++;


        }



    }

    /// <summary>
    /// Vérifie si la position du piège est valide ou non
    /// </summary>
    /// <param name="trapPos">La position du piège</param>
    /// <returns>True si elle est inoccupée, false sinon</returns>
    private bool IsTrapPosValid(Vector2Int trapPos)
    {
        if (deadEnds.Contains(trapPos))
        {
            return false;
        }

        foreach (Vector2Int pos in trapsPos)
        {
            if (pos == trapPos)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Summon du gaz toxique avec certains parametres
    /// </summary>
    /// <param name="position">La position du gaz</param>
    /// <param name="damage">Les degats de poison </param>
    /// <param name="expansionSpeed">Expansion du gaz</param>
    /// <param name="gazDuration">Durée avant la destruction du gaz</param>
    private void SummonToxicGaz(Vector3 position, float damage, float expansionSpeed, float gazDuration)
    {
        GameObject toxicGaz = Instantiate(prefabsTraps[Traps.TOXIC_GAZ], position, Quaternion.identity);
        toxicGaz.GetComponent<NetworkObject>().Spawn();
        toxicGaz.GetComponent<ToxicGaz>().poisonDamage = damage;
        toxicGaz.GetComponent<ToxicGaz>().expansionSpeed = expansionSpeed;

        Destroy(toxicGaz, gazDuration);
    }

    /// <summary>
    /// Summon du gaz qui fait dormir avec certains parametres
    /// </summary>
    /// <param name="position">La position du gaz</param>
    /// <param name="sleepDuration">Les degats de poison </param>
    /// <param name="expansionSpeed">Expansion du gaz</param>
    /// <param name="gazDuration">Durée avant la destruction du gaz</param>
    private void SummonSleepingGaz(Vector3 position, float sleepDuration, float expansionSpeed, float gazDuration)
    {
        GameObject sleepingGaz = Instantiate(prefabsTraps[Traps.SLEEP_GAZ], position, Quaternion.identity);
        sleepingGaz.GetComponent<NetworkObject>().Spawn();
        sleepingGaz.GetComponent<SleepingGaz>().sleepingTime = sleepDuration;
        sleepingGaz.GetComponent<ToxicGaz>().expansionSpeed = expansionSpeed;

        Destroy(sleepingGaz, gazDuration);
    }
    #endregion

}
