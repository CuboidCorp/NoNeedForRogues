/// <summary>
/// Classe qui répresente les sacs d'or/ pièces avec lesquels on peut intéragir pour recuperer de l'or
/// </summary>
public class GoldObject : Interactable
{
    [SerializeField]private int value = 1;

    /// <summary>
    /// Gère l'interaction avec l'objet
    /// </summary>
    protected override void HandleInteraction()
    {
        //Rajoute le gold au truc du serveur
        StatsManager.Instance.AddGold(value);
        StatsManager.Instance.totalGold.Value += value;
    }
}
