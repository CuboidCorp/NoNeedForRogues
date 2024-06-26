using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sawtrap : Trap
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
}
