using System.Collections;
using UnityEngine;

public class HealProjectile : MonoBehaviour
{

    private float healAmount = 10;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<MonPlayerController>().Heal(healAmount);
            Destroy(gameObject);
        }
    }

    public void SetHealAmount(float amount)
    {
        healAmount = amount;
    }

    public IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }
}
