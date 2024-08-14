using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [HideInInspector] public float damage = 1;
    [HideInInspector] public bool isActivated = false;
    private void OnTriggerEnter(Collider other)
    {
        if(isActivated)
        {
            if(other.CompareTag("Player"))
            {
                other.GetComponent<MonPlayerController>().Damage(damage);
            }
            else if (other.CompareTag("Cow"))
            {
                other.GetComponent<CowController>().UnCow();
            }
        }
    }
}
