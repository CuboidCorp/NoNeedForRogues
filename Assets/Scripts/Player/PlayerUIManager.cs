using System.Collections;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private UIDocument uiMenu;

    [SerializeField] private VisualTreeAsset pauseMenu;
    [SerializeField] private VisualTreeAsset gameOverMenu;
    [SerializeField] private VisualTreeAsset optionsMenu;

    [Header("InGameUI Elements")]
    [SerializeField] private GameObject crossHair;
    [SerializeField] private GameObject interactText;
    public GameObject francois;
    public UnityEngine.UI.Slider healthSlider;
    public UnityEngine.UI.Slider manaSlider;
    public TMP_Text healthText;
    public TMP_Text manaText;
    [SerializeField] private TMP_Text goldText;

    private Button resumeButton;
    private Button optionsButton;
    private Button quitButton;

    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Slider voiceVolumeSlider;
    private Button returnToPauseButton;

    [SerializeField] private AudioMixer mainAudioMixer;

    public static PlayerUIManager Instance;

    private readonly string interactButton = "[E]";
    private PlayerControls playControls;

    private void Awake()
    {
        inGameUI.SetActive(false);
        Instance = this;
    }

    #region InGameUI

    /// <summary>
    /// Affiche l'UI du joueur
    /// </summary>
    public void AfficherInGameUI()
    {
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        inGameUI.SetActive(true);
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

    /// <summary>
    /// Set le nombre de gold 
    /// </summary>
    /// <param name="amount">Nouvelle valeur d'or</param>
    public void SetGoldText(int amount)
    {
        goldText.text = amount + " G";
    }
    #endregion

    /// <summary>
    /// Setup les controles du joueur
    /// </summary>
    /// <param name="playerControls">Les controles du joueurs</param>
    public void SetupPlayerControls(PlayerControls playerControls)
    {
        playControls = playerControls;

        playControls.UI.Continue.performed += _ => HidePauseMenu(); // Pas ouf quand sur le menu pause

        playControls.UI.Disable();
    }

    #region PauseMenu

    /// <summary>
    /// Affiche le menu pause
    /// </summary>
    public void ShowPauseMenu()
    {
        playControls.Player.Disable();
        playControls.UI.Enable();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        inGameUI.SetActive(false);
        uiMenu.visualTreeAsset = pauseMenu;
        SetupPauseMenu();
    }

    /// <summary>
    /// Cache le menu pause
    /// </summary>
    public void HidePauseMenu()
    {
        playControls.Player.Enable();
        playControls.UI.Disable();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        inGameUI.SetActive(true);
        uiMenu.visualTreeAsset = null;
        UnSetupPauseMenu();
    }

    /// <summary>
    /// Connecte les events du menu pause
    /// </summary>
    private void SetupPauseMenu()
    {
        VisualElement root = uiMenu.rootVisualElement;
        resumeButton = root.Q<Button>("continueBtn");
        optionsButton = root.Q<Button>("optionsMenu");
        quitButton = root.Q<Button>("quitBtn");

        resumeButton.clicked += HidePauseMenu;
        optionsButton.clicked += ShowOptionsMenu;
        quitButton.clicked += Disconnect;
    }

    /// <summary>
    /// Deconnecte tous les events du menu pause
    /// </summary>
    private void UnSetupPauseMenu()
    {
        resumeButton.clicked -= HidePauseMenu;
        optionsButton.clicked -= ShowOptionsMenu;
        quitButton.clicked -= Disconnect;
    }

    /// <summary>
    /// Affiche le menu des options
    /// </summary>
    public void ShowOptionsMenu()
    {
        uiMenu.visualTreeAsset = optionsMenu;
        SetupOptionsMenu();
    }

    /// <summary>
    /// Permet de se déconnecter du jeu et de revenir au menu principal
    /// </summary>
    public void Disconnect()
    {
        Debug.Log("Disconnect");
        UnSetupOptionsMenu();
        MonPlayerController.instanceLocale.gameObject.GetComponent<PickUpController>().DropObject();
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
        uiMenu.visualTreeAsset = gameOverMenu;
        StartCoroutine(HideGameOver());
    }

    /// <summary>
    /// Reaffiche l'UI du joueur après un certain temps
    /// </summary>
    /// <returns></returns>
    private IEnumerator HideGameOver()
    {
        yield return new WaitForSeconds(2);
        uiMenu.visualTreeAsset = null;
    }

    #endregion

    #region Options

    /// <summary>
    /// Connecte les events du menu des options
    /// </summary>
    private void SetupOptionsMenu()
    {
        VisualElement root = uiMenu.rootVisualElement;

        musicVolumeSlider = root.Q<Slider>("musicSlider");
        sfxVolumeSlider = root.Q<Slider>("sfxSlider");
        voiceVolumeSlider = root.Q<Slider>("voiceSlider");
        returnToPauseButton = root.Q<Button>("returnBtn");

        musicVolumeSlider.RegisterValueChangedCallback(evt =>
        {
            mainAudioMixer.SetFloat("musicVolume", Mathf.Log10(evt.newValue) * 20);
        });

        sfxVolumeSlider.RegisterValueChangedCallback(evt =>
        {
            mainAudioMixer.SetFloat("sfxVolume", Mathf.Log10(evt.newValue) * 20);
        });

        voiceVolumeSlider.RegisterValueChangedCallback(evt =>
        {
            mainAudioMixer.SetFloat("voiceVolume", Mathf.Log10(evt.newValue) * 20);
        });

        returnToPauseButton.clicked += HideOptionsMenu;
    }

    /// <summary>
    /// Deconnecte tous les events du menu des options
    /// </summary>
    private void UnSetupOptionsMenu()
    {
        musicVolumeSlider.UnregisterValueChangedCallback(evt =>
        {
            mainAudioMixer.SetFloat("musicVolume", Mathf.Log10(evt.newValue) * 20);
        });

        sfxVolumeSlider.UnregisterValueChangedCallback(evt =>
        {
            mainAudioMixer.SetFloat("sfxVolume", Mathf.Log10(evt.newValue) * 20);
        });

        voiceVolumeSlider.UnregisterValueChangedCallback(evt =>
        {
            mainAudioMixer.SetFloat("voiceVolume", Mathf.Log10(evt.newValue) * 20);
        });

        returnToPauseButton.clicked -= HideOptionsMenu;
    }

    /// <summary>
    /// Cache le menu des options
    /// </summary>
    public void HideOptionsMenu()
    {
        uiMenu.visualTreeAsset = pauseMenu;
        UnSetupOptionsMenu();
        SetupPauseMenu();
    }
    #endregion

}
