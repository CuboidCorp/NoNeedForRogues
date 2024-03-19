using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour {


    public static LobbyCreateUI Instance { get; private set; }


    [SerializeField] private Button createButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button lobbyNameButton;
    [SerializeField] private Button publicPrivateButton;
    [SerializeField] private Button maxPlayersButton;
    [SerializeField] private Button gameModeButton;
    [SerializeField] private TMP_InputField lobbyNameText;
    [SerializeField] private TextMeshProUGUI publicPrivateText;
    [SerializeField] private TMP_InputField maxPlayersText;
    [SerializeField] private TextMeshProUGUI gameModeText;


    private string lobbyName;
    private bool isPrivate;
    private int maxPlayers;
    private LobbyManager.GameMode gameMode;

    private void Awake() {
        Instance = this;

        createButton.onClick.AddListener(() => {
            lobbyName = lobbyNameText.text;
            try
            {
                maxPlayers = int.Parse(maxPlayersText.text);
            }
            catch (Exception)
            {
                maxPlayersText.text = "";
                maxPlayersText.placeholder.GetComponent<TMP_Text>().text = "Invalid number";
                return;
            }

            LobbyManager.Instance.CreateLobby(
                lobbyName,
                maxPlayers,
                isPrivate,
                gameMode
            );
            Hide();
        });

        cancelButton.onClick.AddListener(() =>
        {
            Hide();
        });

        publicPrivateButton.onClick.AddListener(() => {
            isPrivate = !isPrivate;
            UpdateText();
        });


        gameModeButton.onClick.AddListener(() => {
            //Code original pour changer de gameMode quand on clicke, pas besoin pour le moment
            //Finalement je met juste des trucs random mais ça n'a aucune importance
            switch (gameMode)
            {
                default:
                case LobbyManager.GameMode.Coop:
                    gameMode = LobbyManager.GameMode.NYI;
                    break;
                case LobbyManager.GameMode.NYI:
                    gameMode = LobbyManager.GameMode.Coop;
                    break;
            }
            UpdateText();
        });

        Hide();
    }

    private void UpdateText() {
        lobbyNameText.text = lobbyName;
        publicPrivateText.text = isPrivate ? "Private" : "Public";
        maxPlayersText.text = maxPlayers.ToString();
        gameModeText.text = gameMode.ToString();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    public void Show() {
        gameObject.SetActive(true);

        lobbyName = "MyLobby";
        isPrivate = false;
        maxPlayers = 2;
        gameMode = LobbyManager.GameMode.Coop;

        UpdateText();
    }

}