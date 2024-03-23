using UnityEngine;

/// <summary>
/// G�re le pi�ge de la boule de roche
/// </summary>
public class BoulderTrap : MonoBehaviour
{
    [SerializeField] private int direction = -1; // -1 = random, 0 = x+, 1 = x-, 2 = z+, 3 = z-

    private bool activated = false;

    private GameObject boulderPrefab;

    private void Awake()
    {
        boulderPrefab = Resources.Load<GameObject>("Pieges/Boulder");
    }

    /// <summary>
    /// Permet de lancer le pi�ge
    /// </summary>
    public void SpawnTrap()
    {
        if(!activated)
        {
            GameObject boulder = Instantiate(boulderPrefab, transform.position, Quaternion.identity);
            boulder.GetComponent<Boulder>().direction = direction;
            boulder.GetComponent<Rigidbody>().isKinematic = false; //NEtwork object fait que la boule est kinematic par d�faut TODO : Empecher �a d'arriver au lieu de ce bidouillage
            activated = true;
        }
        
    }

    /// <summary>
    /// Permet de r�initialiser le pi�ge
    /// </summary>
    public void ResetTrap()
    {
        activated = false;
    }
}
