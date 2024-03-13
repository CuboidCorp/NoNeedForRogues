using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public class VivoxServerManager : NetworkBehaviour
{
    public bool soloMode = false;

    public static VivoxServerManager Instance;

    public static Dictionary<string, GameObject> participants = new();

    public static int nbTotalPlayers = 0;

    public static int nbConnectedPlayers = 0;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        VivoxService.Instance.ParticipantAddedToChannel += AddedParticipant;
        VivoxService.Instance.ParticipantRemovedFromChannel += RemovedParticpant;
    }

    /// <summary>
    /// Quand un participant est ajouté au channel
    /// On connecte son audioTap
    /// </summary>
    /// <param name="participant">Le participant</param>
    private void AddedParticipant(VivoxParticipant participant)
    {
        Debug.Log(participant.PlayerId);
        Debug.Log(OwnerClientId);

        GameObject participantTap = participant.CreateVivoxParticipantTap("participantTap");
        participant.ParticipantSpeechDetected += MultiplayerGameManager.Instance.TestSpeech;
    }

    /// <summary>
    /// Quand un participant est retiré du channel
    /// </summary>
    /// <param name="participant"></param>
    private void RemovedParticpant(VivoxParticipant participant)
    {
        //On supprime le participantTap du gars -> En theorie c'est fait car son gameObject est deconnecte
        participant.ParticipantSpeechDetected -= MultiplayerGameManager.Instance.TestSpeech;
    }
}
