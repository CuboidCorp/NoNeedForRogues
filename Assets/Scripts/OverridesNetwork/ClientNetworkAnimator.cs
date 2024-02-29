using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Syncing les animations du joueur sur le réseau, mais on fait confiance au client
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkAnimator: NetworkAnimator
{
    /// <summary>
    /// Permet de faire que le client a le droit de bouger le joueur, on leur fait
    /// confiance pour ne pas tricher
    /// </summary>
    /// <returns></returns>
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
