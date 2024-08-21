using Unity.Netcode;

public abstract class Trap : NetworkBehaviour
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
