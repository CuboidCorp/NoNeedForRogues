using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui r�presente un objet avec un poids (Qui peut �tre tenu par le joueur)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class WeightedObject : NetworkBehaviour, IRamassable
{
    public float weight = 1;
    public string cheminCopie;

    public NetworkVariable<bool> isHeld = new(false);

    /// <summary>
    /// Change l'etat de l'objet si il est tenu ou non
    /// </summary>
    /// <param name="newState">Le nouvel etat de l'objet</param>
    public void ChangeState(bool newState)
    {
        if (!IsServer)
        {
            ChangeStateServerRpc(newState);
            return;
        }
        isHeld.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeStateServerRpc(bool newState)
    {
        isHeld.Value = newState;
    }
}
