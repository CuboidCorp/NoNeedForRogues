using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Trap : MonoBehaviour
{
    /// <summary>
    /// Active le pi�ge
    /// </summary>
    public abstract void ActivateTrap();

    /// <summary>
    /// D�sactive et / ou r�initialise le pi�ge
    /// </summary>
    public abstract void DeactivateTrap();
}
