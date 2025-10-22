/* CommandsExample.cs */
/* Written by Kaz */
namespace UltimateChatBoxExample
{
	using UnityEngine;
	using UnityEngine.Events;
	using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
	using TankAndHealerStudioAssets;

	public class CommandsExample : MonoBehaviour
	{
		public UltimateChatBox chatBox;
		public string myUsername = "LocalPlayer";

		[System.Serializable]
		public class Command
		{
			public string commandValue;
			public string messageToSend;
			public bool requireMessageValue = false;
			public UnityEvent unityEvent;
		}
		public List<Command> Commands = new List<Command>()
	{
		new Command(){ commandValue = "/help", messageToSend = "Available commands: /invite [PlayerName], /block [PlayerUsername], /joke" },
		new Command(){ commandValue = "/invite", messageToSend = "Attempting to invite [{0}].", requireMessageValue = true, },
		new Command(){ commandValue = "/block", messageToSend = "Blocking [{0}] from future communication.", requireMessageValue = true },
		new Command(){ commandValue = "/joke", messageToSend = "Why did the chicken cross the road?" },
	};


		private void Start ()
		{
			// Subscribe to the input field submitted callback.
			chatBox.OnInputFieldSubmitted += ( message ) =>
			{
			// If the chat box contains a command, then just return so that the command is not logged.
			if( chatBox.InputFieldContainsCommand )
					return;

				chatBox.RegisterChat( myUsername, message, UltimateChatBoxStyles.boldUsername );
			};

			// Subscribe to the input field submitting a command value.
			chatBox.OnInputFieldCommandSubmitted += ( command, message ) =>
			{
			// Temporary bool to store if a command from the list is found.
			bool commandFound = false;

			// Loop through all the available commands.
			for( int i = 0; i < Commands.Count; i++ )
				{
				// If the command value from the input field is the same as the one in the list...
				if( command == Commands[ i ].commandValue.ToLower() )
					{
					// If the command requires a message following the command...
					if( Commands[ i ].requireMessageValue )
						{
						// If the message value is empty, then send a chat warning message.
						if( message == string.Empty )
								chatBox.RegisterChat( "[SYSTEM]", $"Please provide a message after the command.", UltimateChatBoxStyles.noticeUsername );
						// Else the message is assigned...
						else
							{
							// If there is a Unity Action assigned to this command, then invoke it.
							Commands[ i ].unityEvent?.Invoke();

							// Register a chat with the command formated.
							chatBox.RegisterChat( "[SYSTEM]", $"{string.Format( Commands[ i ].messageToSend, message )}", UltimateChatBoxStyles.errorUsername );
							}
						}
					// Else the command does not require a following message...
					else
						{
						// If there is a Unity Action assigned to this command, then invoke it.
						Commands[ i ].unityEvent?.Invoke();

						// Register the command message to chat.
						chatBox.RegisterChat( "[SYSTEM]", $"{Commands[ i ].messageToSend}", UltimateChatBoxStyles.errorUsername );
						}

					// Set commandFound to true and break the loop.
					commandFound = true;
						break;
					}
				}
			// If there was no command found, and the chat box is not enabled any more, then send a warning that no command was found in the list.
			if( !commandFound && !chatBox.InputFieldEnabled )
					chatBox.RegisterChat( "[SYSTEM]", $"Unknown Command", UltimateChatBoxStyles.errorMessage );
			};
		}

		private void Update ()
		{
			// If the forward slash key has been pressed this frame, then enable the input field.
#if ENABLE_INPUT_SYSTEM
		if( InputSystem.GetDevice<Keyboard>().slashKey.wasPressedThisFrame )
#else
			if( Input.GetKeyDown( KeyCode.Slash ) )
#endif
				chatBox.EnableInputField( "/" );
		}
	}
}