using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AxeTrap : Trap
{
    /// <summary>
    /// Vitesse de l'animation
    /// </summary>
    public float animationSpeed = 1;

    [SerializeField] private float damage = 5;

    private bool activated = false;

    private Animator animator;

    private DamageZone[] dmgZones;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        animator.speed = animationSpeed;
        dmgZones = GetComponentsInChildren<DamageZone>();
        foreach (DamageZone dmgZone in dmgZones)
        {
            dmgZone.damage = damage;
        }
    }

    public override void ActivateTrap()
    {
        if (!activated)
        {
            activated = true;
            animator.SetBool("Activated", true);
            foreach (DamageZone dmgZone in dmgZones)
            {
                dmgZone.isActivated = true;
            }
        }

    }

    public override void DeactivateTrap()
    {
        activated = false;
        animator.SetBool("Activated", false);
        foreach (DamageZone dmgZone in dmgZones)
        {
            dmgZone.isActivated = false;
        }
    }

}
