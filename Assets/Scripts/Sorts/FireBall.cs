using System.Collections;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    private float explosionRange;
    private float explosionForce;

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
        SpellList.Explosion(transform, explosionRange, explosionForce);
        Destroy(gameObject);
    }
}
