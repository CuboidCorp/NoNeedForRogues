using UnityEngine;


[CreateAssetMenu(fileName = "NewDungeonTilingRules", menuName = "Dungeon/TilingRules")]
public class DungeonTilingRules : ScriptableObject
{
    [System.Serializable]
    public struct TilingRule
    {
        public int bitmask;
        public GameObject asset;
    }

    public TilingRule[] tilingRules;
}