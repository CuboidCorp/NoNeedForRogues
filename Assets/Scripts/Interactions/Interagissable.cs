using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A rajouter aux objets avec lesquels le joueur peut interagir (Boutons, portes, etc.)
/// Il faut implementer la fonction HandleInteraction() pour definir le comportement de l'objet
/// </summary>
public interface Interagissable
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
    public void OnInteract();

    /// <summary>
    /// Renvoie le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    /// <returns>Le string qui correspond au texte d'interaction</returns>
    public string GetInteractText();

    /// <summary>
    /// Gère l'interaction avec l'objet
    /// </summary>
    protected void HandleInteraction();

    

}
