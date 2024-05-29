using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "NewDungeonTilingRules", menuName = "Dungeon/TilingRules")]
public class DungeonTilingRules : ScriptableObject
{
    public List<int> bitmasks;
    public GameObject[] tiles;
}