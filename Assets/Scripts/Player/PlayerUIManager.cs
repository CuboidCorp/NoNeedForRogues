using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject playerUI;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject optionsMenu;

    [Header("PlayerUI Elements")]
    [SerializeField] private GameObject crossHair;
    [SerializeField] private GameObject interactText;
    [SerializeField] private GameObject healthBar;
    [SerializeField] private GameObject manaBar;

    [Header("Pause Menu Elements")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    public static PlayerUIManager Instance;

    private readonly string interactButton = "[E]";
    private PlayerControls.PlayerActions playControls;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        playerUI.SetActive(false);
        pauseMenu.SetActive(false);
        gameOverMenu.SetActive(false);
        optionsMenu.SetActive(false);
    }

    #region PlayerUI

    /// <summary>
    /// Affiche l'UI du joueur
    /// </summary>
    public void AfficherPlayerUi()
    {
        Cursor.lockState = CursorLockMode.Locked;
        playerUI.SetActive(true);
    }

    /// <summary>
    /// Set et affiche le texte d'interaction
    /// </summary>
    /// <param name="text">Le texte à afficher</param>
    public void ShowInteractText(string text)
    {
        interactText.SetActive(true);
        interactText.GetComponentInChildren<TMP_Text>().text = text + " " + interactButton;
    }

    /// <summary>
    /// Cache le texte d'interaction
    /// </summary>
    public void HideInteractText()
    {
        interactText.SetActive(false);
    }
    #endregion

    #region PauseMenu

    /// <summary>
    /// Affiche le menu pause
    /// </summary>
    public void ShowPauseMenu(PlayerControls.PlayerActions playerActions)
    {
        playControls = playerActions;
        playControls.Disable();
        Cursor.lockState = CursorLockMode.None;
        playerUI.SetActive(false);
        pauseMenu.SetActive(true);
    }

    /// <summary>
    /// Cache le menu pause
    /// </summary>
    public void HidePauseMenu()
    {
        playControls.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        playerUI.SetActive(true);
        pauseMenu.SetActive(false);
    }

    /// <summary>
    /// Affiche le menu des options
    /// </summary>
    public void ShowOptionsMenu()
    {
        optionsMenu.SetActive(true);
        pauseMenu.SetActive(false);
        Debug.Log("Pas implémenté encore :(");//TODO Faire les options
    }

    /// <summary>
    /// Permet de se déconnecter du jeu et de revenir au menu principal
    /// </summary>
    public void Disconnect()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MenuPrincipal");
    }

    #endregion

    #region GameOverMenu

    /// <summary>
    /// Affiche le menu de fin de partie
    /// </summary>
    public void ShowGameOverMenu()
    {
        playerUI.SetActive(false);
        pauseMenu.SetActive(false);
        gameOverMenu.SetActive(true);
    }

    #endregion

    #region Options

    public void HideOptionsMenu()
    {
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
        Debug.Log("Pas implémenté encore :("); //TODO Faire les options
    }
    #endregion

}
