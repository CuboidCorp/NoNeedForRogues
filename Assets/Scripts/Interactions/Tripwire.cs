using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Un tripwire est un pi�ge qui se d�clenche lorsqu'un joueur passe dessus, il se detruit lorsque le joueur 
/// </summary>
public class Tripwire : MonoBehaviour
{
    [FormerlySerializedAs("onTrigger")]
    [SerializeField]
    private FunctionAction OnTrigger = new();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SendTriggerServerRpc();
            Destroy(gameObject);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendTriggerServerRpc()
    {
        SendTriggerClientRpc();
    }

    [ClientRpc]
    private void SendTriggerClientRpc()
    {
        OnTrigger.Invoke();
    }
}
