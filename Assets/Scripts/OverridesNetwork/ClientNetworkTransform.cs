using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Syncing le transform du joueur sur le réseau, mais on fait confiance au client
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
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
