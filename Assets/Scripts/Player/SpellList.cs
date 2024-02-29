using UnityEngine;

/// <summary>
/// Stocke toutes les fonctions des sorts
/// </summary>
public class SpellList : MonoBehaviour
{

    /// <summary>
    /// Crée une explosion à l'endroit souhaité
    /// </summary>
    public static void Explosion(Transform target, float radius,float degats)
    {

#pragma warning disable UNT0028 // Use non-allocating physics APIs -> C'est un warning pr l'optimisation, mais on s'en fout
        Collider[] hitColliders = Physics.OverlapSphere(target.position, radius);
#pragma warning restore UNT0028 // Use non-allocating physics APIs

        foreach (Collider objetTouche in hitColliders)
        {
            if(objetTouche.CompareTag("Untagged"))
            {
                continue;
            }
            //On inflige des dégats en fonction de la distance
            float distance = Vector3.Distance(target.position, objetTouche.transform.position);
            float degatsInfliges = degats * (1 - distance / radius);
            float forceExplosion = degatsInfliges * 1000;

            if (objetTouche.CompareTag("Player"))
            {
               
                
                objetTouche.GetComponent<MonPlayerController>().Damage(degatsInfliges);

                //On applique une force d'explosion en fonction de la distance
                objetTouche.GetComponent<Rigidbody>().AddExplosionForce(forceExplosion, target.position, radius);

            }
            else if(objetTouche.CompareTag("Ragdoll"))
            {
                //Les objets ragdoll on rajoute de la force d'explosion
                objetTouche.GetComponent<Rigidbody>().AddExplosionForce(forceExplosion, target.position, radius);
            }
        }
    }
}
