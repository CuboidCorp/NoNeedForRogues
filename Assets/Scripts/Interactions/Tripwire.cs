using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Un tripwire est un pi�ge qui se d�clenche lorsqu'un joueur passe dessus, il se detruit lorsque le joueur 
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
    /// Envoie l'info sur le serveur que le tripwire a �t� d�clench�
    /// </summary>

    [ServerRpc(RequireOwnership = false)]
    private void SendTriggerServerRpc()
    {
        OnTrigger.Invoke();
        gameObject.GetComponent<NetworkObject>().Despawn(true);
    }

    /// <summary>
    /// Set l'action � effectuer lors du d�clenchement du tripwire
    /// </summary>
    /// <param name="action">L'action a effectuer</param>
    public void SetTrigger(UnityAction action)
    {
        OnTrigger.AddListener(action);
    }
}
