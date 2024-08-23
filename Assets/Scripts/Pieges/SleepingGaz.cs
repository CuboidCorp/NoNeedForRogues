using UnityEngine;

/// <summary>
/// Gaz qui fait ragdoll ou "s'endormir" les joueurs qui le respire
/// </summary>
public class SleepingGaz : MonoBehaviour
{
    public float expansionSpeed = .5f;
    public float sleepingTime = 5f;

    private Vector3 maxSize = new(100, 100, 100);


    private void Awake()
    {
        listPlayersInside = [];
        StartCoroutine(Expansion());
    }

    /// <summary>
    /// Gère l'expansion du gaz (qui passe a travers les murs lol)
    /// </summary>
    /// <returns></returns>
    private IEnumerator Expansion()
    {
        while(true)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, maxSize, expansionSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Quand un joueur rentre dans le gaz on l'endort
    /// </summary>
    /// <param name="other">Le collider qui entre dans le gaz</param>
    private void OnTriggerEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            MultiplayerGameManager.Instance.SetRagdollTempClientRpc(sleepingTime, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { other.GetComponent<NetworkObject>().OwnerClientId }
                }
            });
        }
    }
}
