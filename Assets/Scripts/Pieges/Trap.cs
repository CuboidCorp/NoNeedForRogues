using Unity.Netcode;

public abstract class Trap : NetworkBehaviour
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
