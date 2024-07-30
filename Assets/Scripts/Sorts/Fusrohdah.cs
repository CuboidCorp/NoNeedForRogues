using System.Collections;
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
        
        if(objetTouche.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Debug.Log("J'ai touché : " + objetTouche.name);
            float forceExplosion = degatsInfliges * 1000;

            if (objetTouche.CompareTag("Player"))
            {
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

    public void SetupFusRohDah(float expRange,float expForce)
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
