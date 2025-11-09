using UnityEngine;
using Coherence;
using Coherence.Toolkit;
using TankAndHealerStudioAssets;

/// <summary>
/// Minimal network transport for chat messages.
/// Attached to each player prefab - required for Coherence commands.
/// Only handles sending commands over the network and receiving them.
/// All logic lives in ChatUI.
/// </summary>
[RequireComponent(typeof(CoherenceSync))]
public class NetworkedChat : MonoBehaviour
{
    private CoherenceSync coherenceSync;
    private NetworkedPlayer networkedPlayer;

    private void Awake()
    {
        coherenceSync = GetComponent<CoherenceSync>();
        networkedPlayer = GetComponent<NetworkedPlayer>();
    }

    private void Start()
    {
        // Register with ChatUI if this is the local player
        if (coherenceSync.HasStateAuthority)
        {
            ChatUI.Instance?.RegisterNetworkedChat(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from ChatUI if this is the local player
        if (coherenceSync.HasStateAuthority)
        {
            ChatUI.Instance?.UnregisterNetworkedChat();
        }
    }

    /// <summary>
    /// Send a message over the network to other players (network transport only)
    /// </summary>
    public void SendToOthers(string message, string styleType, bool includeUsername)
    {
        if (string.IsNullOrWhiteSpace(message) || !coherenceSync.HasStateAuthority)
            return;

        string username = includeUsername && networkedPlayer != null ? networkedPlayer.playerUsername : "";
        
        coherenceSync.SendCommand<NetworkedChat>(
            nameof(ReceiveMessage),
            MessageTarget.Other,
            username,
            message,
            styleType
        );
    }

    /// <summary>
    /// Coherence command - receives messages from other players and forwards to ChatUI
    /// </summary>
    [Command]
    public void ReceiveMessage(string username, string message, string styleType = "")
    {
        ChatUI.Instance?.DisplayMessage(username, message, ChatUI.Instance.GetStyleFromType(styleType));
    }
}

