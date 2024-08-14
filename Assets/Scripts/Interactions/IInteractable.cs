/// <summary>
/// A rajouter aux objets avec lesquels le joueur peut interagir (Boutons, portes, etc.)
/// Il faut implementer la fonction HandleInteraction() pour definir le comportement de l'objet
/// </summary>
public interface IInteractable
{
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
    public void HandleInteraction();

}
