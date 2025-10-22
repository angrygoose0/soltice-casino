/* UltimateChatBoxReadme.cs */
/* Written by Kaz */
namespace TankAndHealerStudioAssets
{
	using UnityEngine;
	using System.Collections.Generic;

	//[CreateAssetMenu( fileName = "README", menuName = "Tank and Healer Studio/Ultimate Radial Chat Box README File", order = 1 )]
	public class UltimateChatBoxReadme : ScriptableObject
	{
		// GIZMO COLORS //
		[HideInInspector]
		public Color colorDefault = Color.black;
		[HideInInspector]
		public Color colorValueChanged = Color.yellow;
		[HideInInspector]
		public Color colorChatVisible = Color.green;
		[HideInInspector]
		public Color colorChatInvisible = Color.red;

		public static int ImportantChange = 0;
		public class VersionHistory
		{
			public string versionNumber = "";
			public string[] changes;
		}
		public VersionHistory[] versionHistory = new VersionHistory[]
		{
			// VERSION 1.0.2
			new VersionHistory ()
			{
				versionNumber = "1.0.2",
				changes = new string[]
				{
					// QUALITY OF LIFE //
					"Modified some of the minimum values that can be set in the inspector to avoid errors with zero calculations",
					"Added the ability to edit the input field value through code using the InputFieldValue property",
					"Added two new callbacks for the input field: OnInputFieldEnabled and OnInputFieldDisabled",
					"Improved the OnInputFieldCommandUpdated callback",
					// BUG FIXES //
					"Fixed warnings in Unity 2022+ dealing with obsolete FindObjectOfType references",
					"Fixed an error in Unity 2023+ that was caused by TMPro changing the name of some their properties",
					"Fixed a bug that could happen in certain versions of Unity when using the Allow Touch Input option along with the Simulated Touchscreen",
					"Fixed a situation where the touch input would not interact with the emoji window properly",
					"Fixed an issue in Unity 6 with TMPro selecting text in the Input Field automatically when enabling the input field with a string value",
				},
			},
			// VERSION 1.0.1
			new VersionHistory ()
			{
				versionNumber = "1.0.1",
				changes = new string[]
				{
					"Added local Getting Started and Documentation pages to the README file",
				},
			},
			// VERSION 1.0.0
			new VersionHistory ()
			{
				versionNumber = "1.0.0",
				changes = new string[]
				{
					"Initial Release",
				},
			},
		};

		[HideInInspector]
		public List<int> pageHistory = new List<int>();
		[HideInInspector]
		public Vector2 scrollValue = new Vector2();
	}
}