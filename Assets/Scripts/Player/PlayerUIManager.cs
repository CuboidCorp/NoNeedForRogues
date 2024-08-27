using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerUIManager : MonoBehaviour
{
    #region UI Elements
    [Header("UI Elements")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private UIDocument uiMenu;

    [SerializeField] private VisualTreeAsset pauseMenu;
    [SerializeField] private VisualTreeAsset gameOverMenu;
    [SerializeField] private VisualTreeAsset optionsMenu;

    #endregion

    #region In Game UI
    [Header("InGameUI Elements")]
    [SerializeField] private GameObject crossHair;
    [SerializeField] private GameObject interactText;
    public GameObject francois;
    public UnityEngine.UI.Slider healthSlider;
    public UnityEngine.UI.Slider manaSlider;
    public TMP_Text healthText;
    public TMP_Text manaText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text goldChangedText;
    private Coroutine hideGoldChanged;

    #endregion

    #region Pause Menu
    private Button resumeButton;
    private Button optionsButton;
    private Button quitButton;
    #endregion

    #region Options Menu
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Slider voiceVolumeSlider;
    private Button returnToPauseButton;

    [SerializeField] private AudioMixer mainAudioMixer;
    #endregion



    public static PlayerUIManager Instance;

    private readonly string interactButton = "[E]";
    private PlayerControls playControls;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(this);
        Instance = this;
        inGameUI.SetActive(false);
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

    /// <summary>
    /// Montre le nombre de gold qui a changé du total
    /// </summary>
    /// <param name="difference">La difference entre l'ancienne et la nouvelle valeur</param>
    public void ShowGoldChangedText(int difference)
    {
        if(hideGoldChanged != null)
        {
            StopCoroutine(hideGoldChanged);
        }
        if(difference <0)
        {
            goldChangedText.color = Color.Red;
            goldChangedText.text = "- ";
        }
        else
        {
            goldChangedText.color = Color.Green;
            goldChangedText.text = "+ ";
        }
        goldChangedText.text += difference; 
        hideGoldChanged = HideGoldChangedText(1);
    }

    /// <summary>
    /// Cache le texte du goldChangeText au bout de timetohide secondes
    /// </summary>
    /// <param name="timeToHide">Nb de secondes avant de recacher le nombre</param>
    /// <returns></returns>
    private IEnumerator HideGoldChangedText(int timeToHide)
    {
        yield return new WaitForSeconds(timeToHide);
        goldChangedText.text = "";
        hideGoldChanged = null;
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
        MonPlayerController.instanceLocale.Deconnexion();
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("MenuPrincipal");
        Destroy(gameObject);
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
            mainAudioMixer.SetFloat("voiceVolume", Mathf.Log10(evt.newValue) * 20 + 10);
        });

        returnToPauseButton.clicked += HideOptionsMenu;

        //Donc pr les controles on veut :
        //les 4 bindings de move (haut bas gauche droite)
        //Jump
        //Run
        //Long attack
        //Interact
        //Les emote de 1 a 10
        //Crouch --> Le truc de fantome pr descendre
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
            mainAudioMixer.SetFloat("voiceVolume", Mathf.Log10(evt.newValue) * 20 + 10);
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
