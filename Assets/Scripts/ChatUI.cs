using UnityEngine;
using TankAndHealerStudioAssets;
using Coherence.Toolkit;
using Coherence.Connection;
using UnityEngine.InputSystem;

/// <summary>
/// Chat manager - handles UI, input, display, and network messaging.
/// Lives in the scene (not on player prefab).
/// Communicates with NetworkedChat components on players for network transport.
/// </summary>
public class ChatUI : MonoBehaviour
{
    [SerializeField] private PlayerManager playerManager;
    
    private UltimateChatBox chatBox;
    private CoherenceBridge coherenceBridge;
    private bool isConnected;
    private static ChatUI instance;
    private NetworkedChat cachedNetworkedChat;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        if (chatBox != null)
        {
            UIFader.HideImmediate(chatBox.gameObject);
            chatBox.OnInputFieldSubmitted += OnMessageSubmitted;
        }
        else
        {
            GameLogger.LogError("ChatUI: UltimateChatBox not found in scene!");
        }

        coherenceBridge = FindFirstObjectByType<CoherenceBridge>();
        if (coherenceBridge != null)
        {
            coherenceBridge.onConnected.AddListener(OnConnected);
            coherenceBridge.onDisconnected.AddListener(OnDisconnected);
        }
    }

    public void RegisterNetworkedChat(NetworkedChat networkedChat)
    {
        cachedNetworkedChat = networkedChat;
    }

    public void UnregisterNetworkedChat()
    {
        cachedNetworkedChat = null;
    }

    public static ChatUI Instance => instance;

    private void Update()
    {
        // Test: Press '9' to broadcast local hello world message
        if (Keyboard.current != null && Keyboard.current.digit9Key.wasPressedThisFrame)
        {
            var testStyle = CreateCustomStyle(
                usernameColor: Color.cyan,
                messageColor: Color.yellow,
                usernameBold: true,
                messageBold: true
            );
            DisplayMessage("TEST", "Hello World! (Local Only)", testStyle);
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
            UIFader.FadeIn(chatBox.gameObject);
            GameLogger.Log($"ChatUI: Connected to Coherence. ChatBox enabled on client {bridge.ClientID}");
        }
    }

    private void OnDisconnected(CoherenceBridge bridge, ConnectionCloseReason reason)
    {
        isConnected = false;
        if (chatBox != null)
        {
            UIFader.FadeOut(chatBox.gameObject);
        }
    }

    private void OnMessageSubmitted(string message)
    {
        if (string.IsNullOrWhiteSpace(message) || !isConnected || cachedNetworkedChat == null)
            return;

        NetworkedPlayer localPlayer = playerManager.GetLocalPlayer();
        if (localPlayer == null)
            return;

        // Display local version with bold white styling
        DisplayMessage(localPlayer.playerUsername, message, GetStyleFromType("local_player"));

        // Send to others with normal white styling
        cachedNetworkedChat.SendToOthers(message, "other_player", includeUsername: true);
    }

    /// <summary>
    /// Display a message with a custom style. Called by NetworkedChat or used directly for local messages.
    /// Default style is UltimateChatBoxStyles.none if not specified.
    /// </summary>
    public void DisplayMessage(string username, string message, UltimateChatBox.ChatStyle style = default)
    {
        if (chatBox == null)
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        chatBox?.RegisterChat(username, message, style);
    }

    /// <summary>
    /// Display an announcement (e.g., player joined/left) without the colon after username
    /// </summary>
    public void DisplayAnnouncement(string message, bool bold = false)
    {
        if (chatBox == null)
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        var announcementStyle = CreateCustomStyle(
            messageColor: Color.yellow,
            messageBold: bold,
            noUsernameFollowupText: true
        );
        
        chatBox?.RegisterChat("", message, announcementStyle);
    }

    /// <summary>
    /// Display a bet announcement in white/italic text without the colon after username
    /// </summary>
    public void DisplayBetAnnouncement(string message, bool bold = false)
    {
        if (chatBox == null)
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        var betStyle = CreateCustomStyle(
            messageColor: Color.white,
            messageItalic: true,
            messageBold: bold,
            noUsernameFollowupText: true
        );
        
        chatBox?.RegisterChat("", message, betStyle);
    }

    /// <summary>
    /// Display a win announcement in green text without the colon after username
    /// </summary>
    public void DisplayWinAnnouncement(string message, bool bold = false)
    {
        if (chatBox == null)
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        var winStyle = CreateCustomStyle(
            messageColor: Color.green,
            messageBold: bold,
            noUsernameFollowupText: true
        );
        
        chatBox?.RegisterChat("", message, winStyle);
    }

    /// <summary>
    /// Display a crash announcement in red text without the colon after username
    /// </summary>
    public void DisplayCrashAnnouncement(string message, bool bold = false)
    {
        if (chatBox == null)
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        var crashStyle = CreateCustomStyle(
            messageColor: Color.red,
            messageBold: bold,
            noUsernameFollowupText: true
        );
        
        chatBox?.RegisterChat("", message, crashStyle);
    }

    /// <summary>
    /// Display a tick/multiplier announcement in green text without the colon after username
    /// </summary>
    public void DisplayTickAnnouncement(string message, bool bold = false)
    {
        if (chatBox == null)
            chatBox = FindFirstObjectByType<UltimateChatBox>();
        
        var tickStyle = CreateCustomStyle(
            messageColor: Color.green,
            messageBold: bold,
            noUsernameFollowupText: true
        );
        
        chatBox?.RegisterChat("", message, tickStyle);
    }

    /// <summary>
    /// Create a custom chat style with specific settings
    /// </summary>
    public UltimateChatBox.ChatStyle CreateCustomStyle(
        Color? usernameColor = null,
        Color? messageColor = null,
        bool usernameBold = false,
        bool messageBold = false,
        bool usernameItalic = false,
        bool messageItalic = false,
        bool usernameUnderlined = false,
        bool messageUnderlined = false,
        bool disableInteraction = false,
        bool noUsernameFollowupText = false)
    {
        return new UltimateChatBox.ChatStyle
        {
            usernameColor = usernameColor ?? Color.clear,
            messageColor = messageColor ?? Color.clear,
            usernameBold = usernameBold,
            messageBold = messageBold,
            usernameItalic = usernameItalic,
            messageItalic = messageItalic,
            usernameUnderlined = usernameUnderlined,
            messageUnderlined = messageUnderlined,
            disableInteraction = disableInteraction,
            noUsernameFollowupText = noUsernameFollowupText
        };
    }

    /// <summary>
    /// Get a ChatStyle from a string identifier
    /// </summary>
    public UltimateChatBox.ChatStyle GetStyleFromType(string styleType)
    {
        return styleType switch
        {
            "local_player" => new UltimateChatBox.ChatStyle
            {
                usernameColor = Color.white,
                messageColor = Color.white,
                usernameBold = true,
                messageBold = true
            },
            "other_player" => new UltimateChatBox.ChatStyle
            {
                usernameColor = Color.white,
                messageColor = Color.white
            },
            "bold" => UltimateChatBoxStyles.boldUsername,
            "blue" => UltimateChatBoxStyles.blueUsername,
            "green" => UltimateChatBoxStyles.greenUsername,
            "whisper" => UltimateChatBoxStyles.whisperUsername,
            "notice" => UltimateChatBoxStyles.noticeMessage,
            "warning" => UltimateChatBoxStyles.warningMessage,
            "error" => UltimateChatBoxStyles.errorMessage,
            _ => UltimateChatBoxStyles.none
        };
    }
}

