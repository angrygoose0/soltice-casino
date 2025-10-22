# Chat System Migration Guide

## Overview

The chat system has been refactored to follow the same pattern as player movement:
- **OLD**: ChatManager singleton with CoherenceSync in the scene
- **NEW**: NetworkedChat component on each player + ChatUI in the scene

## Why This Change?

1. **Consistency**: Same pattern as NetworkedPlayer - each player handles their own networked behavior
2. **Authority**: Only the local player can send messages (authority check)
3. **Cleaner**: No need for separate scene-owned CoherenceSync object for chat
4. **Scalable**: Each player's chat is part of their networked state

## Architecture

### Old System (ChatManager)
```
Scene: ChatManager (CoherenceSync) → UltimateChatBox
       ↓
       Gets username from PlayerManager
       Sends/receives messages via scene-owned CoherenceSync
```

### New System (NetworkedChat + ChatUI)
```
Player Prefab: NetworkedChat (uses player's CoherenceSync)
       ↑
Scene: ChatUI → UltimateChatBox
       ↓
       Finds local player
       Uses their NetworkedChat to send messages
```

## Migration Steps

### 1. Remove Old ChatManager

1. Open your scene
2. Find the GameObject with the old `ChatManager` component
3. Remove the `ChatManager` component (or delete the GameObject if it only had ChatManager)
4. **Important**: If this GameObject had a CoherenceSync component just for chat, you can remove that too

### 2. Add NetworkedChat to Player Prefab

1. Open `Assets/player.prefab`
2. Select the root GameObject
3. Add Component → Scripts → **Networked Chat**
4. No configuration needed - it automatically finds NetworkedPlayer and CoherenceSync

### 3. Add ChatUI to Scene

1. In your main scene, create a new empty GameObject
2. Name it "ChatUI" (or similar)
3. Add Component → Scripts → **Chat UI**
4. In the Inspector:
   - Assign `playerManager` reference (drag your PlayerManager from the scene)
5. Make sure `UltimateChatBox` is still in your scene (ChatUI finds it automatically)

### 4. Update CoherenceSync Bindings

The player prefab's CoherenceSync now needs to include the NetworkedChat commands:

1. Open the player prefab
2. Select the root GameObject with CoherenceSync
3. Click "Bake" on the CoherenceSync component to regenerate the schema
4. This will include the `ReceiveMessage` command from NetworkedChat

### 5. Test

1. Build and run two instances
2. Both players should be able to send chat messages
3. Messages should appear in both clients
4. Each message should show the correct username

## Key Differences

| Feature | Old (ChatManager) | New (NetworkedChat + ChatUI) |
|---------|-------------------|------------------------------|
| Location | Scene singleton | Per-player component |
| CoherenceSync | Separate scene object | Uses player's CoherenceSync |
| Authority | Scene-owned | Player-owned (HasStateAuthority) |
| Username | Pulled from PlayerManager | From NetworkedPlayer component |
| Send Message | ChatManager.OnMessageSubmitted | NetworkedChat.SendMessage |
| Receive Message | ChatManager.ReceiveMessage | NetworkedChat.ReceiveMessage → ChatUI.DisplayMessage |

## How It Works

### Sending a Message

1. Player types message in UltimateChatBox
2. ChatUI.OnMessageSubmitted is called
3. ChatUI finds the local player (via PlayerManager)
4. ChatUI calls localPlayer.NetworkedChat.SendMessage()
5. NetworkedChat checks authority (HasStateAuthority)
6. If authorized, sends Coherence command to all clients

### Receiving a Message

1. Coherence command arrives at NetworkedChat.ReceiveMessage on all clients
2. NetworkedChat forwards to ChatUI.DisplayMessage
3. ChatUI displays in UltimateChatBox

## Troubleshooting

### Messages not sending
- **Check**: Does the player prefab have NetworkedChat component?
- **Check**: Is the player's CoherenceSync baked with the latest schema?
- **Check**: Does ChatUI have PlayerManager reference assigned?

### Messages not appearing
- **Check**: Is UltimateChatBox in the scene?
- **Check**: Is ChatUI in the scene?
- **Check**: Check console for errors about ChatUI or NetworkedChat

### Wrong username showing
- **Check**: Is NetworkedPlayer.playerUsername synced in CoherenceSync bindings?
- **Check**: Is the username being set correctly in PlayerManager.OnConnected?

## Code Comparison

### Old: Sending a message
```csharp
// ChatManager.cs
coherenceSync.SendCommand<ChatManager>(
    nameof(ReceiveMessage),
    MessageTarget.All,
    username,
    message
);
```

### New: Sending a message
```csharp
// NetworkedChat.cs (called by ChatUI)
coherenceSync.SendCommand<NetworkedChat>(
    nameof(ReceiveMessage),
    MessageTarget.All,
    username,
    message
);
```

The actual command code is similar, but now:
- It's on the player prefab (authority-controlled)
- Username comes from NetworkedPlayer component
- Authority is automatically checked (HasStateAuthority)

## Benefits

✅ **No separate scene-owned CoherenceSync needed**
✅ **Consistent with player movement pattern**
✅ **Automatic authority checking**
✅ **Username from single source of truth**
✅ **Scales better with multiple players**
✅ **Cleaner scene hierarchy**

## Files to Delete (Optional)

Once migration is complete and tested:
- `Assets/Scripts/ChatManager.cs` (deprecated)
- Any prefabs that referenced ChatManager

## Questions?

If chat isn't working after migration:
1. Check Unity Console for error messages
2. Verify player prefab has both NetworkedPlayer and NetworkedChat
3. Verify scene has ChatUI with PlayerManager reference
4. Verify UltimateChatBox is in the scene
5. Rebake CoherenceSync schema on the player prefab

