using System;
using Unity.Netcode;
using UnityEngine.Events;

/// <summary>
/// A rajouter aux objets avec lesquels le joueur peut interagir (Boutons, portes, etc.)
/// </summary>
public abstract class Interactable : NetworkBehaviour
{
    /// <summary>
    /// Quand on interagit avec l'objet
    /// </summary>
    public abstract void OnInteract();

    /// <summary>
    /// Classe d'evenement pour les actions à executer
    /// </summary>
    [Serializable]
    protected class FunctionAction : UnityEvent { }
}
