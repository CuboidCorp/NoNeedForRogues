using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui répresente un objet avec un poids (Qui peut être tenu par le joueur)
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public abstract class WeightedObject : NetworkBehaviour, IRamassable
{
    public float weight = 1;
    public string cheminCopie;

    [SerializeField] private ulong lastOwner = 0;

    public NetworkVariable<bool> isHeld = new(false);

    /// <summary>
    /// Change l'etat de l'objet
    /// </summary>
    /// <param name="newState"></param>
    public void ChangeState(bool newState)
    {
        ChangeStateServerRpc(newState);
    }

    /// <summary>
    /// Change l'etat 
    /// </summary>
    /// <param name="newState"></param>
    [ServerRpc(RequireOwnership = false)]
    private void ChangeStateServerRpc(bool newState)
    {
        isHeld.Value = newState;
    }

    /// <summary>
    /// Dit au serveur l'id du dernier joueur a avoir ramassé l'objet
    /// </summary>
    /// <param name="ownerId">L'id du joueur</param>
    [ServerRpc(RequireOwnership = false)]
    public void SendLastOwnerServerRpc(ulong ownerId)
    {
        lastOwner = ownerId;
    }

    /// <summary>
    /// Retourne l'id du dernier joueur a avoir ramassé l'objet
    /// Ne marche uniquement sur le serveur
    /// </summary>
    /// <returns>L'id du dernier joueur</returns>
    public ulong GetLastOwner()
    {
        return lastOwner;
    }

}
