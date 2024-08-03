using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Fusrohdah : MonoBehaviour
{
    private float explosionRange;
    private float explosionForce;

    private void OnTriggerEnter(Collider other)
    {
        GameObject objetTouche = other.gameObject;
        //Déclenche l'explosion des qu'on touche quelque chose
        if (objetTouche.layer != 0)
        {
            return;
        }

        if (objetTouche.TryGetComponent(out Rigidbody rb))
        {
            Debug.Log("J'ai touché : " + objetTouche.name);
            float distance = Vector3.Distance(transform.position, objetTouche.transform.position);
            float degatsInfliges = explosionForce * (1 - distance / explosionRange);
            float forceExplosion = degatsInfliges * 1000;

            if (objetTouche.CompareTag("Player"))
            {
                //On ragdoll le joueur
                MultiplayerGameManager.Instance.SyncRagdollStateServerRpc(objetTouche.GetComponent<NetworkObject>().OwnerClientId, true);

                Rigidbody[] ragdollElems = objetTouche.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

                foreach (Rigidbody ragdoll in ragdollElems)
                {
                    ragdoll.AddExplosionForce(forceExplosion, transform.position, explosionRange);
                }
            }
            else
            {
                //TODO : Surement faut que le serveur fasse ça
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
