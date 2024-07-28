using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AccelProjectile : MonoBehaviour
{
    private float buffDuration;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MonPlayerController>().ReceiveSpeedBoost(buffDuration);
            Destroy(gameObject);
        }
    }
    public void SetBuffDuration(float duration)
    {
        buffDuration = duration;
    }

    public IEnumerator DestroyAfterTime(float time)
    {
        yield return new WaitForSeconds(time);
        Destroy(gameObject);
    }

}
