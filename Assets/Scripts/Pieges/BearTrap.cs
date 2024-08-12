using UnityEngine;

public class BearTrap : Trap, IInteragissable
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
    public string interactText = ;


    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.speed = speed;
    }

    private void Update()
    {
        if(isActivated)
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
        if(disableTrapCoroutine != null)
        {
            StopCoroutine(disableTrapCoroutine);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            isActivated = true;
            other.GetComponent<MonPlayerController>().Damage(damage);
            playerInside = other.gameObject;
            enterPos = other.transform.position;
            disableTrapCoroutine = StartCoroutine(DisableTrapIn(nbSecParalysed));
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
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, SoundEffectOneShot.FAIL_INTERACT);
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

    public string GetInteractText()
    {
        return interactText;
    }

    public void HandleInteraction()
    {
        DeactivateTrap();
    }

}
