using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sort qui ramène à la vie le fantôme touché
/// </summary>
public class ResurrectionSpell : NetworkBehaviour
{

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerGhost"))
        {
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.RESURRECTION);
            SendRespawnClientRpc(other.gameObject, new ClientRpcParams { Send = new ClientRpcSendParams { TargetClientIds = new[] { other.GetComponent<NetworkObject>().OwnerClientId } } });
            GetComponent<NetworkObject>().Despawn(true);
        }
    }

    /// <summary>
    /// Dit au client touché de respawn 
    /// </summary>
    /// <param name="ghostObj">Le ghost qui doit respawn</param>
    /// <param name="client">Le client qui doit respawn</param>
    [ClientRpc]
    private void SendRespawnClientRpc(NetworkObjectReference ghostObj, ClientRpcParams client)
    {
        ((GameObject)ghostObj).GetComponent<GhostController>().Respawn();
    }

    public IEnumerator DestroyIn(float time)
    {
        yield return new WaitForSeconds(time);
        GetComponent<NetworkObject>().Despawn(true);
    }
}
