using Unity.Netcode;
using UnityEngine;

public class SpikeTrap : Trap
{
    private Animator anim;
    private DamageZone damageZone;

    [SerializeField] private float speed = 1;
    [SerializeField] private float damage = 5;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        anim.speed = speed;
        damageZone = GetComponentInChildren<DamageZone>();
        damageZone.damage = damage;
    }

    public override void ActivateTrap()
    {
        damageZone.isActivated = true;
        SendActivationClientRpc(true);
    }

    public override void DeactivateTrap()
    {
        damageZone.isActivated = false;
        SendActivationClientRpc(false);
    }

    [ClientRpc]
    private void SendActivationClientRpc(bool activated)
    {
        anim.SetBool("Activated", activated);
    }
}
