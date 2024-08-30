using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Zone de vent qui pousse tous les gens dans son trigger
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class ZoneVent : NetworkBehaviour
{
    [SerializeField] private float forceVentilo = 1;
    [SerializeField] private Vector3 posCenter;
    [SerializeField] private Vector3 tailleCollider;

    private ParticleSystem ps;

    public override void OnNetworkSpawn()
    {
        ps = GetComponentInChildren<ParticleSystem>(); //TODO : TRouver comment recup le bon collider aussi

        if (!IsServer)
        {
            GetComponent<BoxCollider>().enabled = false;
        }
        else
        {
            GetComponent<BoxCollider>().size = tailleCollider;
            GetComponent<BoxCollider>().center = posCenter;
        }
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.scale = new Vector3(tailleCollider.x, tailleCollider.y, 1);

        //Distance parcourue par les particules = lifetime * speed
        //Donc lifetime = distance/speed
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = tailleCollider.z / main.startSpeed.constant;

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
    /// Donne au ventilo sa puissance, la taille de son collider et sa position
    /// </summary>
    /// <param name="pushForce">Puissance du ventilo</param>
    /// <param name="posCollider">Position du centre du collider</param>
    /// <param name="tailleCollider">Taille du collider</param>
    public void SetupZoneVent(float pushForce, Vector3 posCollider, Vector3 taille)
    {
        forceVentilo = pushForce;
        posCenter = posCollider;
        tailleCollider = taille;

    }
}
