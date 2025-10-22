namespace TankAndHealerStudioAssets
{
	using TMPro;
	using UnityEngine;
	using UnityEditor;
	using UnityEngine.UI;
	using UnityEngine.EventSystems;
	using System.Collections.Generic;

	[CustomEditor( typeof( UltimateChatBox ) )]
	public class UltimateChatBoxEditor : Editor
	{
		// COMMON SETTINGS //
		UltimateChatBox targ;
		bool showDefaultInspector = false;
		RectTransform parentCanvasTransform;

		// BASIC SETTINGS //
		Image chatBoxImage;
		Color chatBoxImageColor = Color.white;
		Sprite chatBoxImageSprite = null;

		// TEXT OBJECT //
		TextMeshProUGUI textObject;
		float textFontSize = 0.0f;
		TMP_FontAsset textFont;

		// INTERACTABLE USERNAME //
		Image interactableUsernameImage;
		Sprite usernameHighlightSprite;

		// TEXT EMOJI //
		float emojiAssetHorizontalBearing = 0.0f, emojiAssetVerticalBearing = 0.0f;

		// SCROLLBAR //
		Image scrollbarBaseImage, scrollbarHandleImage, navigationArrowImageUp, navigationArrowImageDown;
		Sprite scrollbarBaseSprite, scrollbarHandleSprite, scrollbarNavigationArrowSprite;
		Color scrollbarBaseColor;

		// INPUT FIELD //
		Image inputFieldImage;
		Sprite inputFieldImageSprite = null;
		Color inputFieldImageColor = Color.white;
		TextMeshProUGUI inputFieldPlaceholderText;
		string inputFieldPlaceholderTextValue = "Enter text...";
		Color inputFieldTextColor = Color.white, inputFieldPlaceholderTextColor = Color.white;
		float inputFieldTextSize = 0.0f;
		TMP_FontAsset inputFieldFont;

		// EXTRA IMAGE //
		Image extraImage;
		Sprite extraImageSprite;
		Color extraImageColor = Color.white;

		// EMOJI WINDOW //
		Image emojiButtonImage, emojiWindowImage;
		Sprite emojiButtonSprite, emojiWindowSprite;
		Color emojiButtonColor = Color.white, emojiWindowColor = Color.white;
		TextMeshProUGUI emojiText;
		float textEdgePadding = 0.0f, characterSpacing = 0.0f, lineSpacing = 0.0f;

		// SCENE GUI //
		class DisplaySceneGizmo
		{
			public int frames = maxFrames;
			public bool hover = false;

			public bool HighlightGizmo
			{
				get
				{
					return hover || frames < maxFrames;
				}
			}

			public void PropertyUpdated ()
			{
				frames = 0;
			}
		}
		DisplaySceneGizmo DisplayChatBoxPosition = new DisplaySceneGizmo();
		DisplaySceneGizmo DisplayBoundingBox = new DisplaySceneGizmo();
		DisplaySceneGizmo DisplayInputFieldTextArea = new DisplaySceneGizmo();
		const int maxFrames = 200;
		static bool isDirty = false;
		bool wasDirtyLastFrame = false;
		Rect propertyRect = new Rect();
		// Gizmo Colors //
		Color colorDefault = Color.black, colorValueChanged = Color.cyan;
		Color colorInputInRange = Color.green, colorInputOutOfRange = Color.white;
		Color colorChatVisible = Color.cyan, colorChatInvisible = Color.red;

		// EDITOR STYLES //
		GUIStyle collapsableSectionStyle = new GUIStyle();
		GUIStyle textFieldStyle = new GUIStyle();
		GUIStyle helpBoxStyle = new GUIStyle();

		// MISC //
		bool isInProjectWindow = false;
		bool disableInteraction = false;
		int chatsToAdd = 5;
		class RandomChat
		{
			public string username;
			public string message;
			public UltimateChatBox.ChatStyle style;
		}
		RandomChat[] randomChats = new RandomChat[]
		{
		new RandomChat()
		{
			username = "",
			message = "You gained <color=green>[{0}]</color> experience.",
			style = UltimateChatBoxStyles.none,
		},
		new RandomChat()
		{
			username = "",
			message = "You acquired <color=yellow>[{0}]</color> gold.",
			style = UltimateChatBoxStyles.none,
		},
		new RandomChat()
		{
			username = "PlayerName",
			message = "This is a random message to simulate entries of chat so you can see what messages will look like as the chat box fills up.",
			style = UltimateChatBoxStyles.blueUsername,
		},
		new RandomChat()
		{
			username = "",
			message = "PlayerName dances joyfully.",
			style = UltimateChatBoxStyles.none,
		},
		new RandomChat()
		{
			username = "",
			message = "Game Notification",
			style = UltimateChatBoxStyles.none,
		},
		new RandomChat()
		{
			username = "[SYSTEM]",
			message = "Important system message.",
			style = UltimateChatBoxStyles.errorUsername,
		},
		new RandomChat()
		{
			username = "PlayerName",
			message = "I am whispering this message to you.",
			style = UltimateChatBoxStyles.whisperUsername,
		},
		};

		// DRAG AND DROP //
		bool disableDragAndDrop = false;
		bool isDraggingObject = false;
		Vector2 dragAndDropMousePos = Vector2.zero;
		double dragAndDropStartTime = 0.0f;
		double dragAndDropCurrentTime = 0.0f;


		private void OnEnable ()
		{
			Undo.undoRedoPerformed += StoreReferences;

			StoreReferences();
		}

		private void OnDisable ()
		{
			Undo.undoRedoPerformed -= StoreReferences;
		}

		// --------------------------< INSPECTOR OVERRIDE >---------------------------- //
		public override void OnInspectorGUI ()
		{
			serializedObject.Update();

			if( targ == null )
				return;

			collapsableSectionStyle = new GUIStyle( EditorStyles.label ) { richText = true, alignment = TextAnchor.MiddleCenter, onActive = new GUIStyleState() { textColor = Color.black } };
			collapsableSectionStyle.active.textColor = collapsableSectionStyle.normal.textColor;

			textFieldStyle = new GUIStyle( GUI.skin.textField ) { wordWrap = true };

			helpBoxStyle = new GUIStyle( EditorStyles.label ) { richText = true, wordWrap = true };

			// DEVELOPMENT INSPECTOR //
			if( EditorPrefs.GetBool( "UUI_DevelopmentMode" ) )
			{
				EditorGUILayout.Space();
				GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11, richText = true };
				GUILayout.BeginHorizontal();
				GUILayout.Space( -10 );
				showDefaultInspector = GUILayout.Toggle( showDefaultInspector, ( showDefaultInspector ? "▼" : "►" ) + "<color=#ff0000ff> Development Inspector</color>", toolbarStyle );
				GUILayout.EndHorizontal();

				if( showDefaultInspector )
				{
					EditorGUILayout.Space();

					base.OnInspectorGUI();

					EditorGUILayout.LabelField( "End of Development Inspector", EditorStyles.centeredGreyMiniLabel );
					EditorGUILayout.Space();
					return;
				}
				else if( DragAndDropHover() )
					showDefaultInspector = true;

				EditorGUILayout.Space();
			}
			// END DEVELOPMENT INSPECTOR //

			disableInteraction = false;
			if( targ.ParentCanvas != null && targ.ParentCanvas.renderMode != RenderMode.ScreenSpaceOverlay )
				disableInteraction = true;

			bool valueChanged = false;

			if( Application.isPlaying )
			{
				EditorGUILayout.BeginVertical( "Box" );

				EditorGUILayout.LabelField( "Quick Debug Options", EditorStyles.boldLabel );

				EditorGUILayout.BeginHorizontal();
				if( GUILayout.Button( "Register Random Chats" ) )
				{
					for( int i = 0; i < chatsToAdd; i++ )
					{
						int randomIndex = Random.Range( 0, randomChats.Length );
						targ.RegisterChat( randomChats[ randomIndex ].username, string.Format( randomChats[ randomIndex ].message, Random.Range( 0.0f, 100.0f ).ToString( "00" ) ), randomChats[ randomIndex ].style );
					}
				}

				chatsToAdd = EditorGUILayout.IntField( chatsToAdd, GUILayout.Width( 25 ) );

				EditorGUILayout.EndHorizontal();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Toggle( "Debug Input Data", EditorPrefs.GetBool( "UCB_DebugInputData" ) );
				if( EditorGUI.EndChangeCheck() )
					EditorPrefs.SetBool( "UCB_DebugInputData", !EditorPrefs.GetBool( "UCB_DebugInputData" ) );

				EditorGUILayout.Space();

				EditorGUILayout.LabelField( $"Registered Chats: {targ.ChatInformations.Count} / {serializedObject.FindProperty( "maxTextInChatBox" ).intValue.ToString()}" );

				EditorGUI.BeginDisabledGroup( targ.ChatInformations.Count == 0 );
				if( GUILayout.Button( "Clear Chat" ) )
					targ.ClearChat();
				EditorGUI.EndDisabledGroup();

				EditorGUILayout.EndVertical();
			}

			if( isInProjectWindow )
				EditorGUILayout.HelpBox( "Prefab editing is limited in the Project Window. Please double click the prefab to open up the edit scene, or drag the chat box in to the scene to edit it.", MessageType.Warning );

			/* --------------------------------------------------------------- <<< CHAT BOX SETTINGS >>> --------------------------------------------------------------- */
			EditorGUILayout.LabelField( "Chat Box Settings", EditorStyles.boldLabel );

			EditorGUI.BeginChangeCheck();
			chatBoxImageSprite = ( Sprite )EditorGUILayout.ObjectField( "Background Sprite", chatBoxImageSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
			if( EditorGUI.EndChangeCheck() && chatBoxImage != null )
			{
				Undo.RecordObject( chatBoxImage, "Update Chat Box Image Sprite" );
				chatBoxImage.enabled = false;
				chatBoxImage.sprite = chatBoxImageSprite;
				chatBoxImage.enabled = true;

				if( chatBoxImageSprite != null && chatBoxImageSprite.border != Vector4.zero )
					chatBoxImage.type = Image.Type.Sliced;
				else
					chatBoxImage.type = Image.Type.Simple;
			}

			EditorGUI.BeginChangeCheck();
			chatBoxImageColor = EditorGUILayout.ColorField( "Background Color", chatBoxImageColor );
			if( EditorGUI.EndChangeCheck() && chatBoxImage != null )
			{
				Undo.RecordObject( chatBoxImage, "Update Chat Box Image Color" );
				chatBoxImage.enabled = false;
				chatBoxImage.color = chatBoxImageColor;
				chatBoxImage.enabled = true;
			}

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField( serializedObject.FindProperty( "allowTouchInput" ) );
			if( EditorGUI.EndChangeCheck() )
				serializedObject.ApplyModifiedProperties();

			// CHAT BOX POSITIONING //
			if( DisplayCollapsibleBoxSection( "Chat Box Position", "UCB_ChatBoxPosition" ) )
			{
				// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
				if( Application.isPlaying )
				{
					EditorGUILayout.HelpBox( "The application is running. Changes made here will not be kept.", MessageType.Warning );
					EditorGUI.BeginChangeCheck();
				}
				// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "chatBoxSizeRatio.x" ), 0.1f, 1.0f, new GUIContent( "Horizontal Ratio", "The horizontal ratio of the chat box." ) );
				EditorGUILayout.Slider( serializedObject.FindProperty( "chatBoxSizeRatio.y" ), 0.1f, 1.0f, new GUIContent( "Vertical Ratio", "The vertical ratio of the chat box." ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "chatBoxSize" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					if( serializedObject.FindProperty( "chatBoxSize" ).floatValue < 0.1f )
						serializedObject.FindProperty( "chatBoxSize" ).floatValue = 0.1f;

					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "chatBoxPosition.x" ), 0.0f, 100.0f, new GUIContent( "Horizontal Position", "The horizontal position of the chat box on the screen." ) );
				BeginPreviousPropertyHoverCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "chatBoxPosition.y" ), 0.0f, 100.0f, new GUIContent( "Vertical Position", "The vertical position of the chat box on the screen." ) );
				EndPreviousPropertyHoverCheck( DisplayChatBoxPosition );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					DisplayChatBoxPosition.PropertyUpdated();
				}

				EditorGUILayout.Space();
				EditorGUILayout.LabelField( "Content Position", EditorStyles.boldLabel );
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "horizontalSpacing" ) );
				BeginPreviousPropertyHoverCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "verticalSpacing" ) );
				EndPreviousPropertyHoverCheck( DisplayBoundingBox );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					DisplayBoundingBox.PropertyUpdated();
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.Slider( serializedObject.FindProperty( "contentPosition.x" ), -50, 50, new GUIContent( "Horizontal Position", "The horizontal position of the content within the chat box." ) );
				EditorGUILayout.Slider( serializedObject.FindProperty( "contentPosition.y" ), -50, 50, new GUIContent( "Vertical Position", "The vertical position of the content within the chat box" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				GUILayout.Space( 1 );

				// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
				if( Application.isPlaying )
				{
					if( EditorGUI.EndChangeCheck() )
					{
						if( EditorWindow.focusedWindow != null )
							EditorWindow.focusedWindow.ShowNotification( new GUIContent( "The application is running. Changes made here will not be kept." ) );
						targ.UpdatePositioning();
					}
				}
				// CHANGE CHECK FOR APPLYING SETTINGS DURING RUNTIME //
			}
			EndSection( "UCB_ChatBoxPosition" );
			// END CHAT BOX POSITIONING //

			// TEXT SETTINGS //
			if( DisplayCollapsibleBoxSection( "Text Settings", "UCB_TextSettings", targ.TextObject == null ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "textColor" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					Undo.RecordObject( textObject, "Update Text Color" );
					textObject.color = serializedObject.FindProperty( "textColor" ).colorValue;
				}

				EditorGUI.BeginChangeCheck();
				textFont = ( TMP_FontAsset )EditorGUILayout.ObjectField( "Text Font", textFont, typeof( TMP_FontAsset ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					Undo.RecordObject( textObject, "Update Text Font" );
					textObject.font = textFont;
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "maxTextInChatBox" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "disableRichTextFromPlayers" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "usernameFollowup" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "spaceBetweenChats" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "smartFontSize" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					// If the smart font size is zero, then change to a set font size setting for them.
					if( serializedObject.FindProperty( "smartFontSize" ).floatValue == 0.0f )
					{
						textFontSize = 24;
						Undo.RecordObject( textObject, "Update Font Size" );
						textObject.fontSize = textFontSize;
					}
				}

				if( serializedObject.FindProperty( "smartFontSize" ).floatValue <= 0.0f )
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					textFontSize = EditorGUILayout.FloatField( new GUIContent( "Font Size", "The font size to apply to the chat box text. This font size will not display the same on all screen sizes like the Smart Font Size does." ), textFontSize );
					if( EditorGUI.EndChangeCheck() )
					{
						if( textFontSize < 1 )
							textFontSize = 1;

						Undo.RecordObject( textObject, "Update Font Size" );
						textObject.fontSize = textFontSize;
					}
					EditorGUI.indentLevel--;
					GUILayoutAfterIndentSpace();
				}
			}
			EndSection( "UCB_TextSettings" );
			// END TEXT SETTINGS //

			EditorGUI.BeginDisabledGroup( targ.TextObject == null );

			// TEXT EMOJI //
			if( DisplayCollapsibleBoxSection( "Text Emoji", "UCB_TextEmoji", serializedObject.FindProperty( "useTextEmoji" ), ref valueChanged, serializedObject.FindProperty( "useTextEmoji" ).boolValue && serializedObject.FindProperty( "emojiAsset" ).objectReferenceValue == null ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiAsset" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();

					if( serializedObject.FindProperty( "useTextEmoji" ).boolValue && targ.EmojiAsset != null )
					{
						Undo.RecordObject( targ.TextObject, "Update Sprite Asset" );
						targ.TextObject.spriteAsset = targ.EmojiAsset;
					}

					if( targ.EmojiAsset != null && targ.EmojiAsset.spriteCharacterTable.Count > 0 )
					{
						emojiAssetHorizontalBearing = targ.EmojiAsset.spriteCharacterTable[ 0 ].glyph.metrics.horizontalBearingX;
						emojiAssetVerticalBearing = targ.EmojiAsset.spriteCharacterTable[ 0 ].glyph.metrics.horizontalBearingY;
					}

					UpdateEmojiWindowText();
				}

				EditorGUI.BeginDisabledGroup( targ.EmojiAsset == null || targ.EmojiAsset.spriteCharacterTable.Count == 0 );

				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				emojiAssetHorizontalBearing = EditorGUILayout.FloatField( new GUIContent( "Horizontal Bearing" ), emojiAssetHorizontalBearing );
				emojiAssetVerticalBearing = EditorGUILayout.FloatField( new GUIContent( "Vertical Bearing" ), emojiAssetVerticalBearing );
				if( EditorGUI.EndChangeCheck() )
				{
					Undo.RecordObject( targ.EmojiAsset, "Update Glyph Data" );
					for( int i = 0; i < targ.EmojiAsset.spriteCharacterTable.Count; i++ )
					{
						UnityEngine.TextCore.GlyphMetrics metric = targ.EmojiAsset.spriteCharacterTable[ i ].glyph.metrics;
						metric.horizontalBearingX = emojiAssetHorizontalBearing;
						metric.horizontalBearingY = emojiAssetVerticalBearing;
						targ.EmojiAsset.spriteCharacterTable[ i ].glyph.metrics = metric;
					}
					EditorUtility.SetDirty( targ.EmojiAsset );

					if( serializedObject.FindProperty( "useEmojiWindow" ).boolValue && emojiText != null )
						emojiText.ForceMeshUpdate();
				}
				EditorGUI.indentLevel--;
				GUILayoutAfterIndentSpace();
				EditorGUI.EndDisabledGroup();
			}
			EndSection( "UCB_TextEmoji" );
			if( valueChanged )
			{
				if( serializedObject.FindProperty( "useTextEmoji" ).boolValue && targ.EmojiAsset != null )
				{
					Undo.RecordObject( targ.TextObject, "Update Sprite Asset" );
					targ.TextObject.spriteAsset = targ.EmojiAsset;

					if( targ.EmojiAsset.spriteCharacterTable.Count > 0 )
					{
						emojiAssetHorizontalBearing = targ.EmojiAsset.spriteCharacterTable[ 0 ].glyph.metrics.horizontalBearingX;
						emojiAssetVerticalBearing = targ.EmojiAsset.spriteCharacterTable[ 0 ].glyph.metrics.horizontalBearingY;
					}
				}

				UpdateEmojiWindowText();
			}
			// END TEXT EMOJI //

			EditorGUI.EndDisabledGroup();

			BeginDisableInteractionCheck();
			// INTERACTABLE USERNAMES //
			if( DisplayCollapsibleBoxSection( "Interactable Usernames", "UCB_InteractableUsernames", serializedObject.FindProperty( "useInteractableUsername" ), ref valueChanged, serializedObject.FindProperty( "useInteractableUsername" ).boolValue && interactableUsernameImage == null ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "interactableUsernameImage" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					UpdateUsernameHighlightReferences();

					if( interactableUsernameImage != null )
					{
						serializedObject.FindProperty( "interactableUsernameColor" ).colorValue = interactableUsernameImage.color;
						serializedObject.ApplyModifiedProperties();
					}
				}

				EditorGUI.BeginChangeCheck();
				usernameHighlightSprite = ( Sprite )EditorGUILayout.ObjectField( "Image Sprite", usernameHighlightSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && interactableUsernameImage != null )
				{
					Undo.RecordObject( interactableUsernameImage, "Update Username Highlight Sprite" );
					interactableUsernameImage.enabled = false;
					interactableUsernameImage.sprite = usernameHighlightSprite;
					interactableUsernameImage.enabled = true;

					if( usernameHighlightSprite != null && usernameHighlightSprite.border != Vector4.zero )
						interactableUsernameImage.type = Image.Type.Sliced;
					else
						interactableUsernameImage.type = Image.Type.Simple;
				}

				if( serializedObject.FindProperty( "interactableUsernameImage" ).objectReferenceValue == null && !isInProjectWindow )
				{
					EditorGUILayout.BeginVertical( "Box" );
					EditorGUILayout.HelpBox( "No Username Highlight Image. Please either assign an Image object to use, or click the Generate button below.", MessageType.Error );
					if( GUILayout.Button( "Generate Username Highlight Image", EditorStyles.miniButton ) )
					{
						GameObject usernameHighlightObject = new GameObject( "Username Highlight", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
						RectTransform usernameHighlightRect = usernameHighlightObject.GetComponent<RectTransform>();

						usernameHighlightObject.transform.SetParent( targ.ChatContentBox.transform );
						usernameHighlightObject.transform.SetAsFirstSibling();
						usernameHighlightRect.anchorMin = new Vector2( 0.5f, 1.0f );
						usernameHighlightRect.anchorMax = new Vector2( 0.5f, 1.0f );
						usernameHighlightRect.pivot = new Vector2( 0.5f, 0.5f );
						usernameHighlightRect.localScale = Vector3.one;

						interactableUsernameImage = usernameHighlightObject.GetComponent<Image>();
						interactableUsernameImage.sprite = usernameHighlightSprite;
						interactableUsernameImage.color = serializedObject.FindProperty( "interactableUsernameColor" ).colorValue;

						if( usernameHighlightSprite != null && usernameHighlightSprite.border != Vector4.zero )
							interactableUsernameImage.type = Image.Type.Sliced;
						else
							interactableUsernameImage.type = Image.Type.Simple;

						serializedObject.FindProperty( "interactableUsernameImage" ).objectReferenceValue = interactableUsernameImage;
						serializedObject.ApplyModifiedProperties();

						Undo.RegisterCreatedObjectUndo( usernameHighlightObject, "Create Username Highlight Object" );

						UpdateUsernameHighlightReferences();
					}
					EditorGUILayout.EndVertical();
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "interactableUsernameColor" ), new GUIContent( "Highlight Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						Undo.RecordObject( interactableUsernameImage, "Update Username Highlight Color" );
						interactableUsernameImage.enabled = false;
						interactableUsernameImage.color = serializedObject.FindProperty( "interactableUsernameColor" ).colorValue;
						interactableUsernameImage.enabled = true;
					}

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "interactableUsernameWidthModifier" ), new GUIContent( "Width Modifier" ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();
				}
			}
			EndSection( "UCB_InteractableUsernames" );
			if( ( valueChanged || disableInteraction && serializedObject.FindProperty( "useInteractableUsername" ).boolValue ) && interactableUsernameImage != null )
			{
				if( disableInteraction )
				{
					serializedObject.FindProperty( "useInteractableUsername" ).boolValue = false;
					serializedObject.ApplyModifiedProperties();
				}

				Undo.RecordObject( interactableUsernameImage.gameObject, ( serializedObject.FindProperty( "useInteractableUsername" ).boolValue ? "Enable" : "Disable" ) + " Username Highlight Image" );
				interactableUsernameImage.gameObject.SetActive( serializedObject.FindProperty( "useInteractableUsername" ).boolValue );
			}
			// END INTERACTABLE USERNAMES //
			EndDisableInteractionCheck();

			// FADE WHEN DISABLED //
			if( DisplayCollapsibleBoxSection( "Fade When Disabled", "UCB_FadeWhenDisabled", serializedObject.FindProperty( "fadeWhenDisabled" ), ref valueChanged ) )
			{
				float fadeInDuration = serializedObject.FindProperty( "fadeInSpeed" ).floatValue == 0.0f ? 0.0f : 1.0f / serializedObject.FindProperty( "fadeInSpeed" ).floatValue;
				EditorGUI.BeginChangeCheck();
				fadeInDuration = EditorGUILayout.FloatField( new GUIContent( "Fade In Duration", "The time is seconds that the chat box will transition from disabled to enabled." ), fadeInDuration );
				if( EditorGUI.EndChangeCheck() )
				{
					if( fadeInDuration < 0.0f )
						fadeInDuration = 0.0f;

					serializedObject.FindProperty( "fadeInSpeed" ).floatValue = fadeInDuration == 0.0f ? 0.0f : 1.0f / fadeInDuration;
					serializedObject.ApplyModifiedProperties();
				}

				float fadeOutDuration = serializedObject.FindProperty( "fadeOutSpeed" ).floatValue == 0.0f ? 0.0f : 1.0f / serializedObject.FindProperty( "fadeOutSpeed" ).floatValue;
				EditorGUI.BeginChangeCheck();
				fadeOutDuration = EditorGUILayout.FloatField( new GUIContent( "Fade Out Duration", "The time is seconds that the chat box will transition from disabled to enabled." ), fadeOutDuration );
				if( EditorGUI.EndChangeCheck() )
				{
					if( fadeOutDuration < 0.0f )
						fadeOutDuration = 0.0f;

					serializedObject.FindProperty( "fadeOutSpeed" ).floatValue = fadeOutDuration == 0.0f ? 0.0f : 1.0f / fadeOutDuration;
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "toggledAlpha" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();

				if( !targ.VisibleChatBoundingBox.GetComponent<CanvasGroup>() )
					Undo.AddComponent( targ.VisibleChatBoundingBox.gameObject, typeof( CanvasGroup ) );

				bool leaveTextVisible = targ.LeaveTextVisible;
				EditorGUI.BeginChangeCheck();
				leaveTextVisible = EditorGUILayout.Toggle( new GUIContent( "Leave Text Visible" ), leaveTextVisible );
				if( EditorGUI.EndChangeCheck() )
				{
					Undo.RecordObject( targ.VisibleChatBoundingBox.GetComponent<CanvasGroup>(), "Update Leave Text Visible Option" );
					targ.VisibleChatBoundingBox.GetComponent<CanvasGroup>().ignoreParentGroups = leaveTextVisible;
				}
			}
			EndSection( "UCB_FadeWhenDisabled" );
			// END FADE WHEN DISABLED //

			// COLLAPSE WHEN DISABLED //
			if( DisplayCollapsibleBoxSection( "Collapse When Disabled", "UCB_CollapseWhenDisabled", serializedObject.FindProperty( "collapseWhenDisabled" ), ref valueChanged ) )
			{
				float expandDuration = serializedObject.FindProperty( "expandSpeed" ).floatValue == 0.0f ? 0.0f : 1.0f / serializedObject.FindProperty( "expandSpeed" ).floatValue;
				EditorGUI.BeginChangeCheck();
				expandDuration = EditorGUILayout.FloatField( new GUIContent( "Expand Duration", "The time is seconds that the chat box will expand from a collapsed state." ), expandDuration );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.FindProperty( "expandSpeed" ).floatValue = expandDuration == 0.0f ? 0.0f : 1.0f / expandDuration;
					serializedObject.ApplyModifiedProperties();
				}

				float collapseDuration = serializedObject.FindProperty( "collapseSpeed" ).floatValue == 0.0f ? 0.0f : 1.0f / serializedObject.FindProperty( "collapseSpeed" ).floatValue;
				EditorGUI.BeginChangeCheck();
				collapseDuration = EditorGUILayout.FloatField( new GUIContent( "Collapse Duration", "The time is seconds that the chat box will transition from disabled to enabled." ), collapseDuration );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.FindProperty( "collapseSpeed" ).floatValue = collapseDuration == 0.0f ? 0.0f : 1.0f / collapseDuration;
					serializedObject.ApplyModifiedProperties();
				}

				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "visibleLineCount" ) );
				if( EditorGUI.EndChangeCheck() )
					serializedObject.ApplyModifiedProperties();
			}
			EndSection( "UCB_CollapseWhenDisabled" );
			if( valueChanged )
			{
				// My dear wife found this very obscure error when trying to enable the Collapse option AT RUNTIME FOR NO REASON!!! Forcing a update positioning to avoid a zero calculation error.
				if( Application.isPlaying )
					targ.UpdatePositioning();
			}
			// END COLLAPSE WHEN DISABLED //
			/* ------------------------------------------------------------- <<< END CHAT BOX SETTINGS >>> ------------------------------------------------------------- */

			/* -------------------------------------------------------------- <<< NAVIGATION SETTINGS >>> -------------------------------------------------------------- */
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Navigation Settings", EditorStyles.boldLabel );
			BeginDisableInteractionCheck();

			// VERTICAL SCROLLBAR //
			bool scrollbarError = serializedObject.FindProperty( "useScrollbar" ).boolValue && ( scrollbarBaseImage == null || scrollbarHandleImage == null );
			if( DisplayCollapsibleBoxSection( "Vertical Scrollbar", "UCB_VerticalScrollbar", serializedObject.FindProperty( "useScrollbar" ), ref valueChanged, scrollbarError ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarBase" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					UpdateScrollbarReferences();
				}

				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				scrollbarBaseSprite = ( Sprite )EditorGUILayout.ObjectField( "Base Sprite", scrollbarBaseSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && scrollbarBaseImage != null )
				{
					Undo.RecordObject( scrollbarBaseImage, "Update Scrollbar Base Sprite" );
					scrollbarBaseImage.enabled = false;
					scrollbarBaseImage.sprite = scrollbarBaseSprite;
					scrollbarBaseImage.enabled = true;

					if( scrollbarBaseSprite != null && scrollbarBaseSprite.border != Vector4.zero )
						scrollbarBaseImage.type = Image.Type.Sliced;
					else
						scrollbarBaseImage.type = Image.Type.Simple;
				}
				EditorGUI.indentLevel--;

				if( serializedObject.FindProperty( "scrollbarBase" ).objectReferenceValue == null && !isInProjectWindow )
				{
					EditorGUILayout.BeginVertical( "Box" );
					if( GUILayout.Button( "Generate Scrollbar", EditorStyles.miniButton ) )
					{
						GameObject scrollbarRectObject = new GameObject( "Scrollbar Base", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( UnityEngine.UI.Image ), typeof( CanvasGroup ) );
						Undo.RegisterCreatedObjectUndo( scrollbarRectObject, "Create Scrollbar Base" );
						RectTransform scrollbarRect = scrollbarRectObject.GetComponent<RectTransform>();

						scrollbarRectObject.transform.SetParent( targ.transform );
						scrollbarRect.anchorMin = new Vector2( 0.5f, 0.5f );
						scrollbarRect.anchorMax = new Vector2( 0.5f, 0.5f );
						scrollbarRect.pivot = new Vector2( 0.5f, 0.5f );
						scrollbarRect.localScale = Vector3.one;

						scrollbarBaseImage = scrollbarRectObject.GetComponent<Image>();
						scrollbarBaseImage.sprite = scrollbarBaseSprite;

						if( scrollbarBaseSprite != null && scrollbarBaseSprite.border != Vector4.zero )
							scrollbarBaseImage.type = Image.Type.Sliced;

						serializedObject.FindProperty( "scrollbarBase" ).objectReferenceValue = scrollbarRect;
						serializedObject.FindProperty( "scrollbarCanvasGroup" ).objectReferenceValue = scrollbarRectObject.GetComponent<CanvasGroup>();
						serializedObject.ApplyModifiedProperties();

						if( scrollbarBaseSprite != null && scrollbarHandleSprite == null )
							scrollbarHandleSprite = scrollbarBaseSprite;

						CreateScrollbarHandle( "Create Scrollbar Base" );

						AdjustChildIndexForOptions();
					}
					EditorGUILayout.EndVertical();
				}
				else
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					scrollbarBaseColor = EditorGUILayout.ColorField( "Base Color", scrollbarBaseColor );
					if( EditorGUI.EndChangeCheck() && scrollbarBaseImage != null )
					{
						Undo.RecordObject( scrollbarBaseImage, "Update Scrollbar Base Color" );
						scrollbarBaseImage.enabled = false;
						scrollbarBaseImage.color = scrollbarBaseColor;
						scrollbarBaseImage.enabled = true;
					}
					EditorGUI.indentLevel--;
					GUILayoutAfterIndentSpace();

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarHandle" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();
						UpdateScrollbarReferences();
					}

					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					scrollbarHandleSprite = ( Sprite )EditorGUILayout.ObjectField( "Handle Sprite", scrollbarHandleSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
					if( EditorGUI.EndChangeCheck() && scrollbarHandleImage != null )
					{
						Undo.RecordObject( scrollbarHandleImage, "Update Scrollbar Handle Sprite" );
						scrollbarHandleImage.enabled = false;
						scrollbarHandleImage.sprite = scrollbarHandleSprite;
						scrollbarHandleImage.enabled = true;

						if( scrollbarHandleSprite != null && scrollbarHandleSprite.border != Vector4.zero )
							scrollbarHandleImage.type = Image.Type.Sliced;
						else
							scrollbarHandleImage.type = Image.Type.Simple;
					}
					EditorGUI.indentLevel--;

					if( serializedObject.FindProperty( "scrollbarHandle" ).objectReferenceValue == null && !isInProjectWindow )
					{
						EditorGUILayout.BeginVertical( "Box" );
						if( GUILayout.Button( "Generate Scrollbar Handle", EditorStyles.miniButton ) )
							CreateScrollbarHandle();
						EditorGUILayout.EndVertical();
					}
					else
					{
						EditorGUI.indentLevel++;
						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarHandleNormalColor" ), new GUIContent( "Normal Color" ) );
						if( EditorGUI.EndChangeCheck() && scrollbarHandleImage != null )
						{
							serializedObject.ApplyModifiedProperties();

							Undo.RecordObject( scrollbarHandleImage, "Update Scrollbar Handle Color" );
							scrollbarHandleImage.enabled = false;
							scrollbarHandleImage.color = serializedObject.FindProperty( "scrollbarHandleNormalColor" ).colorValue;
							scrollbarHandleImage.enabled = true;
						}

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarHandleHoverColor" ), new GUIContent( "Hover Color" ) );
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarHandleActiveColor" ), new GUIContent( "Active Color" ) );
						if( EditorGUI.EndChangeCheck() )
							serializedObject.ApplyModifiedProperties();

						EditorGUI.indentLevel--;
						GUILayoutAfterIndentSpace();

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "useScrollWheel" ) );
						if( serializedObject.FindProperty( "useScrollWheel" ).boolValue )
						{
							EditorGUI.indentLevel++;
							EditorGUILayout.PropertyField( serializedObject.FindProperty( "mouseScrollSpeed" ) );
							EditorGUI.indentLevel--;
							GUILayoutAfterIndentSpace();
						}
						if( EditorGUI.EndChangeCheck() )
							serializedObject.ApplyModifiedProperties();

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarMinimumSize" ) );
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarWidth" ) );
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarHorizontalPosition" ), new GUIContent( "Horizontal Position" ) );

						EditorGUILayout.PropertyField( serializedObject.FindProperty( "visibleOnlyOnHover" ) );
						if( serializedObject.FindProperty( "visibleOnlyOnHover" ).boolValue )
						{
							EditorGUI.indentLevel++;
							float scrollbarToggleDuration = serializedObject.FindProperty( "scrollbarToggleSpeed" ).floatValue == 0.0f ? 0.0f : 1.0f / serializedObject.FindProperty( "scrollbarToggleSpeed" ).floatValue;
							EditorGUI.BeginChangeCheck();
							scrollbarToggleDuration = EditorGUILayout.FloatField( new GUIContent( "Toggle Duration", "The time is seconds that the scrollbar will transition visually." ), scrollbarToggleDuration );
							if( EditorGUI.EndChangeCheck() )
							{
								if( scrollbarToggleDuration < 0.0f )
									scrollbarToggleDuration = 0.0f;

								serializedObject.FindProperty( "scrollbarToggleSpeed" ).floatValue = scrollbarToggleDuration == 0.0f ? 0.0f : 1.0f / scrollbarToggleDuration;
								serializedObject.ApplyModifiedProperties();
							}
							EditorGUILayout.PropertyField( serializedObject.FindProperty( "scrollbarInactiveTime" ), new GUIContent( "Disable Inactive Time" ) );
							EditorGUI.indentLevel--;
							GUILayoutAfterIndentSpace();
						}

						EditorGUILayout.PropertyField( serializedObject.FindProperty( "disableBaseNavigation" ), new GUIContent( "Disable Base Navigation" ) );
						if( EditorGUI.EndChangeCheck() )
							serializedObject.ApplyModifiedProperties();
					}
				}
				GUILayout.Space( 1 );
			}
			EndSection( "UCB_VerticalScrollbar" );
			if( valueChanged || ( disableInteraction && serializedObject.FindProperty( "useScrollbar" ).boolValue ) )
			{
				if( disableInteraction )
				{
					serializedObject.FindProperty( "useScrollbar" ).boolValue = false;
					serializedObject.ApplyModifiedProperties();
				}

				if( !serializedObject.FindProperty( "useScrollbar" ).boolValue )
					EditorPrefs.SetBool( "UCB_NavigationArrows", false );

				UpdateScrollbarReferences();

				if( scrollbarBaseImage != null )
				{
					Undo.RecordObject( scrollbarBaseImage.gameObject, serializedObject.FindProperty( "useScrollbar" ).boolValue ? "Enable" : "Disable" + " Vertical Scrollbar" );
					scrollbarBaseImage.gameObject.SetActive( serializedObject.FindProperty( "useScrollbar" ).boolValue );
				}
			}
			// END VERTICAL SCROLLBAR //

			// NAVIGATION ARROWS //
			EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "useScrollbar" ).boolValue || serializedObject.FindProperty( "scrollbarBase" ).objectReferenceValue == null );
			bool navigationArrowsError = serializedObject.FindProperty( "useNavigationArrows" ).boolValue && ( navigationArrowImageUp == null || navigationArrowImageDown == null );
			if( DisplayCollapsibleBoxSection( "Navigation Arrows", "UCB_NavigationArrows", serializedObject.FindProperty( "useNavigationArrows" ), ref valueChanged, navigationArrowsError ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationArrowUp" ), new GUIContent( "Up Arrow Image" ) );
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationArrowDown" ), new GUIContent( "Down Arrow Image" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					UpdateNavigationArrowReferences();
				}

				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				scrollbarNavigationArrowSprite = ( Sprite )EditorGUILayout.ObjectField( "Image Sprite", scrollbarNavigationArrowSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && scrollbarHandleImage != null )
				{
					if( serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue != null )
					{
						Image navigationArrowUp = ( Image )serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue;
						Undo.RecordObject( navigationArrowUp, "Update Navigation Arrow Sprite" );
						navigationArrowUp.enabled = false;
						navigationArrowUp.sprite = scrollbarNavigationArrowSprite;
						navigationArrowUp.enabled = true;
					}

					if( serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue != null )
					{
						Image navigationArrowDown = ( Image )serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue;
						Undo.RecordObject( navigationArrowDown, "Update Navigation Arrow Sprite" );
						navigationArrowDown.enabled = false;
						navigationArrowDown.sprite = scrollbarNavigationArrowSprite;
						navigationArrowDown.enabled = true;
					}

					UpdateNavigationArrowReferences();
				}
				EditorGUI.indentLevel--;

				if( serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue == null || serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue == null )
				{
					if( scrollbarNavigationArrowSprite == null )
						EditorGUILayout.HelpBox( "Please assign a sprite to use for the navigation arrows.", MessageType.Warning );
					EditorGUI.BeginDisabledGroup( scrollbarNavigationArrowSprite == null && !isInProjectWindow );
					if( GUILayout.Button( "Generate Arrows", EditorStyles.miniButton ) )
					{
						if( serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue == null )
						{
							GameObject navigationArrowUpObject = new GameObject( "Navigation Arrow Up", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
							RectTransform navigationArrowUpRect = navigationArrowUpObject.GetComponent<RectTransform>();

							navigationArrowUpObject.transform.SetParent( targ.transform );
							navigationArrowUpRect.anchorMin = new Vector2( 0.5f, 1f );
							navigationArrowUpRect.anchorMax = new Vector2( 0.5f, 1f );
							navigationArrowUpRect.pivot = new Vector2( 0.5f, 0.0f );
							navigationArrowUpRect.localScale = Vector3.one;

							navigationArrowUpRect.SetParent( scrollbarBaseImage.transform );

							navigationArrowUpObject.GetComponent<Image>().sprite = scrollbarNavigationArrowSprite;
							serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue = navigationArrowUpObject.GetComponent<Image>();
							serializedObject.ApplyModifiedProperties();

							Undo.RegisterCreatedObjectUndo( navigationArrowUpObject, "Create Navigation Arrows" );
						}

						if( serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue == null )
						{
							GameObject navigationArrowDownObject = new GameObject( "Navigation Arrow Down", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ) );
							RectTransform navigationArrowDownRect = navigationArrowDownObject.GetComponent<RectTransform>();

							navigationArrowDownObject.transform.SetParent( targ.transform );
							navigationArrowDownRect.anchorMin = new Vector2( 0.5f, 0f );
							navigationArrowDownRect.anchorMax = new Vector2( 0.5f, 0f );
							navigationArrowDownRect.pivot = new Vector2( 0.5f, 0f );
							navigationArrowDownRect.localScale = Vector3.one;
							navigationArrowDownRect.localRotation = Quaternion.Euler( 180, 0, 0 );
							navigationArrowDownRect.SetParent( scrollbarBaseImage.transform );

							navigationArrowDownObject.GetComponent<Image>().sprite = scrollbarNavigationArrowSprite;
							serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue = navigationArrowDownObject.GetComponent<Image>();
							serializedObject.ApplyModifiedProperties();

							Undo.RegisterCreatedObjectUndo( navigationArrowDownObject, "Create Navigation Arrows" );
						}

						UpdateNavigationArrowReferences();
					}
					EditorGUI.EndDisabledGroup();
				}
				else
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationNormalColor" ), new GUIContent( "Normal Color" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						Undo.RecordObject( navigationArrowImageUp, "Update Navigation Arrow Color" );
						navigationArrowImageUp.enabled = false;
						navigationArrowImageUp.color = serializedObject.FindProperty( "navigationNormalColor" ).colorValue;
						navigationArrowImageUp.enabled = true;

						Undo.RecordObject( navigationArrowImageDown, "Update Navigation Arrow Color" );
						navigationArrowImageDown.enabled = false;
						navigationArrowImageDown.color = serializedObject.FindProperty( "navigationNormalColor" ).colorValue;
						navigationArrowImageDown.enabled = true;
					}

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationHoverColor" ), new GUIContent( "Hover Color" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationActiveColor" ), new GUIContent( "Active Color" ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();
					EditorGUI.indentLevel--;
					GUILayoutAfterIndentSpace();

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationInitialHoldDelay" ), new GUIContent( "Initial Delay" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "navigationIntervalDelay" ), new GUIContent( "Repeat Interval" ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();

				}
			}
			EndSection( "UCB_NavigationArrows" );
			if( valueChanged || ( disableInteraction && serializedObject.FindProperty( "useNavigationArrows" ).boolValue ) )
			{
				if( disableInteraction )
				{
					serializedObject.FindProperty( "useNavigationArrows" ).boolValue = false;
					serializedObject.ApplyModifiedProperties();
				}

				UpdateNavigationArrowReferences();

				if( navigationArrowImageUp != null )
				{
					Undo.RecordObject( navigationArrowImageUp.gameObject, ( serializedObject.FindProperty( "useNavigationArrows" ).boolValue ? "Enable" : "Disable" ) + " Navigation Arrows" );
					navigationArrowImageUp.gameObject.SetActive( serializedObject.FindProperty( "useNavigationArrows" ).boolValue );
				}

				if( navigationArrowImageDown != null )
				{
					Undo.RecordObject( navigationArrowImageDown.gameObject, ( serializedObject.FindProperty( "useNavigationArrows" ).boolValue ? "Enable" : "Disable" ) + " Navigation Arrows" );
					navigationArrowImageDown.gameObject.SetActive( serializedObject.FindProperty( "useNavigationArrows" ).boolValue );
				}
			}
			EditorGUI.EndDisabledGroup();
			// END NAVIGATION ARROWS //
			EndDisableInteractionCheck();
			/* ------------------------------------------------------------ <<< END NAVIGATION SETTINGS >>> ------------------------------------------------------------ */

			/* -------------------------------------------------------------- <<< INPUT FIELD SETTINGS >>> -------------------------------------------------------------- */
			EditorGUILayout.Space();
			EditorGUILayout.LabelField( "Input Field Settings", EditorStyles.boldLabel );

			// INPUT FIELD //
			if( DisplayCollapsibleBoxSection( "Use Input Field", "UCB_InputField", serializedObject.FindProperty( "useInputField" ), ref valueChanged, serializedObject.FindProperty( "useInputField" ).boolValue && inputFieldImage == null ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "inputField" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					UpdateInputFieldReferences();
				}

				if( serializedObject.FindProperty( "inputField" ).objectReferenceValue == null && !isInProjectWindow )
				{
					EditorGUILayout.BeginVertical( "Box" );
					if( GUILayout.Button( "Generate Input Field", EditorStyles.miniButton ) )
					{
						// INPUT FIELD OBJECT //
						GameObject inputFieldObject = new GameObject( "Input Field", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( Image ), typeof( TMP_InputField ) );
						RectTransform inputFieldRect = inputFieldObject.GetComponent<RectTransform>();

						inputFieldObject.transform.SetParent( targ.transform );
						inputFieldRect.anchorMin = new Vector2( 0.5f, 0.0f );
						inputFieldRect.anchorMax = new Vector2( 0.5f, 0.0f );
						inputFieldRect.pivot = new Vector2( 0.5f, 1.0f );
						inputFieldRect.localScale = Vector3.one;

						Image inputFieldImage = inputFieldObject.GetComponent<Image>();
						inputFieldImage.color = chatBoxImageColor;
						inputFieldImage.sprite = chatBoxImageSprite;
						if( chatBoxImageSprite != null && chatBoxImageSprite.border != Vector4.zero )
							inputFieldImage.type = Image.Type.Sliced;

						TMP_InputField inputFieldComponent = inputFieldObject.GetComponent<TMP_InputField>();
						inputFieldComponent.interactable = false;
						inputFieldComponent.transition = Selectable.Transition.None;

						serializedObject.FindProperty( "inputFieldTransform" ).objectReferenceValue = inputFieldRect;
						serializedObject.FindProperty( "inputField" ).objectReferenceValue = inputFieldComponent;
						serializedObject.ApplyModifiedProperties();

						Undo.RegisterCreatedObjectUndo( inputFieldObject, "Create Input Field" );
						// END INPUT FIELD OBJECT //

						// TEXT AREA OBJECT //
						GameObject inputFieldTextArea = new GameObject( "Text Area", typeof( RectTransform ), typeof( RectMask2D ) );
						Undo.RegisterCreatedObjectUndo( inputFieldTextArea, "Create Input Field" );
						inputFieldTextArea.transform.SetParent( inputFieldObject.transform );
						inputFieldTextArea.GetComponent<RectMask2D>().padding = new Vector4( -10, 0, 0, 0 );

						SerializedObject inputField = new SerializedObject( inputFieldComponent );
						inputField.FindProperty( "m_TextViewport" ).objectReferenceValue = inputFieldTextArea.GetComponent<RectTransform>();
						inputField.ApplyModifiedProperties();
						// END TEXT AREA OBJECT //

						// TEXT COMPONENT OBJECT //
						GameObject inputFieldTextComponentObject = new GameObject( "Text", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( TextMeshProUGUI ) );
						Undo.RegisterCreatedObjectUndo( inputFieldTextComponentObject, "Create Input Field" );
						RectTransform inputFieldTextComponentRect = inputFieldTextComponentObject.GetComponent<RectTransform>();

						inputFieldTextComponentObject.transform.SetParent( inputFieldTextArea.transform );
						inputFieldTextComponentRect.anchorMin = new Vector2( 0.0f, 0.0f );
						inputFieldTextComponentRect.anchorMax = new Vector2( 1.0f, 1.0f );
						inputFieldTextComponentRect.pivot = new Vector2( 0.5f, 0.5f );
						inputFieldTextComponentRect.localScale = Vector3.one;
						inputFieldTextComponentRect.offsetMax = Vector2.zero;
						inputFieldTextComponentRect.offsetMin = Vector2.zero;

						inputFieldTextComponentObject.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

						inputField.FindProperty( "m_TextComponent" ).objectReferenceValue = inputFieldTextComponentObject.GetComponent<TextMeshProUGUI>();
						inputField.ApplyModifiedProperties();
						// END TEXT COMPONENT OBJECT //

						// PLACEHOLDER OBJECT //
						GameObject inputFieldPlaceholderObject = new GameObject( "Placeholder", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( TextMeshProUGUI ) );
						Undo.RegisterCreatedObjectUndo( inputFieldPlaceholderObject, "Create Input Field" );
						RectTransform inputFieldPlaceholderRect = inputFieldPlaceholderObject.GetComponent<RectTransform>();

						inputFieldPlaceholderObject.transform.SetParent( inputFieldTextArea.transform );
						inputFieldPlaceholderRect.anchorMin = new Vector2( 0.0f, 0.0f );
						inputFieldPlaceholderRect.anchorMax = new Vector2( 1.0f, 1.0f );
						inputFieldPlaceholderRect.pivot = new Vector2( 0.5f, 0.5f );
						inputFieldPlaceholderRect.localScale = Vector3.one;
						inputFieldPlaceholderRect.offsetMax = Vector2.zero;
						inputFieldPlaceholderRect.offsetMin = Vector2.zero;

						inputFieldPlaceholderObject.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
						inputFieldPlaceholderObject.GetComponent<TextMeshProUGUI>().text = "Enter text...";
						inputFieldPlaceholderObject.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;

						inputField.FindProperty( "m_Placeholder" ).objectReferenceValue = inputFieldPlaceholderObject.GetComponent<TextMeshProUGUI>();
						inputField.ApplyModifiedProperties();
						// END TEXT COMPONENT OBJECT //

						// After all the other object have been created, assign the font assign so that it's the same as the main text if it is assigned.
						if( textFont != null )
							inputFieldComponent.fontAsset = textFont;
						// Otherwise just use the input field component font (default font).
						else
							inputFieldComponent.fontAsset = inputFieldTextComponentObject.GetComponent<TextMeshProUGUI>().font;

						UpdateInputFieldReferences();

						AdjustChildIndexForOptions();
					}
					EditorGUILayout.EndVertical();
				}
				else
				{
					EditorGUI.BeginChangeCheck();
					inputFieldImageSprite = ( Sprite )EditorGUILayout.ObjectField( "Background Sprite", inputFieldImageSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
					if( EditorGUI.EndChangeCheck() && inputFieldImage != null )
					{
						Undo.RecordObject( inputFieldImage, "Update Input Field Image Sprite" );
						inputFieldImage.enabled = false;
						inputFieldImage.sprite = inputFieldImageSprite;
						inputFieldImage.enabled = true;

						if( inputFieldImageSprite != null && inputFieldImageSprite.border != Vector4.zero )
							inputFieldImage.type = Image.Type.Sliced;
						else
							inputFieldImage.type = Image.Type.Simple;
					}

					EditorGUI.BeginChangeCheck();
					inputFieldImageColor = EditorGUILayout.ColorField( "Background Color", inputFieldImageColor );
					if( EditorGUI.EndChangeCheck() && inputFieldImage != null )
					{
						Undo.RecordObject( inputFieldImage, "Update Input Field Image Color" );
						inputFieldImage.enabled = false;
						inputFieldImage.color = inputFieldImageColor;
						inputFieldImage.enabled = true;
					}

					EditorGUI.BeginChangeCheck();
					inputFieldFont = ( TMP_FontAsset )EditorGUILayout.ObjectField( "Input Field Font", inputFieldFont, typeof( TMP_FontAsset ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
					if( EditorGUI.EndChangeCheck() )
					{
						Undo.RecordObject( targ.InputField, "Update Input Field Font" );
						targ.InputField.fontAsset = inputFieldFont;
					}

					EditorGUI.BeginChangeCheck();
					inputFieldTextColor = EditorGUILayout.ColorField( "Input Field Text Color", inputFieldTextColor );
					if( EditorGUI.EndChangeCheck() && targ.InputField.textComponent != null )
					{
						Undo.RecordObject( targ.InputField.textComponent, "Update Input Field Text Color" );
						targ.InputField.textComponent.enabled = false;
						targ.InputField.textComponent.color = inputFieldTextColor;
						targ.InputField.textComponent.enabled = true;
					}

					EditorGUI.BeginChangeCheck();
					inputFieldPlaceholderTextValue = EditorGUILayout.TextField( "Placeholder Text", inputFieldPlaceholderTextValue );
					if( EditorGUI.EndChangeCheck() && inputFieldImage != null )
					{
						Undo.RecordObject( inputFieldPlaceholderText, "Update Input Field Image Color" );
						inputFieldPlaceholderText.enabled = false;
						inputFieldPlaceholderText.text = inputFieldPlaceholderTextValue;
						inputFieldPlaceholderText.enabled = true;
					}

					bool inputFieldJustSpaces = true;
					for( int i = 0; i < inputFieldPlaceholderTextValue.Length; i++ )
					{
						if( inputFieldPlaceholderTextValue[ i ].ToString() != " " )
						{
							inputFieldJustSpaces = false;
							break;
						}
					}

					if( inputFieldPlaceholderTextValue == string.Empty || inputFieldJustSpaces )
						EditorGUILayout.HelpBox( "Placeholder text is empty. This will prevent the input field from being able to display the text input.", MessageType.Warning );

					EditorGUI.BeginChangeCheck();
					inputFieldPlaceholderTextColor = EditorGUILayout.ColorField( "Placeholder Text Color", inputFieldPlaceholderTextColor );
					if( EditorGUI.EndChangeCheck() && inputFieldImage != null )
					{
						Undo.RecordObject( inputFieldPlaceholderText, "Update Placeholder Text Color" );
						inputFieldPlaceholderText.enabled = false;
						inputFieldPlaceholderText.color = inputFieldPlaceholderTextColor;
						inputFieldPlaceholderText.enabled = true;
					}

					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Positioning", EditorStyles.boldLabel );
					EditorGUI.BeginChangeCheck();
					EditorGUILayout.Slider( serializedObject.FindProperty( "inputFieldSize.x" ), 0.0f, 100.0f, new GUIContent( "Horizontal Size", "The horizontal size of the input field relative to the chat box." ) );
					EditorGUILayout.Slider( serializedObject.FindProperty( "inputFieldSize.y" ), 0.0f, 50.0f, new GUIContent( "Vertical Size", "The vertical size of the input field relative to the chat box." ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();

					EditorGUI.BeginChangeCheck();
					EditorGUI.BeginDisabledGroup( serializedObject.FindProperty( "inputFieldSize.x" ).floatValue == 100.0f );
					EditorGUILayout.Slider( serializedObject.FindProperty( "inputFieldPosition.x" ), -50.0f, 50.0f, new GUIContent( "Horizontal Position", "The horizontal position of the input field relative to the center of the chat box." ) );
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "inputFieldPosition.y" ), new GUIContent( "Vertical Position", "The vertical position of the input field relative to the bottom of the chat box." ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();
					}

					// TEXT AREA SETTINGS //
					EditorGUILayout.Space();
					EditorGUILayout.LabelField( "Text Area", EditorStyles.boldLabel );

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "inputFieldSmartFontSize" ), new GUIContent( "Smart Font Size" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();

						// If the smart font size is zero, then change to a set font size setting for them.
						if( serializedObject.FindProperty( "inputFieldSmartFontSize" ).floatValue == 0.0f )
						{
							inputFieldTextSize = 24;

							Undo.RecordObject( targ.InputField, "Update Font Size" );
							targ.InputField.pointSize = inputFieldTextSize;

							Undo.RecordObject( targ.InputField.textComponent, "Update Font Size" );
							targ.InputField.textComponent.fontSize = inputFieldTextSize;

							Undo.RecordObject( inputFieldPlaceholderText, "Update Font Size" );
							inputFieldPlaceholderText.fontSize = inputFieldTextSize;
						}
					}

					if( serializedObject.FindProperty( "inputFieldSmartFontSize" ).floatValue <= 0.0f )
					{
						EditorGUI.indentLevel++;
						EditorGUI.BeginChangeCheck();
						inputFieldTextSize = EditorGUILayout.FloatField( new GUIContent( "Font Size", "The font size to apply to the input field text. This font size will not display the same on all screen sizes like the Smart Font Size does." ), inputFieldTextSize );
						if( EditorGUI.EndChangeCheck() )
						{
							if( inputFieldTextSize < 1 )
								inputFieldTextSize = 1;

							Undo.RecordObject( targ.InputField, "Update Font Size" );
							targ.InputField.pointSize = inputFieldTextSize;

							Undo.RecordObject( targ.InputField.textComponent, "Update Font Size" );
							targ.InputField.textComponent.fontSize = inputFieldTextSize;

							Undo.RecordObject( inputFieldPlaceholderText, "Update Font Size" );
							inputFieldPlaceholderText.fontSize = inputFieldTextSize;
						}
						EditorGUI.indentLevel--;
						GUILayoutAfterIndentSpace();
					}

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.Slider( serializedObject.FindProperty( "inputFieldTextAreaSize.y" ), 0.0f, 100.0f, new GUIContent( "Text Area Height" ) );
					Rect propertyRect = GUILayoutUtility.GetLastRect();
					EditorGUILayout.Slider( serializedObject.FindProperty( "inputFieldTextAreaSize.x" ), 0.0f, 100.0f, new GUIContent( "Text Area Width" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "inputFieldTextHorizontalPosition" ), new GUIContent( "Horizontal Position" ) );
					propertyRect.yMax = GUILayoutUtility.GetLastRect().yMax;
					DisplayInputFieldTextArea.hover = false;
					if( Event.current.type == EventType.Repaint && propertyRect.Contains( Event.current.mousePosition ) )
						DisplayInputFieldTextArea.hover = true;

					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();
						DisplayInputFieldTextArea.PropertyUpdated();
					}
				}
			}
			EndSection( "UCB_InputField" );
			if( valueChanged )
			{
				if( !serializedObject.FindProperty( "useInputField" ).boolValue )
				{
					EditorPrefs.SetBool( "UCB_ExtraImage", false );
					EditorPrefs.SetBool( "UCB_UseEmojiWindow", false );
				}

				TMP_InputField inputField = ( TMP_InputField )serializedObject.FindProperty( "inputField" ).objectReferenceValue;
				if( inputField != null )
				{
					Undo.RecordObject( inputField.gameObject, ( serializedObject.FindProperty( "useInputField" ).boolValue ? "Enable" : "Disable" ) + " Input Field" );
					inputField.gameObject.SetActive( serializedObject.FindProperty( "useInputField" ).boolValue );
				}
			}
			// END INPUT FIELD //

			EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "useInputField" ).boolValue || targ.InputField == null );
			// EXTRA IMAGE //
			if( DisplayCollapsibleBoxSection( "Extra Image", "UCB_ExtraImage", serializedObject.FindProperty( "useExtraImage" ), ref valueChanged, serializedObject.FindProperty( "useExtraImage" ).boolValue && extraImage == null ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "extraImage" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					UpdateExtraImageReferences();
				}

				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				extraImageSprite = ( Sprite )EditorGUILayout.ObjectField( "Image Sprite", extraImageSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && extraImage != null )
				{
					Undo.RecordObject( extraImage, "Update Extra Image Sprite" );
					extraImage.enabled = false;
					extraImage.sprite = extraImageSprite;
					extraImage.enabled = true;

					if( extraImageSprite != null && extraImageSprite.border != Vector4.zero )
						extraImage.type = Image.Type.Sliced;
					else
						extraImage.type = Image.Type.Simple;
				}
				EditorGUI.indentLevel--;

				if( serializedObject.FindProperty( "extraImage" ).objectReferenceValue == null && !isInProjectWindow )
				{
					EditorGUILayout.BeginVertical( "Box" );
					if( GUILayout.Button( "Generate Extra Image", EditorStyles.miniButton ) )
					{
						GameObject extraImageObject = new GameObject( "Extra Image", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( UnityEngine.UI.Image ) );
						RectTransform extraImageRect = extraImageObject.GetComponent<RectTransform>();

						extraImageObject.transform.SetParent( targ.InputField.transform );
						extraImageRect.anchorMin = new Vector2( 0.5f, 0.5f );
						extraImageRect.anchorMax = new Vector2( 0.5f, 0.5f );
						extraImageRect.pivot = new Vector2( 0.5f, 0.5f );
						extraImageRect.localScale = Vector3.one;

						extraImage = extraImageObject.GetComponent<Image>();
						extraImage.sprite = extraImageSprite;

						if( extraImageSprite != null && extraImageSprite.border != Vector4.zero )
							extraImage.type = Image.Type.Sliced;

						serializedObject.FindProperty( "extraImage" ).objectReferenceValue = extraImage;
						serializedObject.ApplyModifiedProperties();

						Undo.RegisterCreatedObjectUndo( extraImageObject, "Create Extra Image" );

						UpdateExtraImageReferences();
					}
					EditorGUILayout.EndVertical();
				}
				else
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					extraImageColor = EditorGUILayout.ColorField( "Image Color", extraImageColor );
					if( EditorGUI.EndChangeCheck() && extraImage != null )
					{
						Undo.RecordObject( extraImage, "Update Extra Image Color" );
						extraImage.enabled = false;
						extraImage.color = extraImageColor;
						extraImage.enabled = true;
					}
					EditorGUI.indentLevel--;
					GUILayoutAfterIndentSpace();

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "extraImageWidth" ), new GUIContent( "Image Width" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "extraImageHeight" ), new GUIContent( "Image Height" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "extraImageHorizontalPosition" ), new GUIContent( "Horizontal Position" ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();
				}
			}
			EndSection( "UCB_ExtraImage" );
			if( valueChanged && extraImage != null )
			{
				Undo.RecordObject( extraImage.gameObject, ( serializedObject.FindProperty( "useExtraImage" ).boolValue ? "Enable" : "Disable" ) + " Extra Image" );
				extraImage.gameObject.SetActive( serializedObject.FindProperty( "useExtraImage" ).boolValue );
			}
			// END EXTRA IMAGE //

			BeginDisableInteractionCheck();
			// EMOJI WINDOW //
			EditorGUI.BeginDisabledGroup( !serializedObject.FindProperty( "useTextEmoji" ).boolValue );
			bool emojiWindowError = serializedObject.FindProperty( "useEmojiWindow" ).boolValue && ( emojiButtonImage == null || emojiWindowImage == null );
			if( DisplayCollapsibleBoxSection( "Emoji Window", "UCB_UseEmojiWindow", serializedObject.FindProperty( "useEmojiWindow" ), ref valueChanged, emojiWindowError ) )
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiButtonImage" ) );
				if( EditorGUI.EndChangeCheck() )
				{
					serializedObject.ApplyModifiedProperties();
					UpdateEmojiButtonReferences();
				}

				EditorGUI.indentLevel++;
				EditorGUI.BeginChangeCheck();
				emojiButtonSprite = ( Sprite )EditorGUILayout.ObjectField( "Button Sprite", emojiButtonSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
				if( EditorGUI.EndChangeCheck() && emojiButtonImage != null )
				{
					Undo.RecordObject( emojiButtonImage, "Update Emoji Button Sprite" );
					emojiButtonImage.enabled = false;
					emojiButtonImage.sprite = emojiButtonSprite;
					emojiButtonImage.enabled = true;

					if( emojiButtonSprite != null && emojiButtonSprite.border != Vector4.zero )
						emojiButtonImage.type = Image.Type.Sliced;
					else
						emojiButtonImage.type = Image.Type.Simple;
				}
				EditorGUI.indentLevel--;

				if( serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue == null )
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					emojiWindowSprite = ( Sprite )EditorGUILayout.ObjectField( "Window Background Sprite", emojiWindowSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
					if( EditorGUI.EndChangeCheck() && emojiWindowImage != null )
					{
						Undo.RecordObject( emojiWindowImage, "Update Emoji Button Sprite" );
						emojiWindowImage.enabled = false;
						emojiWindowImage.sprite = emojiWindowSprite;
						emojiWindowImage.enabled = true;

						if( emojiWindowSprite != null && emojiWindowSprite.border != Vector4.zero )
							emojiWindowImage.type = Image.Type.Sliced;
						else
							emojiWindowImage.type = Image.Type.Simple;
					}
					EditorGUI.indentLevel--;
				}

				if( serializedObject.FindProperty( "emojiButtonImage" ).objectReferenceValue == null )
				{
					EditorGUILayout.BeginVertical( "Box" );
					if( GUILayout.Button( "Create Emoji Button", EditorStyles.miniButton ) )
					{
						GameObject emojiButtonObject = new GameObject( "Emoji Button", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( UnityEngine.UI.Image ) );
						RectTransform emojiButtonRect = emojiButtonObject.GetComponent<RectTransform>();

						emojiButtonObject.transform.SetParent( targ.InputField.transform );
						emojiButtonRect.anchorMin = new Vector2( 0.5f, 0.5f );
						emojiButtonRect.anchorMax = new Vector2( 0.5f, 0.5f );
						emojiButtonRect.pivot = new Vector2( 0.5f, 0.5f );
						emojiButtonRect.localScale = Vector3.one;

						emojiButtonImage = emojiButtonObject.GetComponent<Image>();
						emojiButtonImage.sprite = emojiButtonSprite;

						if( emojiButtonSprite != null && emojiButtonSprite.border != Vector4.zero )
							emojiButtonImage.type = Image.Type.Sliced;

						serializedObject.FindProperty( "emojiButtonImage" ).objectReferenceValue = emojiButtonImage;
						serializedObject.ApplyModifiedProperties();

						Undo.RegisterCreatedObjectUndo( emojiButtonObject, "Create Emoji Button" );

						if( serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue == null )
							CreateEmojiWindow();

						UpdateEmojiButtonReferences();
					}
					EditorGUILayout.EndVertical();
				}
				else
				{
					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					emojiButtonColor = EditorGUILayout.ColorField( "Image Color", emojiButtonColor );
					if( EditorGUI.EndChangeCheck() && emojiButtonImage != null )
					{
						Undo.RecordObject( emojiButtonImage, "Update Emoji Button Color" );
						emojiButtonImage.enabled = false;
						emojiButtonImage.color = emojiButtonColor;
						emojiButtonImage.enabled = true;
					}

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiButtonSize" ), new GUIContent( "Button Size" ) );
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiButtonHorizontalPosition" ), new GUIContent( "Horizontal Position" ) );
					if( EditorGUI.EndChangeCheck() )
						serializedObject.ApplyModifiedProperties();

					EditorGUI.indentLevel--;
					GUILayoutAfterIndentSpace();

					EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiWindowImage" ) );
					if( EditorGUI.EndChangeCheck() )
					{
						serializedObject.ApplyModifiedProperties();
						UpdateEmojiWindowReferences();
					}

					EditorGUI.indentLevel++;
					EditorGUI.BeginChangeCheck();
					emojiWindowSprite = ( Sprite )EditorGUILayout.ObjectField( "Image Sprite", emojiWindowSprite, typeof( Sprite ), true, GUILayout.Height( EditorGUIUtility.singleLineHeight ) );
					if( EditorGUI.EndChangeCheck() && emojiWindowImage != null )
					{
						Undo.RecordObject( emojiWindowImage, "Update Emoji Button Sprite" );
						emojiWindowImage.enabled = false;
						emojiWindowImage.sprite = emojiWindowSprite;
						emojiWindowImage.enabled = true;

						if( emojiWindowSprite != null && emojiWindowSprite.border != Vector4.zero )
							emojiWindowImage.type = Image.Type.Sliced;
						else
							emojiWindowImage.type = Image.Type.Simple;
					}
					EditorGUI.indentLevel--;

					if( serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue == null )
					{
						EditorGUILayout.BeginVertical( "Box" );
						EditorGUILayout.HelpBox( "", MessageType.Warning );
						if( GUILayout.Button( "Create Emoji Window", EditorStyles.miniButton ) )
							CreateEmojiWindow();
						EditorGUILayout.EndVertical();
					}
					else
					{
						EditorGUI.indentLevel++;
						EditorGUI.BeginChangeCheck();
						emojiWindowColor = EditorGUILayout.ColorField( "Image Color", emojiWindowColor );
						if( EditorGUI.EndChangeCheck() && emojiWindowImage != null )
						{
							Undo.RecordObject( emojiWindowImage, "Update Emoji Window Color" );
							emojiWindowImage.enabled = false;
							emojiWindowImage.color = emojiWindowColor;
							emojiWindowImage.enabled = true;
						}

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiWindowSize.x" ), new GUIContent( "Width" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							if( serializedObject.FindProperty( "emojiWindowSize.x" ).floatValue < 1.0f )
								serializedObject.FindProperty( "emojiWindowSize.x" ).floatValue = 1.0f;

							serializedObject.ApplyModifiedProperties();
						}

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiWindowSize.y" ), new GUIContent( "Height" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							if( serializedObject.FindProperty( "emojiWindowSize.y" ).floatValue < 1.0f )
								serializedObject.FindProperty( "emojiWindowSize.y" ).floatValue = 1.0f;

							serializedObject.ApplyModifiedProperties();
						}

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiWindowPosition.x" ), new GUIContent( "Horizontal Position" ) );
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiWindowPosition.y" ), new GUIContent( "Vertical Position" ) );
						if( EditorGUI.EndChangeCheck() )
							serializedObject.ApplyModifiedProperties();

						EditorGUI.indentLevel--;
						GUILayoutAfterIndentSpace();

						EditorGUI.BeginChangeCheck();
						EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiText" ) );
						if( EditorGUI.EndChangeCheck() )
						{
							serializedObject.ApplyModifiedProperties();
							UpdateEmojiWindowReferences();
						}

						if( serializedObject.FindProperty( "emojiText" ).objectReferenceValue == null )
						{
							EditorGUILayout.BeginVertical( "Box" );
							if( GUILayout.Button( "Create Emoji Text", EditorStyles.miniButton ) )
								CreateEmojiWindow();
							EditorGUILayout.EndVertical();
						}
						else
						{
							EditorGUI.indentLevel++;

							EditorGUI.BeginChangeCheck();
							EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiAsset" ) );
							if( EditorGUI.EndChangeCheck() )
							{
								serializedObject.ApplyModifiedProperties();

								if( serializedObject.FindProperty( "useTextEmoji" ).boolValue && targ.EmojiAsset != null )
								{
									Undo.RecordObject( targ.TextObject, "Update Sprite Asset" );
									targ.TextObject.spriteAsset = targ.EmojiAsset;
								}

								UpdateEmojiWindowText();
							}

							EditorGUI.BeginDisabledGroup( serializedObject.FindProperty( "emojiAsset" ).objectReferenceValue == null );
							EditorGUI.BeginChangeCheck();
							EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiTextEdgePadding" ) );
							if( EditorGUI.EndChangeCheck() )
								serializedObject.ApplyModifiedProperties();

							EditorGUI.BeginChangeCheck();
							characterSpacing = EditorGUILayout.FloatField( new GUIContent( "Character Spacing" ), characterSpacing );
							if( EditorGUI.EndChangeCheck() )
							{
								Undo.RecordObject( emojiText, "Update Emoji Character Spacing" );
								emojiText.enabled = false;
								emojiText.characterSpacing = characterSpacing;
								emojiText.enabled = true;
							}

							EditorGUI.BeginChangeCheck();
							lineSpacing = EditorGUILayout.FloatField( new GUIContent( "Line Spacing" ), lineSpacing );
							if( EditorGUI.EndChangeCheck() )
							{
								Undo.RecordObject( emojiText, "Update Emoji Line Spacing" );
								emojiText.enabled = false;
								emojiText.lineSpacing = lineSpacing;
								emojiText.enabled = true;
							}

							EditorGUI.BeginChangeCheck();
							EditorGUILayout.PropertyField( serializedObject.FindProperty( "emojiPerRow" ) );
							if( EditorGUI.EndChangeCheck() )
							{
								if( serializedObject.FindProperty( "emojiPerRow" ).intValue < 1 )
									serializedObject.FindProperty( "emojiPerRow" ).intValue = 1;
								else if( serializedObject.FindProperty( "emojiPerRow" ).intValue > targ.EmojiAsset.spriteCharacterTable.Count )
									serializedObject.FindProperty( "emojiPerRow" ).intValue = targ.EmojiAsset.spriteCharacterTable.Count;

								serializedObject.ApplyModifiedProperties();
								Undo.RecordObject( emojiText, "Update Emoji Per Row" );
								UpdateEmojiWindowText();
							}
							EditorGUI.indentLevel--;
							GUILayoutAfterIndentSpace();
							EditorGUI.EndDisabledGroup();
						}
					}
				}
			}
			EndSection( "UCB_UseEmojiWindow" );
			if( valueChanged || ( disableInteraction && serializedObject.FindProperty( "useEmojiWindow" ).boolValue ) )
			{
				if( disableInteraction )
				{
					serializedObject.FindProperty( "useEmojiWindow" ).boolValue = false;
					serializedObject.ApplyModifiedProperties();
				}

				if( emojiButtonImage != null )
				{
					Undo.RecordObject( emojiButtonImage.gameObject, ( serializedObject.FindProperty( "useEmojiWindow" ).boolValue ? "Enable" : "Disable" ) + " Emoji Window" );
					emojiButtonImage.gameObject.SetActive( serializedObject.FindProperty( "useEmojiWindow" ).boolValue );
				}

				if( emojiWindowImage != null )
				{
					Undo.RecordObject( emojiWindowImage.gameObject, ( serializedObject.FindProperty( "useEmojiWindow" ).boolValue ? "Enable" : "Disable" ) + " Emoji Window" );
					emojiWindowImage.gameObject.SetActive( serializedObject.FindProperty( "useEmojiWindow" ).boolValue );
				}

				UpdateEmojiButtonReferences();
				UpdateEmojiWindowReferences();
				UpdateEmojiWindowText();
			}
			EditorGUI.EndDisabledGroup();
			// END EMOJI WINDOW //
			EndDisableInteractionCheck();

			EditorGUI.EndDisabledGroup();
			/* ------------------------------------------------------------ <<< END INPUT FIELD SETTINGS >>> ------------------------------------------------------------ */

			EditorGUILayout.Space();

			if( !disableDragAndDrop && isDraggingObject )
				Repaint();

			if( isDirty || ( !isDirty && wasDirtyLastFrame ) )
				SceneView.RepaintAll();

			wasDirtyLastFrame = isDirty;
			isDirty = false;
		}

		private void OnSceneGUI ()
		{
			if( targ == null )
				return;

			if( targ.VisibleChatBoundingBox == null || targ.ChatContentBox == null )
				return;

			if( parentCanvasTransform == null )
				parentCanvasTransform = targ.ParentCanvas.GetComponent<RectTransform>();

			if( !Application.isPlaying )
			{
				if( EditorPrefs.GetBool( "UCB_ChatBoxPosition" ) )
				{
					if( DisplayChatBoxPosition.HighlightGizmo )
					{
						Handles.color = colorDefault;
						Rect chatBoxRect = CalculateRect( targ.BaseTransform );
						Rect parentCanvasRect = CalculateRect( parentCanvasTransform );
						Handles.DrawLine( new Vector3( chatBoxRect.center.x, parentCanvasRect.yMin, 0 ), new Vector3( chatBoxRect.center.x, parentCanvasRect.yMax, 0 ) );
						Handles.DrawLine( new Vector3( parentCanvasRect.xMin, chatBoxRect.center.y, 0 ), new Vector3( parentCanvasRect.xMax, chatBoxRect.center.y, 0 ) );
						Handles.DrawWireCube( chatBoxRect.center, chatBoxRect.size );
						Handles.DrawWireCube( parentCanvasRect.center, parentCanvasRect.size );
					}

					Rect contentBoxRect = CalculateRect( targ.ChatContentBox );
					Handles.color = colorDefault;
					if( DisplayBoundingBox.HighlightGizmo )
						Handles.color = colorValueChanged;

					Handles.DrawWireCube( contentBoxRect.center, contentBoxRect.size );
				}

				if( EditorPrefs.GetBool( "UCB_TextSettings" ) && targ.TextObject != null && serializedObject.FindProperty( "spaceBetweenChats" ).floatValue > 0.0f )
				{
					Rect textObjectRect = CalculateRect( targ.TextObject.rectTransform );
					for( int i = 0; i < 5; i++ )
					{
						Handles.color = colorDefault;
						Handles.DrawWireCube( textObjectRect.center, textObjectRect.size );

						textObjectRect.y -= textObjectRect.size.y + ( textObjectRect.size.y * serializedObject.FindProperty( "spaceBetweenChats" ).floatValue );
					}
				}

				if( EditorPrefs.GetBool( "UCB_InputField" ) && targ.InputField != null )
				{
					Rect inputFieldTextRect = CalculateRect( targ.InputField.textViewport );
					Handles.color = colorDefault;
					if( DisplayInputFieldTextArea.HighlightGizmo )
						Handles.color = colorValueChanged;

					Handles.DrawWireCube( inputFieldTextRect.center, inputFieldTextRect.size );
				}
			}

			if( !Application.isPlaying || targ.ChatInformations == null )
				return;

			for( int i = 0; i < targ.ChatInformations.Count; i++ )
			{
				if( targ.ChatInformations[ i ].IsVisible )
					Handles.color = colorChatVisible;
				else
					Handles.color = colorChatInvisible;

				Rect chatTextRect = new Rect();
				chatTextRect.center = targ.ChatContentBox.position + new Vector3( -targ.ChatContentBox.sizeDelta.x * parentCanvasTransform.localScale.x / 2, targ.ChatInformations[ i ].anchoredPosition.y * parentCanvasTransform.localScale.y, 0 ) - new Vector3( 0, targ.ChatInformations[ i ].contentSpace * parentCanvasTransform.localScale.y, 0 );
				chatTextRect.size = new Vector2( targ.ChatContentBox.sizeDelta.x, targ.ChatInformations[ i ].contentSpace ) * parentCanvasTransform.localScale;
				Handles.DrawWireCube( chatTextRect.center, chatTextRect.size );
			}

			if( EditorPrefs.GetBool( "UCB_DebugInputData" ) )
			{
				Rect chatBoxScreenRect = serializedObject.FindProperty( "chatBoxScreenRect" ).rectValue;
				if( chatBoxScreenRect.Contains( targ.InputPosition ) )
					Handles.color = colorInputInRange;
				else
					Handles.color = colorInputOutOfRange;

				Handles.DrawWireCube( chatBoxScreenRect.position + ( chatBoxScreenRect.size / 2 ), chatBoxScreenRect.size );

				Handles.DrawSolidDisc( targ.InputPosition, Vector3.forward, 5 );

				if( serializedObject.FindProperty( "useScrollbar" ).boolValue )
				{
					Rect scrollbarBaseRect = serializedObject.FindProperty( "scrollbarBaseRect" ).rectValue;
					Rect scrollbarHandleRect = serializedObject.FindProperty( "scrollbarHandleRect" ).rectValue;
					if( scrollbarBaseRect != null && scrollbarHandleRect != null )
					{
						if( scrollbarHandleRect.Contains( targ.InputPosition ) )
							Handles.color = colorInputInRange;
						else
							Handles.color = colorInputOutOfRange;
						Handles.DrawWireCube( scrollbarHandleRect.position + ( scrollbarHandleRect.size / 2 ), scrollbarHandleRect.size * new Vector2( 0.75f, 1 ) );

						if( !scrollbarHandleRect.Contains( targ.InputPosition ) && scrollbarBaseRect.Contains( targ.InputPosition ) )
							Handles.color = colorInputInRange;
						else
							Handles.color = colorInputOutOfRange;
						Handles.DrawWireCube( scrollbarBaseRect.position + ( scrollbarBaseRect.size / 2 ), scrollbarBaseRect.size );

						if( serializedObject.FindProperty( "useNavigationArrows" ).boolValue && navigationArrowImageUp != null && navigationArrowImageDown != null )
						{
							Rect scrollbarNavigationArrowUpRect = serializedObject.FindProperty( "scrollbarNavigationArrowUpRect" ).rectValue;
							if( scrollbarNavigationArrowUpRect.Contains( targ.InputPosition ) )
								Handles.color = colorInputInRange;
							else
								Handles.color = colorInputOutOfRange;

							Handles.DrawWireCube( scrollbarNavigationArrowUpRect.position + ( scrollbarNavigationArrowUpRect.size / 2 ), scrollbarNavigationArrowUpRect.size );

							Rect scrollbarNavigationArrowDownRect = serializedObject.FindProperty( "scrollbarNavigationArrowDownRect" ).rectValue;
							if( scrollbarNavigationArrowDownRect.Contains( targ.InputPosition ) )
								Handles.color = colorInputInRange;
							else
								Handles.color = colorInputOutOfRange;

							Handles.DrawWireCube( scrollbarNavigationArrowDownRect.position + ( scrollbarNavigationArrowDownRect.size / 2 ), scrollbarNavigationArrowDownRect.size );
						}
					}
				}

				// USERNAMES //
				if( serializedObject.FindProperty( "useInteractableUsername" ).boolValue )
				{
					Vector2 localMousePosition = targ.InputPosition - ( targ.ChatContentBox.position );
					for( int i = 0; i < targ.ChatInformations.Count; i++ )
					{
						if( targ.ChatInformations[ i ].usernameRect.Contains( localMousePosition ) )
							Handles.color = colorInputInRange;
						else
							Handles.color = colorInputOutOfRange;

						Handles.DrawWireCube( ( targ.ChatContentBox.position ) + ( Vector3 )targ.ChatInformations[ i ].usernameRect.center, targ.ChatInformations[ i ].usernameRect.size );
					}
				}

				// INPUT FIELD //
				if( serializedObject.FindProperty( "useInputField" ).boolValue )
				{
					Rect inputFieldRect = serializedObject.FindProperty( "inputFieldRect" ).rectValue;
					Rect emojiButtonRect = serializedObject.FindProperty( "emojiButtonRect" ).rectValue;
					if( inputFieldRect.Contains( targ.InputPosition ) && !( serializedObject.FindProperty( "useEmojiWindow" ).boolValue && emojiButtonRect.Contains( targ.InputPosition ) ) )
						Handles.color = colorInputInRange;
					else
						Handles.color = colorInputOutOfRange;

					Handles.DrawWireCube( inputFieldRect.position + ( inputFieldRect.size / 2 ), inputFieldRect.size );

					// EMOJI BUTTON //
					if( serializedObject.FindProperty( "useEmojiWindow" ).boolValue )
					{
						if( emojiButtonRect.Contains( targ.InputPosition ) )
							Handles.color = colorInputInRange;
						else
							Handles.color = colorInputOutOfRange;

						Handles.DrawWireCube( emojiButtonRect.center, emojiButtonRect.size );

						if( targ.EmojiWindowEnabled )
						{
							// EMOJI WINDOW //
							Rect emojiWindowRect = serializedObject.FindProperty( "emojiWindowRect" ).rectValue;
							if( emojiWindowRect.Contains( targ.InputPosition ) )
								Handles.color = colorInputInRange;
							else
								Handles.color = colorInputOutOfRange;

							Handles.DrawWireCube( emojiWindowRect.center, emojiWindowRect.size );

							// EACH EMOJI //
							for( int i = 0; i < serializedObject.FindProperty( "emojiRects" ).arraySize; i++ )
							{
								Rect emojiRect = serializedObject.FindProperty( string.Format( "emojiRects.Array.data[{0}]", i ) ).rectValue;

								if( emojiRect.Contains( targ.InputPosition ) )
									Handles.color = colorInputInRange;
								else
									Handles.color = colorInputOutOfRange;

								Handles.DrawWireCube( emojiRect.center, emojiRect.size );
							}
						}
					}
				}
			}
		}
		// ---------------------< UNITY FUNCTIONS AND OVERRIDES >----------------------- //

		void StoreReferences ()
		{
			targ = ( UltimateChatBox )target;

			if( targ == null )
				return;

			isInProjectWindow = Selection.activeGameObject != null && AssetDatabase.Contains( Selection.activeGameObject );

			CheckForParentCanvas();

			// If the parent canvas is assigned, then store the RectTransform of it to reference scale and size.
			if( targ.ParentCanvas != null )
				parentCanvasTransform = targ.ParentCanvas.GetComponent<RectTransform>();

			// If the chat box doesn't have an image, then add it.
			if( !targ.GetComponent<Image>() )
				Undo.AddComponent( targ.gameObject, typeof( Image ) );

			// Store the image on this object and store the color and sprite of the chat box so it can be edited.
			chatBoxImage = targ.GetComponent<Image>();
			chatBoxImageColor = chatBoxImage.color;
			chatBoxImageSprite = chatBoxImage.sprite;

			// If there's no visible chat bounding box, create one since it's needed.
			if( serializedObject.FindProperty( "visibleChatBoundingBox" ).objectReferenceValue == null )
			{
				if( isInProjectWindow )
				{
					Debug.LogError( FormatDebug( "This chat box does not have the basic needed objects to function. The needed objects cannot be created within the project window.", "Please drag this prefab into the scene and create all the needed objects and then apply the changes to the prefab before continuing.", targ.gameObject.name ) );
					return;
				}

				GameObject boundingBoxObject = new GameObject( "Visible Chat Bounding Box", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( UnityEngine.UI.Image ), typeof( UnityEngine.UI.Mask ), typeof( CanvasGroup ) );
				Undo.RegisterCreatedObjectUndo( boundingBoxObject, "Create Ultimate Chat Box Objects" );
				RectTransform boundingBoxRect = boundingBoxObject.GetComponent<RectTransform>();

				boundingBoxObject.transform.SetParent( targ.transform );
				boundingBoxObject.transform.SetAsFirstSibling();
				boundingBoxRect.anchorMin = new Vector2( 0.5f, 0.5f );
				boundingBoxRect.anchorMax = new Vector2( 0.5f, 0.5f );
				boundingBoxRect.pivot = new Vector2( 0.5f, 0.5f );
				boundingBoxRect.localScale = Vector3.one;

				boundingBoxObject.GetComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;

				serializedObject.FindProperty( "visibleChatBoundingBox" ).objectReferenceValue = boundingBoxRect;
				serializedObject.ApplyModifiedProperties();
			}

			// If there isn't a chat content box, create one.
			if( serializedObject.FindProperty( "chatContentBox" ).objectReferenceValue == null )
			{
				if( isInProjectWindow )
				{
					Debug.LogError( FormatDebug( "This chat box does not have the basic needed objects to function. The needed objects cannot be created within the project window.", "Please drag this prefab into the scene and create all the needed objects and then apply the changes to the prefab before continuing.", targ.gameObject.name ) );
					return;
				}

				GameObject contentBoxObject = new GameObject( "Chat Content Box", typeof( RectTransform ) );
				Undo.RegisterCreatedObjectUndo( contentBoxObject, "Create Ultimate Chat Box Objects" );
				RectTransform contentBoxRect = contentBoxObject.GetComponent<RectTransform>();

				contentBoxObject.transform.SetParent( targ.VisibleChatBoundingBox.transform );
				contentBoxRect.anchorMin = new Vector2( 0.5f, 1f );
				contentBoxRect.anchorMax = new Vector2( 0.5f, 1f );
				contentBoxRect.pivot = new Vector2( 0.5f, 1f );
				contentBoxRect.localScale = Vector3.one;

				serializedObject.FindProperty( "chatContentBox" ).objectReferenceValue = contentBoxRect;
				serializedObject.ApplyModifiedProperties();
			}

			// If there isn't a text object, then create one.
			if( serializedObject.FindProperty( "textObject" ).objectReferenceValue == null )
			{
				if( isInProjectWindow )
				{
					Debug.LogError( FormatDebug( "This chat box does not have the basic needed objects to function. The needed objects cannot be created within the project window.", "Please drag this prefab into the scene and create all the needed objects and then apply the changes to the prefab before continuing.", targ.gameObject.name ) );
					return;
				}

				GameObject textGameObject = new GameObject( "Text Object", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( TextMeshProUGUI ) );
				RectTransform textObjectRect = textGameObject.GetComponent<RectTransform>();

				textGameObject.transform.SetParent( targ.ChatContentBox.transform );
				textObjectRect.anchorMin = new Vector2( 0.5f, 1.0f );
				textObjectRect.anchorMax = new Vector2( 0.5f, 1.0f );
				textObjectRect.pivot = new Vector2( 0.5f, 1.0f );
				textObjectRect.localScale = Vector3.one;

				textObject = textGameObject.GetComponent<TextMeshProUGUI>();
				textObject.color = serializedObject.FindProperty( "textColor" ).colorValue;

				if( targ.EmojiAsset != null )
					textObject.spriteAsset = targ.EmojiAsset;

				serializedObject.FindProperty( "textObject" ).objectReferenceValue = textObject;
				serializedObject.ApplyModifiedProperties();

				Undo.RegisterCreatedObjectUndo( textGameObject, "Create Text Object" );
			}

			// If the canvas group is unassigned, then assign it.
			if( serializedObject.FindProperty( "chatBoxCanvasGroup" ).objectReferenceValue == null )
			{
				serializedObject.FindProperty( "chatBoxCanvasGroup" ).objectReferenceValue = targ.GetComponent<CanvasGroup>();
				serializedObject.ApplyModifiedProperties();
			}

			UpdateTextObjectReferences();

			UpdateUsernameHighlightReferences();

			if( serializedObject.FindProperty( "useTextEmoji" ).boolValue && targ.EmojiAsset != null && targ.EmojiAsset.spriteCharacterTable.Count > 0 )
			{
				emojiAssetHorizontalBearing = targ.EmojiAsset.spriteCharacterTable[ 0 ].glyph.metrics.horizontalBearingX;
				emojiAssetVerticalBearing = targ.EmojiAsset.spriteCharacterTable[ 0 ].glyph.metrics.horizontalBearingY;

				if( serializedObject.FindProperty( "useEmojiWindow" ).boolValue && emojiText != null )
					emojiText.ForceMeshUpdate();
			}

			UpdateScrollbarReferences();
			UpdateNavigationArrowReferences();

			UpdateInputFieldReferences();
			UpdateExtraImageReferences();
			UpdateEmojiButtonReferences();
			UpdateEmojiWindowReferences();

			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorDefault" ), out colorDefault );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorValueChanged" ), out colorValueChanged );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorChatVisibleHex" ), out colorChatVisible );
			ColorUtility.TryParseHtmlString( EditorPrefs.GetString( "UCB_ColorChatInvisibleHex" ), out colorChatInvisible );
		}

		void CheckForParentCanvas ()
		{
			if( targ == null || Selection.activeGameObject == null || Selection.activeGameObject != targ.gameObject )
				return;

			if( targ.ParentCanvas == null && !AssetDatabase.Contains( Selection.activeGameObject ) && targ.gameObject.activeInHierarchy )
			{
				if( Application.isPlaying )
				{
					Debug.LogWarning( "Ultimate Radial Menu - There is no enabled canvas as a parent of this radial menu." );
					return;
				}

				// Store all canvas objects to check the render mode options.
#if UNITY_2023_1_OR_NEWER
				UnityEngine.Canvas[] allCanvas = Object.FindObjectsByType( typeof( UnityEngine.Canvas ), FindObjectsSortMode.None ) as UnityEngine.Canvas[];
#else
				UnityEngine.Canvas[] allCanvas = Object.FindObjectsOfType( typeof( UnityEngine.Canvas ) ) as UnityEngine.Canvas[];
#endif

				// Loop through each canvas.
				for( int i = 0; i < allCanvas.Length; i++ )
				{
					// Check to see if this canvas is set to Screen Space and it is enabled. Then set the parent and check for an event system.
					if( allCanvas[ i ].renderMode == RenderMode.ScreenSpaceOverlay && allCanvas[ i ].enabled )
					{
						Undo.SetTransformParent( Selection.activeGameObject.transform, allCanvas[ i ].transform, "Update Chat Box Parent" );
						CheckForEventSystem();
						return;
					}
				}

				// If there have been no canvas objects found for this child, then create a new one.
				GameObject newCanvasObject = new GameObject( "Canvas", typeof( RectTransform ), typeof( UnityEngine.Canvas ), typeof( CanvasScaler ), typeof( GraphicRaycaster ) );
				newCanvasObject.layer = LayerMask.NameToLayer( "UI" );
				newCanvasObject.GetComponent<UnityEngine.Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

				Undo.RegisterCreatedObjectUndo( newCanvasObject, "Create New Canvas" );
				Undo.SetTransformParent( Selection.activeGameObject.transform, newCanvasObject.transform, "Create New Canvas" );
				CheckForEventSystem();
			}
		}

		void CheckForEventSystem ()
		{
#if UNITY_2023_1_OR_NEWER
			Object esys = FindAnyObjectByType<EventSystem>();
#else
			Object esys = FindObjectOfType<EventSystem>();
#endif
			if( esys == null )
			{
				GameObject eventSystem = new GameObject( "EventSystem" );
				esys = eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
			eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
				eventSystem.AddComponent<StandaloneInputModule>();
#endif
				Undo.RegisterCreatedObjectUndo( eventSystem, "Create EventSystem" );
			}
		}

		// -----------------------< UPDATE REFERENCE FUNCTIONS >----------------------- //
		void UpdateTextObjectReferences ()
		{
			if( targ == null || targ.TextObject == null )
				return;

			textObject = targ.TextObject;
			textFontSize = targ.TextObject.fontSize;
			textFont = targ.TextObject.font;
		}

		void UpdateUsernameHighlightReferences ()
		{
			if( serializedObject.FindProperty( "interactableUsernameImage" ).objectReferenceValue == null )
				return;

			interactableUsernameImage = ( Image )serializedObject.FindProperty( "interactableUsernameImage" ).objectReferenceValue;
			usernameHighlightSprite = interactableUsernameImage.sprite;
		}

		void UpdateScrollbarReferences ()
		{
			if( serializedObject.FindProperty( "scrollbarBase" ).objectReferenceValue == null )
				return;

			scrollbarBaseImage = ( ( RectTransform )serializedObject.FindProperty( "scrollbarBase" ).objectReferenceValue ).GetComponent<Image>();
			scrollbarBaseSprite = scrollbarBaseImage.sprite;
			scrollbarBaseColor = scrollbarBaseImage.color;

			if( serializedObject.FindProperty( "scrollbarCanvasGroup" ).objectReferenceValue == null )
			{
				serializedObject.FindProperty( "scrollbarCanvasGroup" ).objectReferenceValue = scrollbarBaseImage.GetComponent<CanvasGroup>();
				serializedObject.ApplyModifiedProperties();
			}

			if( serializedObject.FindProperty( "scrollbarHandle" ).objectReferenceValue == null )
				return;

			scrollbarHandleImage = ( ( RectTransform )serializedObject.FindProperty( "scrollbarHandle" ).objectReferenceValue ).GetComponent<Image>();
			scrollbarHandleSprite = scrollbarHandleImage.sprite;

			if( serializedObject.FindProperty( "scrollbarHandleImage" ).objectReferenceValue == null )
			{
				serializedObject.FindProperty( "scrollbarHandleImage" ).objectReferenceValue = scrollbarHandleImage;
				serializedObject.ApplyModifiedProperties();
			}
		}

		void UpdateNavigationArrowReferences ()
		{
			if( serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue != null )
			{
				navigationArrowImageUp = ( Image )serializedObject.FindProperty( "navigationArrowUp" ).objectReferenceValue;
				scrollbarNavigationArrowSprite = navigationArrowImageUp.sprite;
			}

			if( serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue != null )
				navigationArrowImageDown = ( Image )serializedObject.FindProperty( "navigationArrowDown" ).objectReferenceValue;
		}

		void UpdateInputFieldReferences ()
		{
			if( targ.InputField == null )
				return;

			inputFieldImage = targ.InputField.GetComponent<Image>();
			inputFieldImageColor = inputFieldImage.color;
			inputFieldImageSprite = inputFieldImage.sprite;
			inputFieldFont = targ.InputField.fontAsset;

			if( targ.InputField.textComponent != null )
			{
				inputFieldTextColor = targ.InputField.textComponent.color;
				inputFieldTextSize = targ.InputField.textComponent.fontSize;
			}

			if( targ.InputField.placeholder != null )
			{
				inputFieldPlaceholderText = targ.InputField.placeholder.GetComponent<TextMeshProUGUI>();
				inputFieldPlaceholderTextValue = inputFieldPlaceholderText.text;
				inputFieldPlaceholderTextColor = inputFieldPlaceholderText.color;
			}
		}

		void UpdateExtraImageReferences ()
		{
			if( serializedObject.FindProperty( "extraImage" ).objectReferenceValue == null )
				return;

			extraImage = ( Image )serializedObject.FindProperty( "extraImage" ).objectReferenceValue;
			extraImageSprite = targ.ExtraImageSprite;
			extraImageColor = targ.ExtraImageColor;
		}

		void UpdateEmojiButtonReferences ()
		{
			if( serializedObject.FindProperty( "emojiButtonImage" ).objectReferenceValue == null )
				return;

			emojiButtonImage = ( Image )serializedObject.FindProperty( "emojiButtonImage" ).objectReferenceValue;
			emojiButtonSprite = targ.EmojiButtonSprite;
			emojiButtonColor = targ.EmojiButtonColor;
		}

		void UpdateEmojiWindowReferences ()
		{
			if( serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue == null )
				return;

			emojiWindowImage = ( Image )serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue;
			emojiWindowSprite = emojiWindowImage.sprite;
			emojiWindowColor = emojiWindowImage.color;

			// If the emoji text is unassigned, then attempt to find a text component in children and try to assign it. Otherwise, create the emoji text.
			if( serializedObject.FindProperty( "emojiText" ).objectReferenceValue == null )
			{
				if( emojiWindowImage.GetComponentInChildren<TextMeshProUGUI>() )
				{
					serializedObject.FindProperty( "emojiText" ).objectReferenceValue = emojiWindowImage.GetComponentInChildren<TextMeshProUGUI>();
					serializedObject.ApplyModifiedProperties();
				}

				if( serializedObject.FindProperty( "emojiText" ).objectReferenceValue == null )
					CreateEmojiWindow();
			}
			// Else the emoji text is assigned, so store the local variables.
			else
			{
				emojiText = ( TextMeshProUGUI )serializedObject.FindProperty( "emojiText" ).objectReferenceValue;
				textEdgePadding = emojiText.margin.x;
				characterSpacing = emojiText.characterSpacing;
				lineSpacing = emojiText.lineSpacing;
			}

			if( serializedObject.FindProperty( "emojiWindowCanvasGroup" ).objectReferenceValue == null )
				serializedObject.FindProperty( "emojiWindowCanvasGroup" ).objectReferenceValue = emojiWindowImage.GetComponent<CanvasGroup>();

			serializedObject.ApplyModifiedProperties();
		}

		void UpdateEmojiWindowText ()
		{
			if( targ.EmojiAsset == null )
				return;

			if( serializedObject.FindProperty( "useEmojiWindow" ).boolValue && emojiText )
			{
				Undo.RecordObject( emojiText, "Update Emoji Window Text" );
				emojiText.spriteAsset = targ.EmojiAsset;
				emojiText.text = "";
				for( int i = 0; i < targ.EmojiAsset.spriteCharacterTable.Count; i++ )
				{
					if( i > 0 && i % serializedObject.FindProperty( "emojiPerRow" ).intValue == 0 )
						emojiText.text += "\n";

					emojiText.text += $"<sprite={i}>";
				}
				emojiText.ForceMeshUpdate();
			}
		}
		// ---------------------< END UPDATE REFERENCE FUNCTIONS >--------------------- //

		// ----------------------< EDITOR GUI HELPER FUNCTIONS >----------------------- //
		bool DisplayHeaderDropdown ( string headerName, string editorPref )
		{
			EditorGUILayout.Space();

			GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11 };
			GUILayout.BeginHorizontal();
			GUILayout.Space( -10 );
			EditorPrefs.SetBool( editorPref, GUILayout.Toggle( EditorPrefs.GetBool( editorPref ), ( EditorPrefs.GetBool( editorPref ) ? "▼ " : "► " ) + headerName, toolbarStyle ) );
			GUILayout.EndHorizontal();

			if( EditorPrefs.GetBool( editorPref ) )
			{
				EditorGUILayout.Space();
				return true;
			}

			return false;
		}

		bool DisplayCollapsibleBoxSection ( string sectionTitle, string editorPref, bool error = false )
		{
			if( error )
				sectionTitle += " <color=#ff0000ff>*</color>";

			EditorGUILayout.BeginVertical( "Box" );

			if( EditorPrefs.GetBool( editorPref ) )
				collapsableSectionStyle.fontStyle = FontStyle.Bold;

			if( GUILayout.Button( sectionTitle, collapsableSectionStyle ) )
				EditorPrefs.SetBool( editorPref, !EditorPrefs.GetBool( editorPref ) );

			if( EditorPrefs.GetBool( editorPref ) )
				collapsableSectionStyle.fontStyle = FontStyle.Normal;

			return EditorPrefs.GetBool( editorPref );
		}

		bool DisplayCollapsibleBoxSection ( string sectionTitle, string editorPref, SerializedProperty enabledProp, ref bool valueChanged, bool error = false )
		{
			valueChanged = false;

			if( error )
				sectionTitle += " <color=#ff0000ff>*</color>";

			EditorGUILayout.BeginVertical( "Box" );

			if( EditorPrefs.GetBool( editorPref ) && enabledProp.boolValue )
				collapsableSectionStyle.fontStyle = FontStyle.Bold;

			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginChangeCheck();
			enabledProp.boolValue = EditorGUILayout.Toggle( enabledProp.boolValue, GUILayout.Width( 25 ) );
			if( EditorGUI.EndChangeCheck() )
			{
				serializedObject.ApplyModifiedProperties();

				if( enabledProp.boolValue )
					EditorPrefs.SetBool( editorPref, true );
				else
					EditorPrefs.SetBool( editorPref, false );

				valueChanged = true;
			}

			GUILayout.Space( -25 );

			EditorGUI.BeginDisabledGroup( !enabledProp.boolValue );
			if( GUILayout.Button( sectionTitle, collapsableSectionStyle ) )
				EditorPrefs.SetBool( editorPref, !EditorPrefs.GetBool( editorPref ) );
			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();

			if( EditorPrefs.GetBool( editorPref ) )
				collapsableSectionStyle.fontStyle = FontStyle.Normal;

			return EditorPrefs.GetBool( editorPref ) && enabledProp.boolValue;
		}

		void EndSection ( string editorPref )
		{
			if( EditorPrefs.GetBool( editorPref ) )
				GUILayout.Space( 1 );
			else if( DragAndDropHover() )
				EditorPrefs.SetBool( editorPref, true );

			EditorGUILayout.EndVertical();
		}

		bool DragAndDropHover ()
		{
			if( disableDragAndDrop )
				return false;

			if( DragAndDrop.objectReferences.Length == 0 )
			{
				dragAndDropStartTime = 0.0f;
				dragAndDropCurrentTime = 0.0f;
				isDraggingObject = false;
				return false;
			}

			isDraggingObject = true;

			var rect = GUILayoutUtility.GetLastRect();
			if( Event.current.type == EventType.Repaint && rect.Contains( Event.current.mousePosition ) )
			{
				if( dragAndDropStartTime == 0.0f )
				{
					dragAndDropStartTime = EditorApplication.timeSinceStartup;
					dragAndDropCurrentTime = 0.0f;
				}

				if( dragAndDropMousePos == Event.current.mousePosition )
					dragAndDropCurrentTime = EditorApplication.timeSinceStartup - dragAndDropStartTime;
				else
				{
					dragAndDropStartTime = EditorApplication.timeSinceStartup;
					dragAndDropCurrentTime = 0.0f;
				}

				if( dragAndDropCurrentTime >= 0.5f )
				{
					dragAndDropStartTime = 0.0f;
					dragAndDropCurrentTime = 0.0f;
					return true;
				}

				dragAndDropMousePos = Event.current.mousePosition;
			}

			return false;
		}

		void BeginPreviousPropertyHoverCheck ()
		{
			propertyRect = new Rect();
			propertyRect = GUILayoutUtility.GetLastRect();
		}

		void EndPreviousPropertyHoverCheck ( DisplaySceneGizmo displaySceneGizmo )
		{
			displaySceneGizmo.hover = false;
			propertyRect.yMax = GUILayoutUtility.GetLastRect().yMax;
			if( Event.current.type == EventType.Repaint && propertyRect.Contains( Event.current.mousePosition ) )
			{
				displaySceneGizmo.hover = true;
				isDirty = true;
			}
		}

		void BeginDisableInteractionCheck ()
		{
			if( disableInteraction )
			{
				EditorGUILayout.HelpBox( "Input cannot be calculated correctly for chat boxes in a Canvas that is not using the Screen Space - Overlay render mode.", MessageType.Warning );
				EditorGUI.BeginDisabledGroup( disableInteraction );
			}
		}

		void EndDisableInteractionCheck ()
		{
			if( disableInteraction )
			{
				EditorGUI.EndDisabledGroup();
			}
		}

		void GUILayoutAfterIndentSpace ()
		{
			GUILayout.Space( 2 );
		}

		static string FormatDebug ( string error, string solution, string objectName )
		{
			return "<b>Ultimate Chat Box Editor</b>\n" +
				"<color=red><b>×</b></color> <i><b>Error:</b></i> " + error + ".\n" +
				"<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".\n" +
				"<color=cyan><b>∙</b></color> <i><b>Object:</b></i> " + objectName + "\n";
		}
		// --------------------< END EDITOR GUI HELPER FUNCTIONS >--------------------- //

		void AdjustChildIndexForOptions ()
		{
			if( targ == null || targ.VisibleChatBoundingBox == null )
				return;

			targ.VisibleChatBoundingBox.SetAsFirstSibling();

			if( serializedObject.FindProperty( "useScrollbar" ).boolValue && scrollbarBaseImage != null )
				scrollbarBaseImage.transform.SetSiblingIndex( targ.VisibleChatBoundingBox.GetSiblingIndex() + 1 );

			if( serializedObject.FindProperty( "useInputField" ).boolValue && inputFieldImage != null )
			{
				if( serializedObject.FindProperty( "useScrollbar" ).boolValue && scrollbarBaseImage != null )
					inputFieldImage.transform.SetSiblingIndex( scrollbarBaseImage.transform.GetSiblingIndex() + 1 );
				else
					inputFieldImage.transform.SetSiblingIndex( targ.VisibleChatBoundingBox.GetSiblingIndex() + 1 );
			}
		}

		void CreateScrollbarHandle ( string undoMessage = "Create Scrollbar Handle" )
		{
			GameObject scrollbarHandleObject = new GameObject( "Scrollbar Handle", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( UnityEngine.UI.Image ) );
			Undo.RegisterCreatedObjectUndo( scrollbarHandleObject, undoMessage );
			RectTransform scrollbarHandleRect = scrollbarHandleObject.GetComponent<RectTransform>();

			scrollbarHandleObject.transform.SetParent( scrollbarBaseImage.transform );
			scrollbarHandleRect.anchorMin = new Vector2( 0.5f, 1f );
			scrollbarHandleRect.anchorMax = new Vector2( 0.5f, 1f );
			scrollbarHandleRect.pivot = new Vector2( 0.5f, 1f );
			scrollbarHandleRect.localScale = Vector3.one;

			scrollbarHandleImage = scrollbarHandleObject.GetComponent<Image>();
			scrollbarHandleImage.sprite = scrollbarHandleSprite;
			scrollbarHandleImage.color = serializedObject.FindProperty( "scrollbarHandleNormalColor" ).colorValue;

			if( scrollbarHandleSprite != null && scrollbarHandleSprite.border != Vector4.zero )
				scrollbarHandleImage.type = Image.Type.Sliced;

			serializedObject.FindProperty( "scrollbarHandleImage" ).objectReferenceValue = scrollbarHandleImage;
			serializedObject.FindProperty( "scrollbarHandle" ).objectReferenceValue = scrollbarHandleRect;
			serializedObject.ApplyModifiedProperties();

			UpdateScrollbarReferences();
		}

		void CreateEmojiWindow ( string undoMessage = "Create Emoji Window" )
		{
			if( serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue == null )
			{
				// EMOJI WINDOW //
				GameObject emojiWindowObject = new GameObject( "Emoji Window", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( UnityEngine.UI.Image ), typeof( CanvasGroup ) );
				Undo.RegisterCreatedObjectUndo( emojiWindowObject, undoMessage );
				RectTransform emojiWindowRect = emojiWindowObject.GetComponent<RectTransform>();

				emojiWindowObject.transform.SetParent( targ.InputField.transform );
				emojiWindowRect.anchorMin = new Vector2( 1.0f, 0.0f );
				emojiWindowRect.anchorMax = new Vector2( 1.0f, 0.0f );
				emojiWindowRect.pivot = new Vector2( 0.0f, 0.0f );
				emojiWindowRect.localScale = Vector3.one;

				emojiWindowImage = emojiWindowObject.GetComponent<Image>();

				if( emojiWindowSprite == null && chatBoxImageSprite != null )
				{
					emojiWindowSprite = chatBoxImageSprite;
					emojiWindowColor = chatBoxImageColor;
				}

				emojiWindowImage.sprite = emojiWindowSprite;
				emojiWindowImage.color = emojiWindowColor;

				if( emojiWindowSprite != null && emojiWindowSprite.border != Vector4.zero )
					emojiWindowImage.type = Image.Type.Sliced;

				emojiWindowImage.raycastTarget = false;

				serializedObject.FindProperty( "emojiWindowImage" ).objectReferenceValue = emojiWindowImage;
				serializedObject.ApplyModifiedProperties();
			}

			if( serializedObject.FindProperty( "emojiText" ).objectReferenceValue == null )
			{
				// EMOJI DISPLAY //
				GameObject emojiTextObject = new GameObject( "Emoji Text", typeof( RectTransform ), typeof( CanvasRenderer ), typeof( TextMeshProUGUI ) );
				Undo.RegisterCreatedObjectUndo( emojiTextObject, undoMessage );
				RectTransform emojiTextRect = emojiTextObject.GetComponent<RectTransform>();

				emojiTextObject.transform.SetParent( emojiWindowImage.transform );
				emojiTextRect.anchorMin = new Vector2( 0.0f, 0.0f );
				emojiTextRect.anchorMax = new Vector2( 1.0f, 1.0f );
				emojiTextRect.pivot = new Vector2( 0.5f, 0.5f );
				emojiTextRect.localScale = Vector3.one;
				emojiTextRect.offsetMax = Vector2.zero;
				emojiTextRect.offsetMin = Vector2.zero;

				emojiText = emojiTextObject.GetComponent<TextMeshProUGUI>();
				emojiText.alignment = TextAlignmentOptions.Center;
				emojiText.fontSizeMin = 1;
				emojiText.fontSizeMax = 300;
				emojiText.enableAutoSizing = true;
				emojiText.raycastTarget = false;

				serializedObject.FindProperty( "emojiText" ).objectReferenceValue = emojiText;
				serializedObject.ApplyModifiedProperties();
			}

			UpdateEmojiWindowReferences();
			UpdateEmojiWindowText();
		}

		Rect CalculateRect ( RectTransform referenceTransform )
		{
			return new Rect( referenceTransform.position - ( Vector3 )( ( referenceTransform.sizeDelta * referenceTransform.pivot ) * parentCanvasTransform.localScale ), referenceTransform.sizeDelta * parentCanvasTransform.localScale );
		}

		[MenuItem( "GameObject/UI/Ultimate Chat Box" )]
		public static void CreateChatBoxFromScratch ()
		{
			GameObject chatBoxObject = new GameObject( "Ultimate Chat Box", typeof( RectTransform ), typeof( Image ), typeof( CanvasGroup ) );
			chatBoxObject.layer = LayerMask.NameToLayer( "UI" );

			UltimateChatBox chatBox = chatBoxObject.AddComponent<UltimateChatBox>();
			Selection.activeGameObject = chatBoxObject;
			Undo.RegisterCreatedObjectUndo( chatBoxObject, "Create Ultimate Chat Box Object" );
		}
	}
}