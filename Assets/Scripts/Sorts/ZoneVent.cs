using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ZoneVent : NetworkBehaviour
{
    private float pushForce;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if(other.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 direction = other.transform.position - transform.position;
            rb.AddForceAtPosition(direction.normalized * pushForce, transform.position);
        }
    }

    public void SetupZoneVent(float pushForce)
    {
        explosionRange = expRange;
    }
}
