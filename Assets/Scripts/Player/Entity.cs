using Unity.Netcode;
using UnityEngine;
/// <summary>
/// Classe de base pr les entites pr absraction des données commune
/// </summary>
public class Entity : NetworkBehaviour
{
    [Header("Entity Stats")]

    public float vie = 10f;
    public float poids = 20f;
        
}
