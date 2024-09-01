using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TrickshotDebug : NetworkBehaviour
{
    private int compteurTrickshot = 0;

    [SerializeField] private Vector3 posTrickshot = new(-38, -3.8f, 5);
    [SerializeField] private Vector3 rotTrickshot = new(0, 0, 0);

    private GameObject trickshotActuel;
    private GameObject[] trickshots;

    [SerializeField] private TMP_Text nomPrefab;

    private void Awake()
    {
        trickshots = Resources.LoadAll<GameObject>("Donjon/Type1/Trickshots");
    }

    public void SpawnNextTrickshot()
    {
        SendCompteurChangeServerRpc(1);
    }

    public void SpawnPreviousTrickshot()
    {
        SendCompteurChangeServerRpc(-1);
    }

    public void SpawnTrickshot()
    {
        SendSpawnServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendCompteurChangeServerRpc(int change)
    {
        TryDestroy();
        compteurTrickshot = TrueModulo(compteurTrickshot + change, trickshots.Length);
        SetTextClientRpc(trickshots[compteurTrickshot].name);
    }

    [ClientRpc]
    private void SetTextClientRpc(string text)
    {
        nomPrefab.text = text;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendSpawnServerRpc()
    {
        TryDestroy();
        trickshotActuel = Instantiate(trickshots[compteurTrickshot], posTrickshot, Quaternion.Euler(rotTrickshot));
    }






    /// <summary>
    /// Essaie de détruire le trickshot actuel (UNIQUEMENT SUR LE SERV)
    /// </summary>
    private void TryDestroy()
    {
        if (trickshotActuel != null)
        {
            Destroy(trickshotActuel);
        }
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
