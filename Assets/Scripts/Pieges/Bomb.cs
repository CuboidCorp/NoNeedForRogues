using System.Collections;
using UnityEngine;

public class Bomb : MonoBehaviour
{
    private float explosionRange;
    private float explosionForce;
    private bool isTroll;

    public void SetupBomb(float expRange, float expForce, bool isTrollBomb)
    {
        explosionRange = expRange;
        explosionForce = expForce;
        isTroll = isTrollBomb;
    }


    public IEnumerator ExplodeIn(float time)
    {
        yield return new WaitForSeconds(time);
        Explode();
    }

    private void Explode()
    {
        if (!isTroll)
        {
            SpellRecognition.Explosion(transform, explosionRange, explosionForce);
        }
        Destroy(gameObject);
    }
}
