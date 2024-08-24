using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PressurePlate : NetworkBehaviour
{
    /// <summary>
    /// Le poids total sur la plaque (Quand 0, la plaque est reset)
    /// </summary>
    private float totalWeight = 0;

    /// <summary>
    /// Le poids minimum pour presser la plaque
    /// </summary>
    [SerializeField] private float minWeightToPress = 1;

    /// <summary>
    /// L'animator de la plaque
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
    /// Fonction à executer quand la plaque est reset
    /// </summary>
    [FormerlySerializedAs("onReset")]
    [SerializeField]
    private FunctionAction onReset = new();

    /// <summary>
    /// Fonction à executer quand la plaque est pressé
    /// </summary>
    [FormerlySerializedAs("onPress")]
    [SerializeField]
    private FunctionAction onPress = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Entity"))
        {
            SendEnterServerRpc(other.GetComponent<Entity>().poids);
        }
        if (other.CompareTag("PickUp"))
        {
            SendEnterServerRpc(other.GetComponent<WeightedObject>().weight);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Entity"))
        {
            SendExitServerRpc(other.GetComponent<Entity>().poids);
        }
        if (other.CompareTag("PickUp"))
        {
            SendExitServerRpc(other.GetComponent<WeightedObject>().weight);
        }
    }

    #region Entering

    /// <summary>
    /// Fonction qui gère quand on rentre sur la plaque de pression
    /// </summary>
    private void HandleEntering(float weight)
    {
        totalWeight += weight;
        if (totalWeight >= minWeightToPress)
        {
            animator.Play(pressAnimationName);
            StartCoroutine(WaitForEndPress());
        }
    }

    /// <summary>
    /// Envoie un message au serveur pour dire qu'on est sur la plaque
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendEnterServerRpc(float weight)
    {
        HandleEntering(weight);
    }

    /// <summary>
    /// Attend la fin de l'animation de pression pr executer la fonction onPress
    /// </summary>
    private IEnumerator WaitForEndPress()
    {
        yield return new WaitForSeconds(pressAnimation.length);
        onPress.Invoke();
    }

    #endregion

    #region Exiting

    /// <summary>
    /// Fonction qui gère quand on quitte la plaque de pression
    /// </summary>
    private void HandleExiting(float weight)
    {
        totalWeight -= weight;
        if (totalWeight < minWeightToPress)
        {
            StopAllCoroutines();
            animator.Play(resetAnimationName);
            if (onReset != null)
                StartCoroutine(WaitForEndReset());
        }
    }

    /// <summary>
    /// Envoie un message au serveur pour dire qu'on est plus sur la plaque
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void SendExitServerRpc(float weight)
    {
        HandleExiting(weight);
    }

    /// <summary>
    /// Attend la fin de l'animation de pression pr executer la fonction onPress
    /// </summary>
    private IEnumerator WaitForEndReset()
    {
        yield return new WaitForSeconds(resetAnimation.length);
        onReset.Invoke();
    }

    #endregion
}
