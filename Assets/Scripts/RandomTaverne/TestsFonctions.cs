using UnityEngine;

public class TestsFonctions : MonoBehaviour
{
    private int compteurTrickshot = 0;

    [SerializeField] private Vector3 posTrickshot = new(-38, -3.8f, 5);
    [SerializeField] private Vector3 rotTrickshot = new(0, 0, 0);

    private GameObject trickshotActuel;
    private GameObject[] trickshots;

    private void Awake()
    {
        trickshots = Resources.LoadAll<GameObject>("Donjon/Type1/Trickshots");
    }

    public void TestChest()
    {
        Debug.Log("TestChest");
    }

    public void TestChest2()
    {
        GameObject gaz = Resources.Load<GameObject>("Donjon/Traps/ToxicGaz");
        GameObject gazObj = Instantiate(gaz, new Vector3(-19, -3.6f, -8), Quaternion.identity);
        Destroy(gazObj, 5);
    }

    public void SpawnNextTrickshot()
    {
        if (trickshotActuel != null)
        {
            Destroy(trickshotActuel);
        }
        //On summon le truc actuel
        trickshotActuel = Instantiate(trickshots[compteurTrickshot], posTrickshot, Quaternion.Euler(rotTrickshot));

        compteurTrickshot = (compteurTrickshot + 1) % trickshots.Length;
    }

    public void SpawnPreviousTrickshot()
    {
        if (trickshotActuel != null)
        {
            Destroy(trickshotActuel);
        }

        compteurTrickshot = (compteurTrickshot - 1) % trickshots.Length;

        trickshotActuel = Instantiate(trickshots[compteurTrickshot], posTrickshot, Quaternion.Euler(rotTrickshot));
    }

}
