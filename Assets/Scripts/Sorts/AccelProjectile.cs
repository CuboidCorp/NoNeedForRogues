using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class AccelProjectile : NetworkBehaviour
{
    private float buffDuration;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            MultiplayerGameManager.Instance.SendSpeedBoostClientRpc(buffDuration, new ClientRpcParams() { Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { other.GetComponent<NetworkObject>().OwnerClientId } } });
            Destroy(gameObject);
        }
    }
    public void SetBuffDuration(float duration)
    {
        buffDuration = duration;
    }

    public IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

}
