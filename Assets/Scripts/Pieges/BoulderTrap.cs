using System.Collections;
using UnityEngine;

/// <summary>
/// Gère le piège de la boule de roche
/// </summary>
public class BoulderTrap : MonoBehaviour
{
    [SerializeField] private int direction = -1; // -1 = random, 0 = x+, 1 = x-, 2 = z+, 3 = z-

    private GameObject boulderPrefab;

    private void Awake()
    {
        boulderPrefab = Resources.Load<GameObject>("Pieges/Boulder");
    }

    public void SpawnTrap()
    {
        GameObject boulder = Instantiate(boulderPrefab, transform.position, Quaternion.identity);
        boulder.GetComponent<Boulder>().direction = direction;
        boulder.GetComponent<Rigidbody>().isKinematic = false; //NEtwork object fait que la boule est kinematic par défaut TODO : Empecher ça d'arriver au lieu de ce bidouillage
    }
}
