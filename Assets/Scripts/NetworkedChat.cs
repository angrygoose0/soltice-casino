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
    /// Broadcast a bet announcement to all other players
    /// </summary>
    public void BroadcastBet(ulong betAmount)
    {
        if (!coherenceSync.HasStateAuthority || networkedPlayer == null)
            return;

        string username = networkedPlayer.playerUsername;
        string message = $"{username} bet: {betAmount}";
        
        coherenceSync.SendCommand<NetworkedChat>(
            nameof(ReceiveBetAnnouncement),
            MessageTarget.Other,
            message
        );
    }

    /// <summary>
    /// Broadcast a win announcement to all other players
    /// </summary>
    public void BroadcastWin(ulong winAmount)
    {
        if (!coherenceSync.HasStateAuthority || networkedPlayer == null)
            return;

        string username = networkedPlayer.playerUsername;
        string message = $"{username} won: {winAmount}";
        
        coherenceSync.SendCommand<NetworkedChat>(
            nameof(ReceiveWinAnnouncement),
            MessageTarget.Other,
            message
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

    /// <summary>
    /// Coherence command - receives bet announcements from other players
    /// </summary>
    [Command]
    public void ReceiveBetAnnouncement(string message)
    {
        ChatUI.Instance?.DisplayBetAnnouncement(message, bold: false);
    }

    /// <summary>
    /// Coherence command - receives win announcements from other players
    /// </summary>
    [Command]
    public void ReceiveWinAnnouncement(string message)
    {
        ChatUI.Instance?.DisplayWinAnnouncement(message, bold: false);
    }
}

