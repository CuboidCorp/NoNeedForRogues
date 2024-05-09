using UnityEngine;

public class RespawnPoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("RespawnPoint OnTriggerEnter" + other.name);
        if (other.CompareTag("PlayerGhost"))
        {
            other.gameObject.GetComponent<GhostController>().Respawn();
        }
    }
}
