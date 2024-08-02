using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEditor.ShaderGraph;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class PlayerUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject gameOverMenu;
    [SerializeField] private GameObject optionsMenu;

    private UIDocument pauseDoc;
    private UIDocument gameOverDoc;
    private UIDocument optionsDoc;

    [Header("inGameUI Elements")]
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

    [Header("Options Menu")]
    private Slider musicVolumeSlider;
    private Slider sfxVolumeSlider;
    private Slider voiceVolumeSlider;

    [SerializeField] private AudioMixer mainAudioMixer;

    public static PlayerUIManager Instance;

    private readonly string interactButton = "[E]";
    private PlayerControls playControls;

    private void Awake()
    {
        pauseDoc = pauseMenu.GetComponent<UIDocument>();

        resumeButton = pauseDoc.rootVisualElement.Q<Button>("continueBtn");
        optionsButton = pauseDoc.rootVisualElement.Q<Button>("optionsMenu");
        quitButton = pauseDoc.rootVisualElement.Q<Button>("quitBtn");

        gameOverDoc = gameOverMenu.GetComponent<UIDocument>();
        optionsDoc = optionsMenu.GetComponent<UIDocument>();

        musicVolumeSlider = optionsDoc.rootVisualElement.Q<Slider>("musicSlider");
        sfxVolumeSlider = optionsDoc.rootVisualElement.Q<Slider>("sfxSlider");
        voiceVolumeSlider = optionsDoc.rootVisualElement.Q<Slider>("voiceSlider");

        inGameUI.SetActive(false);
        pauseMenu.SetActive(false);
        gameOverMenu.SetActive(false);
        optionsMenu.SetActive(false);
        Instance = this;
    }

    private void OnEnable()
    {

        resumeButton.clickable.clicked += () => Debug.Log("WTF");

        resumeButton.clicked += () => Debug.Log("Wesh");

        resumeButton.RegisterCallback<ClickEvent>(evt => HidePauseMenu());
        optionsButton.RegisterCallback<ClickEvent>(evt => ShowOptionsMenu());
        quitButton.RegisterCallback<ClickEvent>(evt => Disconnect());

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
    }

    private void OnDisable()
    {
        resumeButton.UnregisterCallback<ClickEvent>(evt => HidePauseMenu());
        optionsButton.UnregisterCallback<ClickEvent>(evt => ShowOptionsMenu());
        quitButton.UnregisterCallback<ClickEvent>(evt => Disconnect());

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

    public void SetupPlayerControls(PlayerControls playerControls)
    {
        playControls = playerControls;

        playControls.UI.Continue.performed += _ => HidePauseMenu();
        playControls.UI.Click.performed += _ => Debug.Log("Click");

        playControls.UI.Disable();
    }

    #region PauseMenu

    /// <summary>
    /// Affiche le menu pause
    /// </summary>
    public void ShowPauseMenu()
    {
        Debug.Log("ShowPauseMenu");
        playControls.Player.Disable();
        playControls.UI.Enable();
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        inGameUI.SetActive(false);
        pauseMenu.SetActive(true);
    }

    /// <summary>
    /// Cache le menu pause
    /// </summary>
    public void HidePauseMenu()
    {
        Debug.Log("HidePauseMenu");
        playControls.Player.Enable();
        playControls.UI.Disable();
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        inGameUI.SetActive(true);
        pauseMenu.SetActive(false);
    }

    /// <summary>
    /// Affiche le menu des options
    /// </summary>
    public void ShowOptionsMenu()
    {
        optionsMenu.SetActive(true);
        pauseMenu.SetActive(false);
    }

    /// <summary>
    /// Permet de se déconnecter du jeu et de revenir au menu principal
    /// </summary>
    public void Disconnect()
    {
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
        pauseMenu.SetActive(false);
        optionsMenu.SetActive(false);
        gameOverMenu.SetActive(true);
        StartCoroutine(HideGameOver());
    }

    /// <summary>
    /// Reaffiche l'UI du joueur après un certain temps
    /// </summary>
    /// <returns></returns>
    private IEnumerator HideGameOver()
    {
        yield return new WaitForSeconds(2);
        gameOverMenu.SetActive(false);
    }

    #endregion

    #region Options

    public void HideOptionsMenu()
    {
        optionsMenu.SetActive(false);
        pauseMenu.SetActive(true);
    }
    #endregion

}
