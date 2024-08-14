using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class BearTrap : Trap, IInteractable
{
    private Animator anim;

    private bool isActivated = false;
    private Vector3 enterPos;
    private GameObject playerInside;

    private Coroutine disableTrapCoroutine;

    public float damage = 5;
    public float nbSecParalysed = 10;

    /// <summary>
    /// Le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    public string interactText = "Desactiver";


    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Update()
    {
        if (isActivated)
        {
            playerInside.transform.position = enterPos;
        }
    }

    public override void ActivateTrap()
    {
        anim.SetBool("Activated", true);
    }

    public override void DeactivateTrap()
    {
        anim.SetBool("Activated", false);
        isActivated = false;
        if (disableTrapCoroutine != null)
        {
            StopCoroutine(disableTrapCoroutine);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!isActivated)
        {
            if(other.CompareTag("Player"))
            {
                isActivated = true;
                other.GetComponent<MonPlayerController>().Damage(damage);
                playerInside = other.gameObject;
                enterPos = other.transform.position;
                disableTrapCoroutine = StartCoroutine(DisableTrapIn(nbSecParalysed));
            }
            else if(other.CompareTag("Cow"))
            {
                isActivated = true;
                other.GetComponent<CowController>().UnCow();
                playerInside = other.GetComponent<CowController>().root;
                enterPos = other.transform.position;
                disableTrapCoroutine = StartCoroutine(DisableTrapIn(nbSecParalysed));
            }
        }
    }

    private IEnumerator DisableTrapIn(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        DeactivateTrap();
    }

    public void OnInteract()
    {
        if (!isActivated)
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

    public void HandleInteraction()
    {
        DeactivateTrap();
    }

}
