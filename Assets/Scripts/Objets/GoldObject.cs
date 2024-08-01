using UnityEngine;

/// <summary>
/// Classe qui r�presente les sacs d'or/ pi�ces avec lesquels on peut int�ragir pour recuperer de l'or
/// </summary>
public class GoldObject : Interactable
{
    [SerializeField] private int value = 1;

    /// <summary>
    /// G�re l'interaction avec l'objet
    /// </summary>
    public override void HandleInteraction()
    {
        //Rajoute le gold au truc du serveur
        StatsManager.Instance.AddGold(value);
        StatsManager.Instance.totalGold.Value += value;
    }
}
