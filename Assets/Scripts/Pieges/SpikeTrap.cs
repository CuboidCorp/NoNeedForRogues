using UnityEngine;

public class SpikeTrap : Trap
{
    private Animator anim;

    private void Awake()
    {
        anim = transform.parent.GetComponent<Animator>();
    }

    public override void ActivateTrap()
    {
        anim.SetBool("Activated", true);
    }

    public override void DeactivateTrap()
    {
        anim.SetBool("Activated", false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MonPlayerController>().Damage(5);
            Debug.Log("Dégats : " + 5);
        }
    }
}
