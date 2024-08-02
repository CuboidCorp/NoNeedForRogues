/// <summary>
/// Classe qui représente les escaliers pour allez vers le haut / bas des niveaux
/// </summary>
public class Escalier : NetworkBehaviour
{
    /// <summary>
    /// Si les escaliers vont vers le haut
    /// </summary>
    private bool isUpStairs;

    private List<ulong> playersInside;

    private TMP_Text titreEscalier;
    private TMP_Text countdownEscalier;

    public IEnumerator countDownCoroutine;

    private void Awake()
    {
        playersInside = new();
    }

    private void OnTriggerEnter(Collider other)
    {
        //Le joueur qui rentre est donc ready
        if(MultiplayerGameManager.Instance.gameCanStart && other.gameObject.CompareTag("Player"))
        {
            ulong playerId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            playersInside.Add(playerId);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.CompareTag("Player"))
        {
            ulong playerId = other.gameObject.GetComponent<NetworkObject>().OwnerClientId;
            playersInside.Remove(playerId);
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
        while(cptSec < nbSec)
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
        StopCoroutine(countDownCoroutine);
        titreEscalier.text = "";
        countdownEscalier.text = "";
    }

    #endregion
}