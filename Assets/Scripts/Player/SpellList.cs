using System.Collections;
using UnityEngine;

/// <summary>
/// Stocke toutes les fonctions des sorts
/// </summary>
public class SpellList : MonoBehaviour
{
    public static string[] spells = { "Crepitus", "Lux", "Mortuus", "Ragdoll", "Infernum", "Sesamae occludit", "Penitus", "FusRoDah", "Capere", "Emitto", "Dimittas", "François François François", "Resurrectio", "Acceleratio", "Curae", "Saltus", "Polyphorphismus", "Offendas" };

    /// <summary>
    /// Crée une explosion à l'endroit souhaité
    /// </summary>
    public static void Explosion(Transform target, float radius, float degats)
    {
        AudioManager.instance.PlayOneShotClipServerRpc(target.position, AudioManager.SoundEffectOneShot.EXPLOSION);
        MultiplayerGameManager.Instance.SummonExplosionServerRpc(target.position, radius, 1);

#pragma warning disable UNT0028 // Use non-allocating physics APIs -> C'est un warning pr l'optimisation, mais on s'en fout
        Collider[] hitColliders = Physics.OverlapSphere(target.position, radius);
#pragma warning restore UNT0028 // Use non-allocating physics APIs

        foreach (Collider objetTouche in hitColliders)
        {
            if (objetTouche.CompareTag("Untagged"))
            {
                continue;
            }
            if (objetTouche.CompareTag("Cow"))
            {
                objetTouche.GetComponent<CowController>().UnCow();
                continue;
            }
            //On inflige des dégats en fonction de la distance
            float distance = Vector3.Distance(target.position, objetTouche.transform.position);
            float degatsInfliges = degats * (1 - distance / radius);
            float forceExplosion = degatsInfliges * 1000;


            if (objetTouche.CompareTag("Player"))
            {
                objetTouche.GetComponent<MonPlayerController>().Damage(degatsInfliges);

                Rigidbody[] ragdollElems = objetTouche.GetComponent<MonPlayerController>().GetRagdollRigidbodies();

                foreach (Rigidbody ragdoll in ragdollElems)
                {
                    ragdoll.AddExplosionForce(forceExplosion, target.position, radius);
                }

            }
            else if (objetTouche.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddExplosionForce(forceExplosion, target.position, radius);
            }
        }

    }

    /// <summary>
    /// On cast un ray d'une certaine distance depuis la position de la caméra du joueur, si le premier truc qu'on touche possede un script Openable, on l'ouvre
    /// </summary>
    /// <param name="source">Le transform de la cam du joueur</param>
    /// <param name="distanceInteract">La distance possible d'interaction du sort</param>
    public static void OpenSesame(Transform source, float distanceInteract)
    {
        //On ouvre la porte ou l'objet
#if UNITY_EDITOR
        Debug.DrawRay(source.position, source.forward * distanceInteract, Color.yellow, 1f);
#endif

        if (Physics.Raycast(source.position, source.forward, out RaycastHit hit, distanceInteract))
        {
            if (hit.transform.TryGetComponent(out Openable openable)) //On utilise hit.transform pr chopper le parent qui a un rigidbody
            {
                openable.Open();
            }
        }
    }

}
