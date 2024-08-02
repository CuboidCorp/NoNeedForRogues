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
        GameObject potion = Instantiate(potionsPrefab[Random.Range(0, potionsPrefab.length)], position, Quaternion.identity);
        int potionType = Random.Range(0,Enum.GetValues(typeof(PotionType)).Cast<PotionType>().Max());
        potion.GetComponent<PotionObject>().SetType(potionType);
    }
}
