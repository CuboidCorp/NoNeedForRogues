using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class PressurePlate : NetworkBehaviour
{
    /// <summary>
    /// Le poids total sur la plaque (Quand 0, la plaque est reset)
    /// </summary>
    [SerializeField] private float totalWeight = 0;

    /// <summary>
    /// Le poids minimum pour presser la plaque
    /// </summary>
    [SerializeField] private float minWeightToPress = 1;

    /// <summary>
    /// La vitesse de l'animation
    /// </summary>
    [SerializeField]
    private float animationSpeed = 1;

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
    /// L'animation de press (pour obtenir la dur�e)
    /// </summary>
    [SerializeField] private AnimationClip pressAnimation;

    /// <summary>
    /// L'animation de reset (pour obtenir la dur�e)
    /// </summary>
    [SerializeField] private AnimationClip resetAnimation;

    /// <summary>
    /// Fonction � executer quand la plaque est reset
    /// </summary>
    [FormerlySerializedAs("onReset")]
    [SerializeField]
    private FunctionAction onReset = new();

    /// <summary>
    /// Fonction � executer quand la plaque est press�
    /// </summary>
    [FormerlySerializedAs("onPress")]
    [SerializeField]
    private FunctionAction onPress = new();

    private void Awake()
    {
        animator = GetComponent<Animator>();
        animator.speed = animationSpeed;
    }

    public override void OnNetworkSpawn()
    {
        if (!MultiplayerGameManager.Instance.IsServer && MultiplayerGameManager.Instance.IsClient) //Parfois bug pour la zone ou c'est d�ja plac�
        {
            GetComponent<Collider>().enabled = false;//On ne g�re pas le traitement 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Entity"))
        {
            SendEnterServerRpc(other.GetComponent<Entity>().poids);
        }
        else if (other.CompareTag("PickUp"))
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
        else if (other.CompareTag("PickUp"))
        {
            SendExitServerRpc(other.GetComponent<WeightedObject>().weight);
        }
    }

    #region Entering

    /// <summary>
    /// Fonction qui g�re quand on rentre sur la plaque de pression
    /// </summary>
    private void HandleEntering(float weight)
    {
        totalWeight += weight;
        if (totalWeight >= minWeightToPress)
        {
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.PP_DOWN);
            animator.Play(pressAnimationName);
            onPress.Invoke();
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

    }

    #endregion

    #region Exiting

    /// <summary>
    /// Fonction qui g�re quand on quitte la plaque de pression
    /// </summary>
    private void HandleExiting(float weight)
    {
        totalWeight -= weight;
        if (totalWeight < minWeightToPress)
        {
            Debug.Log("Reset");
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

    /// <summary>
    /// Set l'action � effectuer lors de la pression de la plaque
    /// </summary>
    /// <param name="action">L'action a effectuer</param>
    public void SetOnPress(UnityAction action)
    {
        onPress.AddListener(action);
    }

    /// <summary>
    /// Set l'action � effectuer lors du reset de la plaque
    /// </summary>
    /// <param name="action">L'action a effectuer</param>
    public void SetOnReset(UnityAction action)
    {
        onReset.AddListener(action);
    }
}
