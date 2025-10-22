/* ChatStylesExample.cs */
/* Written by Kaz */
namespace UltimateChatBoxExample
{
	using UnityEngine;
	using System.Collections.Generic;
	using TankAndHealerStudioAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

	public class ChatStylesExample : MonoBehaviour
	{
		public UltimateChatBox chatBox;
		public string myUsername = "LocalPlayer";

		public UltimateChatBox.ChatStyle customStyle = new UltimateChatBox.ChatStyle();
		List<UltimateChatBox.ChatStyle> chatStyles = new List<UltimateChatBox.ChatStyle>();
		int currentChatTypeIndex;


		private void Start ()
		{
			// Create a new list of chat styles and populate it with some of the default chat styles.
			chatStyles = new List<UltimateChatBox.ChatStyle>();
			chatStyles.Add( UltimateChatBoxStyles.boldUsername );
			chatStyles.Add( UltimateChatBoxStyles.whisperUsername );
			chatStyles.Add( UltimateChatBoxStyles.blueUsername );
			chatStyles.Add( UltimateChatBoxStyles.greenUsername );

			// Add the custom chat style to the list.
			chatStyles.Add( customStyle );

			// Subscribe to the input field submit.
			chatBox.OnInputFieldSubmitted += ( message ) =>
			{
			// Registered the local input field chat with the local username, with the current chat style.
			chatBox.RegisterChat( myUsername, message, chatStyles[ currentChatTypeIndex ] );
			};
		}

		private void Update ()
		{
			// If the chat box is enabled and the Tab key has been pressed this frame...
#if ENABLE_INPUT_SYSTEM
		if( chatBox.IsEnabled && InputSystem.GetDevice<Keyboard>().tabKey.wasPressedThisFrame )
#else
			if( chatBox.IsEnabled && Input.GetKeyDown( KeyCode.Tab ) )
#endif
			{
				// Increase the stored chat style index value.
				currentChatTypeIndex++;

				// If the value is greater than the stored style list count, then reset to index 0.
				if( currentChatTypeIndex >= chatStyles.Count )
					currentChatTypeIndex = 0;

				// Update the prefix image color to the username color of the chat style.
				chatBox.ExtraImageColor = chatStyles[ currentChatTypeIndex ].usernameColor == Color.clear ? Color.white : chatStyles[ currentChatTypeIndex ].usernameColor;
			}
		}
	}
}