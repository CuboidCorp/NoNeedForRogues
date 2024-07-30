/// <summary>
/// Stats de chaque joueur de la game
/// </summary>
[System.Serializable]
public class PlayerStats
{
    public int nbGoldCollected; //Nombre de gold recupere
    public int nbJumps; //Nombre de sauts
    public int spellsCasted; //Nb spells lancé
    public float damageTaken; //Total de dégâts reçus
    public float healTaken; //Total de Soin reçu
    public float distanceMoved; //Distance courue/Marchée
    public int nbPotionDrank; //Nombre de potions consommée

    /// <summary>
    /// Constructeur de base qui met tout a 0
    /// </summary>
    public PlayerStats()
    {
        nbGoldCollected = 0;
        nbJumps = 0;
        spellsCasted = 0;
        damageTaken = 0;
        healTaken = 0;
        distanceMoved = 0;
        nbPotionDrank = 0;
    }
}
