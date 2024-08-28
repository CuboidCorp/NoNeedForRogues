using UnityEngine;

/// <summary>
/// Despawn les items qui tombent dedans
/// </summary>
[RequireComponent(typeof(Collider))]
public class Void : MonoBehaviour
{
    private void Awake()
    {
        if(!MultiplayerGameManager.Instance.IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("PickUp"))
        {
            if(other.TryGetComponent(out TreasureObject tres))
            {
                MultiplayerGameManager.Instance.SendItemLostClientRpc(MultiplayerGameManager.SendRpcToPlayer(tres.GetLastOwner()));
            }
            other.GetComponent<NetworkObject>().Despawn(true);
        }
    }

    
    
}
