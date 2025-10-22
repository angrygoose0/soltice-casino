/* KillFeedExampleTest.cs */
/* Written by Kaz */
namespace UltimateChatBoxExample
{
	using TMPro;
	using UnityEngine;
	using System.Collections.Generic;
	using TankAndHealerStudioAssets;

	public class KillFeedExampleTest : MonoBehaviour
	{
		public UltimateChatBox chatBox;
		public TMP_SpriteAsset spriteAsset;

		public List<string> names = new List<string>()
		{
			"<color=#FF0011>Player00</color>",
			"<color=#007BFF>Player01</color>",
			"<color=#FF0011>Player02</color>",
			"<color=#FF0011>Player03</color>",
			"<color=#FF0011>Player04</color>",
		};

		public void SendKillFeed ( string weaponName )
		{
			// Store the name of the winner of the kill.
			string winner = names[ Random.Range( 0, names.Count ) ];

			// Store another name for the one who was killed.
			string loser = names[ Random.Range( 0, names.Count ) ];

			// While the loser value is the same as the winner, keep trying to get a new name. (this is so it doesn't display something like Player00 killed Player00)
			while( loser == winner )
				loser = names[ Random.Range( 0, names.Count ) ];

			// Store the message as the winner name plus the weapon sprite emoji.
			string message = $"{winner} <sprite name={weaponName}>";

			// Randomize the head shot emoji.
			if( Random.Range( 0, 10 ) > 8 )
				message += "<sprite name=Headshot>";

			// Add the loser to the end of the string.
			message += $" {loser}";

			// Register the kill feed to the chat.
			chatBox.RegisterChat( message );
		}
	}
}