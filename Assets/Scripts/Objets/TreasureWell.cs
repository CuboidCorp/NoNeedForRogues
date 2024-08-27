using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Classe qui répresente les puits qui transforment les objets treasure en gold
/// </summary>
public class TreasureWell : NetworkBehaviour
{
    /// <summary>
    /// Desactivation du collider si on est pas le serveur pour avoir uniquement le serv qui fait tourner la logique
    /// </summary>
    private void Awake()
    {
        if(!IsServer)
        {
            GetComponent<Collider>().enabled = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out TreasureObject tres))
        {
            int value = tres.TransformToGold();
            AudioManager.instance.PlayOneShotClipServerRpc(transform.position, AudioManager.SoundEffectOneShot.MONEY_GAINED);
            AddGoldCollectedClientRpc(value, MultiplayerGameManager.SendRpcToPlayer(tres.lastOwner));
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

    /// <summary>
    /// Ajoute le gold de la part du dernier client 
    /// </summary>
    /// <param name="value">Valeur de l'objet a rajoute</param>
    [ClientRpc]
    private void AddGoldCollectedClientRpc(int value, ClientRpcParams cRpcParams)
    {
        StatsManager.Instance.AddGold(value);
        StatsManager.Instance.AddTrickshot();
    }
}
