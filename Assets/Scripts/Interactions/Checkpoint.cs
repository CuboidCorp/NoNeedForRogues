using UnityEngine;

/// <summary>
/// Un checkpoint qui permet de changer les coordonnées de respawn des joueurs
/// </summary>
public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<MonPlayerController>().SetRespawnPoint(transform.position);
        }
    }
}
