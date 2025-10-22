using UnityEngine;
using TankAndHealerStudioAssets;
using Coherence.Toolkit;
using Coherence.Connection;

/// <summary>
/// Chat UI manager - handles the chat UI and input.
/// Lives in the scene (not on player prefab).
/// Finds the local player and uses their NetworkedChat to send messages.
/// </summary>
public class ChatUI : MonoBehaviour
{
    [SerializeField] private PlayerManager playerManager;
    
    private UltimateChatBox chatBox;
    private CoherenceBridge coherenceBridge;
    private bool isConnected;

    private void Start()
    {
        // Find the ChatBox in the scene
        chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        if (chatBox != null)
        {
            chatBox.gameObject.SetActive(false);
            chatBox.OnInputFieldSubmitted += OnMessageSubmitted;
        }
        else
        {
            Debug.LogError("ChatUI: UltimateChatBox not found in scene!");
        }

        coherenceBridge = FindFirstObjectByType<CoherenceBridge>();
        if (coherenceBridge != null)
        {
            coherenceBridge.onConnected.AddListener(OnConnected);
            coherenceBridge.onDisconnected.AddListener(OnDisconnected);
        }
    }

    private void OnDestroy()
    {
        if (chatBox != null)
        {
            chatBox.OnInputFieldSubmitted -= OnMessageSubmitted;
        }

        if (coherenceBridge != null)
        {
            coherenceBridge.onConnected.RemoveListener(OnConnected);
            coherenceBridge.onDisconnected.RemoveListener(OnDisconnected);
        }
    }

    private void OnConnected(CoherenceBridge bridge)
    {
        isConnected = true;
        if (chatBox != null)
        {
            chatBox.gameObject.SetActive(true);
            Debug.Log($"ChatUI: Connected to Coherence. ChatBox enabled on client {bridge.ClientID}");
        }
    }

    private void OnDisconnected(CoherenceBridge bridge, ConnectionCloseReason reason)
    {
        isConnected = false;
        if (chatBox != null)
        {
            chatBox.gameObject.SetActive(false);
        }
    }

    private void OnMessageSubmitted(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!isConnected)
        {
            Debug.LogWarning("ChatUI: Cannot send message - not connected to Coherence");
            return;
        }

        // Find the local player and use their NetworkedChat to send the message
        if (playerManager != null)
        {
            NetworkedPlayer localPlayer = playerManager.GetLocalPlayer();
            if (localPlayer != null)
            {
                NetworkedChat chat = localPlayer.GetComponent<NetworkedChat>();
                if (chat != null)
                {
                    chat.SendMessage(message);
                }
                else
                {
                    Debug.LogError("ChatUI: Local player doesn't have NetworkedChat component!");
                }
            }
            else
            {
                Debug.LogWarning("ChatUI: Local player not spawned yet");
            }
        }
        else
        {
            Debug.LogError("ChatUI: PlayerManager not assigned!");
        }
    }

    /// <summary>
    /// Called by NetworkedChat when a message is received from any player
    /// </summary>
    public void DisplayMessage(string username, string message)
    {
        // Find the local chatbox (in case it changed or was late-initialized)
        if (chatBox == null)
        {
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        }
        
        if (chatBox != null)
        {
            chatBox.RegisterChat(username, message);
        }
        else
        {
            Debug.LogError("ChatUI: Cannot display message - UltimateChatBox not found!");
        }
    }
}

