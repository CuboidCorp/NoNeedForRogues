using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A rajouter aux elements avec lesquels le joueur peut interagir 
/// Les objets de type Levier ont 2 etats et executent des actions differentes en fonction de leur etat
/// En interagissant avec le levier, on change son etat
/// </summary>
[RequireComponent(typeof(Animator))]
public class Lever : NetworkBehaviour, IInteractable
{

    /// <summary>
    /// La position du levier
    /// </summary>
    public bool isSwitchedOn = false;

    /// <summary>
    /// L'animator du levier
    /// </summary>
    private Animator animator;

    /// <summary>
    /// L'animation de on (pour obtenir la durée)
    /// </summary>
    [SerializeField] private AnimationClip onAnimation;

    /// <summary>
    /// L'animation de off (pour obtenir la durée)
    /// </summary>
    [SerializeField] private AnimationClip offAnimation;

    /// <summary>
    /// Fonction à executer quand le levier est off
    /// </summary>
    [FormerlySerializedAs("offAction")]
    [SerializeField]
    private FunctionAction offAction = new();

    /// <summary>
    /// Fonction à executer quand le levier est mis a on
    /// </summary>
    [FormerlySerializedAs("onAction")]
    [SerializeField]
    private FunctionAction onAction = new();

    /// <summary>
    /// Si on peut interagir avec l'objet
    /// </summary>
    public bool isInteractable = true;

    /// <summary>
    /// Le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    public string interactText;

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
        if (!isSwitchedOn)
        {
            animator.SetBool("Activated", true);
            interactText = "Eteindre";
            onAction.Invoke();
            StartCoroutine(WaitForEndAnimationOn());
        }
        else
        {
            animator.SetBool("Activated", false);
            offAction.Invoke();
            interactText = "Allumer";
            StartCoroutine(WaitForEndAnimationOff());
        }
        isSwitchedOn = !isSwitchedOn;
    }

    /// <summary>
    /// Coroutine pour attendre la fin de l'animation de switch (Pour eviter de spammer le bouton)
    /// </summary>
    /// <returns>Quand on peut switch encore</returns>
    private IEnumerator WaitForEndAnimationOn()
    {
        isInteractable = false;
        yield return new WaitForSeconds(onAnimation.length);
        isInteractable = true;
    }

    /// <summary>
    /// Coroutine pour attendre la fin de l'animation de switch (Pour eviter de spammer le bouton)
    /// </summary>
    /// <returns>Quand on peut switch encore</returns>
    private IEnumerator WaitForEndAnimationOff()
    {
        isInteractable = false;
        yield return new WaitForSeconds(offAnimation.length);
        isInteractable = true;
    }
}
