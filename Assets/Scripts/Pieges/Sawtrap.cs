using UnityEngine;

public class Sawtrap : Trap
{
    private Animator anim;

    [SerializeField] private float speed = 1;
    [SerializeField] private float damage = 1;

    private DamageZone damageZone;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        anim.speed = speed;
        damageZone = GetComponentInChildren<DamageZone>();
        damageZone.damage = damage;
    }
    public override void ActivateTrap()
    {
        anim.SetBool("Activated", true);
        damageZone.isActivated = true;
    }

    public override void DeactivateTrap()
    {
        anim.SetBool("Activated", false);
        damageZone.isActivated = false;
    }
}
