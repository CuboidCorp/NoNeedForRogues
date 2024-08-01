using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// A rajouter aux elements avec lesquels le joueur peut interagir 
/// Les objets de type Bouton se reinitialisent apres un certain temps
/// </summary>
public class Bouton : Interactable
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

    /// <summary>
    /// Gère l'interaction avec l'objet
    /// </summary>
    public override void HandleInteraction()
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




}
