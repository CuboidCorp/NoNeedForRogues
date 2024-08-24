using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui répresente les sacs d'or/ pièces avec lesquels on peut intéragir pour recuperer de l'or
/// </summary>
[RequireComponent(typeof(Collider))]
public class GoldObject : NetworkBehaviour, IInteractable
{
    public int value = 1;

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
    /// Gère l'interaction avec l'objet
    /// </summary>
    public void HandleInteraction()
    {
        //Rajoute le gold au truc du serveur
        AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.MONEY_GAINED);
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
