using Unity.Netcode;

/// <summary>
/// Classe qui répresente un objet avec un poids (Qui peut être tenu par le joueur)
/// </summary>
public class WeightedObject : NetworkBehaviour
{
    public float weight = 1;

    public NetworkVariable<bool> isHeld = new(false);

    /// <summary>
    /// Change l'etat de l'objet si il est tenu ou non
    /// </summary>
    /// <param name="newState">Le nouvel etat de l'objet</param>
    public void ChangeState(bool newState)
    {
        if(!IsServer)
        {
            ChangeStateServerRpc(newState);
            return;
        }
        isHeld.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]   
    private void ChangeStateServerRpc(bool newState)
    {
        isHeld.Value = newState;
    }
}
