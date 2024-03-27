using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Un tripwire est un piège qui se déclenche lorsqu'un joueur passe dessus, il se detruit lorsque le joueur 
/// </summary>
public class Tripwire : NetworkBehaviour
{
    [FormerlySerializedAs("onTrigger")]
    [SerializeField]
    private FunctionAction OnTrigger = new();

    /// <summary>
    /// Quand un joueur passe sur le tripwire
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SendTriggerServerRpc();
        }
    }

    /// <summary>
    /// Envoie l'info sur le serveur que le tripwire a été déclenché
    /// </summary>

    [ServerRpc(RequireOwnership = false)]
    private void SendTriggerServerRpc()
    {
        SendTriggerClientRpc();
    }

    /// <summary>
    /// Envoie l'info sur le client que le tripwire a été déclenché
    /// </summary>
    [ClientRpc]
    private void SendTriggerClientRpc()
    {
        OnTrigger.Invoke();
        Destroy(gameObject);
    }
}
