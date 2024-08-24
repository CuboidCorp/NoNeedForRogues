using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToxicGaz : MonoBehaviour
{
    public float expansionSpeed = .5f;
    public float poisonDamage = 1f;
    public int poisonDuration = 1;
    public float damageInterval = 1.5f;

    private Vector3 maxSize = new(100, 100, 100);
    private List<MonPlayerController> listPlayersInside;


    private void Awake()
    {
        listPlayersInside = new();
        StartCoroutine(Expansion());
        StartCoroutine(DamageAllPlayers());
    }

    /// <summary>
    /// Gère l'expansion du gaz (qui passe a travers les murs lol)
    /// </summary>
    /// <returns></returns>
    private IEnumerator Expansion()
    {
        while (true)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, maxSize, expansionSpeed * Time.deltaTime);
            yield return null;
        }
    }

    /// <summary>
    /// Fait des degats tous les damageInterval secondes aux joueurs dans le gaz sous forme de degats de poison
    /// </summary>
    /// <returns></returns>
    private IEnumerator DamageAllPlayers()
    {
        while (true)
        {
            foreach (MonPlayerController player in listPlayersInside)
            {
                player.StartPoison(poisonDamage, poisonDuration);
            }
            yield return new WaitForSeconds(damageInterval);
        }
    }

    /// <summary>
    /// Quand un joueur rentre dans le gaz on le rajoute dans la liste des joueur a empoisonner
    /// </summary>
    /// <param name="other">Le collider qui entre dans le gaz</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            listPlayersInside.Add(other.GetComponent<MonPlayerController>());
        }
    }

    /// <summary>
    /// Quand un joueur sort du gaz on le rajoute dans la liste des joueur a empoisonner
    /// </summary>
    /// <param name="other">Le collider qui sort du gaz</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            listPlayersInside.Remove(other.GetComponent<MonPlayerController>());
        }
    }
}
