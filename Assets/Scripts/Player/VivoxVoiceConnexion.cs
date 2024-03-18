using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using UnityEngine;
public class VivoxVoiceConnexion : NetworkBehaviour
{

    private const string channelName = "Global";
    private IVivoxService servVivox;

    #region Config Channel
    /// <summary>
    /// La distance max pr entendre qqn
    /// </summary>
    [SerializeField] private int maxDistance = 64;
    /// <summary>
    /// La distance min pr entendre a 100% qqn
    /// </summary>
    [SerializeField] private int minAudibleDistance = 5;

    /// <summary>
    /// ???????? Je sais pas en vrai et flemme
    /// </summary>
    [SerializeField] private float audioFadeIntensity = 1.0f;

    #endregion
    [SerializeField] private float updateInterval = .5f; // en s

    private IEnumerator UpdateVivox3DPos()
    {
        while (true)
        {
            servVivox.Set3DPosition(gameObject, channelName);
            yield return new WaitForSeconds(updateInterval);
        }
    }

    #region Vivox

    /// <summary>
    /// Initialisation de Vivox
    /// </summary>
    /// <returns>Quand l'initialisation est terminée</returns>
    public async Task InitVivox()
    {
        await UnityServices.InitializeAsync(); 
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        servVivox = VivoxService.Instance;
        servVivox.LoggedIn += OnLoggedIn;
        servVivox.LoggedOut += OnLoggedOut;
        await servVivox.InitializeAsync();
        await servVivox.LoginAsync();
    }

    /// <summary>
    /// Rejoint le channel de chat 
    /// </summary>
    /// <returns>Quand on a rejoint le voice chat</returns>
    private async Task JoinPositionalChannelAsync()
    {
        ChatCapability chat = ChatCapability.AudioOnly;
        Channel3DProperties channel3DProperties = new(maxDistance, minAudibleDistance, audioFadeIntensity, AudioFadeModel.InverseByDistance);
        ChannelOptions channelOptions = null;
        await servVivox.JoinPositionalChannelAsync(channelName, chat, channel3DProperties, channelOptions);
        StartCoroutine(UpdateVivox3DPos());
    }

    /// <summary>
    /// Log in de Vivox
    /// </summary>
    private async void OnLoggedIn()
    {
        Debug.Log("Vivox Logged In");
        if (MultiplayerGameManager.Instance.soloMode)
        {
            await servVivox.JoinEchoChannelAsync("EchoTest", ChatCapability.AudioOnly);
        }
        else
        {
            await JoinPositionalChannelAsync();
        }
        MultiplayerGameManager.Instance.ActiveAudioTaps();

    }

    /// <summary>
    /// Quand on se deconnecte de Vivox on quitte tous les channels
    /// </summary>
    private async void OnLoggedOut()
    {
        Debug.Log("Vivox Logged Out");
        StopAllCoroutines();
        await servVivox.LeaveAllChannelsAsync();
        await servVivox.LogoutAsync();
    }
    #endregion
}
