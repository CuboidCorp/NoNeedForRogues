using System;
using System.IO;
using System.Text.RegularExpressions;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using ParrelSync;
using TMPro;
using UnityEngine.UIElements;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenu;
    private VisualElement mainMenuRootVisualElement;
    [SerializeField] private GameObject popupMenu;
    [SerializeField] private GameObject errorPopup;
    [SerializeField] private TMP_InputField inputField;

    private string Profile = "Original";
#if UNITY_EDITOR
    private string filename = "originalInfo.json";

#else
    private string filename = "playerInfo.json";
#endif

    private void Awake()
    {
        mainMenuRootVisualElement = mainMenu.GetComponent<UIDocument>().rootVisualElement;
    }

    private void OnEnable()
    {
        mainMenuRootVisualElement.Q<Button>("playBtn").clicked += StartGame;
        mainMenuRootVisualElement.Q<Button>("quitBtn").clicked += QuitGame;
    }

    private void OnDisable()
    {
        mainMenuRootVisualElement.Q<Button>("playBtn").clicked -= StartGame;
        mainMenuRootVisualElement.Q<Button>("quitBtn").clicked -= QuitGame;
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            filename = "cloneInfo.json";
            Profile = "Clone";
        }
#endif
        GameObject errorHandler = GameObject.FindGameObjectWithTag("ErrorHandler");
        if (errorHandler != null)
        {
            ShowError(errorHandler.GetComponent<ErrorHandler>().message);
            Destroy(errorHandler);
        }
    }

    /// <summary>
    /// Affiche un message d'erreur
    /// </summary>
    /// <param name="message">Le message à afficher</param>
    private void ShowError(string message)
    {
        errorPopup.SetActive(true);
        errorPopup.GetComponentInChildren<TMP_Text>().text = message;
    }

    /// <summary>
    /// Lance le jeu si le joueur à un fichier de sauvegarde
    /// </summary>
    public void StartGame()
    {
        if (File.Exists(Path.Combine(Application.persistentDataPath, filename)))
        {
            try
            {
                PlayerInfo playerInfo = JsonUtility.FromJson<PlayerInfo>(File.ReadAllText(Path.Combine(Application.persistentDataPath, filename)));

                if (IsPlayerInfoValid(playerInfo.playerName))
                {
                    CreationPlayerInfoGameObject(playerInfo);
                }
                else
                {
                    AfficherPopup();
                }
            }
            catch (ArgumentException)
            {
                File.Delete(Path.Combine(Application.persistentDataPath, filename));
                AfficherPopup();
            }

        }
        else
        {
            AfficherPopup();
        }

    }

    /// <summary>
    /// Quitte le jeu
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
    }

    /// <summary>
    /// Affiche la popup
    /// </summary>
    private void AfficherPopup()
    {
        mainMenu.SetActive(false);
        popupMenu.SetActive(true);
    }

    /// <summary>
    /// Quand on valide la popup
    /// </summary>
    public void Valider()
    {
        //On verifie que le nom est valide (Pas vide, pas d'espace, pas de caractere speciaux)
        inputField.text = inputField.text.Trim();

        if (IsPlayerInfoValid(inputField.text))
        {
            PlayerInfo playerInfo = new()
            {
                playerName = inputField.text
            };

            string json = JsonUtility.ToJson(playerInfo);
            Debug.Log(json);
            File.WriteAllText(Path.Combine(Application.persistentDataPath, filename), json);

            CreationPlayerInfoGameObject(playerInfo);

        }
        else
        {
            if (inputField.text.Length > 5)
            {
                inputField.text = "";
                inputField.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text = "Nom trop court <5 caracteres";
            }
            if (inputField.text.Length < 15)
            {
                inputField.text = "";
                inputField.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text = "Nom trop long >15 caracteres";
            }
            else
            {
                inputField.text = "";
                inputField.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text = "Nom invalide";
            }
        }

    }

    /// <summary>
    /// Verifie que le nom du joueur est valide
    /// </summary>
    /// <param name="playerName">Le nom du joueur a verifier</param>
    /// <returns>True si il est valide, false sinon</returns>
    private bool IsPlayerInfoValid(string playerName)
    {
        return Regex.IsMatch(playerName, @"^[a-zA-Z0-9]{5,15}$");
    }

    /// <summary>
    /// Cree un GameObject pour stocker les informations du joueur
    /// </summary>
    private async void CreationPlayerInfoGameObject(PlayerInfo playerInfo)
    {
        GameObject playerInfoGameObject = new("PlayerInfo");
        playerInfoGameObject.AddComponent<DataHolder>();
        playerInfoGameObject.GetComponent<DataHolder>().PlayerInfo = playerInfo;

        DontDestroyOnLoad(playerInfoGameObject);

        await UnityServices.InitializeAsync();
#if UNITY_EDITOR
        AuthenticationService.Instance.SwitchProfile(Profile);
#endif
        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //TODO : Faire gaffe a pas etre deja connecté
        //On se deplace vers le TavernLobby
        SceneManager.LoadScene("TavernLobby");
    }

    /// <summary>
    /// Quand on annule la popup
    /// </summary>
    public void Annuler()
    {
        inputField.text = "";
        inputField.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text = "Entrez votre nom (5 à 15 caractères) alphanumériques";
        mainMenu.SetActive(true);
        popupMenu.SetActive(false);
    }
}
