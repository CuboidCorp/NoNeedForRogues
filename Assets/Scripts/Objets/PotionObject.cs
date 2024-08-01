/// <summary>
/// Classe qui répresente les potions avec lesquels on peut intéragir pour recuperer certains effets (recup HP, recup mana, poison
/// </summary>
public class PotionObject : Interactable, Ramassable
{
    [SerializeField] private float power = 1;

    public NetworkVariable<bool> isHeld = new(false);

    private PotionType type;

    public void SetType(int typeId)
    {
        switch (typeId)
        {
            case 0:
                type = PotionType.HEAL; break;
            case 1:
                type = PotionType.MANA_REGEN; break;
            case 2:
                type = PotionType.POISON; break;
            default: type = PotionType.HEAL;
        }
    }

    #region Interaction
    /// <summary>
    /// Gère l'interaction avec l'objet
    /// </summary>
    protected override void HandleInteraction()
    {
        //Quand on interagit avec la potion on la boit
        
    }

    #endregion

    #region Pickup

    /// <summary>
    /// Change l'etat de l'objet si il est tenu ou non
    /// </summary>
    /// <param name="newState">Le nouvel etat de l'objet</param>
    public void ChangeState(bool newState)
    {
        if (!IsServer)
        {
            ChangeStateServerRpc(newState);
            return;
        }
        isHeld.Value = newState;
    }

    [ServerRpc(RequireOwnership = false)]
    private virtual void ChangeStateServerRpc(bool newState)
    {
        isHeld.Value = newState;
    }

    #endregion

}

public enum PotionType
{
    HEAL,
    MANA_REGEN,
    POISON
}
