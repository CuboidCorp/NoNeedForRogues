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

    private bool isConnected = false;

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
        await servVivox.InitializeAsync();
        await servVivox.LoginAsync();
        PlayerUIManager.Instance.SetConnexionVivoxTexte("Initialisation");
    }

    /// <summary>
    /// Deconnexion de vivox
    /// </summary>
    /// <returns>Quand la deconnexion est termin�e</returns>
    public async Task LeaveVivox()
    {
        PlayerUIManager.Instance.SetConnexionVivoxTexte("Deconnect�");
        isConnected = false;
        await servVivox.LeaveAllChannelsAsync();
        await servVivox.LogoutAsync();
        AuthenticationService.Instance.SignOut();
    }

    /// <summary>
    /// Rejoint le channel de chat 
    /// </summary>
    /// <returns>Quand on a rejoint le voice chat</returns>
    private async Task JoinPositionalChannelAsync()
    {
        ChatCapability chat = ChatCapability.AudioOnly;
        Channel3DProperties channel3DProperties = new(maxDistance, minAudibleDistance, audioFadeIntensity, AudioFadeModel.InverseByDistance);
        await servVivox.JoinPositionalChannelAsync(channelName, chat, channel3DProperties);
        PlayerUIManager.Instance.SetConnexionVivoxTexte("Connect�");
        StartCoroutine(ClearVivoxTexte());
    }

    private IEnumerator ClearVivoxTexte()
    {
        yield return new WaitForSeconds(5);
        PlayerUIManager.Instance.SetConnexionVivoxTexte("");
    }

    /// <summary>
    /// Log in de Vivox
    /// </summary>
    private async void OnLoggedIn()
    {
        MultiplayerGameManager.Instance.SendAuthIdServerRpc(OwnerClientId, AuthenticationService.Instance.PlayerId);
        if (MultiplayerGameManager.Instance.soloMode)
        {
            try
            {
                await servVivox.JoinEchoChannelAsync(echoChannelName, ChatCapability.AudioOnly);
                Debug.Log("Echo Channel joined");
                PlayerUIManager.Instance.SetConnexionVivoxTexte("");
                GameObject channelTap = new("ChannelTap")
                {
                    tag = "ChannelTap" //Pour les retrouver plus tard
                };
                DontDestroyOnLoad(channelTap);
                channelTap.AddComponent<AudioSource>();
                channelTap.AddComponent<VivoxChannelAudioTap>().ChannelName = echoChannelName;
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }

        }
        else
        {
            await JoinPositionalChannelAsync();

        }
        MultiplayerGameManager.Instance.ActiveAudioTaps();

    }

    /// <summary>
    /// Quand le participant est ajout� au channel
    /// </summary>
    /// <param name="vivoxParticipant">Le participant ajout�</param>
    private void ParticipantAdded(VivoxParticipant vivoxParticipant)
    {
        int playIndex = MultiplayerGameManager.Instance.AddPlayerVivoxInfo(vivoxParticipant.PlayerId, vivoxParticipant);
        if (!vivoxParticipant.IsSelf)
        {
            participants.Add(vivoxParticipant);
            GameObject tap = vivoxParticipant.CreateVivoxParticipantTap("Tap " + vivoxParticipant.PlayerId);
            MultiplayerGameManager.Instance.AddParamToParticipantAudioSource(playIndex);
            Transform playerTransform = MultiplayerGameManager.Instance.GetPlayerTransformFromAuthId(vivoxParticipant.PlayerId);
            tap.transform.SetParent(playerTransform); //Pas de null check = Programmation de gros porc ici flemme
            tap.transform.localPosition = new Vector3(0, 1.6f, 0);
        }
        else if (!MultiplayerGameManager.Instance.soloMode)
        {
            isConnected = true;
        }

    }

    /// <summary>
    /// Retourne si on est connect� � Vivox ou non
    /// </summary>
    /// <returns>True si connect�, false sinon</returns>
    public bool IsConnected()
    {
        return isConnected;
    }

    private void Update()
    {
        if (isConnected)
        {
            servVivox.Set3DPosition(gameObject, channelName); //Check si �a marche sans avec les participants taps
        }
    }

    /// <summary>
    /// Enleve un participant du channel
    /// </summary>
    /// <param name="vivoxParticipant">Le participant qui se deconnecte</param>
    private void ParticipantRemoved(VivoxParticipant vivoxParticipant)
    {
        if (!vivoxParticipant.IsSelf)
        {
            //On enleve le participant dans l'array de multiplayerGameManager
            vivoxParticipant.DestroyVivoxParticipantTap();
        }
    }
}
