using UnityEngine;

public class Sawtrap : Trap
{
    [SerializeField] private float speed = 1;
    [SerializeField] private float damage = 1;

    private Plateforme scieMobile;
    private DamageZone damageZone;

    private void Awake()
    {
        scieMobile = GetComponentInChildren<Plateforme>();
        scieMobile.SetSpeed(0);
        damageZone = GetComponentInChildren<DamageZone>();
        damageZone.damage = damage;
    }
    public override void ActivateTrap()
    {
        scieMobile.SetSpeed(speed);
        damageZone.isActivated = true;
    }

    public override void DeactivateTrap()
    {
        scieMobile.SetSpeed(0);
        damageZone.isActivated = false;
    }
}
