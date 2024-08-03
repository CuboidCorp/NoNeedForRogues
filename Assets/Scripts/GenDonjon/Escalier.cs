using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui représente les escaliers pour allez vers le haut / bas des niveaux
/// </summary>
public class Escalier : NetworkBehaviour
{
    /// <summary>
    /// Si les escaliers vont vers le haut
    /// </summary>
    [SerializeField] private bool isUpStairs;

    private List<ulong> playersInside;

    [SerializeField] private TMP_Text titreEscalier;
    [SerializeField] private TMP_Text countdownEscalier;

    public Coroutine countDownCoroutine;

    private void Awake()
    {
        playersInside = new();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Le joueur qui rentre est donc ready
        Debug.Log(MultiplayerGameManager.Instance.gameCanStart);
        if ((MultiplayerGameManager.Instance.gameCanStart || !IsHost) && other.gameObject.CompareTag("Player"))
        {
            ulong playerId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            playersInside.Add(playerId);
            MultiplayerGameManager.Instance.SyncPlayerStateServerRpc(playerId, true, isUpStairs);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            ulong playerId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            playersInside.Remove(playerId);
            MultiplayerGameManager.Instance.SyncPlayerStateServerRpc(playerId, false);
        }
    }

    #region Countdown

    /// <summary>
    /// Commence le countdown affiché près des escaliers
    /// </summary>
    /// <param name="nbSec">Le nombre de sec a faire pr le countdown</param>
    public void StartCountdown(int nbSec)
    {
        countDownCoroutine = StartCoroutine(DoCountdown(nbSec));
    }

    /// <summary>
    /// Le compte à rebours pour le déplacement dans le niveau
    /// </summary>
    /// <param name="nbSec">Le nombre de sec a faire pr le countdown</param>
    private IEnumerator DoCountdown(int nbSec)
    {
        titreEscalier.text = "Déplacement dans";
        int cptSec = 0;
        while (cptSec < nbSec)
        {
            countdownEscalier.text = (nbSec - cptSec) + "";
            yield return new WaitForSeconds(1);
            cptSec++;
        }
        titreEscalier.text = "";
        countdownEscalier.text = "";
    }

    /// <summary>
    /// Annule le countdown
    /// </summary>
    public void CancelCountdown()
    {
        if (countDownCoroutine != null)
        {
            StopCoroutine(countDownCoroutine);
        }
        titreEscalier.text = "";
        countdownEscalier.text = "";
    }

    #endregion
}