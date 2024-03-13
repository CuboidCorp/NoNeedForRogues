using Unity.Netcode;
using Unity.Services.Vivox.AudioTaps;
using UnityEngine;

/// <summary>
/// Gère le mode multijoueur
/// </summary>
public class MultiplayerGameManager : MonoBehaviour
{
    public bool soloMode = false;

    public bool gameCanStart = false;

    public static MultiplayerGameManager Instance;

    private static int nbTotalPlayers = 0;

    public static int nbConnectedPlayers = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    /// <summary>
    /// Set le nombre de joueurs dans le lobby qui vont jouer
    /// </summary>
    /// <param name="nb">Le nombre de joueur</param>
    public void SetNbPlayersLobby(int nb)
    {
        nbTotalPlayers = nb;
    }

    /// <summary>
    /// Active tous les audioTaps de la scène
    /// </summary>
    public void ActiveAudioTaps()
    {
        GameObject[] audioTaps = GameObject.FindGameObjectsWithTag("AudioTap");

        foreach (GameObject tap in audioTaps)
        {
            tap.GetComponent<VivoxAudioTap>().enabled = true;
        }
    }

    /// <summary>
    /// When a player is connected
    /// </summary>
    /// <param name="id">Player id</param>
    private void OnClientConnected(ulong id)
    {
        Debug.Log("Player connected : "+id);
        Debug.Log(nbConnectedPlayers+ "/"+nbTotalPlayers);
        GameObject player = GameObject.FindWithTag("Temp");
        player.tag = "Player";
        player.name = "Player" + id+1;
        nbConnectedPlayers++;
        if(nbConnectedPlayers == nbTotalPlayers)
        {
            gameCanStart = true;
        }
    }

    /// <summary>
    /// When a player is disconnected
    /// </summary>
    /// <param name="id">Player id </param>
    private void OnClientDisconnected(ulong id)
    {
        nbConnectedPlayers--;
        if (nbConnectedPlayers <nbTotalPlayers)
        {
            gameCanStart = false;
        }
    }

    /// <summary>
    /// Test qd on parle 
    /// TODO : Pr utiliser faudrait le mettre dans une boucle update avec affichage d'une icone qd on parle
    /// </summary>
    public void TestSpeech()
    {
        Debug.Log("Speech");
    }
}
