using UnityEngine;
using Coherence;
using Coherence.Toolkit;

/// <summary>
/// Networked chat component attached to each player.
/// Handles sending and receiving chat messages via Coherence.
/// Only the local player can send messages (authority check).
/// All players receive and relay messages to the ChatUI.
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

    /// <summary>
    /// Send a chat message (only works if this is the local player with authority)
    /// </summary>
    public void SendMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!coherenceSync.HasStateAuthority)
        {
            Debug.LogWarning("NetworkedChat: Cannot send message - no authority over this player");
            return;
        }

        string username = networkedPlayer != null ? networkedPlayer.playerUsername : "Player";
        
        Debug.Log($"NetworkedChat: Sending message from {username}: {message}");
        
        // Send command to all clients (including self)
        coherenceSync.SendCommand<NetworkedChat>(
            nameof(ReceiveMessage),
            MessageTarget.All,
            username,
            message
        );
    }

    /// <summary>
    /// Coherence command - called on all clients when any player sends a message
    /// </summary>
    [Command]
    public void ReceiveMessage(string username, string message)
    {
        Debug.Log($"NetworkedChat: Received message from {username}: {message}");
        
        // Forward the message to the ChatUI (which exists in the scene)
        ChatUI chatUI = FindFirstObjectByType<ChatUI>();
        if (chatUI != null)
        {
            chatUI.DisplayMessage(username, message);
        }
        else
        {
            Debug.LogError("NetworkedChat: ChatUI not found in scene!");
        }
    }
}

