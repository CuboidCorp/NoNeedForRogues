using UnityEngine;
using Unity.Netcode;

/// <summary>
/// A rajouter aux objets qui peuvent être ouverts (Portes, coffres, etc.)
/// </summary>
[RequireComponent(typeof(Animator))]
public class Openable : NetworkBehaviour //TODO : Ptet plus opti de faire juste un network animator et de rajouter des triggers
{
    /// <summary>
    /// L'animator de l'objet
    /// </summary>
    protected Animator anim;

    /// <summary>
    /// Etat de l'objet
    /// </summary>
    protected NetworkVariable<bool> isOpen = new();

    /// <summary>
    /// Valeur initiale de si l'objet est ouvert ou non
    /// </summary>
    [SerializeField] protected bool initialValueIsOpen = false;

    /// <summary>
    /// Le nom de l'animation d'ouverture
    /// </summary>
    [SerializeField] protected string openingAnimationName = "Opening";

    /// <summary>
    /// Le nom de l'animation de fermeture
    /// </summary>
    [SerializeField] protected string closingAnimationName = "Closing";

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            isOpen.Value = initialValueIsOpen;
        }
        isOpen.OnValueChanged += OnOpenValueChanged;
    }

    protected virtual void OnOpenValueChanged(bool previous, bool current)
    {
        if (current)
        {
            anim.Play(openingAnimationName);
        }
        else
        {
            anim.Play(closingAnimationName);
        }
    }

    /// <summary>
    /// Echange l'etat de l'objet
    /// </summary>
    public void ChangeState()
    {
        ChangeStateServerRpc(!isOpen.Value);
    }

    /// <summary>
    /// Ouvre l'objet
    /// </summary>
    public void Open()
    {
        ChangeStateServerRpc(true);
    }

    /// <summary>
    /// Ferme l'objet
    /// </summary>
    public void Close()
    {
        ChangeStateServerRpc(false);
    }

    /// <summary>
    /// Change l'etat de la network variable isOpen
    /// </summary>
    /// <param name="newState">Le nouvel etat de la variable</param>
    [ServerRpc(RequireOwnership = false)]
    private void ChangeStateServerRpc(bool newState)
    {
        isOpen.Value = newState;
    }
}
