using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{

    public static RelayManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    /// <summary>
    /// Create a new relay with the given number of players -1 (for the host)
    /// </summary>
    /// <param name="nbPlayers">The number of players including the host</param>
    /// <returns>The join code for the relay</returns>
    public async Task<string> CreateRelay(int nbPlayers)
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(nbPlayers - 1);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            Debug.Log("Join code: " + joinCode);

            RelayServerData relayServerData = new(alloc, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartHost();

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Error creating relay: " + e.Message);
            return "";
        }
    }

    /// <summary>
    /// Join an existing relay with the given join code
    /// </summary>
    /// <param name="joinCode">Le join code pr rejoindre le relay</param>
    public async void JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            RelayServerData relayServerData = new(joinAlloc, "dtls");

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("Error joining relay: " + e.Message);
        }
    }
}
