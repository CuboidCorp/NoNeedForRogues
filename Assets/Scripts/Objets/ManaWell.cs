using UnityEngine;

/// <summary>
/// Classe qui répresente les puits qui permet de regen la mana quand on va dedans
/// </summary>
public class ManaWell : MonoBehaviour
{
    private Coroutine manaGainCoroutine;

    [SerializeField] private float interval = 1.0f;
    [SerializeField] private int manaGain = 2;
    [SerializeField] private int maxMana = -1; //Si le maxMana est a -1 la source est infinie
    [SerializeField] private int manaDistributed = 0;

    private void OnTriggerStay(Collider other)
    {
        if(other.gameObject.CompareTag("Player") && manaGainCoroutine == null)
        {
            manaGainCoroutine = StartCoroutine(DistrubuteMana(other.gameObject));
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.CompareTag("Player") && manaGainCoroutine != null)
        {
            StopCoroutine(manaGainCoroutine);
        }
    }

    private IEnumerator DistrubuteMana(GameObject player)
    {
        yield return new WaitForSeconds(interval);
        player.GetComponent<MonPlayerController>().GainMana(manaGain);
        manaDistributed += manaGain;
        if(maxMana !=-1 &&  manaDistributed > maxMana)
        {
            enabled = false;
        }
        manaGainCoroutine = null;

    }
}
