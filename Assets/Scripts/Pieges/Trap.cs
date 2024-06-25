using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Trap : MonoBehaviour
{
    /// <summary>
    /// Active le piège
    /// </summary>
    public abstract void ActivateTrap();

    /// <summary>
    /// Désactive et / ou réinitialise le piège
    /// </summary>
    public abstract void DeactivateTrap();
}
