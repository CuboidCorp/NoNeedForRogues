using Unity.Netcode;

/// <summary>
/// Stats de chaque joueur de la game
/// </summary>
[System.Serializable]
public class PlayerStats : INetworkSerializable
{
    public int nbGoldCollected; //Nombre de gold recupere
    public int nbJumps; //Nombre de sauts
    public int spellsCasted; //Nb spells lanc�
    public float damageTaken; //Total de d�g�ts re�us
    public float healTaken; //Total de Soin re�u
    public float distanceMoved; //Distance courue/March�e
    public int nbPotionDrank; //Nombre de potions consomm�e

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

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref nbGoldCollected);
        serializer.SerializeValue(ref nbJumps);
        serializer.SerializeValue(ref spellsCasted);
        serializer.SerializeValue(ref damageTaken);
        serializer.SerializeValue(ref healTaken);
        serializer.SerializeValue(ref distanceMoved);
        serializer.SerializeValue(ref nbPotionDrank);
    }
}
