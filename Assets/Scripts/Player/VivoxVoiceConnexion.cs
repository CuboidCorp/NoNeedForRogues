using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using Unity.Services.Vivox.AudioTaps;
using UnityEngine;
public class VivoxVoiceConnexion : NetworkBehaviour
{

    public const string channelName = "Global";
    public const string echoChannelName = "EchoTest";

    private IVivoxService servVivox;

    #region Config Channel
    /// <summary>
    /// La distance max pr entendre qqn
    /// </summary>
    public const int maxDistance = 32;
    /// <summary>
    /// La distance min pr entendre a 100% qqn
    /// </summary>
    public const int minAudibleDistance = 8;

    /// <summary>
    /// ???????? Je sais pas en vrai et flemme
    /// </summary>
    [SerializeField] private float audioFadeIntensity = 1.0f;

    [SerializeField] private float checkSpeakingInterval = 0.5f;

    public readonly List<VivoxParticipant> participants = new();

    #endregion

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
        servVivox.ParticipantAddedToChannel += ParticipantAdded;
        servVivox.ParticipantRemovedFromChannel += ParticipantRemoved;
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
    }

    /// <summary>
    /// Log in de Vivox
    /// </summary>
    private async void OnLoggedIn()
    {
        if (MultiplayerGameManager.Instance.soloMode)
        {
            await servVivox.JoinEchoChannelAsync(echoChannelName, ChatCapability.AudioOnly);
            GameObject channelTap = new GameObject("ChannelTap");
            channelTap.tag = "ChannelTap"; //Pour les retrouver plus tard
            channelTap.AddComponent<AudioSource>();
            channelTap.AddComponent<VivoxChannelAudioTap>().ChannelName = echoChannelName;
        }
        else
        {
            await JoinPositionalChannelAsync();
            StartCoroutine(CheckPlayerSkeaking());
        }
        MultiplayerGameManager.Instance.ActiveAudioTaps();

    }

    /// <summary>
    /// Quand le participant est ajouté au channel
    /// </summary>
    /// <param name="vivoxParticipant">Le participant ajouté</param>
    private void ParticipantAdded(VivoxParticipant vivoxParticipant)
    {
        if (vivoxParticipant.PlayerId != AuthenticationService.Instance.PlayerId)
        {
            Debug.Log("Player added" + vivoxParticipant.PlayerId);
            participants.Add(vivoxParticipant);
            //vivoxParticipant.SpeechDetected
            GameObject tap = vivoxParticipant.CreateVivoxParticipantTap("Tap " + vivoxParticipant.PlayerId);
            MultiplayerGameManager.Instance.AddParamToParticipantAudioSource(vivoxParticipant.ParticipantTapAudioSource);
            Transform playerTransform = MultiplayerGameManager.Instance.GetPlayerTransformFromAuthId(vivoxParticipant.PlayerId);
            tap.transform.SetParent(playerTransform); //Pas de null check = Programmation de gros porc ici flemme
            tap.transform.localPosition = new Vector3(0, 1.6f, 0);
            MultiplayerGameManager.Instance.AddPlayerVivoxInfo(vivoxParticipant.PlayerId, vivoxParticipant, gameObject.GetComponent<MonPlayerController>().playerUI.transform.GetChild(1).gameObject);
        }

    }

    /// <summary>
    /// Enleve un participant du channel
    /// </summary>
    /// <param name="vivoxParticipant">Le participant qui se deconnecte</param>
    private void ParticipantRemoved(VivoxParticipant vivoxParticipant)
    {
        participants.Remove(vivoxParticipant);
        vivoxParticipant.DestroyVivoxParticipantTap();
    }

    /// <summary>
    /// Quand on se deconnecte de Vivox on quitte tous les channels
    /// </summary>
    private async void OnLoggedOut()
    {
        await servVivox.LeaveAllChannelsAsync();
        await servVivox.LogoutAsync();
        StopAllCoroutines();
    }

    /// <summary>
    /// Faut vérifier si tous les joueurs sont a speaking
    /// </summary>
    private void CheckSpeaking()
    {
        foreach ((VivoxParticipant, GameObject) participantEtGo in MultiplayerGameManager.Instance.authServicePlayerIds.Values)
        {
            if (participantEtGo.Item1 != null)
            {
                if (participantEtGo.Item1.SpeechDetected)
                {
                    participantEtGo.Item2.SetActive(true);
                }
                else
                {
                    participantEtGo.Item2.SetActive(false);
                }
            }

        }
    }

    private IEnumerator CheckPlayerSkeaking()
    {
        while (true)
        {
            CheckSpeaking();
            yield return new WaitForSeconds(checkSpeakingInterval);
        }
    }
}
