/* ChatStylePropertyDrawer.cs */
/* Written by Kaz */
namespace TankAndHealerStudioAssets
{
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer( typeof( UltimateChatBox.ChatStyle ) )]
	public class ChatStylePropertyDrawer : PropertyDrawer
	{
		bool showMore = false, customUsernameColor = false, customMessageColor = false;

		public override float GetPropertyHeight ( SerializedProperty property, GUIContent label )
		{
			int lineCount = 1;
			float endSpacingModifier = 0;
			if( showMore )
			{
				endSpacingModifier = EditorGUIUtility.singleLineHeight;
				lineCount = 13;

				if( customUsernameColor )
					lineCount++;

				if( customMessageColor )
					lineCount++;
			}

			return EditorGUIUtility.singleLineHeight * lineCount + ( ( lineCount * 2 ) + endSpacingModifier );
		}

		public override void OnGUI ( Rect position, SerializedProperty property, GUIContent label )
		{
			EditorGUI.BeginProperty( position, label, property );

			int i = 0;

			GUIStyle toolbarStyle = new GUIStyle( EditorStyles.toolbarButton ) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold, fontSize = 11, stretchWidth = false };
			showMore = GUI.Toggle( GetNewPositionRect( EditorGUI.IndentedRect( position ), i++ ), showMore, ( showMore ? "▼" : "►" ) + label.text, toolbarStyle );

			position.y += 2;

			if( showMore )
			{
				position.y += EditorGUIUtility.singleLineHeight / 4;
				EditorGUI.indentLevel++;

				EditorGUI.LabelField( GetNewPositionRect( position, i++ ), "Basic Settings", EditorStyles.boldLabel );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "disableInteraction" ), new GUIContent( "Disable Interaction", "Tooltip" ) );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "noUsernameFollowupText" ), new GUIContent( "Disable Separator Text", "Tooltip" ) );

				position.y += EditorGUIUtility.singleLineHeight / 2;

				EditorGUI.LabelField( GetNewPositionRect( position, i++ ), "Username Settings", EditorStyles.boldLabel );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "usernameBold" ), new GUIContent( "Bold", "Tooltip" ) );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "usernameItalic" ), new GUIContent( "Italic", "Tooltip" ) );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "usernameUnderlined" ), new GUIContent( "Underlined", "Tooltip" ) );

				customUsernameColor = property.FindPropertyRelative( "usernameColor" ).colorValue != Color.clear;

				EditorGUI.BeginChangeCheck();
				customUsernameColor = EditorGUI.Toggle( GetNewPositionRect( position, i++ ), "Custom Color", customUsernameColor );
				if( EditorGUI.EndChangeCheck() )
				{
					property.FindPropertyRelative( "usernameColor" ).colorValue = customUsernameColor ? Color.white : Color.clear;
				}
				if( customUsernameColor )
					EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "usernameColor" ), new GUIContent( "Color", "Tooltip" ) );

				position.y += EditorGUIUtility.singleLineHeight / 2;

				EditorGUI.LabelField( GetNewPositionRect( position, i++ ), "Message Settings", EditorStyles.boldLabel );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "messageBold" ), new GUIContent( "Bold", "Tooltip" ) );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "messageItalic" ), new GUIContent( "Italic", "Tooltip" ) );
				EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "messageUnderlined" ), new GUIContent( "Underlined", "Tooltip" ) );

				customMessageColor = property.FindPropertyRelative( "messageColor" ).colorValue != Color.clear;

				EditorGUI.BeginChangeCheck();
				customMessageColor = EditorGUI.Toggle( GetNewPositionRect( position, i++ ), "Custom Color", customMessageColor );
				if( EditorGUI.EndChangeCheck() )
				{
					property.FindPropertyRelative( "messageColor" ).colorValue = customMessageColor ? Color.white : Color.clear;
				}
				if( customMessageColor )
					EditorGUI.PropertyField( GetNewPositionRect( position, i++ ), property.FindPropertyRelative( "messageColor" ), new GUIContent( "Color", "Tooltip" ) );

				EditorGUI.indentLevel--;
			}
			EditorGUI.EndProperty();
		}

		Rect GetNewPositionRect ( Rect position, int i, int height = 16 )
		{
			return new Rect( position.x, position.y + ( 18 * i ), position.width, height );
		}
	}
}