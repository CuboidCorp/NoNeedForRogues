using UnityEngine;

/// <summary>
/// A rajouter aux objets qui peuvent être ouverts (Portes, coffres, etc.)
/// </summary>
public class Openable : MonoBehaviour //Le joueur n'interagit pas avec donc pas besoin de sync ??
{
    /// <summary>
    /// L'animator de l'objet
    /// </summary>
    private Animator anim;

    /// <summary>
    /// Etat de l'objet
    /// </summary>
    [SerializeField] private bool isOpen = false;

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

    /// <summary>
    /// Echange l'etat de l'objet
    /// </summary>
    public void ChangeState()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            anim.Play(openingAnimationName);
        }
        else
        {
            anim.Play(closingAnimationName);
        }
    }

    /// <summary>
    /// Ouvre l'objet
    /// </summary>
    public void Open()
    {
        isOpen = true;
        anim.Play(openingAnimationName);
    }

    /// <summary>
    /// Ferme l'objet
    /// </summary>
    public void Close()
    {
        isOpen = false;
        anim.Play(closingAnimationName);
    }
}
