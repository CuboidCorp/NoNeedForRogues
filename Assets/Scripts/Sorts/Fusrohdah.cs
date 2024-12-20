using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Fusrohdah : NetworkBehaviour
{
    private float explosionRange;
    private float explosionForce;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject objetTouche = other.gameObject;
        //D�clenche l'explosion des qu'on touche quelque chose
        if (objetTouche.layer != 0)
        {
            return;
        }

        if (objetTouche.TryGetComponent(out Rigidbody rb))
        {
            float distance = Vector3.Distance(transform.position, objetTouche.transform.position);
            float degatsInfliges = explosionForce * (1 - distance / explosionRange);
            float forceExplosion = degatsInfliges * 1000;

            if (objetTouche.CompareTag("Player"))
            {
                //On ragdoll le joueur
                MultiplayerGameManager.Instance.SetRagdollTempClientRpc(2, new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new ulong[] { objetTouche.GetComponent<NetworkObject>().OwnerClientId }
                    }
                });
                Rigidbody[] ragdollElems = objetTouche.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

                foreach (Rigidbody ragdoll in ragdollElems)
                {
                    ragdoll.AddExplosionForce(forceExplosion, transform.position, explosionRange);
                }
            }
            else
            {
                rb.AddExplosionForce(forceExplosion, transform.position, explosionRange);
            }
        }
    }

    public void SetupFusRohDah(float expRange, float expForce)
    {
        explosionRange = expRange;
        explosionForce = expForce;
    }

    public IEnumerator DestroyIn(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
