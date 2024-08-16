using UnityEngine;

/// <summary>
/// Classe qui r�presente les objets tr�sors qui ont une valeur, et sont des objets ramassables
/// </summary>
public class TreasureObject : WeightedObject
{
    public int value = 1;

    public int TransformToGold()
    {
        return value;
    }
}
