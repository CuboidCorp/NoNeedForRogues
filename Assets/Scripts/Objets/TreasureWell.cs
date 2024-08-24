using UnityEngine;

/// <summary>
/// Classe qui répresente les puits qui transforment les objets treasure en gold
/// </summary>
public class TreasureWell : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out TreasureObject tres))
        {
            int value = tres.TransformToGold();
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.MONEY_GAINED);
            StatsManager.Instance.AddGold(value);
            Destroy(other.gameObject);
        }
        else
        {
            if (other.gameObject.TryGetComponent(out Rigidbody rb))
            {
                //On essaye de renvoyer le truc qu'on a reçu
                rb.velocity = -1 * rb.velocity;
            }
        }
    }
}
