using System.Collections;
using Unity.Netcode;
using UnityEngine;

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
            other.gameObject.GetComponent<GhostController>().Respawn();
            Destroy(gameObject);
        }
    }

    public IEnumerator DestroyIn(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
