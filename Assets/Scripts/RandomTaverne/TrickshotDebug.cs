using TMPro;
using UnityEngine;

public class TrickshotDebug : MonoBehaviour
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

    public void SpawnNextTrickshot(TMP_Text text)
    {
        if (trickshotActuel != null)
        {
            Destroy(trickshotActuel);
        }

        compteurTrickshot = TrueModulo(compteurTrickshot + 1, trickshots.Length);

        text.text = trickshots[compteurTrickshot].name;


    }

    public void SpawnTrickshot()
    {
        trickshotActuel = Instantiate(trickshots[compteurTrickshot], posTrickshot, Quaternion.Euler(rotTrickshot));
    }


    public void SpawnPreviousTrickshot(TMP_Text text)
    {
        if (trickshotActuel != null)
        {
            Destroy(trickshotActuel);
        }

        compteurTrickshot = TrueModulo(compteurTrickshot - 1, trickshots.Length);
        text.text = trickshots[compteurTrickshot].name;

    }

    /// <summary>
    /// Parce qu'en c# le modulo n'est pas un vrai modulo c'est juste le reste -->Ce qui pose problème avec les nombres négatifs
    /// </summary>
    /// <param name="a">Numero A</param>
    /// <param name="b">Numero B</param>
    /// <returns>Retourne A modulo B</returns>
    private int TrueModulo(int a, int b)
    {
        return (a % b + b) % b;
    }

}
