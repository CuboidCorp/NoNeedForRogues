using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A rajouter aux elements avec lesquels le joueur peut interagir 
/// Les objets de type Levier ont 2 etats et executent des actions differentes en fonction de leur etat
/// En interagissant avec le levier, on change son etat
/// </summary>
[RequireComponent(typeof(Animator))]
public class Lever : Interactable
{

    /// <summary>
    /// La position du levier
    /// </summary>
    [SerializeField] private bool isSwitchedOn = false;

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

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Gère l'interaction avec l'objet
    /// </summary>
    protected override void HandleInteraction()
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
