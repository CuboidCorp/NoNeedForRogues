using UnityEngine;

public class TriggerMusique : MonoBehaviour
{
    private bool isTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTriggered)
        {
            isTriggered = true;
            GetComponent<AudioSource>().enabled = true;
        }
    }
}
