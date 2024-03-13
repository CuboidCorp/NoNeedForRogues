using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Un tripwire est un piège qui se déclenche lorsqu'un joueur passe dessus, il se detruit lorsque le joueur 
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
