using System.Collections;
using UnityEngine;

public class ResurrectionSpell : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerGhost"))
        {
            other.gameObject.GetComponent<GhostController>().Respawn();
            Destroy(gameObject);
        }
    }

    public IEnumerator DestroyIn(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
