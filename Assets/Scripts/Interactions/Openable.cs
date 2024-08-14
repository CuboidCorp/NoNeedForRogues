using UnityEngine;

/// <summary>
/// A rajouter aux objets qui peuvent être ouverts (Portes, coffres, etc.)
/// </summary>
public class Openable : NetworkBehaviour //TODO : Ptet plus opti de faire juste un network animator et de rajouter des triggers
{
    /// <summary>
    /// L'animator de l'objet
    /// </summary>
    private Animator anim;

    /// <summary>
    /// Etat de l'objet
    /// </summary>
    private NetworkVariable<bool> isOpen = new NetworkVariable<bool>();

    /// <summary>
    /// Valeur initiale de si l'objet est ouvert ou non
    /// </summary>
    [SerializeField] private bool initialValueIsOpen = false;

    /// <summary>
    /// Le nom de l'animation d'ouverture
    /// </summary>
    [SerializeField] private string openingAnimationName = "Opening";

    /// <summary>
    /// Le nom de l'animation de fermeture
    /// </summary>
    [SerializeField] private string closingAnimationName = "Closing";

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public override void OnNetworkSpawn()
    {
        if(IsSever)
        {
            isOpen = initialValueIsOpen;
        }
        else
        {
            isOpen.OnValueChanged += OnOpenValueChanged;
        }
    }

    private void OnOpenValueChanged(bool previous, bool current)
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
    [ServerRpc(RequireOwnerShip = false)]
    private void ChangeStateServerRpc(bool newState)
    {
        isOpen.Value = newState;
    }
}
