using System;
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
    /// <returns>Quand l'initialisation est termin�e</returns>
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
        try
        {
            await servVivox.InitializeAsync();
            await servVivox.LoginAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning(e);
            enabled = false;
        }
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
            GameObject channelTap = new("ChannelTap")
            {
                tag = "ChannelTap" //Pour les retrouver plus tard
            };
            DontDestroyOnLoad(channelTap);
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
    /// Quand le participant est ajout� au channel
    /// </summary>
    /// <param name="vivoxParticipant">Le participant ajout�</param>
    private void ParticipantAdded(VivoxParticipant vivoxParticipant)
    {
        Debug.Log(vivoxParticipant.PlayerId + " / " + AuthenticationService.Instance.PlayerId);
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
        AuthenticationService.Instance.SignOut();
        StopAllCoroutines();
    }

    /// <summary>
    /// Faut v�rifier si tous les joueurs sont a speaking
    /// </summary>
    private void CheckSpeaking()
    {
        foreach ((VivoxParticipant, GameObject) participantEtGo in MultiplayerGameManager.Instance.authServicePlayerIds.Values) //TODO : Sur les clients on a pas du tout �a
        {
            if (participantEtGo.Item1 != null)
            {
                Debug.Log("Speech :" + participantEtGo.Item1.SpeechDetected);
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
