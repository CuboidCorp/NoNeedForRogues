using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Gère le piège de la boule de roche
/// </summary>
public class BoulderTrap : Trap
{
    [SerializeField] private int direction = -1; // -1 = random, 0 = x+, 1 = x-, 2 = z+, 3 = z-

    private bool activated = false;

    private GameObject boulderPrefab;

    private void Awake()
    {
        boulderPrefab = Resources.Load<GameObject>("Objets/Boulder");
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            enabled = false;
        }
    }

    /// <summary>
    /// Set la direction du boulder trap
    /// </summary>
    /// <param name="newDirection">La direction qu'on veut donner au piege</param>
    public void SetDirection(int newDirection)
    {
        direction = newDirection;
    }

    /// <summary>
    /// Permet de lancer le piège
    /// </summary>
    public override void ActivateTrap()
    {
        if (!activated)
        {
            activated = true;
            SummonBoulderServerRpc();
        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void SummonBoulderServerRpc()
    {
        GameObject boulder = Instantiate(boulderPrefab, transform.position, Quaternion.identity);
        boulder.GetComponent<Boulder>().direction = direction;
        boulder.GetComponent<NetworkObject>().Spawn();
    }

    public override void DeactivateTrap()
    {
        activated = false;
    }
}
