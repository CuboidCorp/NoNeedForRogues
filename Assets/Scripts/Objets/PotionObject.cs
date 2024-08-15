using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Classe qui r�presente les potions avec lesquels on peut int�ragir pour recuperer certains effets (recup HP, recup mana, poison
/// </summary>
public class PotionObject : WeightedObject, IInteractable
{
    [SerializeField] private string interactText = "BOIRE";

    [SerializeField] private float power = 1;

    private PotionType type;

    [SerializeField] private int nbSecPoison;

    public void SetType(int typeId)
    {
        type = typeId switch
        {
            0 => PotionType.HEAL,
            1 => PotionType.MANA_REGEN,
            2 => PotionType.POISON,
            _ => PotionType.HEAL,
        };
    }

    #region Interaction

    public void OnInteract()
    {
        //On boit la potion localement
        HandleInteraction();
        DespawnObjectServerRpc();
    }

    /// <summary>
    /// Renvoie le texte a afficher qd on peut interagir avec l'objet
    /// </summary>
    /// <returns>Le string qui correspond au texte d'interaction</returns>
    public string GetInteractText()
    {
        return interactText;
    }

    /// <summary>
    /// G�re l'interaction avec l'objet
    /// </summary>
    public void HandleInteraction()
    {
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

    /// <summary>
    /// Si qqn interagit avec le bouton on envoie un message au serv pr lui dire
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    private void DespawnObjectServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }

    #endregion

}

public enum PotionType
{
    HEAL,
    MANA_REGEN,
    POISON
}
