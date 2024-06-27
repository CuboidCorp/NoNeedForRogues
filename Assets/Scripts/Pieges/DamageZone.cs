using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [HideInInspector] public float damage = 1;
    [HideInInspector] public bool isActivated = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && isActivated)
        {
            other.GetComponent<MonPlayerController>().Damage(damage);
        }
    }
}
