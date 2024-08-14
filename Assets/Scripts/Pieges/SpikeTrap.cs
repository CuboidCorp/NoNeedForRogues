using UnityEngine;

public class SpikeTrap : Trap
{
    private Animator anim;

    [SerializeField] private float speed = 1;
    [SerializeField] private float damage = 5;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.speed = speed;
    }

    public override void ActivateTrap()
    {
        anim.SetBool("Activated", true);
    }

    public override void DeactivateTrap()
    {
        anim.SetBool("Activated", false);
    }

    private void OnTriggerEnter(Collider other) //TODO : Temp on fera en fonction de la vélocité je pense
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MonPlayerController>().Damage(damage);
        }
        else if(other.CompareTag("Cow"))
        {
            other.GetComponent<CowController>().UnCow();
        }
    }
}
