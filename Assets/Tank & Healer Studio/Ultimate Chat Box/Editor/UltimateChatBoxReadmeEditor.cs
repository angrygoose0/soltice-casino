/* UltimateChatBoxReadmeEditor.cs */
/* Written by Kaz */
namespace TankAndHealerStudioAssets
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections.Generic;

	[InitializeOnLoad]
	[CustomEditor( typeof( UltimateChatBoxReadme ) )]
	public class UltimateChatBoxReadmeEditor : Editor
	{
		static UltimateChatBoxReadme readme;

		// LAYOUT STYLES //
		public Texture2D productLogo;
		public Texture2D settings;
		const string linkColor = "0062ff";
		const string Indent = "    ";
		const int sectionSpace = 20;
		const int itemHeaderSpace = 10;
		const int paragraphSpace = 5;
		GUIStyle titleStyle = new GUIStyle();
		GUIStyle sectionHeaderStyle = new GUIStyle();
		GUIStyle itemHeaderStyle = new GUIStyle();
		GUIStyle paragraphStyle = new GUIStyle();
		GUIStyle versionStyle = new GUIStyle();

		// PAGE INFORMATION //
		class PageInformation
		{
			public string pageName = "";
			public delegate void TargetMethod ();
			public TargetMethod targetMethod;
		}
		static List<PageInformation> pageHistory = new List<PageInformation>();
		static PageInformation[] AllPages = new PageInformation[]
		{
		// MAIN MENU - 0 //
		new PageInformation()
		{
			pageName = "Product Manual"
		},
		// Getting Started - 1 //
		new PageInformation()
		{
			pageName = "Getting Started"
		},
		// Documentation - 2 //
		new PageInformation()
		{
			pageName = "Documentation"
		},
		// Version History - 3 //
		new PageInformation()
		{
			pageName = "Version History"
		},
		// Settings - 4 //
		new PageInformation()
		{
			pageName = "Settings"
		},
		// Thank You! - 5 //
		new PageInformation()
		{
			pageName = "Thank You!"
		},
		// Important Change - 6 //
		new PageInformation()
		{
			pageName = "Important Change"
		},
		};
		static int pageIndex_GettingStarted = 1, pageIndex_Documentation = 2, pageIndex_Versions = 3, pageIndex_Settings = 4, pageIndex_ThankYou = 5, pageIndex_ImportantChange = 6;

		// DOCUMENTATION //
		class DocumentationInfo
		{
			public string functionName = "";
			public bool showMore = false;
			public string[] parameter;
			public string returnType = "";
			public string description = "";
			public string codeExample = "";
		}
		DocumentationInfo[] UltimateChatBox_PublicFunctions = new DocumentationInfo[]
		{
		// RegisterChat
		new DocumentationInfo()
		{
			functionName = "RegisterChat()",
			parameter = new string[]
			{
				"string username - The username of the chat being registered.",
				"string message - The message to register.",
				"UltimateChatBox.ChatStyle style - The style for the chat to be displayed in.",
			},
			description = "Registers the chat information to the Ultimate Chat Box to be displayed.",
			codeExample = "chatBox.RegisterChat( \"PlayerName\", \"This will be the content of the message.\", UltimateChatBoxStyles.boldUsername );"
		},
		// Enable
		new DocumentationInfo()
		{
			functionName = "Enable()",
			description = "Enables the chat box.",
			codeExample = "chatBox.Enable();"
		},
		// Disable
		new DocumentationInfo()
		{
			functionName = "Disable()",
			description = "Disables the chat box.",
			codeExample = "chatBox.Disable();"
		},
		// ClearChat
		new DocumentationInfo()
		{
			functionName = "ClearChat()",
			description = "Clears the chat box of all entries.",
			codeExample = "chatBox.ClearChat();"
		},
		// EnableInputField
		new DocumentationInfo()
		{
			functionName = "EnableInputField()",
			parameter = new string[]
			{
				"string inputFieldValue - [OPTIONAL] This value will make the input field enable with a default value already in the chat box.",
			},
			description = "Enables the input field so that it can by typed in.",
			codeExample = "chatBox.EnableInputField( \"/\" );"
		},
		// ToggleInputField
		new DocumentationInfo()
		{
			functionName = "ToggleInputField()",
			description = "Toggles the state of the input field.",
			codeExample = "chatBox.ToggleInputField();"
		},
		// DisableInputField
		new DocumentationInfo()
		{
			functionName = "DisableInputField()",
			description = "Disables the input field associated with the Ultimate Chat Box.",
			codeExample = "chatBox.DisableInputField();"
		},
		// SendCustomInput
		new DocumentationInfo()
		{
			functionName = "SendCustomInput()",
			parameter = new string[]
			{
				"Vector2 inputPosition - The position of the input to process.",
				"bool getButtonDown - The state of the button being pressed down this frame.",
				"bool getButton - The state of the button being pressed.",
			},
			description = "Sends custom input to the chat box to process.",
			codeExample = "chatBox.SendCustomInput( Input.mousePosition, Input.GetButtonDown( \"Submit\" ), Input.GetButton( \"Submit\" ) );"
		},
		// FindChatsFromUser
		new DocumentationInfo()
		{
			functionName = "FindChatsFromUser()",
			returnType = "List<UltimateChatBox.ChatInformation>",
			parameter = new string[]
			{
				"string username - The username of the chats to find.",
			},
			description = "Returns a list of chat information for the targeted username.",
			codeExample = "List<UltimateChatBox.ChatInformation> chatsFromUser = chatBox.FindChatsFromUser( \"PlayerName\" );"
		},
		// FindChatsOfStyle
		new DocumentationInfo()
		{
			functionName = "FindChatsOfStyle()",
			returnType = "List<UltimateChatBox.ChatInformation>",
			parameter = new string[]
			{
				"UltimateChatBox.ChatStyle style - The style of the chats to find.",
			},
			description = "Returns a list of chat informations that were registered with the provided style.",
			codeExample = "List<UltimateChatBox.ChatInformation> greenChats = chatBox.FindChatsOfStyle( UltimateChatBoxStyles.greenUsername );"
		},
		// UpdatePositioning
		new DocumentationInfo()
		{
			functionName = "UpdatePositioning()",
			description = "[INTERNAL] Updates the positioning of the chat box on the screen.",
			codeExample = "chatBox.UpdatePositioning();"
		},
		};
		DocumentationInfo[] UltimateChatBox_PublicEvents = new DocumentationInfo[]
		{
		// OnChatRegistered
		new DocumentationInfo()
		{
			functionName = "OnChatRegistered",
			parameter = new string[]
			{
				"UltimateChatBox.ChatInformation - The information about the chat that was registered.",
			},
			description = "This callback is invoked any time a chat is registered to the Ultimate Chat Box.",
		},
		// OnUsernameHover
		new DocumentationInfo()
		{
			functionName = "OnUsernameHover",
			parameter = new string[]
			{
				"Vector2 - The input position over the username.",
				"UltimateChatBox.ChatInformation - The information about the chat that is being hovered.",
			},
			description = "This callback is invoked when a username has been hovered in the chat box. Only called when Interactable Usernames is enabled.",
		},
		// OnUsernameInteract
		new DocumentationInfo()
		{
			functionName = "OnUsernameInteract",
			parameter = new string[]
			{
				"UltimateChatBox.ChatInformation - The information about the chat that was registered.",
			},
			description = "This callback is invoked when a username has been interacted with in the chat box. Only called when Interactable Usernames is enabled.",
		},
		// OnInputFieldUpdated
		new DocumentationInfo()
		{
			functionName = "OnInputFieldUpdated",
			parameter = new string[]
			{
				"string - The string value of the input field.",
			},
			description = "This event is called when the input field of the chat box has been updated.",
		},
		// OnInputFieldSubmitted
		new DocumentationInfo()
		{
			functionName = "OnInputFieldSubmitted",
			parameter = new string[]
			{
				"string - The string value of the input field.",
			},
			description = "This event is called when the input field of the chat box has been submitted.",
		},
		// OnInputFieldCommandUpdated
		new DocumentationInfo()
		{
			functionName = "OnInputFieldCommandUpdated",
			parameter = new string[]
			{
				"string - The string value of the potential command.",
				"string - The string value of the message.",
			},
			description = "This event is called when the input field is updated and contains a potential command.",
		},
		// OnInputFieldCommandSubmitted
		new DocumentationInfo()
		{
			functionName = "OnInputFieldCommandSubmitted",
			parameter = new string[]
			{
				"string - The string value of the potential command.",
				"string - The string value of the message.",
			},
			description = "This event is called when the input field is submitted and contains a potential command.",
		},
		// OnExtraImageInteract
		new DocumentationInfo()
		{
			functionName = "OnExtraImageInteract",
			description = "This event is called when the extra image associated with the input field has been interacted with.",
		},
		};

		// END PAGE COMMENTS //
		class EndPageComment
		{
			public string comment = "";
			public string url = "";
		}
		EndPageComment[] endPageComments = new EndPageComment[]
		{
		new EndPageComment()
		{
			comment = $"Enjoying the Ultimate Chat Box? Leave us a review on the <b><color=#{linkColor}>Unity Asset Store</color></b>!",
			url = "https://assetstore.unity.com/packages/slug/260966"
		},
		new EndPageComment()
		{
			comment = $"Looking for a radial menu for your game? Check out the <b><color=#{linkColor}>Ultimate Radial Menu</color></b>!",
			url = "https://www.tankandhealerstudio.com/ultimate-radial-menu.html"
		},
		new EndPageComment()
		{
			comment = $"Looking for a health bar for your game? Check out the <b><color=#{linkColor}>Ultimate Status Bar</color></b>!",
			url = "https://www.tankandhealerstudio.com/ultimate-status-bar.html"
		},
		new EndPageComment()
		{
			comment = $"Check out our <b><color=#{linkColor}>other products</color></b>!",
			url = "https://www.tankandhealerstudio.com/assets.html"
		},
		};
		int randomComment = 0;


		static UltimateChatBoxReadmeEditor ()
		{
			EditorApplication.update += WaitForCompile;
		}

		static void WaitForCompile ()
		{
			if( EditorApplication.isCompiling )
				return;

			EditorApplication.update -= WaitForCompile;

			if( !EditorPrefs.HasKey( "UltimateChatBoxVersion" ) )
			{
				EditorPrefs.SetInt( "UltimateChatBoxVersion", UltimateChatBoxReadme.ImportantChange );
				SelectReadmeFile();

				if( readme != null )
					NavigateForward( pageIndex_ThankYou );
			}
			else if( EditorPrefs.GetInt( "UltimateChatBoxVersion" ) < UltimateChatBoxReadme.ImportantChange )
			{
				EditorPrefs.SetInt( "UltimateChatBoxVersion", UltimateChatBoxReadme.ImportantChange );
				SelectReadmeFile();

				if( readme != null )
					NavigateForward( pageIndex_ImportantChange );
			}
		}

		void OnEnable ()
		{
			readme = ( UltimateChatBoxReadme )target;

			if( !EditorPrefs.HasKey( "UCB_ColorHexSetup" ) )
			{
				EditorPrefs.SetBool( "UCB_ColorHexSetup", true );
				ResetColors();
			}

			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorDefaultHex" ), out readme.colorDefault );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorValueChangedHex" ), out readme.colorValueChanged );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorChatVisibleHex" ), out readme.colorChatVisible );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorChatInvisibleHex" ), out readme.colorChatInvisible );

			AllPages[ 0 ].targetMethod = MainPage;
			AllPages[ 1 ].targetMethod = GettingStarted;
			AllPages[ 2 ].targetMethod = Documentation;
			AllPages[ 3 ].targetMethod = VersionHistory;
			AllPages[ 4 ].targetMethod = Settings;
			AllPages[ 5 ].targetMethod = ThankYou;
			AllPages[ 6 ].targetMethod = ImportantChange;

			pageHistory = new List<PageInformation>();
			for( int i = 0; i < readme.pageHistory.Count; i++ )
				pageHistory.Add( AllPages[ readme.pageHistory[ i ] ] );

			if( !pageHistory.Contains( AllPages[ 0 ] ) )
			{
				pageHistory.Insert( 0, AllPages[ 0 ] );
				readme.pageHistory.Insert( 0, 0 );
			}

			randomComment = Random.Range( 0, endPageComments.Length );

			Undo.undoRedoPerformed += UndoRedoCallback;
		}

		void OnDisable ()
		{
			// Remove the UndoRedoCallback from the Undo event.
			Undo.undoRedoPerformed -= UndoRedoCallback;
		}

		void UndoRedoCallback ()
		{
			if( pageHistory[ pageHistory.Count - 1 ] != AllPages[ 9 ] )
				return;

			EditorPrefs.SetString( "UCB_ColorDefaultHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorDefault ) );
			EditorPrefs.SetString( "UCB_ColorValueChangedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorValueChanged ) );
			EditorPrefs.SetString( "UCB_ColorChatVisibleHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorChatVisible ) );
			EditorPrefs.SetString( "UCB_ColorChatInvisibleHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorChatInvisible ) );
		}

		protected override void OnHeaderGUI ()
		{
			UltimateChatBoxReadme readme = ( UltimateChatBoxReadme )target;

			var iconWidth = Mathf.Min( EditorGUIUtility.currentViewWidth, 350f );

			if( productLogo == null )
				return;

			Vector2 ratio = new Vector2( productLogo.width, productLogo.height ) / ( productLogo.width > productLogo.height ? productLogo.width : productLogo.height );

			GUILayout.BeginHorizontal( "In BigTitle" );
			{
				GUILayout.FlexibleSpace();
				GUILayout.BeginVertical();
				GUILayout.Label( productLogo, GUILayout.Width( iconWidth * ratio.x ), GUILayout.Height( iconWidth * ratio.y ) );
				GUILayout.Space( -20 );
				if( GUILayout.Button( readme.versionHistory[ 0 ].versionNumber, versionStyle ) && !pageHistory.Contains( AllPages[ pageIndex_Versions ] ) )
					NavigateForward( pageIndex_Versions );
				var rect = GUILayoutUtility.GetLastRect();
				if( pageHistory[ pageHistory.Count - 1 ] != AllPages[ pageIndex_Versions ] )
					EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
				GUILayout.EndVertical();
				GUILayout.FlexibleSpace();
			}
			GUILayout.EndHorizontal();
		}

		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			paragraphStyle = new GUIStyle( EditorStyles.label ) { wordWrap = true, richText = true, fontSize = 12 };
			itemHeaderStyle = new GUIStyle( paragraphStyle ) { fontSize = 12, fontStyle = FontStyle.Bold };
			sectionHeaderStyle = new GUIStyle( paragraphStyle ) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			titleStyle = new GUIStyle( paragraphStyle ) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			versionStyle = new GUIStyle( paragraphStyle ) { alignment = TextAnchor.MiddleCenter, fontSize = 10 };

			paragraphStyle.active.textColor = paragraphStyle.normal.textColor;

			// SETTINGS BUTTON //
			GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if( GUILayout.Button( settings, versionStyle, GUILayout.Width( 24 ), GUILayout.Height( 24 ) ) && !pageHistory.Contains( AllPages[ pageIndex_Settings ] ) )
				NavigateForward( pageIndex_Settings );
			var rect = GUILayoutUtility.GetLastRect();
			if( pageHistory[ pageHistory.Count - 1 ] != AllPages[ pageIndex_Settings ] )
				EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
			GUILayout.EndHorizontal();
			GUILayout.Space( -24 );
			GUILayout.EndVertical();

			// BACK BUTTON //
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup( pageHistory.Count <= 1 );
			if( GUILayout.Button( "◄", titleStyle, GUILayout.Width( 24 ) ) )
				NavigateBack();
			if( pageHistory.Count > 1 )
			{
				rect = GUILayoutUtility.GetLastRect();
				EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.Space( -24 );

			// PAGE TITLE //
			GUILayout.FlexibleSpace();
			EditorGUILayout.LabelField( pageHistory[ pageHistory.Count - 1 ].pageName, titleStyle );
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			// DISPLAY PAGE //
			pageHistory[ pageHistory.Count - 1 ].targetMethod?.Invoke();

			//Repaint();
		}

		void StartPage ()
		{
			readme.scrollValue = EditorGUILayout.BeginScrollView( readme.scrollValue, false, false );
			GUILayout.Space( 15 );
		}

		void EndPage ()
		{
			EditorGUILayout.EndScrollView();
		}

		static void NavigateBack ()
		{
			readme.pageHistory.RemoveAt( readme.pageHistory.Count - 1 );
			pageHistory.RemoveAt( pageHistory.Count - 1 );
			GUI.FocusControl( "" );

			readme.scrollValue = Vector2.zero;

			if( readme.pageHistory.Count == 1 )
				EditorUtility.SetDirty( readme );
		}

		static void NavigateForward ( int menuIndex )
		{
			pageHistory.Add( AllPages[ menuIndex ] );
			GUI.FocusControl( "" );

			readme.scrollValue = Vector2.zero;
			readme.pageHistory.Add( menuIndex );
		}

		void MainPage ()
		{
			StartPage();

			EditorGUILayout.LabelField( "We hope that you are enjoying using the Ultimate Chat Box in your project! Here is a list of helpful resources for this asset:", paragraphStyle );

			EditorGUILayout.Space();

			if( GUILayout.Button( $"  • Ready to begin? The <b><color=#{linkColor}>Getting Started</color></b> section has you covered!", paragraphStyle ) )
				NavigateForward( pageIndex_GettingStarted );
			var rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EditorGUILayout.Space();

			if( GUILayout.Button( $"  • Check out the <b><color=#{linkColor}>Documentation</color></b> section.", paragraphStyle ) )
				NavigateForward( pageIndex_Documentation );
			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EditorGUILayout.Space();

			if( GUILayout.Button( $"  • Join our <b><color=#{linkColor}>Discord Server</color></b> so that you can get live help from us and other community members.", paragraphStyle ) )
			{
				Debug.Log( "Ultimate Chat Box\nOpening Tank & Healer Studio Discord Server" );
				Application.OpenURL( "https://discord.gg/YrtEHRqw6y" );
			}
			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EditorGUILayout.Space();

			if( GUILayout.Button( $"  • <b><color=#{linkColor}>Contact Us</color></b> directly with any issue you might encounter! We'll try to help you out as much as we can.", paragraphStyle ) )
			{
				Debug.Log( "Ultimate Chat Box\nOpening Online Contact Form" );
				Application.OpenURL( "https://www.tankandhealerstudio.com/contact-us.html" );
			}
			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Now you have the tools you need to get the Ultimate Chat Box working in your project. Now get out there and make your awesome game!", paragraphStyle );

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Happy Game Making,\n" + Indent + "Tank & Healer Studio", paragraphStyle );

			GUILayout.Space( 20 );

			GUILayout.FlexibleSpace();

			if( GUILayout.Button( endPageComments[ randomComment ].comment, paragraphStyle ) )
				Application.OpenURL( endPageComments[ randomComment ].url );
			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EndPage();
		}

		void GettingStarted ()
		{
			StartPage();

			EditorGUILayout.BeginVertical( "Box" );
			if( GUILayout.Button( $"Check out the <b><color=#{linkColor}>Online Documentation</color></b> on our website.", paragraphStyle ) )
			{
				Debug.Log( "Ultimate Chat Box\nOpening Online Documentation" );
				Application.OpenURL( "https://docs.tankandhealerstudio.com/ultimatechatbox/" );
			}
			var rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
			EditorGUILayout.EndVertical();

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "Introduction", sectionHeaderStyle );

			EditorGUILayout.LabelField( Indent + "The Ultimate Chat Box is designed to be as simple and easy to use as possible. You will only need a few lines of code in order to get the Ultimate Chat Box working in your project.", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "To begin we'll look at how to simply create an Ultimate Chat Box in your scene. After that we will go over how to reference the Ultimate Chat Box in your custom scripts. Let's begin!", paragraphStyle );

			GUILayout.Space( sectionSpace );

			EditorGUILayout.LabelField( "How To Create", sectionHeaderStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( Indent + "To create an Ultimate Chat Box in your scene, simply find the Ultimate Chat Box prefab that you would like to add and drag the prefab into your scene. It should automatically set itself as a child of a UI Canvas, or create one if necessary.", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "Prefabs can be found at: <i>Assets/Tank & Healer Studio/Ultimate Chat Box/Prefabs</i>.", paragraphStyle );

			GUILayout.Space( sectionSpace );

			EditorGUILayout.LabelField( "How To Reference", sectionHeaderStyle );
			EditorGUILayout.LabelField( Indent + "The Ultimate Chat Box was created from ground up to be as simple to implement as possible. While there is a lot that can be done with the chat box, implementing it into your code is incredibly simple.", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "The first thing you will need to do is add the TankAndHealerStudioAssets namespace to your script so you can reference the Ultimate Chat Box.", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.TextArea( "using TankAndHealerStudioAssets;", GUI.skin.textArea );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "After this, you can add chat to the Ultimate Chat Box by calling the RegisterChat function along with the information about the chat. Then the Ultimate Chat Box will do the rest.", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.TextArea( "// SIMPLE EXAMPLE CODE //\nchatBox.RegisterChat( \"Username\", \"Message\" );", GUI.skin.textArea );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "<b>QUICK NOTE:</b> If you have the Input Field option enabled on the chat box, you can use the OnInputFieldSubmitted callback to be notified when the player has submitted chat through the input field. Then, inside your own code, you can send the chat to the server and then the Ultimate Chat Box if you would like.", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "Now you are ready to start adding chat into the Ultimate Chat Box!", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			if( GUILayout.Button( $"For a full list of the functions available, please see the <b><color=#{linkColor}>Documentation</color></b> section of this README.", paragraphStyle ) )
				NavigateForward( pageIndex_Documentation );

			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EndPage();
		}

		void Documentation ()
		{
			StartPage();

			EditorGUILayout.BeginVertical( "Box" );
			if( GUILayout.Button( $"Check out the <b><color=#{linkColor}>Online Documentation</color></b> on our website.", paragraphStyle ) )
			{
				Debug.Log( "Ultimate Chat Box\nOpening Online Documentation" );
				Application.OpenURL( "https://docs.tankandhealerstudio.com/ultimatechatbox/" );
			}
			var rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );
			EditorGUILayout.EndVertical();

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "UltimateChatBox Methods", sectionHeaderStyle );

			GUILayout.Space( paragraphSpace );

			for( int i = 0; i < UltimateChatBox_PublicFunctions.Length; i++ )
				ShowDocumentation( UltimateChatBox_PublicFunctions[ i ] );

			GUILayout.Space( sectionSpace );

			EditorGUILayout.LabelField( "UltimateChatBox Events", sectionHeaderStyle );

			GUILayout.Space( paragraphSpace );

			for( int i = 0; i < UltimateChatBox_PublicEvents.Length; i++ )
				ShowDocumentation( UltimateChatBox_PublicEvents[ i ] );

			EndPage();
		}

		void VersionHistory ()
		{
			StartPage();

			for( int i = 0; i < readme.versionHistory.Length; i++ )
			{
				EditorGUILayout.LabelField( "Version " + readme.versionHistory[ i ].versionNumber, itemHeaderStyle );

				for( int n = 0; n < readme.versionHistory[ i ].changes.Length; n++ )
					EditorGUILayout.LabelField( "• " + readme.versionHistory[ i ].changes[ n ] + ".", paragraphStyle );

				if( i < ( readme.versionHistory.Length - 1 ) )
					GUILayout.Space( itemHeaderSpace );
			}

			EndPage();
		}

		void ImportantChange ()
		{
			StartPage();

			EditorGUILayout.LabelField( "No major update yet.", paragraphStyle );

			var rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if( GUILayout.Button( "Main Menu", GUILayout.Width( Screen.width / 2 ) ) )
				NavigateBack();

			rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EndPage();
		}

		void ThankYou ()
		{
			StartPage();

			EditorGUILayout.LabelField( "The two of us at Tank & Healer Studio would like to thank you for purchasing the Ultimate Chat Box!", paragraphStyle );

			GUILayout.Space( paragraphSpace );

			EditorGUILayout.LabelField( "We hope that the chat box will be a great help to you in the development of your game. After clicking the <i>Continue</i> button below, you will be presented with information to assist you in getting to know the Ultimate Chat Box and getting it implemented into your project.", paragraphStyle );

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "You can access this information at any time by clicking on the <b>README</b> file inside the Ultimate Chat Box folder.", paragraphStyle );

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Again, thank you for downloading the Ultimate Chat Box. We hope that your project is a success!", paragraphStyle );

			EditorGUILayout.Space();

			EditorGUILayout.LabelField( "Happy Game Making,\n" + Indent + "Tank & Healer Studio", paragraphStyle );

			GUILayout.Space( 15 );

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if( GUILayout.Button( "Continue", GUILayout.Width( Screen.width / 2 ) ) )
				NavigateBack();

			var rect2 = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect2, MouseCursor.Link );

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EndPage();
		}

		void Settings ()
		{
			StartPage();

			EditorGUILayout.LabelField( "Gizmo Colors", sectionHeaderStyle );
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorDefault" ), new GUIContent( "Default" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				EditorPrefs.SetString( "UCB_ColorDefaultHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorDefault ) );
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorValueChanged" ), new GUIContent( "Value Changed" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				EditorPrefs.SetString( "UCB_ColorValueChangedHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorValueChanged ) );
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorChatVisible" ), new GUIContent( "Chat Visible (Play Mode Only)" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				EditorPrefs.SetString( "UCB_ColorChatVisibleHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorChatVisible ) );
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "colorChatInvisible" ), new GUIContent( "Chat Invisible (Play Mode Only)" ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();
				EditorPrefs.SetString( "UCB_ColorChatInvisibleHex", "#" + ColorUtility.ToHtmlStringRGBA( readme.colorChatInvisible ) );
			}

			EditorGUILayout.Space();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if( GUILayout.Button( "Reset", GUILayout.Width( EditorGUIUtility.currentViewWidth / 2 ) ) )
			{
				if( EditorUtility.DisplayDialog( "Reset Gizmo Color", "Are you sure that you want to reset the gizmo colors back to default?", "Yes", "No" ) )
				{
					ResetColors();
				}
			}
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();
			GUILayout.Toggle( EditorPrefs.GetBool( "UCB_DebugInputData" ), " Debug Input Data", EditorStyles.radioButton );
			if( EditorGUI.EndChangeCheck() )
				EditorPrefs.SetBool( "UCB_DebugInputData", !EditorPrefs.GetBool( "UCB_DebugInputData" ) );

			EditorGUILayout.Space();

			EditorGUI.BeginChangeCheck();
			GUILayout.Toggle( EditorPrefs.GetBool( "UUI_DisableDragAndDrop" ), " Disable Drag & Drop Opens Sections", EditorStyles.radioButton );
			if( EditorGUI.EndChangeCheck() )
				EditorPrefs.SetBool( "UUI_DisableDragAndDrop", !EditorPrefs.GetBool( "UUI_DisableDragAndDrop" ) );

			GUILayout.FlexibleSpace();

			GUILayout.Space( sectionSpace );

			EditorGUI.BeginChangeCheck();
			GUILayout.Toggle( EditorPrefs.GetBool( "UUI_DevelopmentMode" ), ( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) ? "Disable" : "Enable" ) + " Development Mode", EditorStyles.radioButton );
			if( EditorGUI.EndChangeCheck() )
			{
				if( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) == false )
				{
					if( EditorUtility.DisplayDialog( "Enable Development Mode", "Are you sure you want to enable development mode for Tank & Healer Studio assets? This mode will allow you to see the default inspector for this asset which is useful when adding variables to this script without having to edit the custom editor script.", "Enable", "Cancel" ) )
					{
						EditorPrefs.SetBool( "UUI_DevelopmentMode", !EditorPrefs.GetBool( "UUI_DevelopmentMode" ) );
					}
				}
				else
					EditorPrefs.SetBool( "UUI_DevelopmentMode", !EditorPrefs.GetBool( "UUI_DevelopmentMode" ) );
			}

			EndPage();
		}

		void ResetColors ()
		{
			serializedObject.FindProperty( "colorDefault" ).colorValue = Color.black;
			serializedObject.FindProperty( "colorValueChanged" ).colorValue = Color.yellow;
			serializedObject.FindProperty( "colorChatVisible" ).colorValue = Color.green;
			serializedObject.FindProperty( "colorChatInvisible" ).colorValue = Color.red;
			serializedObject.ApplyModifiedProperties();

			EditorPrefs.SetString( "UCB_ColorDefaultHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.black ) );
			EditorPrefs.SetString( "UCB_ColorValueChangedHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.yellow ) );
			EditorPrefs.SetString( "UCB_ColorChatVisibleHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.green ) );
			EditorPrefs.SetString( "UCB_ColorChatInvisibleHex", "#" + ColorUtility.ToHtmlStringRGBA( Color.red ) );
		}

		void ShowDocumentation ( DocumentationInfo info )
		{
			GUILayout.Space( paragraphSpace );

			if( GUILayout.Button( info.functionName, itemHeaderStyle ) )
			{
				info.showMore = !info.showMore;
				GUI.FocusControl( "" );
			}
			var rect = GUILayoutUtility.GetLastRect();
			EditorGUIUtility.AddCursorRect( rect, MouseCursor.Link );

			if( info.showMore )
			{
				EditorGUILayout.LabelField( Indent + "<i>Description:</i> " + info.description, paragraphStyle );

				if( info.parameter != null )
				{
					for( int i = 0; i < info.parameter.Length; i++ )
						EditorGUILayout.LabelField( Indent + "<i>Parameter:</i> " + info.parameter[ i ], paragraphStyle );
				}
				if( info.returnType != string.Empty )
					EditorGUILayout.LabelField( Indent + "<i>Return type:</i> " + info.returnType, paragraphStyle );

				if( info.codeExample != string.Empty )
					EditorGUILayout.TextArea( info.codeExample, GUI.skin.textArea );

				GUILayout.Space( paragraphSpace );
			}
		}


		[MenuItem( "Window/Tank and Healer Studio/Ultimate Chat Box", false, 6 )]
		public static void SelectReadmeFile ()
		{
			var ids = AssetDatabase.FindAssets( "README t:UltimateChatBoxReadme" );
			if( ids.Length == 1 )
			{
				var readmeObject = AssetDatabase.LoadMainAssetAtPath( AssetDatabase.GUIDToAssetPath( ids[ 0 ] ) );
				Selection.objects = new Object[] { readmeObject };
				readme = ( UltimateChatBoxReadme )readmeObject;
			}
			else
				Debug.LogError( "There is no README object in the Ultimate Chat Box folder." );
		}
	}
}