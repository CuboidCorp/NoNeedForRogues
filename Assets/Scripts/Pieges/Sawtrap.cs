using Unity.Netcode;
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
        ChangePlateformeClientRpc(speed);
        damageZone.isActivated = true;
    }

    [ClientRpc]
    private void ChangePlateformeClientRpc(float moveSpeed)
    {
        scieMobile.SetSpeed(moveSpeed);
    }

    public override void DeactivateTrap()
    {
        ChangePlateformeClientRpc(0);
        damageZone.isActivated = false;
    }
}
