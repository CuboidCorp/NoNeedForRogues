using Unity.Netcode;
using UnityEngine;

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
            GetComponent<Collider>().enabled = false;
        }
        else
        {
            GetComponent<Collider>().size = tailleCollider;
            GetComponent<Collider>().center = posCenter;
        }
        //TODO : Changer la taille du truc de particule aussi et la vitesse pr que les particules arrivent a la fin
        Shape shape = ps.shape;
        shape.scale = tailleCollider;

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
