using System.Collections;
using Unity.Netcode;
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

    public bool isInteractable = true;

    [SerializeField] private string interactText = "Presser";

    /// <summary>
    /// Le nom de l'animation de pression
    /// </summary>
    [SerializeField] private string pressAnimationName = "Press";

    /// <summary>
    /// Le nom de l'animation de reset
    /// </summary>
    [SerializeField] private string resetAnimationName = "Reset";

    /// <summary>
    /// L'animation de press (pour obtenir la durée)
    /// </summary>
    [SerializeField] private AnimationClip pressAnimation;

    /// <summary>
    /// L'animation de reset (pour obtenir la durée)
    /// </summary>
    [SerializeField] private AnimationClip resetAnimation;

    /// <summary>
    /// Fonction à executer quand le bouton est reset
    /// </summary>
    [FormerlySerializedAs("onReset")]
    [SerializeField]
    private FunctionAction onReset = new();

    /// <summary>
    /// Fonction à executer quand le bouton est pressé
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
    /// Gère l'interaction avec l'objet
    /// </summary>
    public void HandleInteraction()
    {
        StopAllCoroutines();
        animator.Play(pressAnimationName);
        onClick.Invoke();
        StartCoroutine(ResetState());
    }

    /// <summary>
    /// Reinitialise le bouton à la fin de l'animation de reset
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
    /// Le serv gère l'interaction, si les actions ont besoin d'etre sur tt le monde elles doivent etre faites avec une client rpc
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendInteractionServerRpc()
    {
        HandleInteraction();
    }


}
