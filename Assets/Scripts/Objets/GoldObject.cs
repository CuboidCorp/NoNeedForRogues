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
        AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.MONEY_GAINED, .75f);
        SendInteractionServerRpc(MonPlayerController.instanceLocale.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendInteractionServerRpc(ulong ramasseurId)
    {
        SendStatsClientRpc(value, MultiplayerGameManager.SendRpcToPlayer(ramasseurId));
    }

    /// <summary>
    /// On renvoie au joueur la bonne info et on supprime l'objet
    /// </summary>
    /// <param name="value">La valeur de l'objet</param>
    /// <param name="clientParams">Les params pr l'envoyer à la personne concernée</param>
    [ClientRpc]
    private void SendStatsClientRpc(int value, ClientRpcParams clientParams)
    {
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
