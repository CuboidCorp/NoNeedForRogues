using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A rajouter aux objets avec lesquels le joueur peut interagir (Boutons, portes, etc.)
/// Il faut implementer la fonction HandleInteraction() pour definir le comportement de l'objet
/// </summary>
public abstract class Interactable : NetworkBehaviour
{

    /// <summary>
    /// Si on peut interagir avec l'objet
    /// </summary>
    public bool isInteractable = true;

    /// <summary>
    /// Le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    public string interactText;

    /// <summary>
    /// Le sound effect a jouer quand on ne peut pas interagir avec l'objet
    /// </summary>
    //private 

    /// <summary>
    /// Quand on interagit avec l'objet
    /// </summary>
    public virtual void OnInteract()
    {
        if(!isInteractable)
        {
            //TODO : Faire le sound effect qd on peut pas interagir
            return;
        }
        SendInteractionServerRpc();
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
    /// Si qqn interagit avec le bouton on envoie un message au serv pr lui dire
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendInteractionServerRpc()
    {
        SendInteractionClientRpc();
    }

    /// <summary>
    /// Le serveur envoie un message a tt le monde pr synchroniser l'interaction
    /// </summary>
    [ClientRpc]
    private void SendInteractionClientRpc()
    {
        HandleInteraction();
    }

    /// <summary>
    /// Gère l'interaction avec l'objet
    /// </summary>
    protected abstract void HandleInteraction();

    

}
