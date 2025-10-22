/* UltimateChatBoxStyles.cs */
/* Written by Kaz */
namespace TankAndHealerStudioAssets
{
	using UnityEngine;

	/// <summary>
	/// A collection of basic chat box styles.
	/// </summary>
	public static class UltimateChatBoxStyles
	{
		/// <summary>
		/// Displays the chat with no style.
		/// </summary>
		public static UltimateChatBox.ChatStyle none = new UltimateChatBox.ChatStyle() { };

		// BASIC STYLES //
		/// <summary>
		/// Displays the chat with a bold username.
		/// </summary>
		public static UltimateChatBox.ChatStyle boldUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
		};

		/// <summary>
		/// Displays the chat with a bold blue username.
		/// </summary>
		public static UltimateChatBox.ChatStyle blueUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = new Color( 0.0f, 0.5f, 1.0f, 1.0f ),
		};

		/// <summary>
		/// Displays the chat with a bold green username.
		/// </summary>
		public static UltimateChatBox.ChatStyle greenUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = new Color( 0.0f, 1f, 0f, 1.0f ),
		};

		/// <summary>
		/// Displays the chat with a bold and italicized username, along with the message being just italicized also.
		/// </summary>
		public static UltimateChatBox.ChatStyle whisperUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameItalic = true,
			usernameColor = new Color( 1.0f, 0.25f, 1.0f, 1.0f ),
			messageItalic = true,
		};
		// END BASIC STYLES //

		// SYSTEM //
		/// <summary>
		/// Mainly used for system type messages. Displays the username as yellow.
		/// </summary>
		public static UltimateChatBox.ChatStyle noticeUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = Color.yellow,
			messageBold = true,
			disableInteraction = true,
			noUsernameFollowupText = true,
		};

		/// <summary>
		/// Mainly used for system type messages. Displays the username and message as yellow.
		/// </summary>
		public static UltimateChatBox.ChatStyle noticeMessage = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = Color.yellow,
			messageBold = true,
			messageColor = Color.yellow,
			disableInteraction = true,
			noUsernameFollowupText = true,
		};

		/// <summary>
		/// Mainly used for system type messages. Displays the username as orange.
		/// </summary>
		public static UltimateChatBox.ChatStyle warningUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = new Color( 1.0f, 0.465f, 0.0f, 1.0f ),
			messageBold = true,
			disableInteraction = true,
			noUsernameFollowupText = true,
		};

		/// <summary>
		/// Mainly used for system type messages. Displays the username and message as orange.
		/// </summary>
		public static UltimateChatBox.ChatStyle warningMessage = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = new Color( 1.0f, 0.465f, 0.0f, 1.0f ),
			messageBold = true,
			messageColor = new Color( 1.0f, 0.465f, 0.0f, 1.0f ),
			disableInteraction = true,
			noUsernameFollowupText = true,
		};

		/// <summary>
		/// Mainly used for system type messages. Displays the username as red.
		/// </summary>
		public static UltimateChatBox.ChatStyle errorUsername = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = Color.red,
			messageBold = true,
			disableInteraction = true,
			noUsernameFollowupText = true,
		};

		/// <summary>
		/// Mainly used for system type messages. Displays the username and message as red.
		/// </summary>
		public static UltimateChatBox.ChatStyle errorMessage = new UltimateChatBox.ChatStyle()
		{
			usernameBold = true,
			usernameColor = Color.red,
			messageBold = true,
			messageColor = Color.red,
			disableInteraction = true,
			noUsernameFollowupText = true,
		};
		// END SYSTEM //
	}
}