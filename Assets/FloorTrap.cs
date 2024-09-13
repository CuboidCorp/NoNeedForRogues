using UnityEngine;

public class FloorTrap : MonoBehaviour
{
    [SerializeField] private string cheminSolOuvrant = "Pieges/SolOuvrant";

    public string GetSolOuvrant()
    {
        return cheminSolOuvrant;
    }
}
