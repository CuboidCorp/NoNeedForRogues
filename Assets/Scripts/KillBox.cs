using UnityEngine;

/// <summary>
/// Instant kill tt les gens qui rentrent dedans
/// </summary>
public class KillBox : MonoBehaviour
{

    /// <summary>
    /// Lorsque le joueur rentre dans la zone de mort il meurt
    /// </summary>
    /// <param name="other">Le collider du truc qui rentre dedans</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            other.gameObject.GetComponent<MonPlayerController>().Damage(1000);
        }
    }
}
