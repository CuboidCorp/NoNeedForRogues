using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui r�presente les potions avec lesquels on peut int�ragir pour recuperer certains effets (recup HP, recup mana, poison
/// </summary>
public class PotionObject : Interactable, IRamassable
{
    [SerializeField] private float power = 1;

    public NetworkVariable<bool> isHeld = new(false);

    private PotionType type;

    [SerializeField] private int nbSecPoison;

    public void SetType(int typeId)
    {
        switch (typeId)
        {
            case 0:
                type = PotionType.HEAL;
                break;
            case 1:
                type = PotionType.MANA_REGEN; 
                break;
            case 2:
                type = PotionType.POISON; 
                break;
            default: type = PotionType.HEAL; 
                break;
        }
    }

    #region Interaction
    /// <summary>
    /// G�re l'interaction avec l'objet
    /// </summary>
    public override void HandleInteraction()
    {
        //Quand on interagit avec la potion on la boit
        //TODO : Recup le joueur qui int�ragit avec la boisson
        StatsManager.Instance.AddPotionDrank();
        switch (type)
        {
            case PotionType.HEAL:
                MonPlayerController.instanceLocale.Heal(power);
                break;
            case PotionType.MANA_REGEN:
                MonPlayerController.instanceLocale.GainMana(power);
                break;
            case PotionType.POISON:
                MonPlayerController.instanceLocale.StartPoison(power, nbSecPoison);
                break;
        }
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
    private void ChangeStateServerRpc(bool newState)
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
