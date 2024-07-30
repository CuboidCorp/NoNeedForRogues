using System.Collections;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    private GameObject explosion;

    private float explosionRange;
    private float explosionForce;

    private void Awake()
    {
        explosion = Resources.Load<GameObject>("Sorts/Explosion");
    }
    public void OnTriggerEnter(Collider other)
    {
        //Déclenche l'explosion des qu'on touche quelque chose
        if (other.gameObject.layer != 0)
        {
            return;
        }
        Debug.Log("J'ai touché : " + other.gameObject.name);
        Explode(); //Ptet regarder ce qu'on touche avant d'exploser
    }

    public void SetupFireBall(float expRange, float expForce)
    {
        explosionRange = expRange;
        explosionForce = expForce;
    }

    public IEnumerator ExplodeIn(float time)
    {
        yield return new WaitForSeconds(time);
        Explode();
    }

    private void Explode()
    {
        GameObject explosionGo = Instantiate(explosion, transform.position, Quaternion.identity); //Explosion
        explosionGo.transform.localScale = new Vector3(explosionRange, explosionRange, explosionRange);

        //Explosion
#pragma warning disable UNT0028 // Use non-allocating physics APIs -> C'est un warning pr l'optimisation, mais on s'en fout
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRange);
#pragma warning restore UNT0028 // Use non-allocating physics APIs

        foreach (Collider objetTouche in hitColliders)
        {
            if (objetTouche.CompareTag("Untagged"))
            {
                continue;
            }
            //On inflige des dégats en fonction de la distance
            float distance = Vector3.Distance(transform.position, objetTouche.transform.position);
            float degatsInfliges = explosionForce * (1 - distance / explosionRange);
            float forceExplosion = degatsInfliges * 1000;

            if (objetTouche.CompareTag("Player"))
            {
                objetTouche.GetComponent<MonPlayerController>().Damage(degatsInfliges);

                Rigidbody[] ragdollElems = objetTouche.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

                foreach (Rigidbody ragdoll in ragdollElems)
                {
                    ragdoll.AddExplosionForce(forceExplosion, transform.position, explosionRange);
                }

            }
            else if (objetTouche.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddExplosionForce(forceExplosion, transform.position, explosionRange);
            }
        }
        gameObject.GetComponent<Collider>().enabled = false;
        StartCoroutine(DeleteAfterExp(explosionGo));
    }

    private IEnumerator DeleteAfterExp(GameObject exp)
    {
        yield return new WaitForSeconds(0.5f);
        Destroy(exp);
        Destroy(gameObject);
    }
}
