using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui r�presente les sacs d'or/ pi�ces avec lesquels on peut int�ragir pour recuperer de l'or
/// </summary>
public class GoldObject : NetworkBehaviour, IInteractable
{
    [SerializeField] private int value = 1;

    public string interactText = "Prendre";

    public void OnInteract()
    {
        HandleInteraction();
    }

    /// <summary>
    /// Renvoie le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    /// <returns>Le string qui correspond au texte d'interaction</returns>
    public string GetInteractText()
    {
        return interactText;
    }

    /// <summary>
    /// G�re l'interaction avec l'objet
    /// </summary>
    public void HandleInteraction()
    {
        //Rajoute le gold au truc du serveur
        StatsManager.Instance.AddGold(value);
        DespawnServerRpc();
    }

    /// <summary>
    /// Si qqn interagit avec le bouton on envoie un message au serv pr lui dire
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void DespawnServerRpc()
    {
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }
}
