using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Classe pour les coffres, avec lesquels on peut interagir pr les ouvrir ou utiliser un sort
/// </summary>
[RequireComponent(typeof(Collider))]
public class Chest : Openable, IInteractable
{
    /// <summary>
    /// Si on peut interagir avec l'objet
    /// </summary>
    public bool isInteractable = true; //Pr le moment les coffres sont toujours ouvrables

    /// <summary>
    /// Le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    public string interactText = "Ouvrir";

    private bool hasNeverBeenOpened = true;

    public Transform posObjetInterne;

    /// <summary>
    /// Fonction à executer quand le coffre est ouvert pr la premiere fois
    /// </summary>
    [FormerlySerializedAs("onOpen")]
    [SerializeField]
    public FunctionAction onOpen = new();

    protected override void OnOpenValueChanged(bool previous, bool current)
    {
        if (current) //Donc ouverture
        {
            anim.Play(openingAnimationName);
            interactText = "Fermer";
            if (hasNeverBeenOpened)
            {
                hasNeverBeenOpened = false;
                onOpen.Invoke();
            }
        }
        else
        {
            anim.Play(closingAnimationName);
            interactText = "Ouvrir";
        }
    }

    /// <summary>
    /// Quand on interagit avec l'objet
    /// </summary>
    public void OnInteract()
    {
        if (!isInteractable)
        {
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.FAIL_INTERACT);
            return;
        }
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
        ChangeState();
    }
}
