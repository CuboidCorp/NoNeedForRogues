using UnityEngine;
/// <summary>
/// Classe de la camera troisieme personne pr qu'elle essaye de regarder le joueur quand elle est active --> Ptet regarder le ragdoll au lieu du joueur a voir
/// </summary>
public class CameraThirdPersonController : MonoBehaviour
{
    private void Update()
    {
        transform.LookAt(transform.parent);
    }


}
