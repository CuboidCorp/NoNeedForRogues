using System;
using System.Linq;
using UnityEngine;

public class GenerationItems : MonoBehaviour
{
    private GameObject[] potionsPrefab;

    private void Awake()
    {
        potionsPrefab = Resources.LoadAll<GameObject>("Objets/Potions");
    }

    private void GeneratePotion(Vector3 position)
    {
        GameObject potion = Instantiate(potionsPrefab[UnityEngine.Random.Range(0, potionsPrefab.Length)], position, Quaternion.identity);
        int potionType = UnityEngine.Random.Range(0, (int)Enum.GetValues(typeof(PotionType)).Cast<PotionType>().Max());
        potion.GetComponent<PotionObject>().SetType(potionType);
    }
}
