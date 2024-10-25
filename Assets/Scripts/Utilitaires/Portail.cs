using UnityEngine;

public class Portail : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MonPlayerController>().TpSpawn();
        }
    }
}
