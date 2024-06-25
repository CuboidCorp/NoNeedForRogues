using UnityEngine;

public class SpikeTrap : Trap
{
    private Animator anim;

    private void Awake()
    {
        anim = GetComponent<Animator>();
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
            //On regarde la vélocité du joueur en y et on fait des dégats en fonction
            float yVelocity = other.attachedRigidbody.velocity.y;
            other.GetComponent<MonPlayerController>().Damage(yVelocity * 2);
            Debug.Log("Dégats : " + yVelocity * 2);
        }
    }
}
