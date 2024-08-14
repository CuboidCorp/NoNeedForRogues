using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A rajouter aux elements avec lesquels le joueur peut interagir 
/// Les objets de type Bouton se reinitialisent apres un certain temps
/// </summary>
[RequireComponent(typeof(Animator))]
public class Bouton : NetworkBehaviour, IInteractable
{

    /// <summary>
    /// L'animator du bouton
    /// </summary>
    private Animator animator;

    /// <summary>
    /// Le nom de l'animation de pression
    /// </summary>
    [SerializeField] private string pressAnimationName = "Press";

    /// <summary>
    /// Le nom de l'animation de reset
    /// </summary>
    [SerializeField] private string resetAnimationName = "Reset";

    /// <summary>
    /// L'animation de press (pour obtenir la dur�e)
    /// </summary>
    [SerializeField] private AnimationClip pressAnimation;

    /// <summary>
    /// L'animation de reset (pour obtenir la dur�e)
    /// </summary>
    [SerializeField] private AnimationClip resetAnimation;

    /// <summary>
    /// Fonction � executer quand le bouton est reset
    /// </summary>
    [FormerlySerializedAs("onReset")]
    [SerializeField]
    private FunctionAction onReset = new();

    /// <summary>
    /// Fonction � executer quand le bouton est press�
    /// </summary>
    [FormerlySerializedAs("onClick")]
    [SerializeField]
    private FunctionAction onClick = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void OnInteract()
    {
        if (!isInteractable)
        {
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.FAIL_INTERACT);
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
    /// G�re l'interaction avec l'objet
    /// </summary>
    public void HandleInteraction()
    {
        StopAllCoroutines();
        animator.Play(pressAnimationName);
        onClick.Invoke();
        StartCoroutine(ResetState());
    }

    /// <summary>
    /// Reinitialise le bouton � la fin de l'animation de reset
    /// </summary>
    /// <returns>Quand tout est fini</returns>
    private IEnumerator ResetState()
    {
        yield return new WaitForSeconds(pressAnimation.length);
        animator.Play(resetAnimationName);
        yield return new WaitForSeconds(resetAnimation.length);
        onReset.Invoke();
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


}
