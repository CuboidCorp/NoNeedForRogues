using Unity.Netcode;
using UnityEngine;

public class ZoneVent : NetworkBehaviour
{
    [SerializeField] private float forceVentilo = 1;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    public void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out Rigidbody rb))
        {
            Vector3 direction = other.transform.position - transform.position;
            rb.AddForceAtPosition(direction.normalized * forceVentilo, transform.position);
        }
    }

    /// <summary>
    /// Donne au ventilo sa puissance
    /// </summary>
    /// <param name="pushForce">Puissance du ventilo</param>
    public void SetupZoneVent(float pushForce)
    {
        forceVentilo = pushForce;
    }
}
