using TMPro;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{

    public static PlayerUI Instance;

    [Header("Interaction")]
    [SerializeField] private GameObject crossHair;
    [SerializeField] private GameObject interactText;

    [Header("Health and Mana")]
    [SerializeField] private GameObject healthBar;
    [SerializeField] private GameObject manaBar;

    private readonly string interactButton = "[E]";

    private void Awake()
    {
        Instance = this;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Set et affiche le texte d'interaction
    /// </summary>
    /// <param name="text">Le texte à afficher</param>
    public void ShowInteractText(string text)
    {
        interactText.SetActive(true);
        interactText.GetComponentInChildren<TMP_Text>().text = text+ " "+interactButton;
    }


    /// <summary>
    /// Cache le texte d'interaction
    /// </summary>
    public void HideInteractText()
    {
        interactText.SetActive(false);
    }
}
