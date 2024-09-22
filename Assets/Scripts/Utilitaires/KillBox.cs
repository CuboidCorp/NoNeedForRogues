using UnityEngine;

/// <summary>
/// Instant kill tt les gens qui rentrent dedans
/// </summary>
public class KillBox : MonoBehaviour
{

    [SerializeField] private bool isOOB = true;

    /// <summary>
    /// Lorsque le joueur rentre dans la zone de mort il meurt
    /// </summary>
    /// <param name="other">Le collider du truc qui rentre dedans</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            if (isOOB)
            {
                //Le joueur est pas censé être dans la killbox
                Debug.LogWarning("Player IN KILLBOX OOB" + other.gameObject.name);
            }
            other.gameObject.GetComponent<MonPlayerController>().Die();
            other.gameObject.GetComponent<MonPlayerController>().TpSpawn();
            //TODO : Gerer le cas des vaches aussi
        }
    }
}
