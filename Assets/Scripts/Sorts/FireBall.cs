using System.Collections;
using UnityEngine;

public class FireBall : MonoBehaviour
{
    public void OnTriggerEnter(Collider other)
    {
        //Déclenche l'explosion des qu'on touche quelque chose
        Explode(); //Ptet regarder ce qu'on touche avant d'exploser
    }

    public IEnumerator ExplodeIn(float time)
    {
        yield return new WaitForSeconds(time);
        Explode();
    }

    private void Explode()
    {
        //Explosion
    }
}
