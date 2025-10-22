/* UltimateChatBox.cs */
/* Written by Kaz */
namespace TankAndHealerStudioAssets
{
	using TMPro;
	using System;
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections.Generic;
	using System.Text.RegularExpressions;
	#if ENABLE_INPUT_SYSTEM
	using UnityEngine.InputSystem;
	using UnityEngine.InputSystem.EnhancedTouch;
	#endif

	[ExecuteAlways]
	[RequireComponent( typeof( CanvasGroup ) )]
	public class UltimateChatBox : MonoBehaviour
	{
		// INTERNAL //
		[Serializable]
		public class ChatInformation
		{
			public UltimateChatBox chatBox;

			/// <summary>
			/// Returns the current state of this chat being visible.
			/// </summary>
			public bool IsVisible
			{
				get;
				private set;
			}
			public TextMeshProUGUI chatText;
			/// <summary>
			/// The exact username that was provided when registering a chat. If the provided username contained a number, this string includes that number for reference.
			/// </summary>
			public string Username { get; set; }
			/// <summary>
			/// The display username. If the username provided when registering a chat contained a number, then this string will be just the name portion, excluding the associated number.
			/// </summary>
			public string DisplayUsername { get; set; }
			/// <summary>
			/// The exact string value provided to the chat box for the message.
			/// </summary>
			public string Message { get; set; }
			/// <summary>
			/// The modified string value with all the users options applied to it so that it looks correct.
			/// </summary>
			public string DisplayMessage { get; set; }
			public Rect usernameRect = new Rect();
			public float contentSpace = 0.0f;
			public Vector2 anchoredPosition = Vector2.zero;
			public float usernameWidth = 0.0f;
			public float lineHeight = 0.0f;
			public int lineCount = 0;
			public ChatStyle chatBoxStyle;


			/// <summary>
			/// [INTERNAL] Updates the text component of this chat so that it can be display.
			/// </summary>
			public void UpdateText ()
			{
				// If the assigned text is null, or the provided style is null, return.
				if( chatText == null || chatBoxStyle == null )
					return;

				// Update the text component to display the chat.
				chatText.text = chatBoxStyle.FormatMessage( DisplayUsername, DisplayMessage );
			}

			/// <summary>
			/// [INTERNAL] Updates the visibility of this chat.
			/// </summary>
			public void UpdateVisibility ()
			{
				// Check the top position of the text and if the chat box contains the position then set IsVisible to true.
				if( chatBox.chatBoxVisiblityRect.Contains( anchoredPosition * -1 ) )
					IsVisible = true;
				// Else check the position of the text + the content space, and if it is within the chat box then set IsVisible to true.
				else if( chatBox.chatBoxVisiblityRect.Contains( ( anchoredPosition * -1 ) + new Vector2( 0, contentSpace - 0.01f ) ) )
					IsVisible = true;
				// Else if the content space is actually larger than the visible bounds of the chat box... 
				else if( contentSpace > chatBox.visibleChatBoundingBox.sizeDelta.y )
				{
					// Calculate difference of the visible bounds of the chat box and the space of this content.
					float differenceModifier = chatBox.visibleChatBoundingBox.sizeDelta.y / contentSpace;

					// Loop for adjusting the checks for the content space to see if this chat is indeed visible...
					for( float mod = 0.0f; mod < 1.0f; mod += differenceModifier )
					{
						// If the chat box contains the content spaced * mod above, then set IsVisible to true and return.
						if( chatBox.chatBoxVisiblityRect.Contains( ( anchoredPosition * -1 ) + new Vector2( 0, contentSpace * mod ) ) )
						{
							IsVisible = true;
							return;
						}
					}

					// Else set IsVisible to false.
					IsVisible = false;
				}
				// Else none of the other checks were true so the chat isn't visible. Set IsVisible to false.
				else
					IsVisible = false;
			}

			/// <summary>
			/// Removes this chat information from the chat box.
			/// </summary>
			public void RemoveChat ()
			{
				// If the chat text is assigned, then send it to the pool to be reused.
				if( chatText != null )
					chatBox.SendTextToPool( chatText );

				// Remove this chat information from the list.
				chatBox.ChatInformations.Remove( this );

				// Set isDirty to true so the chat box knows to update the positioning of everything.
				chatBox.isDirty = true;
			}
		}
		/// <summary>
		/// The list of all the registered chat informations.
		/// </summary>
		public List<ChatInformation> ChatInformations { get; private set; } = new List<ChatInformation>();
		/// <summary>
		/// The list of all the text objects that have been created to display the registered chat information.
		/// </summary>
		public List<TextMeshProUGUI> AllTextObjects { get; private set; } = new List<TextMeshProUGUI>();
		List<TextMeshProUGUI> UnusedTextPool = new List<TextMeshProUGUI>();
		/// <summary>
		/// The parent Canvas that this chat box is placed inside.
		/// </summary>
		public Canvas ParentCanvas { get; private set; }
		RectTransform parentCanvasRectTrans;
		Vector3 parentCanvasScale = Vector3.one;
		Vector2 parentCanvasSize = Vector2.zero;
		/// <summary>
		/// The current state of the chat box being interactable. Setting this value to <see langword="false"/>will not allow input to be processed on the chat box.
		/// </summary>
		public bool Interactable { get; set; } = true;
		/// <summary>
		/// The current state of this Ultimate Chat Box being enabled or disabled.
		/// </summary>
		public bool IsEnabled { get; private set; }
		/// <summary>
		/// Returns the state of the input being on the chat box or not.
		/// </summary>
		public bool InputOnChatBox { get; private set; }
		/// <summary>
		/// The line height of the TextObject to use for chat box navigation and chat spacing.
		/// </summary>
		public float LineHeight { get; private set; }
		float totalContentSpace = 0.0f;
		bool isDraggingScrollHandle = false;
		/// <summary>
		/// The stored position of the input for calculating on the chat box.
		/// </summary>
		public Vector3 InputPosition { get; private set; }
		Vector3 previousInputPosition = Vector2.zero;
		/// <summary>
		/// The state of the input being down on this frame for calculations.
		/// </summary>
		public bool GetButtonDown { get; private set; }
		/// <summary>
		/// The current state of the input being pressed.
		/// </summary>
		public bool GetButton { get; private set; }
		float scrollValue = 0.0f;
		/// <summary>
		/// Set the current scroll value to apply to the chat box.
		/// </summary>
		public float ScrollValue
		{
			set
			{
				scrollValue = value;
			}
		}
		/// <summary>
		/// The total size that the chat box occupies on the screen. This value includes the input field if that is used.
		/// </summary>
		public Vector2 TotalChatBoxSize { get; private set; }
		int inputFieldStringPosition = -1;
		bool isDirty = false;
		[SerializeField] [HideInInspector]
		private CanvasGroup chatBoxCanvasGroup;
		// Touch Input //
		[SerializeField] [Tooltip( "Should the chat box calculate any touch input on it?" )]
		private bool allowTouchInput = false;
		bool isDraggingWithTouch = false;
		int currentTouchId = -1;
		// Custom Input //
		bool customInputRecieved = false;
		Vector2 customScreenPosition = Vector2.zero;
		bool customGetButtonDown = false, customGetButton = false;

		// CHAT BOX POSITION //
		RectTransform baseTransform;
		/// <summary>
		/// The base transform of the Ultimate Chat Box.
		/// </summary>
		public RectTransform BaseTransform
		{
			get
			{
				if( baseTransform == null )
					baseTransform = GetComponent<RectTransform>();

				return baseTransform;
			}
		}
		[SerializeField] [Tooltip( "The ratio of the chat box." )]
		private Vector2 chatBoxSizeRatio = new Vector2( 1.0f, 0.5f );
		[SerializeField] [Tooltip( "The overall size of the chat box." )]
		private float chatBoxSize = 5.0f;
		[SerializeField] [Tooltip( "The position of the chat box on the screen. These values are calculated as percentages, so they are divided by 100 and calculated off the canvas size so that it will be consistent across all screen sizes." )]
		private Vector2 chatBoxPosition = new Vector2( 5.0f, 10.0f );
		[SerializeField] [Tooltip( "The visible bounding box for the chat in the chat box." )]
		private RectTransform visibleChatBoundingBox;
		/// <summary>
		/// Returns the RectTransform that is used as the visible mask for the chat box.
		/// </summary>
		public RectTransform VisibleChatBoundingBox
		{
			get
			{
				// If the boundingBox is null, inform the user on how to fix the issue. This should never happen though.
				if( visibleChatBoundingBox == null )
					Debug.LogError( FormatDebug( "There is no bounding box assigned to this chat box", "Please exit play mode and click on the Ultimate Chat Box in your scene. This will ensure that the bounding box object is created", gameObject.name ) );

				// Return the boundingBox RectTransform.
				return visibleChatBoundingBox;
			}
		}
		[SerializeField] [Tooltip( "The horizontal bounds for the content of the chat box." )]
		private RectTransform chatContentBox;
		/// <summary>
		/// Returns the RectTransform that contains the text objects for the chat box.
		/// </summary>
		public RectTransform ChatContentBox
		{
			get
			{
				// If the contentBox is null, inform the user on how to fix the issue. This should never happen though.
				if( chatContentBox == null )
					Debug.LogError( FormatDebug( "There is no content box assigned to this chat box", "Please exit play mode and click on the Ultimate Chat Box in your scene. This will ensure that the content box object is created", gameObject.name ) );

				// Return the contentBox RectTransform.
				return chatContentBox;
			}
		}
		[SerializeField] [Tooltip( "The horizontal spacing for the left and right of the content box." )] [Range( 0.0f, 50.0f )]
		private float horizontalSpacing = 2.0f;
		[SerializeField] [Tooltip( "The vertical spacing for the top and bottom of the bounding box." )] [Range( 0.0f, 50.0f )]
		private float verticalSpacing = 2.0f;
		[SerializeField] [Tooltip( "The position of the content within the chat box." )]
		private Vector2 contentPosition = Vector2.zero;
		[SerializeField] [HideInInspector]
		private Rect chatBoxScreenRect = new Rect();
		Rect chatBoxVisiblityRect = new Rect();
		bool chatBoxPositionCustom = false;
		Vector2 bottomContentPosition;

		// TEXT SETTINGS //
		[SerializeField] [Tooltip( "The TextMeshPro GameObject to use as base settings for all the chats in the chat box." )]
		private TextMeshProUGUI textObject;
		/// <summary>
		/// Returns the TextMeshPro GameObject used as the basis for all the chats in the chat box.
		/// </summary>
		public TextMeshProUGUI TextObject
		{
			get
			{
				return textObject;
			}
		}
		[SerializeField] [Tooltip( "The color of the text in the chat box." )]
		private Color textColor = Color.white;
		/// <summary>
		/// The color of the text in the chat box. Assigning a value here will update all the text in the chat box.
		/// </summary>
		public Color TextColor
		{
			get
			{
				// Return the current textColor.
				return textColor;
			}
			set
			{
				// Assign the provided value to the stored textColor.
				textColor = value;

				// Loop through all the text objects and apply the color.
				for( int i = 0; i < AllTextObjects.Count; i++ )
					AllTextObjects[ i ].color = textColor;
			}
		}
		[SerializeField] [Tooltip( "The maximum number of chats in the chat box before the old ones will be filtered out." )]
		private int maxTextInChatBox = 500;
		[SerializeField] [Tooltip( "Determines if the players should be able to use custom rich text in the input field or if the chat box should filter out the rich text." )]
		private bool disableRichTextFromPlayers = true;
		[SerializeField] [Tooltip( "The string to add at the end of a username." )]
		private string usernameFollowup = ": ";
		[SerializeField] [Tooltip( "The space between each chat registered to the chat box." )] [Range( 0.0f, 1.0f )]
		private float spaceBetweenChats = 0.0f;
		[SerializeField] [Range( 0.0f, 0.2f )] [Tooltip( "The relative size of the font to the chat box height." )]
		private float smartFontSize = 0.1f;
		/// <summary>
		/// Returns the calculated font size based off the user defined Smart Font Size percentage.
		/// </summary>
		public float CalculatedFontSize
		{
			get
			{
				// If the text object is unassigned, then just return zero.
				if( textObject == null )
					return 0.0f;

				// Return the current font size of the base text object.
				return textObject.fontSize;
			}
		}

		// INTERACTABLE USERNAME //
		[SerializeField] [Tooltip( "Should the usernames of the chats registered by interactable?" )]
		private bool useInteractableUsername = false;
		[SerializeField] [Tooltip( "The image used in association with highlighting the username." )]
		private Image interactableUsernameImage;
		/// <summary>
		/// The image component of the username highlight.
		/// </summary>
		public Image InteractableUsernameImage
		{
			get
			{
				return interactableUsernameImage;
			}
		}
		[SerializeField] [Tooltip( "The color of the highlight image when the input is hovering over the username." )]
		private Color interactableUsernameColor = Color.white;
		/// <summary>
		/// The color of the interactable username image. Assigning a value here will update image if there is a username currently being hovered over.
		/// </summary>
		public Color InteractableUsernameColor
		{
			get
			{
				// Return the current usernameHighlightColor.
				return interactableUsernameColor;
			}
			set
			{
				// Assign the provided value to the stored usernameHighlightColor.
				interactableUsernameColor = value;

				// If a username is currently hovered, and the image is assigned, then apply the new color.
				if( UsernameHighlighted && interactableUsernameImage != null )
					interactableUsernameImage.color = interactableUsernameColor;
			}
		}
		[SerializeField] [Tooltip( "The percentage of the line height to add as a modifier to the username highlight image." )] [Range( 0.0f, 1.0f )]
		private float interactableUsernameWidthModifier = 0.0f;
		/// <summary>
		/// The current state of the player hovering over a username in the chat box.
		/// </summary>
		public bool UsernameHighlighted { get; private set; }
		/// <summary>
		/// The index of the ChatInformation that is currently being hovered.
		/// </summary>
		public int CurrentHoveredChatIndex { get; private set; }

		// FADE WHEN DISABLED //
		[SerializeField] [Tooltip( "Should the chat box fade the alpha of the CanvasGroup when enabling/disabling the chat box?" )]
		private bool fadeWhenDisabled = false;
		[SerializeField] [Tooltip( "The speed for the chat box to fade in." )]
		private float fadeInSpeed = 4.0f;
		[SerializeField] [Tooltip( "The speed for the chat box to fade out." )]
		private float fadeOutSpeed = 4.0f;
		[SerializeField] [Range( 0.0f, 1.0f )] [Tooltip( "The alpha to apply to the chat box when it is disabled." )]
		private float toggledAlpha = 0.25f;
		/// <summary>
		/// Determines if the text inside the chat box should remain fully visible even when the chat box itself is disabled.
		/// </summary>
		public bool LeaveTextVisible
		{
			get
			{
				return visibleChatBoundingBox.GetComponent<CanvasGroup>().ignoreParentGroups;
			}
			set
			{
				visibleChatBoundingBox.GetComponent<CanvasGroup>().ignoreParentGroups = value;
			}
		}
		bool fadeIn = false, fadeOut = false;
		float fadeLerpValue = 0.0f;

		// COLLAPSE WHEN DISABLED //
		[SerializeField] [Tooltip( "Should the chat box collapse when enabling/disabling the chat box?" )]
		private bool collapseWhenDisabled = false;
		[SerializeField] [Tooltip( "The speed for the chat box to expand." )]
		private float expandSpeed = 4.0f;
		[SerializeField] [Tooltip( "The speed for the chat box to collapse." )]
		private float collapseSpeed = 4.0f;
		[SerializeField] [Tooltip( "How many lines of chat should be visible when the chat box is in a collapsed state?" )]
		private int visibleLineCount = 3;
		Vector2 baseTransformSize = Vector2.zero;
		Vector2 baseTransformCollapsedSize = Vector2.zero;
		Vector2 visibleBoundingBoxSize = Vector2.zero;
		Vector2 boundingBoxCollapsedSize = Vector2.zero;
		bool expandChatBox = false, collapseChatBox = false;
		float collapseLerpValue;

		// TEXT EMOJI //
		[SerializeField] [Tooltip( "Should emojis be allowed in chat?" )]
		private bool useTextEmoji = false;
		[SerializeField] [Tooltip( "The emoji asset to assign to all the chats." )]
		private TMP_SpriteAsset emojiAsset;
		/// <summary>
		/// Returns the assigned TextMeshPro sprite asset.
		/// </summary>
		public TMP_SpriteAsset EmojiAsset
		{
			get
			{
				return emojiAsset;
			}
		}

		// VERTICAL SCROLLBAR //
		[SerializeField] [Tooltip( "Should the chat box display a vertical scrollbar to help navigate chat?" )]
		private bool useScrollbar = false;
		[SerializeField]
		private RectTransform scrollbarBase = null, scrollbarHandle = null;
		[SerializeField]
		private Image scrollbarHandleImage;
		[SerializeField] [Tooltip( "The default color for the scrollbar handle." )]
		private Color scrollbarHandleNormalColor = Color.white;
		[SerializeField] [Tooltip( "The color for when the input is hovering the scrollbar handle." )]
		private Color scrollbarHandleHoverColor = Color.white;
		[SerializeField] [Tooltip( "The color of the scrollbar handle when the input is active." )]
		private Color scrollbarHandleActiveColor = Color.white;
		[SerializeField] [Tooltip( "Should the player be able to use the scroll wheel on their mouse to navigate the chat box?" )]
		private bool useScrollWheel = true;
		[SerializeField] [Tooltip( "The speed that the chat box will navigate using the scroll wheel." )]
		private float mouseScrollSpeed = 1.0f;
		[SerializeField] [Tooltip( "The minimum size that the scrollbar handle will get when the chat box is filled with chats." )] [Range( 0.01f, 0.25f )]
		private float scrollbarMinimumSize = 0.15f;
		[SerializeField] [Tooltip( "The width of the scrollbar relative to the chat box width." )] [Range( 0.01f, 0.25f )]
		private float scrollbarWidth = 0.02f;
		[SerializeField] [Tooltip( "The horizontal position of the scrollbar relative to the chat box center." )]
		private float scrollbarHorizontalPosition = 49.0f;
		[SerializeField] [Tooltip( "Should the scrollbar only be visible when hovering input over the chat box?" )]
		private bool visibleOnlyOnHover = false;
		[SerializeField] [Tooltip( "The speed to apply for the scrollbar to toggle visually." )]
		private float scrollbarToggleSpeed = 4.0f;
		[SerializeField] [Tooltip( "The time in seconds that the input has NOT been in the chat box for the scrollbar to disable itself." )]
		private float scrollbarInactiveTime = 1.0f;
		[SerializeField] [Tooltip( "Should clicking on the base of the scrollbar to navigate be disabled?" )]
		private bool disableBaseNavigation = false;
		[SerializeField] [HideInInspector]
		private Rect scrollbarBaseRect = new Rect(), scrollbarHandleRect = new Rect();
		[SerializeField] [HideInInspector]
		private CanvasGroup scrollbarCanvasGroup;
		/// <summary>
		/// The current state of the scrollbar being visible or not.
		/// </summary>
		public bool ScrollbarActive { get; private set; }
		Vector2 scrollbarHandleInputStart = Vector2.zero;
		Vector2 scrollbarBottomPosition;
		float scrollbarToggleLerpValue = 0.0f, _inactiveTime = 0.0f;
		bool scrollbarToggle = false, scrollbarHandleHovered = false, scrollbarHandleActive = false;

		// NAVIGATION ARROWS //
		[SerializeField] [Tooltip( "Determines if you want to have navigation arrows at the top and bottom of the scrollbar to help navigate the chat box." )]
		private bool useNavigationArrows = false;
		[SerializeField] [Tooltip( "The image component to use for the up arrow." )]
		private Image navigationArrowUp;
		[SerializeField] [Tooltip( "The image component to use for the down arrow." )]
		private Image navigationArrowDown;
		[SerializeField] [Tooltip( "The default color for the navigation arrows to be." )]
		private Color navigationNormalColor = Color.white;
		[SerializeField] [Tooltip( "The color to apply when the navigation arrows are hovered." )]
		private Color navigationHoverColor = Color.white;
		[SerializeField] [Tooltip( "The color to apply when the input is active on the navigation arrows." )]
		private Color navigationActiveColor = Color.white;
		[SerializeField] [Tooltip( "The time in seconds before the navigation arrows will repeat the navigation when the input is held." )]
		private float navigationInitialHoldDelay = 0.25f;
		[SerializeField] [Tooltip( "The time in seconds between repeating navigation from holding the input on the navigation arrows." )]
		private float navigationIntervalDelay = 0.1f;
		[SerializeField] [HideInInspector]
		private Rect scrollbarNavigationArrowUpRect = new Rect(), scrollbarNavigationArrowDownRect = new Rect();
		bool scrollbarNavigationArrowUpHovered = false, scrollbarNavigationArrowUpActivated = false;
		bool scrollbarNavigationArrowDownHovered = false, scrollbarNavigationArrowDownActivated = false;
		float navigationIntervalTime = 0.0f;
		float navigationInitialHoldTime = 0.0f;

		// INPUT FIELD //
		[SerializeField] [Tooltip( "Should an input field be available for players to use?" )]
		private bool useInputField = false;
		[SerializeField] [Tooltip( "The input field component to use for the chat box." )]
		private TMP_InputField inputField = null;
		/// <summary>
		/// The input field component used in connection with the chat box.
		/// </summary>
		public TMP_InputField InputField
		{
			get
			{
				return inputField;
			}
		}
		[SerializeField] [Tooltip( "The size of the input field relative to the chat box." )]
		private Vector2 inputFieldSize = new Vector2( 100, 12.5f );
		[SerializeField] [Tooltip( "The position of the input field relative to the chat box." )]
		private Vector2 inputFieldPosition = new Vector2( 0, -2 );
		[SerializeField] [Range( 0.0f, 1.0f )] [Tooltip( "The relative size of the font to the input field height." )]
		private float inputFieldSmartFontSize = 1.0f;
		[SerializeField] [Tooltip( "The size of the input field text area relative to the input field transform." )]
		private Vector2 inputFieldTextAreaSize = new Vector2( 95.0f, 95.0f );
		[SerializeField] [Range( -50.0f, 50.0f )]
		private float inputFieldTextHorizontalPosition = 0.0f;
		[SerializeField] [HideInInspector]
		private RectTransform inputFieldTransform;
		[SerializeField] [HideInInspector]
		private Rect inputFieldRect = new Rect();
		/// <summary>
		/// Returns the current state of the input field having focus or not.
		/// </summary>
		public bool InputFieldEnabled { get; private set; }
		string inputFieldValue = string.Empty;
		/// <summary>
		/// The current string value of the input field.
		/// </summary>
		public string InputFieldValue
		{
			get => inputFieldValue;
			set
			{
				inputFieldValue = value;
				inputField.text = inputFieldValue;
				inputField.caretPosition = inputFieldValue.Length;
			}
		}
		/// <summary>
		/// Returns if the current input field value contains a command value or not.
		/// </summary>
		public bool InputFieldContainsCommand { get; private set; }

		// EXTRA IMAGE //
		[SerializeField] [Tooltip( "Should an extra image be used inside the input field?" )]
		private bool useExtraImage = false;
		[SerializeField] [Tooltip( "The image component to use as the extra image." )]
		private Image extraImage;
		[SerializeField] [HideInInspector]
		private Rect extraImageRect = new Rect();
		/// <summary>
		/// The sprite associated with the extra image.
		/// </summary>
		public Sprite ExtraImageSprite
		{
			get
			{
				if( !useExtraImage || extraImage == null )
					return null;

				return extraImage.sprite;
			}
			set
			{
				if( useExtraImage && extraImage != null )
					extraImage.sprite = value;
			}
		}
		/// <summary>
		/// The extra image color.
		/// </summary>
		public Color ExtraImageColor
		{
			get
			{
				if( !useExtraImage || extraImage == null )
					return Color.clear;

				return extraImage.color;
			}
			set
			{
				if( useExtraImage && extraImage != null )
					extraImage.color = value;
			}
		}
		[SerializeField] [Range( 0.0f, 100.0f )] [Tooltip( "The height of the input field extra image." )]
		private float extraImageHeight = 100.0f;
		[SerializeField] [Range( 0.0f, 100.0f )] [Tooltip( "The width of the input field extra image." )]
		private float extraImageWidth = 10.0f;
		[SerializeField] [Range( -50.0f, 50.0f )] [Tooltip( "The horizontal position in relation to the input field." )]
		private float extraImageHorizontalPosition = -50.0f;

		// EMOJI WINDOW //
		[SerializeField] [Tooltip( "Should the players be able to add emojis through a provided window?" )]
		private bool useEmojiWindow = false;
		[SerializeField] [Tooltip( "The image component to be used as a button to open the emoji window." )]
		private Image emojiButtonImage;
		[SerializeField] [Tooltip( "The size of the button." )]
		private float emojiButtonSize = 0.9f;
		[SerializeField] [Range( 0.0f, 150.0f )] [Tooltip( "The horizontal position of the emoji button in relation to the left side of the input field." )]
		private float emojiButtonHorizontalPosition = 99.0f;
		[SerializeField] [Tooltip( "The image component used as the background of the emoji window." )]
		private Image emojiWindowImage;
		[SerializeField] [Tooltip( "The overall size of the emoji window." )]
		private Vector2 emojiWindowSize = new Vector2( 25, 25 );
		[SerializeField] [Tooltip( "The position of the emoji window in relation to the bottom right of the input field." )]
		private Vector2 emojiWindowPosition = Vector2.zero;
		[SerializeField] [Tooltip( "The text to display all the available emojis." )]
		private TextMeshProUGUI emojiText;
		[SerializeField] [Range( 0.0f, 0.2f )] [Tooltip( "The padding to add to the edges of the emoji window text." )]
		private float emojiTextEdgePadding = 0.0f;
		[SerializeField] [Tooltip( "How many emojis should be displayed in a row inside the window?" )]
		private int emojiPerRow = 5;
		[SerializeField] [HideInInspector]
		private List<Rect> emojiRects = new List<Rect>();
		[SerializeField] [HideInInspector]
		private Rect emojiButtonRect = new Rect(), emojiWindowRect = new Rect();
		[SerializeField] [HideInInspector]
		private CanvasGroup emojiWindowCanvasGroup;
		/// <summary>
		/// Returns the current state of the emoji window being enabled and interactable.
		/// </summary>
		public bool EmojiWindowEnabled { get; private set; } = true;
		/// <summary>
		/// The sprite of the emoji button on the chat box. 
		/// </summary>
		public Sprite EmojiButtonSprite
		{
			get
			{
				if( !useEmojiWindow || emojiButtonImage == null )
					return null;

				return emojiButtonImage.sprite;
			}
			set
			{
				if( useEmojiWindow && emojiButtonImage != null )
					emojiButtonImage.sprite = value;
			}
		}
		/// <summary>
		/// The color of the emoji button on the chat box.
		/// </summary>
		public Color EmojiButtonColor
		{
			get
			{
				// If the user doesn't want to use an emoji window, or the emoji button is unassigned, return a clear color.
				if( !useEmojiWindow || emojiButtonImage == null )
					return Color.clear;

				// Otherwise, return the emoji button's color.
				return emojiButtonImage.color;
			}
			set
			{
				// If the user does want to use a window for the emojis and the button is assigned, assign the emoji button color to the provided value.
				if( useEmojiWindow && emojiButtonImage != null )
					emojiButtonImage.color = value;
			}
		}

		[Serializable]
		public class ChatStyle
		{
			public bool usernameBold = false;
			public bool usernameItalic = false;
			public bool usernameUnderlined = false;
			public Color usernameColor = Color.clear;
			public bool disableInteraction = false;
			public bool noUsernameFollowupText = false;
			public bool messageBold = false;
			public bool messageItalic = false;
			public bool messageUnderlined = false;
			public Color messageColor = Color.clear;

			/// <summary>
			/// [INTERNAL] Formats the content of the chat according to the style settings.
			/// </summary>
			public string FormatMessage ( string username, string message )
			{
				if( username != string.Empty )
				{
					if( usernameBold )
						username = "<b>" + username + "</b>";

					if( usernameItalic )
						username = "<i>" + username + "</i>";

					if( usernameUnderlined )
						username = "<u>" + username + "</u>";

					if( usernameColor != Color.clear )
						username = $"<color=#{ColorUtility.ToHtmlStringRGB( usernameColor )}>" + username + "</color>";
				}

				if( messageBold )
					message = "<b>" + message + "</b>";

				if( messageItalic )
					message = "<i>" + message + "</i>";

				if( messageUnderlined )
					message = "<u>" + message + "</u>";

				if( messageColor != Color.clear )
					message = $"<color=#{ColorUtility.ToHtmlStringRGB( messageColor )}>" + message + "</color>";

				return username + message;
			}

			/// <summary>
			/// [INTERNAL] Formats the username only for calculations.
			/// </summary>
			public string FormatUsernameOnly ( string username )
			{
				if( username.Contains( "#" ) )
					username = username.Split( '#' )[ 0 ];

				if( usernameBold )
					username = "<b>" + username + "</b>";

				if( usernameItalic )
					username = "<i>" + username + "</i>";

				if( usernameUnderlined )
					username = "<u>" + username + "</u>";

				return username;
			}
		}

		// CALLBACKS //
		/// <summary>
		/// This event is called when a chat has been registered to the chat box.
		/// </summary>
		public event Action<ChatInformation> OnChatRegistered;
		/// <summary>
		/// Callback for when a new username has been hovered by the player.
		/// </summary>
		public event Action<Vector2, ChatInformation> OnUsernameHover;
		/// <summary>
		/// Callback for when the player has interacted with a username.
		/// </summary>
		public event Action<Vector2, ChatInformation> OnUsernameInteract;
		/// <summary>
		/// Callback for when the input field has been enabled.
		/// </summary>
		public event Action OnInputFieldEnabled;
		/// <summary>
		/// Callback for when the input field has been disabled.
		/// </summary>
		public event Action OnInputFieldDisabled;
		/// <summary>
		/// This event is called when the input field of the chat box has been updated.
		/// </summary>
		public event Action<string> OnInputFieldUpdated;
		/// <summary>
		/// This event is called when the input field of the chat box has been submitted.
		/// </summary>
		public event Action<string> OnInputFieldSubmitted;
		/// <summary>
		/// This event is called when the input field is updated and contains a potential command.
		/// </summary>
		public event Action<string, string> OnInputFieldCommandUpdated;
		/// <summary>
		/// This event is called when the input field is submitted and contains a potential command.
		/// </summary>
		public event Action<string, string> OnInputFieldCommandSubmitted;
		/// <summary>
		/// This event is called when the extra image associated with the input field has been interacted with.
		/// </summary>
		public event Action OnExtraImageInteract;


		// ---------------------- <INTERNAL FUNCTIONS> ---------------------- //
		private void Start ()
		{
#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
#if UNITY_2022_2_OR_NEWER
			UnityEngine.EventSystems.EventSystem eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
#else
			UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
#endif
			// If the user has the new Input System and there is still an old input system component on the event system...
			if( eventSystem != null && eventSystem.gameObject.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() )
			{
				if( Application.isPlaying )
				{
					// Destroy the old component and add the new one so there will be no errors.
					DestroyImmediate( eventSystem.gameObject.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() );
					eventSystem.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
				}
				else
				{
					UnityEditor.Undo.DestroyObjectImmediate( eventSystem.gameObject.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() );
					UnityEditor.Undo.AddComponent( eventSystem.gameObject, typeof( UnityEngine.InputSystem.UI.InputSystemUIInputModule ) );
				}
			}
#endif
			// If the game is not currently playing, then return.
			if( !Application.isPlaying )
				return;

			// If the user wants to be able to highlight usernames and the image is assigned, the set the color to clear to start.
			if( useInteractableUsername && interactableUsernameImage != null )
				interactableUsernameImage.color = Color.clear;

			// If the user wants to use a scrollbar and the base is assigned...
			if( useScrollbar && scrollbarBase != null )
			{
				// Set the alpha of the scrollbar to zero so it's invisible.
				scrollbarCanvasGroup.alpha = 0.0f;

				// Set the hovered controller to false to start.
				scrollbarHandleHovered = false;
			}

			// If the text object is assigned, then settings need to be checked as well as potential emojis...
			if( textObject != null )
			{
				// If the alignment is wrong, then correct it.
				if( textObject.alignment != TextAlignmentOptions.TopLeft )
					textObject.alignment = TextAlignmentOptions.TopLeft;

				// If the text object is not maskable, then force it to maskable.
				if( !textObject.maskable )
					textObject.maskable = true;

				// If the text object is requiring raycast, then disable it to save resources.
				if( textObject.raycastTarget )
					textObject.raycastTarget = false;
#if UNITY_2023_1_OR_NEWER
				// If the text doesn't allow word wrap, fix it.
				if( textObject.textWrappingMode != TextWrappingModes.Normal )
					textObject.textWrappingMode = TextWrappingModes.Normal;
#else
				// If the text doesn't allow word wrap, fix it.
				if( !textObject.enableWordWrapping )
					textObject.enableWordWrapping = true;
#endif
				// If the text is attempting auto size, disable it.
				if( textObject.enableAutoSizing )
					textObject.enableAutoSizing = false;

				// If the user wants to allow emojis in the chat box...
				if( useTextEmoji )
				{
					// Assign the emoji sprite asset to the text object.
					textObject.spriteAsset = emojiAsset;

					// Loop through any current chat objects and update the emoji asset.
					for( int n = 0; n < AllTextObjects.Count; n++ )
						AllTextObjects[ n ].spriteAsset = emojiAsset;

					// If the user wants an input field displayed for the players...
					if( useInputField )
					{
						// Update the sprite asset of the input field.
						inputField.textComponent.spriteAsset = emojiAsset;

						// If the user wants to display an emoji window to the players and the emoji text is assigned...
						if( useEmojiWindow && emojiText != null )
						{
							// Update the emoji text's sprite asset and reset the string value of the text.
							emojiText.spriteAsset = emojiAsset;
							emojiText.text = "";

							// Loop through all the sprite characters in the sprite asset...
							for( int i = 0; i < emojiAsset.spriteCharacterTable.Count; i++ )
							{
								// If the index is exactly divisible by the user defined emojiPerRow, then add a break.
								if( i > 0 && i % emojiPerRow == 0 )
									emojiText.text += "\n";

								// Add the emoji to the text value.
								emojiText.text += $"<sprite={i}>";
							}
						}
					}
				}
			}
			// Else the text object is unassigned...
			else
			{
				// Disable this component to avoid errors, inform the user, and return.
				enabled = false;
				Debug.LogError( FormatDebug( "No Text Object has been created. Disabling Ultimate Chat Box component to avoid errors.", "Go to the Text Settings section of the Ultimate Chat Box and click the Generate Text Object button.", gameObject.name ) );
				return;
			}

			// Update the positioning of the chat box and all it's components.
			UpdatePositioning();

#if ENABLE_INPUT_SYSTEM
			// If the user wants to allow touch input, then enable the enhanced touch support.
			if( allowTouchInput )
				EnhancedTouchSupport.Enable();
#endif
			if( allowTouchInput && useInputField && inputField != null )
			{
				inputField.onTouchScreenKeyboardStatusChanged.AddListener( ( TouchScreenKeyboard.Status status ) =>
				{
					if( status == TouchScreenKeyboard.Status.Done )
						DisableInputField();
				} );
			}
		}

		private void Update ()
		{
			// If this is running in the editor, then just update the positioning.
			if( !Application.isPlaying )
			{
				UpdatePositioning();
				return;
			}

			// If the chat box is dirty from removing chats or something else, then reposition all the chats to make sure everything is displayed correctly.
			if( isDirty )
			{
				isDirty = false;
				RepositionAllChats();
			}

			// Reset the GetButtonDown bool since it should only be calculated as down on one frame.
			GetButtonDown = false;

#if ENABLE_INPUT_SYSTEM
			// Store the mouse from the input system.
			Mouse mouse = InputSystem.GetDevice<Mouse>();

			// If the mouse was stored from the input system...
			if( mouse != null )
			{
				// Store the input data.
				InputPosition = mouse.position.ReadValue();
				GetButtonDown = mouse.leftButton.wasPressedThisFrame;
				GetButton = mouse.leftButton.isPressed;

				// If the mouse has been disabled from Unity, then inform the user that they will need to disable Simulated Touchscreen in order to continue.
				if( !mouse.enabled && !allowTouchInput && Touchscreen.current != null && Touchscreen.current.enabled )
					Debug.LogWarning( FormatDebug( "Mouse device is disabled by Unity", "Please disable the Simulated Touchscreen option. To do this, go to Window > Analysis > Input Debugger. In this window, disable the Simulated Touchscreen by going to Options > Simulate Touch Input From Mouse Or Pen", "NA" ) );
			}
#else
			// Store the input data.
			InputPosition = Input.mousePosition;
			GetButtonDown = Input.GetMouseButtonDown( 0 );
			GetButton = Input.GetMouseButton( 0 );
#endif

			// If the user wants to calculate touch input...
			if( allowTouchInput )
			{
				// Loop through all the active fingers on the screen...
#if ENABLE_INPUT_SYSTEM
				for( int i = 0; i < UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count; i++ )
#else
				for( int i = 0; i < Input.touchCount; i++ )
#endif
				{
#if ENABLE_INPUT_SYSTEM
					int touchId = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ i ].touchId;
					bool touchPhaseBegan = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ i ].phase == UnityEngine.InputSystem.TouchPhase.Began;
					Vector2 touchPosition = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[ i ].screenPosition;
#else
					int touchId = Input.touches[ i ].fingerId;
					bool touchPhaseBegan = Input.touches[ i ].phase == TouchPhase.Began;
					Vector2 touchPosition = Input.touches[ i ].position;
#endif
					// If the stored touch ID is unassigned, and this finger has just began the touch on the screen...
					if( currentTouchId < 0 && touchPhaseBegan )
					{
						// If the chat box contains the touch position...
						if( chatBoxScreenRect.Contains( touchPosition ) || ( useEmojiWindow && ( emojiButtonRect.Contains( touchPosition ) || ( EmojiWindowEnabled && emojiWindowRect.Contains( touchPosition ) ) ) ) )
						{
							// Store the touch position and set GetButtonDown to true since the touch has just began this frame.
							InputPosition = touchPosition;
							GetButtonDown = true;
						
							// Store the touch ID and store the input position for calculations.
							currentTouchId = touchId;
							previousInputPosition = InputPosition;

							// If the user wants a scrollbar, then allow them to scroll with the touch.
							if( useScrollbar )
								isDraggingWithTouch = true;

							// Break the loop of touch inputs.
							break;
						}
					}

					// If this touch input is not the same as the one that is actually interacting with the chat box, then continue.
					if( touchId != currentTouchId )
						continue;

					// Since this is the same touch input, store the input position and set GetButton to true.
					InputPosition = touchPosition;
					GetButton = true;
				}
			}

			// If the user sent some custom input values...
			if( customInputRecieved )
			{
				// If there was touch input found, but there is custom input, then set isDraggingWithTouch to false.
				if( allowTouchInput && isDraggingWithTouch )
					isDraggingWithTouch = false;

				// Use the custom input values that were sent.
				InputPosition = customScreenPosition;
				GetButtonDown = customGetButtonDown;
				GetButton = customGetButton;

				// Set the customInputRecieved controller to false so that if there isn't custom input provided on the next frame the default will be used.
				customInputRecieved = false;
			}

			// If the user wants to allow touch input, and the player is dragging the touch on the chat box...
			if( allowTouchInput && isDraggingWithTouch )
			{
				// If the input is still pressed...
				if( GetButton )
				{
					// Adjust the content box's anchored position by the difference of the previously stored input position and the current input position.
					chatContentBox.anchoredPosition -= new Vector2( 0, chatContentBox.transform.InverseTransformDirection( previousInputPosition ).y - chatContentBox.transform.InverseTransformDirection( InputPosition ).y );

					// Store the current input position as previous for next frame calculations.
					previousInputPosition = InputPosition;

					// Constrain the content box.
					ConstrainContentBox();
				}
				// Else the input is no longer pressed, so reset the touch ID and isDraggingWithTouch controller.
				else
				{
					isDraggingWithTouch = false;
					currentTouchId = -1;
				}
			}

			// If the stored canvas size is not the same as the current canvas size, then update positioning.
			if( parentCanvasSize != parentCanvasRectTrans.sizeDelta )
				UpdatePositioning();

			// If the chat box is not interactable...
			if( !Interactable )
			{
				// Then process everything that needs to be processed, then return.
				ProcessToggle();
				ProcessCollapse();
				ProcessScrollbarToggle();
				return;
			}

			// Store the result of the input position being inside the chat box rect.
			InputOnChatBox = chatBoxScreenRect.Contains( InputPosition );

			// Process the navigation sections.
			ProcessNavigationSections();

			// If the user wants a scrollbar and the content box is larger than the visible chat area...
			if( useScrollbar && chatContentBox.sizeDelta.y > visibleChatBoundingBox.sizeDelta.y )
			{
				// If the user wants to have the scrollbar visible only when the input is hovering in the chat box...
				if( visibleOnlyOnHover )
				{
					// If the user doesn't want any inactive time before the scrollbar disables itself, and the scrollbar is active, input not in range, scrollbar not navigating in any way, then disable the scrollbar.
					if( scrollbarInactiveTime <= 0.0f && ScrollbarActive && !InputOnChatBox && !isDraggingScrollHandle )
						DisableScrollbar();
					// Else the user does want some inactive time...
					else
					{
						// If the input is on the chat box and ( scrollbar is not active OR the inactive time is less than the max inactive time )...
						if( InputOnChatBox && ( !ScrollbarActive || _inactiveTime < scrollbarInactiveTime ) )
						{
							// Set ScrollbarActive to true since the scrollbar is now enabled.
							ScrollbarActive = true;

							// Set the canvas group alpha to full alpha.
							scrollbarCanvasGroup.alpha = 1.0f;

							// Set the scrollbarToggle controller to false and reset the lerp value.
							scrollbarToggle = false;
							scrollbarToggleLerpValue = 0.0f;

							// If the inactive time has a value other than zero, then set it to zero.
							if( _inactiveTime > 0.0f )
								_inactiveTime = 0.0f;
						}
						// Else if the scrollbar needs to be disabled after an inactivity delay...
						else if( ScrollbarActive && !InputOnChatBox && !isDraggingScrollHandle )
						{
							// Increase the inactive time for calculations.
							_inactiveTime += Time.deltaTime;

							// If the chat box has been inactive for more than the user defined time, finalize the inactive time and disable the scrollbar.
							if( _inactiveTime >= scrollbarInactiveTime )
							{
								_inactiveTime = 0.0f;
								DisableScrollbar();
							}
						}
					}
				}
			}

			// If the user wants usernames in the chat box to be interactable...
			if( useInteractableUsername )
			{
				// Calculate the local position of the mouse relative to the chat content box position.
				Vector2 localMousePosition = InputPosition - chatContentBox.position;

				// Temporary bool to store if a username has been hovered or not.
				bool usernameHovered = false;

				// Loop through all the chat informations...
				for( int i = 0; i < ChatInformations.Count; i++ )
				{
					// If the chat box is currently navigating in some way, then break the loop. No need to calculate input on usernames when the chat box is moving.
					if( isDraggingWithTouch || isDraggingScrollHandle )
						break;

					// If this current chat is not visible...
					if( !ChatInformations[ i ].IsVisible )
					{
						// If the loop is past the first index and the index BEFORE this IS visible, then we know that this is the end of our visible buttons, so break the loop.
						if( i > 0 && ChatInformations[ i - 1 ].IsVisible )
							break;

						// Else just continue because there's no point in wasting any other resources on this chat since it is not visible.
						continue;
					}

					// If this current chat doesn't have a username, or the chat box style is null, or the user doesn't want the usernames to be interactable, then skip to the next index.
					if( ChatInformations[ i ].DisplayUsername == string.Empty || ChatInformations[ i ].chatBoxStyle == null || ChatInformations[ i ].chatBoxStyle.disableInteraction )
						continue;

					// If the chat username contains the mouse position...
					if( ChatInformations[ i ].usernameRect.Contains( localMousePosition ) )
					{
						// Set username hovered bool to true so that it can be calculated.
						usernameHovered = true;

						// If the last stored index is not this index...
						if( CurrentHoveredChatIndex != i )
						{
							// Store the new index and update the text.
							CurrentHoveredChatIndex = i;

							if( interactableUsernameImage != null )
							{
								// If the username highlight is not currently visible, set the controller to true for reference and assign the highlight color to the image.
								if( !UsernameHighlighted )
								{
									UsernameHighlighted = true;
									interactableUsernameImage.color = interactableUsernameColor;
								}

								// Set the size and position of the highlight image.
								interactableUsernameImage.rectTransform.sizeDelta = ( ChatInformations[ i ].usernameRect.size + new Vector2( LineHeight * interactableUsernameWidthModifier, 0 ) ) / parentCanvasScale;
								interactableUsernameImage.rectTransform.anchoredPosition = ChatInformations[ i ].usernameRect.center / parentCanvasScale;
							}
						}

						// Inform any subscribers that a new username has been hovered.
						OnUsernameHover?.Invoke( InputPosition, ChatInformations[ i ] );

#if ENABLE_INPUT_SYSTEM
						// If the mouse buttons were pressed this frame, inform any subscribers with the information.
						if( mouse != null && ( mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame ) )
#else
						// If the mouse buttons were pressed this frame, inform any subscribers with the information.
						if( Input.GetMouseButtonDown( 0 ) || Input.GetMouseButtonDown( 1 ) )
#endif
							OnUsernameInteract?.Invoke( InputPosition, ChatInformations[ i ] );

						break;
					}
				}

				// If there are no usernames being hovered and the stored index is actually within range of the list...
				if( !usernameHovered && CurrentHoveredChatIndex >= 0 )
				{
					// Reset the stored chat index for hover.
					CurrentHoveredChatIndex = -1;

					// If there was a username highlighted before, then reset the controller and image color.
					if( UsernameHighlighted )
					{
						UsernameHighlighted = false;
						interactableUsernameImage.color = Color.clear;
					}
				}
			}

			// Process all the toggles and input.
			ProcessToggle();
			ProcessCollapse();
			ProcessScrollbarToggle();
			ProcessInputField();

			if( useInputField && inputField != null )
				inputFieldStringPosition = inputField.stringPosition;
		}

		private void OnEnable ()
		{
			// Since this function is called by Unity any time the object is enabled, update the positioning of the chat box so everything is set up correctly.
			UpdatePositioning();
		}

		/// <summary>
		/// [INTERNAL] Processes the navigation section of the chat box (scroll wheel, scrollbar, arrows, etc...)
		/// </summary>
		private void ProcessNavigationSections ()
		{
			// If the user doesn't want a scrollbar, or the content is smaller than the chat box, then return since there is no need to navigate.
			if( !useScrollbar || chatContentBox.sizeDelta.y < visibleChatBoundingBox.sizeDelta.y )
				return;

			// If the user wants to allow the mouse scroll wheel to navigate the chat box...
			if( useScrollWheel )
			{
				// If the handle of the scrollbar is not be interacted with and the input is indeed on the chat box...
				if( !isDraggingScrollHandle && InputOnChatBox )
				{
#if ENABLE_INPUT_SYSTEM
					// Increase the scroll value by the scroll wheel of the mouse.
					scrollValue += InputSystem.GetDevice<Mouse>().scroll.ReadValue().normalized.y;
#else
					// Increase the scroll value by the scroll wheel of the mouse.
					scrollValue += Input.mouseScrollDelta.y;
#endif
				}

				// If the scroll value is not zero, then adjust the anchored position of the content box and reset the scroll value for the next calculation.
				if( scrollValue != 0.0f )
				{
					chatContentBox.anchoredPosition -= new Vector2( 0, LineHeight ) * scrollValue * mouseScrollSpeed;
					ConstrainContentBox();
					scrollValue = 0.0f;
					return;
				}
			}

			// If the user wants to allow touch input, and the player is dragging the touch on the chat box...
			if( allowTouchInput && isDraggingWithTouch )
			{
				// If the input is still pressed...
				if( GetButton )
				{
					// Adjust the content box's anchored position by the difference of the previously stored input position and the current input position.
					chatContentBox.anchoredPosition -= new Vector2( 0, chatContentBox.transform.InverseTransformDirection( previousInputPosition ).y - chatContentBox.transform.InverseTransformDirection( InputPosition ).y );

					// Store the current input position as previous for next frame calculations.
					previousInputPosition = InputPosition;

					// Constrain the content box.
					ConstrainContentBox();
				}
				// Else the input is no longer pressed, so reset the touch ID and isDraggingWithTouch controller.
				else
				{
					isDraggingWithTouch = false;
					currentTouchId = -1;
				}
			}

			// If the chat box is being dragged with touch input, then return since the chat box is already navigating.
			if( isDraggingWithTouch )
				return;

			// If the scrollbar handle has focus...
			if( scrollbarHandleRect.Contains( InputPosition ) )
			{
				// If the scrollbar is not currently hovered, and the input button is not currently in the pressed state...
				if( !scrollbarHandleHovered && !GetButton )
				{
					// Set Hovered to true, and Active to false.
					scrollbarHandleHovered = true;
					scrollbarHandleActive = false;

					// Set the scrollbar handle color to the hovered color.
					scrollbarHandleImage.color = scrollbarHandleHoverColor;
				}

				// If the input is down on this frame...
				if( GetButtonDown )
				{
					// Store the current input position for calculations since the input was initiated on the chat box this frame.
					previousInputPosition = InputPosition;

					// Configure the position that the input is in relation to the scrollbar handle for reference.
					scrollbarHandleInputStart = InputPosition - ( Vector3 )scrollbarHandleRect.center;

					// Set the isDraggingScrollHandle controller to true since the handle is now being dragged.
					isDraggingScrollHandle = true;

					// Set the Hovered controller to false, and Active to true since the input is active.
					scrollbarHandleHovered = false;
					scrollbarHandleActive = true;

					// Set the scrollbar handle color to active.
					scrollbarHandleImage.color = scrollbarHandleActiveColor;
				}
			}
			// Else the player is not currently dragging the scrollbar, and either the scrollbar handle was hovered or active...
			else if( !isDraggingScrollHandle && ( scrollbarHandleHovered || scrollbarHandleActive ) )
			{
				// Set both state controllers to false.
				scrollbarHandleHovered = false;
				scrollbarHandleActive = false;

				// Set the scrollbar handle color back to normal.
				scrollbarHandleImage.color = scrollbarHandleNormalColor;
			}

			// If the player is currently dragging the scrollbar handle...
			if( isDraggingScrollHandle )
			{
				// If the input is currently down...
				if( GetButton )
				{
					// Configure the target direction of the input by the difference of the current and previous input positions.
					float targetDirection = InputPosition.y - previousInputPosition.y;

					// Configure the relative position of the current input vs the initial input position when interacting with the handle.
					float relativeVerticalPosition = ( ( Vector2 )InputPosition - ( scrollbarHandleRect.position + scrollbarHandleRect.size / 2 ) - scrollbarHandleInputStart ).y;

					// If the direction is up, and the input is above the initial position, or if the direction is down and the input is below the initial position, adjust the scrollbar handle to follow the input.
					if( ( targetDirection > 0 && relativeVerticalPosition >= 0 ) || ( targetDirection < 0 && relativeVerticalPosition <= 0 ) )
						scrollbarHandle.anchoredPosition += new Vector2( 0, targetDirection );

					// Constrain the scrollbar handle.
					ConstrainScrollbarHandle();

					// Store the previous input position for the next frame and return to avoid any other navigation.
					previousInputPosition = InputPosition;
					return;
				}
				// Else the input is no longer pressed down, so release the navigation.
				else
					isDraggingScrollHandle = false;
			}

			// If the user wants to be able to navigate when clicking the scrollbar base, and the scrollbar handle is not being dragged, and the base has the current input...
			if( !disableBaseNavigation && !isDraggingScrollHandle && scrollbarBaseRect.Contains( InputPosition ) )
			{
				// If the input is down this frame...
				if( GetButtonDown )
				{
					// Set the handle's position to the input position modified by the base so it's relative, and constrain the scrollbar handle.
					scrollbarHandle.anchoredPosition = ( Vector2 )InputPosition - ( scrollbarBaseRect.center + ( scrollbarBaseRect.size / 2 ) ) + new Vector2( 0, scrollbarHandle.sizeDelta.y / 2 );
					ConstrainScrollbarHandle();
				}
			}

			// If the up arrow contains the input position...
			if( scrollbarNavigationArrowUpRect.Contains( InputPosition ) )
			{
				// If the up arrow was not previously hovered, and either the input is not pressed OR the up arrow is currently in the "activated" state...
				if( !scrollbarNavigationArrowUpHovered && ( !GetButton || scrollbarNavigationArrowUpActivated ) )
				{
					// Set the hovered controller to true for reference.
					scrollbarNavigationArrowUpHovered = true;

					// If the up arrow is currently in the "activated" state, then set the color to activated.
					if( scrollbarNavigationArrowUpActivated )
						navigationArrowUp.color = navigationActiveColor;
					// Else just set the color to the hover color.
					else
						navigationArrowUp.color = navigationHoverColor;
				}

				// If the button was pressed on this frame...
				if( GetButtonDown )
				{
					// Set the "activated" controller to true and the arrow color to active.
					scrollbarNavigationArrowUpActivated = true;
					navigationArrowUp.color = navigationActiveColor;

					// Adjust the content box position since the arrow was interacted with and constrain the content.
					chatContentBox.anchoredPosition -= new Vector2( 0, LineHeight );
					ConstrainContentBox();
				}

				// If the up arrow is activated...
				if( scrollbarNavigationArrowUpActivated )
				{
					// If the button is still pressed down...
					if( GetButton )
					{
						// Increase the stored initial delay time.
						navigationInitialHoldTime += Time.deltaTime;

						// If the current initial delay time is over the hold delay set by the user...
						if( navigationInitialHoldTime >= navigationInitialHoldDelay )
						{
							// Now increase the current hold time of the navigation arrow.
							navigationIntervalTime += Time.deltaTime;

							// If the input has been held on the arrow for longer than the navigation interval...
							if( navigationIntervalTime >= navigationIntervalDelay )
							{
								// Reduce the stored time by the target hold interval.
								navigationIntervalTime -= navigationIntervalDelay;

								// Adjust the content box position and constrain the content.
								chatContentBox.anchoredPosition -= new Vector2( 0, LineHeight );
								ConstrainContentBox();
							}
						}
					}
					// Else the input has been released...
					else
					{
						// Set the activated controller to false, reset the stored times, and the up arrow color.
						scrollbarNavigationArrowUpActivated = false;
						navigationInitialHoldTime = 0.0f;
						navigationIntervalTime = 0.0f;
						navigationArrowUp.color = navigationHoverColor;
					}
				}

				// Return because the up arrow has focus.
				return;
			}
			// Else the up arrow does not have the input over it...
			else
			{
				// If the up arrow was being hovered the last frame, reset the controller and color.
				if( scrollbarNavigationArrowUpHovered )
				{
					scrollbarNavigationArrowUpHovered = false;
					navigationArrowUp.color = navigationNormalColor;
				}

				// If the up arrow was active last frame and the input is no longer pressed down...
				if( scrollbarNavigationArrowUpActivated && !GetButton )
				{
					// Set the activated controller to false, reset the stored times, and the up arrow color.
					scrollbarNavigationArrowUpActivated = false;
					navigationInitialHoldTime = 0.0f;
					navigationIntervalTime = 0.0f;
					navigationArrowUp.color = navigationNormalColor;
				}
			}

			// If the down arrow has the focus of the players input...
			if( scrollbarNavigationArrowDownRect.Contains( InputPosition ) )
			{
				// If the down arrow was not previously hovered, and either the input is not pressed OR the down arrow is currently in the "activated" state...
				if( !scrollbarNavigationArrowDownHovered && ( !GetButton || scrollbarNavigationArrowDownActivated ) )
				{
					// Set the hovered controller to true for reference.
					scrollbarNavigationArrowDownHovered = true;

					// If the down arrow is currently in the "activated" state, then set the color to activated.
					if( scrollbarNavigationArrowDownActivated )
						navigationArrowDown.color = navigationActiveColor;
					// Else just set the color to the hover color.
					else
						navigationArrowDown.color = navigationHoverColor;
				}

				// If the button was pressed on this frame...
				if( GetButtonDown )
				{
					// Set the "activated" controller to true and the arrow color to active.
					scrollbarNavigationArrowDownActivated = true;
					navigationArrowDown.color = navigationActiveColor;

					// Adjust the content box position since the arrow was interacted with and constrain the content.
					chatContentBox.anchoredPosition += new Vector2( 0, LineHeight );
					ConstrainContentBox();
				}

				// If the down arrow is activated...
				if( scrollbarNavigationArrowDownActivated )
				{
					// If the button is still pressed down...
					if( GetButton )
					{
						// Increase the stored initial delay time.
						navigationInitialHoldTime += Time.deltaTime;

						// If the current initial delay time is over the hold delay set by the user...
						if( navigationInitialHoldTime >= navigationInitialHoldDelay )
						{
							// Now increase the current hold time of the navigation arrow.
							navigationIntervalTime += Time.deltaTime;

							// If the input has been held on the arrow for longer than the navigation interval...
							if( navigationIntervalTime >= navigationIntervalDelay )
							{
								// Reduce the stored time by the target hold interval.
								navigationIntervalTime -= navigationIntervalDelay;

								// Adjust the content box position and constrain the content.
								chatContentBox.anchoredPosition += new Vector2( 0, LineHeight );
								ConstrainContentBox();
							}
						}
					}
					// Else the input has been released...
					else
					{
						// Set the activated controller to false, reset the stored times, and the down arrow color.
						scrollbarNavigationArrowDownActivated = false;
						navigationInitialHoldTime = 0.0f;
						navigationIntervalTime = 0.0f;
						navigationArrowDown.color = navigationHoverColor;
					}
				}

				// Return because the down arrow has focus.
				return;
			}
			// Else the down arrow does not have the input over it...
			else
			{
				// If the down arrow was being hovered the last frame, reset the controller and color.
				if( scrollbarNavigationArrowDownHovered )
				{
					scrollbarNavigationArrowDownHovered = false;
					navigationArrowDown.color = navigationNormalColor;
				}

				// If the down arrow was active last frame and the input is no longer pressed down...
				if( scrollbarNavigationArrowDownActivated && !GetButton )
				{
					// Set the activated controller to false, reset the stored times, and the down arrow color.
					scrollbarNavigationArrowDownActivated = false;
					navigationInitialHoldTime = 0.0f;
					navigationIntervalTime = 0.0f;
					navigationArrowDown.color = navigationNormalColor;
				}
			}
		}

		/// <summary>
		/// [INTERNAL] Process the toggle over time for the chat box.
		/// </summary>
		private void ProcessToggle ()
		{
			// If the user doesn't want to fade the alpha when the chat box is disabled, then return.
			if( !fadeWhenDisabled )
				return;

			// If the chat box needs to be faded in...
			if( fadeIn )
			{
				// Lerp the toggle value over time according to the users settings.
				fadeLerpValue += Time.unscaledDeltaTime * fadeInSpeed;

				// Transition the alpha from current to full by the lerp value.
				chatBoxCanvasGroup.alpha = Mathf.Lerp( toggledAlpha, 1.0f, fadeLerpValue );

				// If the menu is still focused and the lerp value is complete...
				if( fadeLerpValue >= 1.0f )
				{
					// Set fadeIn and the lerp value to default.
					fadeIn = false;
					fadeLerpValue = 1.0f;

					// Apply the full alpha.
					chatBoxCanvasGroup.alpha = 1.0f;
				}
			}
			// Else if the chat box needs to be faded out...
			else if( fadeOut )
			{
				// Lerp the toggle value.
				fadeLerpValue -= Time.unscaledDeltaTime * fadeOutSpeed;

				// Transition the alpha from current to zero by the lerp value.
				chatBoxCanvasGroup.alpha = Mathf.Lerp( toggledAlpha, 1.0f, fadeLerpValue );

				// If the lerp value is over 1.0f...
				if( fadeLerpValue <= 0.0f )
				{
					// Set fadeOut and the lerp value to default.
					fadeOut = false;
					fadeLerpValue = 0.0f;

					// Apply the zero alpha.
					chatBoxCanvasGroup.alpha = toggledAlpha;
				}
			}
		}

		/// <summary>
		/// [INTERNAL] Process the collapse and expand for the chat box according to the users options.
		/// </summary>
		private void ProcessCollapse ()
		{
			// If the user doesn't want to collapse the chat box, then just return.
			if( !collapseWhenDisabled )
				return;

			// If the chat box needs to be expanded...
			if( expandChatBox )
			{
				// Increase the collapse lerp value by the expand speed.
				collapseLerpValue += Time.unscaledDeltaTime * expandSpeed;

				// Apply the collapse lerp value to the base transform and bounding box transform.
				BaseTransform.sizeDelta = Vector2.Lerp( baseTransformCollapsedSize, baseTransformSize, collapseLerpValue );
				visibleChatBoundingBox.sizeDelta = Vector2.Lerp( boundingBoxCollapsedSize, visibleBoundingBoxSize, collapseLerpValue );

				// If the lerp value is over 1, then it is done, so...
				if( collapseLerpValue >= 1.0f )
				{
					// Set expandChatBox to false and reset the lerp value since it is done.
					expandChatBox = false;
					collapseLerpValue = 1.0f;

					// Apply the final size to the base and bounding box transforms.
					BaseTransform.sizeDelta = baseTransformSize;
					visibleChatBoundingBox.sizeDelta = visibleBoundingBoxSize;
				}

				// If the chat box is NOT at a custom position, then force the chat box down to the bottom.
				if( !chatBoxPositionCustom )
					chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

				// Update the size of the chat box component that have moved.
				UpdateChatBoxComponentSizes();

				// Constrain the content box and update it.
				ConstrainContentBox();
			}
			else if( collapseChatBox )
			{
				// Decrease the lerp value by the collapse speed set by the user.
				collapseLerpValue -= Time.unscaledDeltaTime * collapseSpeed;

				// Apply the lerp value to the base and bounding box transforms.
				BaseTransform.sizeDelta = Vector2.Lerp( baseTransformCollapsedSize, baseTransformSize, collapseLerpValue );
				visibleChatBoundingBox.sizeDelta = Vector2.Lerp( boundingBoxCollapsedSize, visibleBoundingBoxSize, collapseLerpValue );

				// If the lerp value is less than 0, it's done...
				if( collapseLerpValue < 0.0f )
				{
					// Set collapseChatBox to false and reset the lerp value.
					collapseChatBox = false;
					collapseLerpValue = 0.0f;

					// Finalize the collapsed size to the base and bounding box transforms.
					BaseTransform.sizeDelta = baseTransformCollapsedSize;
					visibleChatBoundingBox.sizeDelta = boundingBoxCollapsedSize;
				}

				// Keep the content box anchored to the bottom of the content box.
				chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

				// Update the size of the chat box components that have moved.
				UpdateChatBoxComponentSizes();

				// Constrain the content box and update it.
				ConstrainContentBox();
			}
		}

		/// <summary>
		/// [INTERNAL] Process the toggle for the scrollbar of the chat box.
		/// </summary>
		private void ProcessScrollbarToggle ()
		{
			// If the scrollbar toggle speed is 0, or the scrollbar is not needing to toggle, then just return.
			if( scrollbarToggleSpeed <= 0.0f || !scrollbarToggle )
				return;

			// Increase the lerp value according to the speed.
			scrollbarToggleLerpValue += Time.unscaledDeltaTime * scrollbarToggleSpeed;

			// Lerp the alpha of the scrollbar canvas group according to the lerp value above.
			scrollbarCanvasGroup.alpha = Mathf.Lerp( 1.0f, 0.0f, scrollbarToggleLerpValue );

			// If the lerp value is complete, then reset the stored values and apply the final alpha.
			if( scrollbarToggleLerpValue >= 1.0f )
			{
				scrollbarToggle = false;
				scrollbarToggleLerpValue = 0.0f;
				scrollbarCanvasGroup.alpha = 0.0f;
			}
		}

		/// <summary>
		/// [INTERNAL] Disables the scrollbar visually.
		/// </summary>
		private void DisableScrollbar ()
		{
			// Set ScrollbarActive to false since the scrollbar is now disabled.
			ScrollbarActive = false;

			// If the toggle duration is actually assigned, then set scrollbarToggle to true and reset the lerp value.
			if( scrollbarToggleSpeed > 0.0f )
			{
				scrollbarToggle = true;
				scrollbarToggleLerpValue = 0.0f;
			}
			// Else just set the canvas group alpha for the scrollbar to zero.
			else
				scrollbarCanvasGroup.alpha = 0.0f;
		}

		/// <summary>
		/// [INTERNAL] Processes the input field if the user has the input field enabled.
		/// </summary>
		private void ProcessInputField ()
		{
			// If the user doesn't want to use the input field, then just return.
			if( !useInputField )
				return;

#if ENABLE_INPUT_SYSTEM
			// If the input system keyboard enter key was pressed, then toggle the input field.
			if( InputSystem.GetDevice<Keyboard>().enterKey.wasPressedThisFrame || InputSystem.GetDevice<Keyboard>().numpadEnterKey.wasPressedThisFrame )
#else
			// If the enter key is pressed this frame, then toggle the input field.
			if( Input.GetKeyDown( KeyCode.Return ) || Input.GetKeyDown( KeyCode.KeypadEnter ) )
#endif
				ToggleInputField();

			// If the input is pressed down this frame, and the input field contains the input position, enable the input field.
			if( !InputFieldEnabled && GetButtonDown && inputFieldRect.Contains( InputPosition ) )
				EnableInputField();

			// If the user wants an extra image and it's assigned...
			if( useExtraImage && extraImage != null )
			{
				// If the input was down this frame, and it was on the extra image, then invoke the callback.
				if( GetButtonDown && extraImageRect.Contains( InputPosition ) )
					OnExtraImageInteract?.Invoke();
			}

			// If the input field is currently active...
			if( InputFieldEnabled )
			{
				// If the current text value is different than the stored input value...
				if( inputField.text != InputFieldValue )
				{
					// If the user wants to disable rich text coming in from player...
					if( disableRichTextFromPlayers )
					{
						// Create a pattern for the regular expression to look for, namely any rich text.
						Regex richText = new Regex( @"<[^>]*>" );

						// Store all of the matches to the pattern above from the input field.
						MatchCollection matches = richText.Matches( inputField.text );

						// Loop through all the matches...
						for( int i = 0; i < matches.Count; i++ )
						{
							// If the current match does NOT contain "sprite"...
							if( !useTextEmoji || !matches[ i ].ToString().Contains( "sprite" ) )
							{
								// Store the target string position as the current string position minus the length of the rich text.
								int targetStringPosition = inputField.stringPosition - matches[ i ].Length;

								// If the string position is currently at the end of the input field text, then assign the target position at the end.
								if( inputField.stringPosition == inputField.text.Length )
									targetStringPosition = inputField.text.Length;

								// Remove the match from the input field.
								inputField.text = inputField.text.Replace( matches[ i ].ToString(), string.Empty );

								// Apply the target string position.
								inputField.stringPosition = targetStringPosition;
							}
						}
					}

					// If the input field text contains a "tab", then remove it.
					if( inputField.text.Contains( "	" ) )
						inputField.text = inputField.text.Replace( "	", string.Empty );

					// Check for any line breaks.
					Regex lineBreaks = new Regex( Environment.NewLine );
					MatchCollection lineBreakMatches = lineBreaks.Matches( inputField.text );
					for( int i = 0; i < lineBreakMatches.Count; i++ )
						inputField.text = inputField.text.Replace( lineBreakMatches[ i ].ToString(), " " );

					// Store the current input field value.
					InputFieldValue = inputField.text;

					// Notify any subscribers that the input field has been updated.
					OnInputFieldUpdated?.Invoke( InputFieldValue );

					// If the stored input value is assigned, the first character is a forward slash, and there is a space after the forward slash, then notify any subscribers that there may be a command being typed.
					if( InputFieldValue != string.Empty && InputFieldValue[ 0 ].ToString() == "/" )
					{
						string followup = "";
						if( InputFieldValue.Contains( " " ) )
							followup = InputFieldValue.Remove( 0, InputFieldValue.Split( ' ' )[ 0 ].Length + 1 );

						OnInputFieldCommandUpdated?.Invoke( InputFieldValue.Split( ' ' )[ 0 ].ToLower(), followup );
						InputFieldContainsCommand = true;
					}
					// Else set the InputFieldContainsCommandValue to false.
					else
						InputFieldContainsCommand = false;
				}
			}

			// If the user has enabled the emoji window option, and the emoji button image is assigned...
			if( useEmojiWindow && emojiButtonImage != null && emojiWindowImage != null )
			{
				// If the emoji window is enabled and the input button is down this frame...
				if( EmojiWindowEnabled && GetButtonDown )
				{
					// Loop through all the emojis to see which one the input is down on...
					for( int i = 0; i < emojiRects.Count; i++ )
					{
						// If the emoji has been clicked/tapped on...
						if( emojiRects[ i ].Contains( InputPosition ) )
						{
							// Insert the emoji in to the input field.
							inputField.text = inputField.text.Insert( inputField.stringPosition, $"<sprite={i}>" );

							// Adjust the string and caret positions of the input field to put the caret behind the new emoji.
							inputField.stringPosition += $"<sprite={i}>".Length;
							inputField.caretPosition++;

							// Make sure that the input field is activated so the player can begin typing immediately afterwards.
							inputField.ActivateInputField();

							// Break the loop since an emoji was interacted with.
							break;
						}
					}
				}

				// If the input button is pressed down this frame...
				if( GetButtonDown )
				{
					// Check the emoji button to see if the input is on it, and if it is the emoji window needs to be enabled...
					if( emojiButtonRect.Contains( InputPosition ) )
					{
						// Set EmojiWindowEnabled to true so the emoji window will process the input.
						EmojiWindowEnabled = true;

						// Set the canvas group of the emoji window to visible.
						emojiWindowCanvasGroup.alpha = 1.0f;

						// Set the position of the caret in the string to the last know string position when the emoji window opens. This is to prevent the input field from sending the caret to the end of the field.
						inputField.stringPosition = inputFieldStringPosition;

						// Enable the input field so that the input field will be active when adding emojis.
						EnableInputField();
					}
					// Else if the emoji window is enabled, and the input is NOT within the emoji window rect, then disable the emoji window.
					else if( EmojiWindowEnabled && !emojiWindowRect.Contains( InputPosition ) )
					{
						// Set the EmojiWindowEnabled controller to false for reference.
						EmojiWindowEnabled = false;

						// Set the alpha of the window to zero.
						emojiWindowCanvasGroup.alpha = 0.0f;
					}
				}
			}
		}

		/// <summary>
		/// [INTERNAL] Updates the size of the content area.
		/// </summary>
		private void UpdateChatBoxComponentSizes ()
		{
			// If the total content space of the chat is different than the size of the content box then update the size of the content box.
			if( totalContentSpace < chatContentBox.sizeDelta.y || totalContentSpace > chatContentBox.sizeDelta.y )
				chatContentBox.sizeDelta = new Vector2( chatContentBox.sizeDelta.x, totalContentSpace );

			// Configure the bottom position of the content box.
			bottomContentPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

			// Update the visible rect of the chat box for calculations, and adjust for the scrollbar and input field so that the interactable area includes them.
			chatBoxScreenRect = CalculateRect( BaseTransform );
			AdjustChatBoxScreenRect();

			// If the user doesn't want a vertical scrollbar OR the scrollbar is unassigned, then return.
			if( !useScrollbar || scrollbarBase == null )
				return;

			// If the content is smaller than the bounding box, and the scrollbar is active, then make it invisible.
			if( chatContentBox.sizeDelta.y <= visibleChatBoundingBox.sizeDelta.y && ScrollbarActive )
			{
				ScrollbarActive = false;
				scrollbarCanvasGroup.alpha = 0.0f;
			}
			// Else if the content is larger than the bounding box, the scrollbar is invisible, and the user wants the scrollbar to always be visible, then make it visible.
			else if( chatContentBox.sizeDelta.y > visibleChatBoundingBox.sizeDelta.y && !ScrollbarActive && !visibleOnlyOnHover )
			{
				ScrollbarActive = true;
				scrollbarCanvasGroup.alpha = 1.0f;
			}

			// Update the size of the scrollbar base, and if the user wants navigation arrows, compensate for that.
			scrollbarBase.sizeDelta = new Vector2( scrollbarBase.sizeDelta.x, visibleChatBoundingBox.sizeDelta.y );
			if( useNavigationArrows && navigationArrowUp != null )
			{
				scrollbarBase.sizeDelta -= new Vector2( 0, navigationArrowUp.rectTransform.sizeDelta.y * 2 );
				scrollbarNavigationArrowUpRect = CalculateRect( navigationArrowUp.rectTransform );
			}

			// Update the scrollbar base rect for input calculations.
			scrollbarBaseRect = CalculateRect( scrollbarBase );

			// If the scrollbar handle is assigned...
			if( scrollbarHandle != null )
			{
				// Configure the scrollbar size by dividing the bounding box by the content box size.
				float scrollbarSize = Mathf.Abs( visibleChatBoundingBox.sizeDelta.y / chatContentBox.sizeDelta.y );

				// If the content box is smaller than the bounding box, then just set the scrollbar size value to 1.
				if( chatContentBox.sizeDelta.y <= visibleChatBoundingBox.sizeDelta.y )
					scrollbarSize = 1.0f;

				// Lerp the size value between the minimum size for the scrollbar and max.
				scrollbarSize = Mathf.Lerp( scrollbarMinimumSize, 1.0f, scrollbarSize );

				// Apply the scrollbar size to the handle.
				scrollbarHandle.sizeDelta = new Vector2( scrollbarBase.sizeDelta.x, scrollbarBase.sizeDelta.y * scrollbarSize );

				// Configure the bottom position for the scrollbar handle.
				scrollbarBottomPosition = new Vector2( 0, scrollbarHandle.sizeDelta.y - scrollbarBase.sizeDelta.y );

				// If the chat box is not in a custom position, then set the position of the handle to the bottom.
				if( !chatBoxPositionCustom )
					scrollbarHandle.anchoredPosition = scrollbarBottomPosition;

				// Update the input calculations for the scrollbar handle.
				scrollbarHandleRect = CalculateRect( scrollbarHandle );
			}
		}

		/// <summary>
		/// Calculates the input for the provided rect transform, taking in to consideration canvas scale options.
		/// </summary>
		/// <param name="rectTransform">The RectTransform to calculate the rect for.</param>
		private Rect CalculateRect ( RectTransform rectTransform )
		{
			// Return the rect transforms position - calculated size of the rect transform, then the size of the rect transform modified by the canvas scale.
			return new Rect( rectTransform.position - ( Vector3 )( rectTransform.sizeDelta * parentCanvasScale * rectTransform.pivot ), rectTransform.sizeDelta * parentCanvasScale );
		}

		/// <summary>
		/// [INTERNAL] Adjusts the chatBoxScreenRect to include the scrollbar and input field if they are used.
		/// </summary>
		private void AdjustChatBoxScreenRect ()
		{
			// If the user wants a scrollbar and it's assigned...
			if( useScrollbar && scrollbarBase != null )
			{
				// If the scrollbar is outside the left of chat box, then set the minimum position value of the rect to the scrollbars minimum position.
				if( scrollbarBaseRect.xMin < chatBoxScreenRect.xMin )
					chatBoxScreenRect.xMin = scrollbarBaseRect.xMin;
				// Else if the scrollbar is outside the chat box to the right, then set the max position of the chat box input rect to the max value of the scrollbar rect.
				else if( scrollbarBaseRect.xMax > chatBoxScreenRect.xMax )
					chatBoxScreenRect.xMax = scrollbarBaseRect.xMax;
			}

			// If the user wants to display a input field and it's assigned...
			if( useInputField && inputField != null )
			{
				// 1f 7h3 m1n p051710n 0f 7h3 1npu7 f13ld 15 0u7 0f 7h3 ch47 b0x, 7h3n u53 7h3 m1n p051710n v4lu3.
				if( inputFieldRect.yMin < chatBoxScreenRect.yMin )
					chatBoxScreenRect.yMin = inputFieldRect.min.y;
				// Else if the max position of the input field rect is out the right of the chat box, then use the max position value.
				else if( inputFieldRect.yMax > chatBoxScreenRect.yMax )
					chatBoxScreenRect.yMax = inputFieldRect.yMax;
			}
		}

		/// <summary>
		/// Calculates the rect for the username of the provided chat.
		/// </summary>
		/// <param name="chatInfo">The ChatInformation to calculate the rect for.</param>
		private void CalculateUsernameRect ( ChatInformation chatInfo )
		{
			chatInfo.usernameRect = new Rect( new Vector2( -chatContentBox.sizeDelta.x / 2, chatInfo.anchoredPosition.y - chatInfo.lineHeight ) * parentCanvasScale, new Vector2( chatInfo.usernameWidth * parentCanvasScale.x, chatInfo.lineHeight * parentCanvasScale.y ) );
		}

		/// <summary>
		/// [INTERNAL] Constrains the content box to being visible within the bounding box. This function also updates the scrollbar to match the content box position.
		/// </summary>
		private void ConstrainContentBox ()
		{
			// If the position is outside the top of the bounding box, then set it to right at the top.
			if( chatContentBox.anchoredPosition.y < 0 && chatContentBox.sizeDelta.y >= visibleChatBoundingBox.sizeDelta.y )
				chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, 0 );
			// Else if the content position is too far down, set it to the bottom.
			else if( chatContentBox.anchoredPosition.y > chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y )
				chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

			// Update the visibility of the chat box.
			UpdateChatBoxVisibility();

			// If the user wants a vertical scrollbar and the scrollbar handle is assigned...
			if( useScrollbar && scrollbarHandle != null )
			{
				// Update the position of the scrollbar handle.
				scrollbarHandle.anchoredPosition = Vector2.Lerp( Vector2.zero, scrollbarBottomPosition, chatContentBox.anchoredPosition.y == 0 ? 0 : chatContentBox.anchoredPosition.y / ( chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y ) );

				// Update the Rect of the scrollbar handle so that it will be ready for calculating input on it's new position.
				scrollbarHandleRect = CalculateRect( scrollbarHandle );
			}
		}

		/// <summary>
		/// [INTERNAL] This function is for when the scrollbar is updated, it updates the content box to match, the opposite of the ConstrainContentBox function.
		/// </summary>
		private void ConstrainScrollbarHandle ()
		{
			// If the scrollbar handle is too far up, then set it to the minimum position.
			if( scrollbarHandle.anchoredPosition.y > 0 )
				scrollbarHandle.anchoredPosition = new Vector2( 0, 0 );
			// Else if the scrollbar handle is to far down, then set it to max.
			else if( scrollbarHandle.anchoredPosition.y < scrollbarHandle.sizeDelta.y - scrollbarBase.sizeDelta.y )
				scrollbarHandle.anchoredPosition = new Vector2( 0, scrollbarHandle.sizeDelta.y - scrollbarBase.sizeDelta.y );

			// Zero the horizontal position to force the handle to line up in the base.
			scrollbarHandle.anchoredPosition = new Vector2( 0, scrollbarHandle.anchoredPosition.y );

			// Update the Rect of the scrollbar handle so that it will be ready for calculating input on it's new position.
			scrollbarHandleRect = CalculateRect( scrollbarHandle );

			// Update the content box position since the scrollbar has been updated.
			chatContentBox.anchoredPosition = Vector2.Lerp( new Vector2( chatContentBox.anchoredPosition.x, 0 ), bottomContentPosition, scrollbarHandle.anchoredPosition.y == 0 ? 0 : scrollbarHandle.anchoredPosition.y / scrollbarBottomPosition.y );

			// Update the chat box visibility since the content has been moved.
			UpdateChatBoxVisibility();
		}

		/// <summary>
		/// [INTERNAL] Updates the visibility of all the chats in the chat box and makes sure everything is displayed correctly.
		/// </summary>
		private void UpdateChatBoxVisibility ()
		{
			// If there are no chats registered, then just return.
			if( ChatInformations.Count == 0 )
				return;

			// Calculate the current visibility rect of the chat box for checking if the chat object is visible or not.
			chatBoxVisiblityRect = new Rect( new Vector2( 0, chatContentBox.anchoredPosition.y ), visibleChatBoundingBox.sizeDelta );

			// Store a temporary list for integers of the chats that will need objects.
			List<int> chatIndexThatNeedText = new List<int>();

			// Loop through all the registered chats.
			for( int i = 0; i < ChatInformations.Count; i++ )
			{
				// Update IsVisible of this chat.
				ChatInformations[ i ].UpdateVisibility();

				// If the chat is visible, but does not have a text object assigned, then add this index to the list.
				if( ChatInformations[ i ].IsVisible && ChatInformations[ i ].chatText == null )
					chatIndexThatNeedText.Add( i );
				// Else if the chat is NOT visible and the chat text object IS assigned, then send the text object to the pool.
				else if( !ChatInformations[ i ].IsVisible && ChatInformations[ i ].chatText != null )
				{
					SendTextToPool( ChatInformations[ i ].chatText );
					ChatInformations[ i ].chatText = null;
				}
			}

			// Loop through all the needed chat indexes.
			for( int i = 0; i < chatIndexThatNeedText.Count; i++ )
			{
				// Get a chat text object from the pool, update the text, size and position of the chat.
				ChatInformations[ chatIndexThatNeedText[ i ] ].chatText = GetTextFromPool();
				ChatInformations[ chatIndexThatNeedText[ i ] ].UpdateText();
				ChatInformations[ chatIndexThatNeedText[ i ] ].chatText.rectTransform.sizeDelta = new Vector2( chatContentBox.sizeDelta.x, ChatInformations[ chatIndexThatNeedText[ i ] ].contentSpace );
				ChatInformations[ chatIndexThatNeedText[ i ] ].chatText.rectTransform.anchoredPosition = ChatInformations[ chatIndexThatNeedText[ i ] ].anchoredPosition;
			}

			// If the last chat in the list is visible, then the chat box is NOT at a custom position.
			chatBoxPositionCustom = !ChatInformations[ ChatInformations.Count - 1 ].IsVisible;
		}

		/// <summary>
		/// [INTERNAL] Repositions all the registered chats to make sure that their positions are up to date.
		/// </summary>
		private void RepositionAllChats ()
		{
			// If there are no chats in the chat box, just return.
			if( ChatInformations.Count == 0 )
				return;

			// Reset the total content space value.
			totalContentSpace = 0.0f;

			// Loop through all the registered chats...
			for( int i = 0; i < ChatInformations.Count; i++ )
			{
				// Store the anchored position for the chat.
				ChatInformations[ i ].anchoredPosition = new Vector2( 0, -totalContentSpace );

				// If the chat text is assigned, then send the text to the pool since everything will be shifted.
				if( ChatInformations[ i ].chatText != null )
				{
					SendTextToPool( ChatInformations[ i ].chatText );
					ChatInformations[ i ].chatText = null;
				}

				// If this chat index is at the very end of the list and the user wants some space between messages, then force the calculated chat content to not have any space between chats so that it looks correct.
				if( i == ChatInformations.Count - 1 && spaceBetweenChats > 0.0f )
					ChatInformations[ i ].contentSpace = ChatInformations[ i ].lineCount * ChatInformations[ i ].lineHeight;

				// Increase the total content space value for the next chat.
				totalContentSpace += ChatInformations[ i ].contentSpace;
			}

			// Make sure that the content area size is up to date, and constrain the content box to ensure it's within range of the chat box.
			UpdateChatBoxComponentSizes();
			ConstrainContentBox();

			// If the user wants to have interactable usernames...
			if( useInteractableUsername )
			{
				// Loop through all the chats, and calculate the rect of the username.
				for( int i = 0; i < ChatInformations.Count; i++ )
					CalculateUsernameRect( ChatInformations[ i ] );

				// Reset the stored index since everything has been shifted.
				CurrentHoveredChatIndex = -1;

				// If there was a username highlighted before, then reset the controller and image color.
				if( UsernameHighlighted )
				{
					UsernameHighlighted = false;
					interactableUsernameImage.color = Color.clear;
				}
			}
		}

		/// <summary>
		/// [INTERNAL] This function is called by Unity when the parent of this transform changes.
		/// </summary>
		private void OnTransformParentChanged ()
		{
			ParentCanvas = null;

			// Store the parent of this object.
			Transform parent = transform.parent;

			// If the parent is null, then just return.
			if( parent == null )
				return;

			// While the parent is assigned...
			while( parent != null )
			{
				// If the parent object has a Canvas component, then assign the ParentCanvas and transform.
				if( parent.transform.GetComponent<Canvas>() )
				{
					ParentCanvas = parent.transform.GetComponent<Canvas>();
					parentCanvasRectTrans = ParentCanvas.GetComponent<RectTransform>();
					return;
				}

				// If the parent does not have a canvas, then store it's parent to loop again.
				parent = parent.transform.parent;
			}
		}

		/// <summary>
		/// [INTERNAL] Returns the first available text from the pool if there are some in the pool, otherwise creates a new text object to use.
		/// </summary>
		private TextMeshProUGUI GetTextFromPool ()
		{
			// Temporary text component to return.
			TextMeshProUGUI chatText;

			// If there are available text components in the UnusedTextPool, then use the first available one and remove it from the list.
			if( UnusedTextPool.Count > 0 )
			{
				chatText = UnusedTextPool[ 0 ];
				UnusedTextPool.RemoveAt( 0 );
			}
			// Else there are no text components available from the pool...
			else
			{
				// Instantiate a new chat object based off of the text prefab object.
				GameObject newChatTextObj = Instantiate( textObject.gameObject, chatContentBox.transform );

				// Assign the name to be just "ChatText".
				newChatTextObj.name = "ChatText";

				// Since the text prefab gameObject is disabled, enable the new object.
				newChatTextObj.SetActive( true );

				// Add the text component to the list of all the text objects.
				AllTextObjects.Add( newChatTextObj.GetComponent<TextMeshProUGUI>() );

				// Assign the created chat text object.
				chatText = newChatTextObj.GetComponent<TextMeshProUGUI>();
			}

			// Enable the text visually.
			chatText.color = textColor;

			// Return the text that was found/created.
			return chatText;
		}

		/// <summary>
		/// [INTERNAL] Sends the chat text object to the unused pool for later use.
		/// </summary>
		private void SendTextToPool ( TextMeshProUGUI chatText )
		{
			// Add the text to the pool.
			UnusedTextPool.Add( chatText );

			// Make the text invisible.
			chatText.color = Color.clear;
		}

		/// <summary>
		/// [INTERNAL] Adds chat to the chat box.
		/// </summary>
		/// <param name="username">The username associated with this chat.</param>
		/// <param name="message">The message of this chat.</param>
		/// <param name="style">The style to apply to the chat.</param>
		private void RegisterChatInternal ( string username, string message, ChatStyle style )
		{
			// If there is more than one chat in the chat box already, and the user wants some space between the chats...
			if( ChatInformations.Count > 0 && spaceBetweenChats > 0.0f )
			{
				// Increase the last chat's content space by the users defined "spaceBetweenChats" value.
				ChatInformations[ ChatInformations.Count - 1 ].contentSpace += LineHeight * spaceBetweenChats;

				// Adjust the total content space since the last chat has some added space.
				totalContentSpace += LineHeight * spaceBetweenChats;

				// If the chat is visible, then update the text transform size to reflect the new content size.
				if( ChatInformations[ ChatInformations.Count - 1 ].chatText != null )
					ChatInformations[ ChatInformations.Count - 1 ].chatText.rectTransform.sizeDelta = new Vector2( chatContentBox.sizeDelta.x, ChatInformations[ ChatInformations.Count - 1 ].contentSpace );
			}

			// Create a new chat info and assign the chat box as this component.
			ChatInformation newChat = new ChatInformation { chatBox = this };

			// If the username was provided, then register it to the chat info.
			if( username != string.Empty )
			{
				// Store the internal username to use for sending information about this user.
				newChat.Username = username;

				// If the provided username contains a number symbol, then store the external username as the first split of the string.
				if( username.Contains( "#" ) )
					newChat.DisplayUsername = username.Split( '#' )[ 0 ];
				// Else just store the provided username.
				else
					newChat.DisplayUsername = username;
			}

			// Store the direct message value.
			newChat.Message = message;

			// Store the message that should be actually displayed on the chat, according to the users options.
			newChat.DisplayMessage = ( username != string.Empty ? $"{( !style.noUsernameFollowupText ? usernameFollowup : " " )}" : "" ) + message;

			// Store the style provided.
			newChat.chatBoxStyle = style;

			// Get a chat from the pool (this function will create a new chat text object if needed).
			newChat.chatText = GetTextFromPool();

			// If there was a username provided, store the username width by the preferred values of the username after formatting.
			if( username != string.Empty )
				newChat.usernameWidth = newChat.chatText.GetPreferredValues( style.FormatUsernameOnly( newChat.DisplayUsername ) ).x;

			// Update the text of this chat info to display the whole chat content.
			newChat.UpdateText();

			// Force a mesh update for the text here so that the line count and height can be calculated.
			newChat.chatText.ForceMeshUpdate();
			newChat.lineCount = newChat.chatText.textInfo.lineCount;
			newChat.lineHeight = newChat.chatText.renderedHeight / newChat.lineCount;

			// If the calculated line height is less than a very small value, then likely it's an almost infinite number, so just use the LineHeight of the text object. This can happen if something gets registered that shouldn't be, like only spaces.
			if( newChat.lineHeight < 0.1f )
				newChat.lineHeight = LineHeight;

			// Apply the size and position to the chat text transform.
			newChat.chatText.rectTransform.sizeDelta = new Vector2( chatContentBox.sizeDelta.x, newChat.lineHeight * newChat.lineCount );
			newChat.chatText.rectTransform.anchoredPosition = new Vector2( 0, -totalContentSpace );

			// Store the anchored position value for reference when this chat returns to a visible state.
			newChat.anchoredPosition = newChat.chatText.rectTransform.anchoredPosition;

			// Store the amount of space that this text object requires.
			newChat.contentSpace = newChat.chatText.rectTransform.sizeDelta.y;

			// Calculate and store the username rect values for interactable usernames.
			CalculateUsernameRect( newChat );

			// Add this chat information to the list.
			ChatInformations.Add( newChat );

			// Add the content space of this chat to the total space of the content.
			totalContentSpace += newChat.contentSpace;

			// If the chat information list has more entries than the max text allowed in the chat box...
			if( maxTextInChatBox > 0 && ChatInformations.Count > maxTextInChatBox )
			{
				// If the chat text object is assigned for the first index in the list (which means that it is currently visible), send it to pool.
				if( ChatInformations[ 0 ].chatText != null )
					SendTextToPool( ChatInformations[ 0 ].chatText );

				// Remove the first chat information in the list since it has exceeded the max chats allowed.
				ChatInformations.RemoveAt( 0 );

				// Since the first chat has been removed, all the other chats need to reposition themselves.
				RepositionAllChats();
			}

			// Inform any subscribers that a chat has been registered.					
			OnChatRegistered?.Invoke( newChat );

			// If the chat box game object is inactive...
			if( !gameObject.activeInHierarchy )
			{
				// Send the text to the pool since it's not visible anyways, nullify the stored chatText, then return since no positioning needs to happen.
				SendTextToPool( newChat.chatText );
				newChat.chatText = null;
				return;
			}

			// The content area has changed because a chat has been added, so update the content area size. This will update the scrollbar to match it as well.
			UpdateChatBoxComponentSizes();

			// If the user doesn't have a custom navigation position on the chat box, then anchor the content at the bottom so the newly added chat is visible.
			if( !chatBoxPositionCustom )
				chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

			// Constrain the content box, which will also update the scrollbar and check visibility for each chat.
			ConstrainContentBox();
		}

		/// <summary>
		/// [INTERNAL] Formats and sends detailed information to the user.
		/// </summary>
		private static string FormatDebug ( string error, string solution, string objectName )
		{
			return "<b>Ultimate Chat Box</b>\n" +
				"<color=red><b>×</b></color> <i><b>Error:</b></i> " + error + ".\n" +
				"<color=green><b>√</b></color> <i><b>Solution:</b></i> " + solution + ".\n" +
				"<color=cyan><b>∙</b></color> <i><b>Object:</b></i> " + objectName + "\n";
		}
		// ---------------------- <END INTERNAL FUNCTIONS> ---------------------- //

		// -------------------- <PUBLIC FUNCTIONS FOR THE USER> -------------------- //
		/// <summary>
		/// Updates the positioning of the chat box on the screen. Only call this function if you have changed some positioning settings and you want to apply the changed position.
		/// </summary>
		public void UpdatePositioning ()
		{
			// If the parent canvas is null, then try to get the parent canvas component.
			if( ParentCanvas == null )
				OnTransformParentChanged();

			// If it is still null, then log a error and return.
			if( ParentCanvas == null )
			{
				if( Application.isPlaying )
					Debug.LogError( FormatDebug( "There is no parent canvas object", "Please make sure that the Ultimate Chat Box is placed within a canvas object in your scene", gameObject.name ) );
				return;
			}

			// Store the rect trans size. If the canvas size is ever different, this function will run since the position will need to be updated.
			parentCanvasSize = parentCanvasRectTrans.sizeDelta;
			parentCanvasScale = parentCanvasRectTrans.localScale;

			// Set the current reference size for scaling.
			float referenceSize = Mathf.Min( parentCanvasSize.y, parentCanvasSize.x );

			// Configure the size of the base of the chat box based on the users options.
			BaseTransform.sizeDelta = ( chatBoxSize / 10 ) * referenceSize * chatBoxSizeRatio * Vector2.one;

			// If the base transform size is zero, then return to avoid errors (and also it's pointless because it would not be visible at all with a size of zero).
			if( BaseTransform.sizeDelta.x <= 0.0f || BaseTransform.sizeDelta.y <= 0.0f )
				return;

			// Position on canvas - ( offset of the chat box transform to fit correctly INSIDE the screen, not outside ) + half the chat box size to center with anchors - ( half the canvas size to start in the bottom left ).
			Vector2 _chatBoxPosition = parentCanvasSize * ( chatBoxPosition / 100 ) - ( BaseTransform.sizeDelta * ( chatBoxPosition / 100 ) ) + new Vector2( BaseTransform.sizeDelta.x / 2, 0 ) - ( parentCanvasSize / 2 );

			// Store the total chat box size as the base transform size initially.
			TotalChatBoxSize = BaseTransform.sizeDelta;

			// Force anchors of the base transform to ensure that it functions correctly.
			BaseTransform.anchorMin = new Vector2( 0.5f, 0.0f );
			BaseTransform.anchorMax = new Vector2( 0.5f, 0.0f );
			BaseTransform.pivot = new Vector2( 0.5f, 0.0f );
			BaseTransform.localScale = Vector3.one;

			// If the needed rect transform boxes are unassigned, then just return to avoid errors.
			if( visibleChatBoundingBox == null || chatContentBox == null )
				return;

			// Force anchors for the visible bounding box.
			visibleChatBoundingBox.anchorMin = new Vector2( 0.5f, 0.5f );
			visibleChatBoundingBox.anchorMax = new Vector2( 0.5f, 0.5f );
			visibleChatBoundingBox.pivot = new Vector2( 0.5f, 0.5f );

			// Apply the size of the visible bounding box to be the BaseTransform size delta - the users vertical spacing percentage.
			visibleChatBoundingBox.sizeDelta = BaseTransform.sizeDelta - new Vector2( 0, Mathf.Max( BaseTransform.sizeDelta.x, BaseTransform.sizeDelta.y ) * ( verticalSpacing / 50 ) );

			// If somehow the visible bounding box is zero, then force the size to be the same as the base transform to avoid errors.
			if( visibleChatBoundingBox.sizeDelta.x <= 0.0 || visibleChatBoundingBox.sizeDelta.y <= 0.0 )
				visibleChatBoundingBox.sizeDelta = BaseTransform.sizeDelta;

			// Set the anchored position as X = 0, position inside the chat box - percentage of the visible box so that it fits inside the chat box base.
			visibleChatBoundingBox.anchoredPosition = new Vector2( 0, ( BaseTransform.sizeDelta.y * ( contentPosition.y / 100 ) ) - ( visibleChatBoundingBox.sizeDelta.y * ( contentPosition.y / 100 ) ) );

			// Store the size of the visible bounding box and base transform for reference.
			visibleBoundingBoxSize = visibleChatBoundingBox.sizeDelta;
			baseTransformSize = BaseTransform.sizeDelta;

			// Force anchors for the content box.
			chatContentBox.anchorMin = new Vector2( 0.5f, 1.0f );
			chatContentBox.anchorMax = new Vector2( 0.5f, 1.0f );
			chatContentBox.pivot = new Vector2( 0.5f, 1.0f );

			// Apply the size and position to the content box.
			chatContentBox.sizeDelta = visibleChatBoundingBox.sizeDelta - new Vector2( Mathf.Max( BaseTransform.sizeDelta.x, BaseTransform.sizeDelta.y ) * ( horizontalSpacing / 50 ), 0 );
			chatContentBox.anchoredPosition = new Vector2( ( BaseTransform.sizeDelta.x * ( contentPosition.x / 100 ) ) - ( chatContentBox.sizeDelta.x * ( contentPosition.x / 100 ) ), 0 );

			// If the content box size is zero, then force the size to be the same as the base to avoid errors.
			if( chatContentBox.sizeDelta.x <= 0.0 || chatContentBox.sizeDelta.y <= 0.0 )
				chatContentBox.sizeDelta = BaseTransform.sizeDelta;

			// If the user wants a scrollbar for the chat box, and the needed objects are assigned...
			if( useScrollbar && scrollbarBase != null && scrollbarHandle != null )
			{
				// Force anchors and pivot of the scrollbar base to ensure that it functions correctly.
				scrollbarBase.anchorMin = new Vector2( 0.5f, 0.5f );
				scrollbarBase.anchorMax = new Vector2( 0.5f, 0.5f );
				scrollbarBase.pivot = new Vector2( 0.5f, 0.5f );

				// Apply the size to the scrollbar base.
				scrollbarBase.sizeDelta = new Vector2( visibleBoundingBoxSize.x * scrollbarWidth, visibleBoundingBoxSize.y );

				// Apply the local position = base trans size * position percentage - scrollbar base size * position percentage
				scrollbarBase.localPosition = new Vector3( BaseTransform.sizeDelta.x * ( scrollbarHorizontalPosition / 100 ) - ( scrollbarBase.sizeDelta.x * ( scrollbarHorizontalPosition / 100 ) ), visibleChatBoundingBox.localPosition.y, 0 );

				// If the user wants navigation arrows for the scrollbar...
				if( useNavigationArrows && navigationArrowUp != null && navigationArrowDown != null )
				{
					// Store the sprite aspect ratio.
					Vector2 navArrowSpriteRatio = Vector2.one;
					if( navigationArrowUp.sprite != null )
						navArrowSpriteRatio = navigationArrowUp.sprite.rect.size / Mathf.Max( navigationArrowUp.sprite.rect.width, navigationArrowUp.sprite.rect.height );

					// Force anchors of the navigation arrow to ensure that it functions correctly.
					navigationArrowUp.rectTransform.anchorMin = new Vector2( 0.5f, 1 );
					navigationArrowUp.rectTransform.anchorMax = new Vector2( 0.5f, 1 );
					navigationArrowUp.rectTransform.pivot = new Vector2( 0.5f, 0 );

					// Apply the size and position.
					navigationArrowUp.rectTransform.sizeDelta = Vector2.one * scrollbarBase.sizeDelta.x * navArrowSpriteRatio;
					navigationArrowUp.rectTransform.anchoredPosition = Vector2.zero;

					// Force anchors of the navigation arrow to ensure that it functions correctly.
					navigationArrowDown.rectTransform.anchorMin = new Vector2( 0.5f, 0 );
					navigationArrowDown.rectTransform.anchorMax = new Vector2( 0.5f, 0 );
					navigationArrowDown.rectTransform.pivot = new Vector2( 0.5f, 0 );

					// Apply the size and position.
					navigationArrowDown.rectTransform.anchoredPosition = Vector2.zero;
					navigationArrowDown.rectTransform.sizeDelta = Vector2.one * scrollbarBase.sizeDelta.x * navArrowSpriteRatio;

					// Adjust the size of the scrollbar base to compensate for the navigation arrows.
					scrollbarBase.sizeDelta -= new Vector2( 0, navigationArrowUp.rectTransform.sizeDelta.y * 2 );

					// Apply the normal color to the arrows.
					navigationArrowUp.color = navigationNormalColor;
					navigationArrowDown.color = navigationNormalColor;

					scrollbarNavigationArrowUpHovered = false;
					scrollbarNavigationArrowUpActivated = false;
					scrollbarNavigationArrowDownHovered = false;
					scrollbarNavigationArrowDownActivated = false;
				}

				// Force anchors of the scrollbar handle to ensure that it functions correctly.
				scrollbarHandle.anchorMin = new Vector2( 0.5f, 1 );
				scrollbarHandle.anchorMax = new Vector2( 0.5f, 1 );
				scrollbarHandle.pivot = new Vector2( 0.5f, 1 );

				// Apply the size and position of the handle.
				scrollbarHandle.sizeDelta = new Vector2( scrollbarBase.sizeDelta.x, scrollbarBase.sizeDelta.y / 4 );
				scrollbarHandle.anchoredPosition = new Vector2( 0, -scrollbarBase.sizeDelta.y + scrollbarHandle.sizeDelta.y );

				// If the scrollbar is outside of the right side of the base image...
				if( scrollbarBase.localPosition.x + ( scrollbarBase.sizeDelta.x / 2 ) > BaseTransform.sizeDelta.x / 2 )
				{
					// Compensate for the difference in position including how much the scrollbar is out of the chat box base.
					TotalChatBoxSize = new Vector2( TotalChatBoxSize.x + ( scrollbarBase.localPosition.x + ( scrollbarBase.sizeDelta.x / 2 ) ) - ( BaseTransform.sizeDelta.x / 2 ), TotalChatBoxSize.y );

					// Adjust the horizontal position of the chat box.
					_chatBoxPosition.x = parentCanvasSize.x * ( chatBoxPosition.x / 100 ) - ( TotalChatBoxSize.x * ( chatBoxPosition.x / 100 ) ) + ( ( BaseTransform.sizeDelta.x / 2 ) - ( parentCanvasSize.x / 2 ) );
				}
				// Else if the scrollbar is out of the left side of the base image...
				else if( scrollbarBase.localPosition.x - ( scrollbarBase.sizeDelta.x / 2 ) < -( BaseTransform.sizeDelta.x / 2 ) )
				{
					float horizontalDifference = -( BaseTransform.sizeDelta.x / 2 ) - ( scrollbarBase.localPosition.x - ( scrollbarBase.sizeDelta.x / 2 ) );

					// Increase the reference total size by the vertical difference above.
					TotalChatBoxSize = new Vector2( TotalChatBoxSize.x + horizontalDifference, TotalChatBoxSize.y );

					// Position on canvas - ( position percentage applied to size to offset ) + chat box size/2  to center with anchors, vertical difference makes the new bottom the input field - ( half the canvas size to start in the bottom left ).
					_chatBoxPosition.x = parentCanvasSize.x * ( chatBoxPosition.x / 100 ) - ( TotalChatBoxSize.x * ( chatBoxPosition.x / 100 ) ) + horizontalDifference + ( ( BaseTransform.sizeDelta.x / 2 ) - ( parentCanvasSize.x / 2 ) );
				}
			}

			// If the user wants to have an input field provided for the player and it's assigned...
			if( useInputField && inputField != null )
			{
				// Apply the size and position to the input field.
				inputFieldTransform.sizeDelta = baseTransformSize * ( inputFieldSize / 100 );
				inputFieldTransform.localPosition = BaseTransform.sizeDelta * ( inputFieldPosition / 100 ) - ( inputFieldTransform.sizeDelta * ( inputFieldPosition / 100 ) );

				// Apply the size and position to the viewport of the chat box.
				inputField.textViewport.sizeDelta = inputFieldTransform.sizeDelta * ( new Vector2( inputFieldTextAreaSize.x, inputFieldTextAreaSize.y ) / 100 );
				inputField.textViewport.anchoredPosition = new Vector2( inputFieldTransform.sizeDelta.x * ( inputFieldTextHorizontalPosition / 100 ), 0 ) - new Vector2( inputField.textViewport.sizeDelta.x * ( inputFieldTextHorizontalPosition / 100 ), 0 );

				// Center the text and placeholder positions.
				inputField.textComponent.rectTransform.localPosition = Vector3.zero;
				inputField.placeholder.rectTransform.localPosition = Vector3.zero;

				// Store the input field line height for calculations.
				float inputFieldLineHeight = LineHeight;

				// If the placeholder has a text mesh component...
				if( inputField.placeholder.GetComponent<TextMeshProUGUI>() )
				{
					// Force a mesh update and then store the line height of the placeholder.
					inputField.placeholder.GetComponent<TextMeshProUGUI>().ForceMeshUpdate();
					inputFieldLineHeight = inputField.placeholder.GetComponent<TextMeshProUGUI>().renderedHeight;
				}

				// Apply a consistent padding to the input field so that the text will not be cut off for letters that hang down or italic fonts.
				inputField.textViewport.GetComponent<RectMask2D>().padding = new Vector4( -inputFieldLineHeight / 8, -inputFieldLineHeight / 4, 0, 0 );

				// If the user has a smart font size set for the input field...
				if( inputFieldSmartFontSize > 0.0f )
				{
					// Apply the calculated font size to the input field (rounding to an integer so that it will look correct and more crisp).
					inputField.pointSize = ( int )( inputField.textViewport.sizeDelta.y * inputFieldSmartFontSize );

					// If the calculated size is zero, then set it to one to avoid errors.
					if( inputField.pointSize <= 0 )
						inputField.pointSize = 1;
				}

				// Apply the font size of the input field to the child components.
				inputField.textComponent.fontSize = inputField.pointSize;
				inputField.placeholder.GetComponent<TextMeshProUGUI>().fontSize = inputField.pointSize;

				// If the user wants an extra image and it's assigned...
				if( useExtraImage && extraImage != null )
				{
					// Apply the size and position of the extra image.
					extraImage.rectTransform.sizeDelta = inputFieldTransform.sizeDelta * ( new Vector2( extraImageWidth, extraImageHeight ) / 100 );
					extraImage.rectTransform.anchoredPosition = new Vector2( inputFieldTransform.sizeDelta.x * ( extraImageHorizontalPosition / 100 ), 0 ) - new Vector2( extraImage.rectTransform.sizeDelta.x * ( extraImageHorizontalPosition / 100 ), 0 );
				}

				// If the user is allowing emojis in the chat box, and the user also wants a window to be displayed with the emojis to the players...
				if( useTextEmoji && useEmojiWindow )
				{
					// If the emoji button image is assigned...
					if( emojiButtonImage != null )
					{
						// Force the anchors and pivot.
						emojiButtonImage.rectTransform.anchorMin = new Vector2( 0.5f, 0.5f );
						emojiButtonImage.rectTransform.anchorMax = new Vector2( 0.5f, 0.5f );
						emojiButtonImage.rectTransform.pivot = new Vector2( 0.5f, 0.5f );

						// Apply the size and position to the rect transform.
						emojiButtonImage.rectTransform.sizeDelta = ( Vector2.one * inputFieldTransform.sizeDelta.y ) * emojiButtonSize;
						emojiButtonImage.rectTransform.anchoredPosition = inputFieldTransform.sizeDelta * ( new Vector2( ( emojiButtonHorizontalPosition - 50 ) / 100, 0.0f ) ) - new Vector2( emojiButtonImage.rectTransform.sizeDelta.x * ( ( emojiButtonHorizontalPosition - 50 ) / 100 ), 0 );
					}

					// If the emoji window image is assigned...
					if( emojiWindowImage != null )
					{
						// Force the anchors and pivot.
						emojiWindowImage.rectTransform.anchorMin = new Vector2( 1.0f, 0.0f );
						emojiWindowImage.rectTransform.anchorMax = new Vector2( 1.0f, 0.0f );
						emojiWindowImage.rectTransform.pivot = new Vector2( 0.0f, 0.0f );

						// Apply the size and position to the rect transform.
						emojiWindowImage.rectTransform.sizeDelta = ( Vector2.one * TotalChatBoxSize.y ) * ( new Vector2( emojiWindowSize.x, emojiWindowSize.y ) / 100 );
						emojiWindowImage.rectTransform.anchoredPosition = ( baseTransformSize + new Vector2( 0, inputFieldTransform.sizeDelta.y + ( baseTransformSize.y * ( -inputFieldPosition.y / 100 ) ) ) ) * ( new Vector2( emojiWindowPosition.x, emojiWindowPosition.y ) / 100 );

						// If the emoji text is assigned...
						if( emojiText != null )
						{
							// Apply the calculated margin for the text to give it some padding that will be consistent across all screen sizes.
							emojiText.margin = Vector4.one * ( emojiWindowImage.rectTransform.sizeDelta.x * emojiTextEdgePadding );

							// Force the anchors and pivot.
							emojiText.rectTransform.anchorMin = Vector2.zero;
							emojiText.rectTransform.anchorMax = Vector2.one;
							emojiText.rectTransform.pivot = new Vector2( 0.5f, 0.5f );

							// Zero out the position.
							emojiText.rectTransform.anchoredPosition = Vector2.zero;
						}
					}
				}

				// If the input fields position is outside the top of the base transform...
				if( inputFieldTransform.localPosition.y > BaseTransform.sizeDelta.y )
				{
					// Increase the reference total size for the chat box by the difference of the top of the input field and the top of the base transform.
					TotalChatBoxSize = new Vector2( TotalChatBoxSize.x, TotalChatBoxSize.y + inputFieldTransform.localPosition.y - BaseTransform.sizeDelta.y );

					// Recalculate the chat box position to use the new totalSizeWithInputField size.
					_chatBoxPosition.y = parentCanvasSize.y * ( chatBoxPosition.y / 100 ) - ( TotalChatBoxSize.y * ( chatBoxPosition.y / 100 ) ) - ( parentCanvasSize.y / 2 );
				}
				// Else if the bottom of the input field is outside of the bottom of the base transform...
				else if( inputFieldTransform.localPosition.y - inputFieldTransform.sizeDelta.y < 0.0f )
				{
					// Increase the reference total size by the vertical difference above.
					TotalChatBoxSize = new Vector2( TotalChatBoxSize.x, TotalChatBoxSize.y + ( inputFieldTransform.sizeDelta.y - inputFieldTransform.localPosition.y ) );

					// Position on canvas - ( position percentage applied to size to offset ) + chat box size/2  to center with anchors, vertical difference makes the new bottom the input field - ( half the canvas size to start in the bottom left ).
					_chatBoxPosition.y = parentCanvasSize.y * ( chatBoxPosition.y / 100 ) - ( TotalChatBoxSize.y * ( chatBoxPosition.y / 100 ) ) + ( inputFieldTransform.sizeDelta.y - inputFieldTransform.localPosition.y ) - ( parentCanvasSize.y / 2 );
				}
			}

			// Apply the calculated position.
			BaseTransform.localPosition = _chatBoxPosition;

			// If the text object to use as a base for all the texts is assigned...
			if( textObject != null )
			{
				// If the text object is not active, set it active.
				if( !textObject.gameObject.activeInHierarchy )
					textObject.gameObject.SetActive( true );

				// Set some default text so that everything can be calculated.
				textObject.text = $"<b>Username</b>{usernameFollowup}Message";

				// If the user wants to allow emojis in chat and the emoji asset is assigned, then add a emoji to the text value.
				if( useTextEmoji && emojiAsset != null )
					textObject.text += " <sprite=0>";

				// If the user wants to calculate a smart font size to keep it consistent across all screens...
				if( smartFontSize > 0.0f )
				{
					// Calculate and store the font size (forcing it to an integer to make sure it displays correctly).
					textObject.fontSize = ( int )( baseTransformSize.y * smartFontSize );

					// If the calculated font size is 0, then set it to 1 to avoid errors.
					if( textObject.fontSize <= 0 )
						textObject.fontSize = 1;

					// Loop through all the text objects in the chat box and apply the new font size.
					for( int i = 0; i < AllTextObjects.Count; i++ )
						AllTextObjects[ i ].fontSize = textObject.fontSize;
				}

				// Store the chat's RectTransform for modifications.
				RectTransform chatTextTrans = textObject.GetComponent<RectTransform>();

				// Force the anchor and pivot.
				chatTextTrans.anchorMin = new Vector2( 0.5f, 1 );
				chatTextTrans.anchorMax = new Vector2( 0.5f, 1 );
				chatTextTrans.pivot = new Vector2( 0.5f, 1 );

				// Apply the size and position to the text.
				chatTextTrans.sizeDelta = new Vector2( chatContentBox.sizeDelta.x, textObject.renderedHeight );
				chatTextTrans.anchoredPosition = Vector2.zero;

				// If the application is actually playing currently...
				if( Application.isPlaying )
				{
					// Force an update to the text mesh so that it's references will be up to date.
					textObject.ForceMeshUpdate();

					// Store the render height of the text object as the average line height for navigation and other settings.
					LineHeight = textObject.renderedHeight;

					// Reset the total space used so it will be accurate.
					totalContentSpace = 0.0f;

					// Loop through all the chats if there are any...
					for( int i = 0; i < ChatInformations.Count; i++ )
					{
						// If the chat information is visible and has a text currently, then send it to the text pool.
						if( ChatInformations[ i ].chatText != null )
							SendTextToPool( ChatInformations[ i ].chatText );

						// Set the chat informations text to the text object for calculations.
						ChatInformations[ i ].chatText = textObject;

						// Store the username width as the preferred values of the username.
						if( ChatInformations[ i ].DisplayUsername != null )
							ChatInformations[ i ].usernameWidth = ChatInformations[ i ].chatText.GetPreferredValues( ChatInformations[ i ].chatBoxStyle.FormatUsernameOnly( ChatInformations[ i ].DisplayUsername ) ).x;

						// Update the text so that the text value is the whole message of this chat and force a mesh update on it.
						ChatInformations[ i ].UpdateText();
						ChatInformations[ i ].chatText.ForceMeshUpdate();

						// Store the line count and line height.
						ChatInformations[ i ].lineCount = ChatInformations[ i ].chatText.textInfo.lineCount;
						ChatInformations[ i ].lineHeight = ChatInformations[ i ].chatText.renderedHeight / ChatInformations[ i ].lineCount;

						// Apply the size and position of the chat.
						ChatInformations[ i ].chatText.rectTransform.sizeDelta = new Vector2( chatContentBox.sizeDelta.x, ChatInformations[ i ].lineHeight * ChatInformations[ i ].lineCount );
						ChatInformations[ i ].anchoredPosition = new Vector2( 0, -totalContentSpace );

						// Store the vertical content space that it will occupy in the chat box.
						ChatInformations[ i ].contentSpace = ChatInformations[ i ].chatText.rectTransform.sizeDelta.y;

						// Calculate the username rect for this chat.
						CalculateUsernameRect( ChatInformations[ i ] );

						// Nullify the referenced chat text.
						ChatInformations[ i ].chatText = null;

						// Increase the chat content space by the users defined "spaceBetweenChats" value.
						if( spaceBetweenChats > 0.0f && i < ChatInformations.Count - 1 )
							ChatInformations[ i ].contentSpace += ( LineHeight * spaceBetweenChats );

						// Increase the total content space value for the next chat.
						totalContentSpace += ChatInformations[ i ].contentSpace;
					}

					// Since the game is running, then turn the object off and reset the text value.
					textObject.gameObject.SetActive( false );
					textObject.text = "";
				}

				// If the user wants usernames to be interactable, the image is assigned, and the game is NOT playing...
				if( useInteractableUsername && interactableUsernameImage != null && !Application.isPlaying )
				{
					// Set the size and position to display the username highlight image over the text object username.
					interactableUsernameImage.rectTransform.sizeDelta = new Vector2( textObject.GetPreferredValues( "<b>Username</b>" ).x, textObject.renderedHeight / textObject.textInfo.lineCount );
					interactableUsernameImage.rectTransform.anchoredPosition = new Vector2( -chatContentBox.sizeDelta.x / 2 + ( interactableUsernameImage.rectTransform.sizeDelta.x / 2 ), -interactableUsernameImage.rectTransform.sizeDelta.y / 2 );

					// If the user wants to add some width to the image, then apply it now the anchored position has been applied.
					if( interactableUsernameWidthModifier > 0.0f )
						interactableUsernameImage.rectTransform.sizeDelta = new Vector2( textObject.GetPreferredValues( "<b>Username</b>" ).x + ( textObject.renderedHeight * interactableUsernameWidthModifier ), textObject.renderedHeight / textObject.textInfo.lineCount );
				}
			}

			// If the application is NOT playing, then just return.
			if( !Application.isPlaying )
				return;

			// If the user wants to collapse the chat box when it is disabled...
			if( collapseWhenDisabled )
			{
				// Store the size of the visible bounding box when it is collapsed.
				boundingBoxCollapsedSize = new Vector2( BaseTransform.sizeDelta.x, ( LineHeight * visibleLineCount ) + ( ( LineHeight * ( visibleLineCount - 1 ) ) * spaceBetweenChats ) );

				// Store the size of the base transform when the chat box is going to be collapsed.
				baseTransformCollapsedSize = new Vector2( BaseTransform.sizeDelta.x, boundingBoxCollapsedSize.y + ( BaseTransform.sizeDelta.x * ( verticalSpacing / 50 ) ) );
			}

			// If the user wants to use a scrollbar then the rect for the scrollbar components need to be calculated here since the positioning is finished.
			if( useScrollbar && scrollbarBase != null )
			{
				if( useNavigationArrows && navigationArrowUp != null && navigationArrowDown != null )
				{
					scrollbarNavigationArrowUpRect = CalculateRect( navigationArrowUp.rectTransform );
					scrollbarNavigationArrowDownRect = CalculateRect( navigationArrowDown.rectTransform );
					scrollbarNavigationArrowDownRect.center = new Vector2( scrollbarNavigationArrowDownRect.center.x, scrollbarNavigationArrowDownRect.center.y - scrollbarNavigationArrowDownRect.size.y );
				}
				scrollbarBaseRect = CalculateRect( scrollbarBase );
			}

			// If the user wants to have an input field provided for players...
			if( useInputField && inputField != null )
			{
				// Calculate and store the input fields input rect.
				inputFieldRect = CalculateRect( inputFieldTransform );

				// If the user wants an extra image on the input field and it is assigned, then calculate the input rect for it.
				if( useExtraImage && extraImage != null )
					extraImageRect = CalculateRect( extraImage.rectTransform );

				// If the user also wants to have an emoji window...
				if( useTextEmoji && useEmojiWindow )
				{
					// If the emoji button image is assigned, then store the input rect for the button.
					if( emojiButtonImage != null )
						emojiButtonRect = CalculateRect( emojiButtonImage.rectTransform );

					// If the emoji window is assigned...
					if( emojiWindowImage != null )
					{
						// Store the input rect for the emoji window.
						emojiWindowRect = CalculateRect( emojiWindowImage.rectTransform );

						// Store the canvas group of the emoji window and set the alpha of the window to zero.
						emojiWindowCanvasGroup = emojiWindowImage.gameObject.GetComponent<CanvasGroup>();
						emojiWindowCanvasGroup.alpha = 0.0f;

						// If the emoji text is assigned and the game is running...
						if( emojiText != null && Application.isPlaying )
						{
							// Force an update on the emoji text.
							emojiText.ForceMeshUpdate();

							// Reset the emoji rect list. 
							emojiRects = new List<Rect>();

							// Loop through all the characters of the emoji text...
							for( int i = 0; i < emojiText.textInfo.characterCount; i++ )
							{
								// If the current character is not a sprite, then just skip this index.
								if( emojiText.textInfo.characterInfo[ i ].elementType == TMP_TextElementType.Character )
									continue;

								// Add a new rect for the emoji as the bottom left of the character, and the calculated size.
								emojiRects.Add( new Rect( emojiText.rectTransform.TransformPoint( emojiText.textInfo.characterInfo[ i ].bottomLeft ), ( emojiText.textInfo.characterInfo[ i ].topRight - emojiText.textInfo.characterInfo[ i ].bottomLeft ) * ( Vector2 )parentCanvasScale ) );
							}
						}
					}

					// Set the EmojiWindowEnabled controller to false for reference.
					EmojiWindowEnabled = false;
				}
			}

			// Update the chat box component sizes. This function is made for any rect that moves at runtime (scrollbar handle, up arrow, etc...).
			UpdateChatBoxComponentSizes();

			// Reposition all the chats in the chat box since the position has been updated.
			RepositionAllChats();

			// Set the content box to the bottom position.
			chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

			// Constrain the content box and update it.
			ConstrainContentBox();

			// If the chat box is disabled, then call the DisableChatBoxImmeditate function to make sure the chat box is displaying correctly.
			if( !IsEnabled )
				Disable( true );
		}

		/// <summary>
		/// Adds chat to the chat box instance in the scene.
		/// </summary>
		/// <param name="message">The message content of the chat.</param>
		public void RegisterChat ( string message )
		{
			// Register the message internally with no username and no style.
			RegisterChatInternal( "", message, UltimateChatBoxStyles.none );
		}

		/// <summary>
		/// Adds chat to the chat box with the assigned style.
		/// </summary>
		/// <param name="message">The message content of the chat.</param>
		/// <param name="style">The style for the chat to be displayed in.</param>
		public void RegisterChat ( string message, ChatStyle style )
		{
			// If the style parameter is null, inform the user and assign a default style.
			if( style == null )
			{
				Debug.LogWarning( FormatDebug( "The provided style is null. Defaulting to None for the style", "Please make sure that your style value is actually assigned before sending it in to the Ultimate Chat Box", "Unknown (User Script)" ) );
				style = UltimateChatBoxStyles.none;
			}

			// Register the chat internally with no username and the provided style.
			RegisterChatInternal( "", message, style );
		}

		/// <summary>
		/// Adds the chat, displaying it from the specific user.
		/// </summary>
		/// <param name="username">The username of the person that added the chat.</param>
		/// <param name="message">The message content of the chat.</param>
		public void RegisterChat ( string username, string message )
		{
			// Register the chat internally with no chat box style.
			RegisterChatInternal( username, message, UltimateChatBoxStyles.none );
		}

		/// <summary>
		/// Adds the chat with the assigned style, displaying it from the specific user.
		/// </summary>
		/// <param name="username">The username of the person that added the chat.</param>
		/// <param name="message">The message content of the chat.</param>
		/// <param name="style">The style for the chat to be displayed in.</param>
		public void RegisterChat ( string username, string message, ChatStyle style )
		{
			// If the style parameter is null, inform the user and assign a default style.
			if( style == null )
			{
				Debug.LogWarning( FormatDebug( "The provided style is null. Defaulting to None for the style", "Please make sure that your style value is actually assigned before sending it in to the Ultimate Chat Box", "Unknown (User Script)" ) );
				style = UltimateChatBoxStyles.none;
			}

			// Register the chat internally with the provided style.
			RegisterChatInternal( username, message, style );
		}

		/// <summary>
		/// Enables the chat box visually.
		/// </summary>
		public void Enable ()
		{
			// If the chat box is already enabled, then just return.
			if( IsEnabled )
				return;

			// Set InEnabled to true for reference.
			IsEnabled = true;

			// If the toggle in duration is nothing, then the transition needs to be instant...
			if( fadeInSpeed <= 0.0f )
			{
				// Set the alpha of the canvas group to 1.
				chatBoxCanvasGroup.alpha = 1.0f;

				// Max the lerp value and set toggle out to false since the chat has just been enabled.
				fadeLerpValue = 1.0f;
				fadeOut = false;
			}
			// Else the user has some duration set for the toggle in...
			else
			{
				// Set fadeIn to true for processing the toggle over time.
				fadeIn = true;

				// If the chat box is currently toggling out, then set the toggleOut to false to stop it.
				if( fadeOut )
					fadeOut = false;
			}

			// If the user wants to collapse the chat box when it's disabled...
			if( collapseWhenDisabled )
			{
				// If the expand speed is nothing, then the transition needs to be instant...
				if( expandSpeed <= 0.0f )
				{
					// Set the lerp value to 1 so that the collapse will start at the right point.
					collapseLerpValue = 1.0f;

					// Set the BaseTransform and boundingBox size to expanded.
					BaseTransform.sizeDelta = baseTransformSize;
					visibleChatBoundingBox.sizeDelta = visibleBoundingBoxSize;

					// Update the size of the chat box components that move.
					UpdateChatBoxComponentSizes();

					// Since the size of the chat box has been modified, make sure the content is within range.
					ConstrainContentBox();

					// Update the chat box rect for calculations.
					chatBoxScreenRect = CalculateRect( BaseTransform );
					AdjustChatBoxScreenRect();
				}
				// Else the user has some duration set for the toggle in...
				else
				{
					// Set expandChatBox to true to process the collapse.
					expandChatBox = true;

					// If the chat box was collapsing, then set collapseChatBox to false to stop it from collapsing further.
					if( collapseChatBox )
						collapseChatBox = false;
				}
			}
		}

		/// <summary>
		/// Disables the chat box visually.
		/// </summary>
		/// <param name="instant">If true, disabled the chat box immediately without any smooth transition set from the user.</param>
		public void Disable ( bool instant = false )
		{
			// If the chat box is already disabled, then return.
			if( !IsEnabled && !instant )
				return;

			// Set IsEnabled to false for reference.
			IsEnabled = false;

			// If the user wants to fade the chat box when disabled...
			if( fadeWhenDisabled )
			{
				// If the toggle out duration is set to zero, then the transition needs to be instant.
				if( fadeOutSpeed <= 0.0f || instant )
				{
					// Set the alpha to the users toggled alpha setting.
					chatBoxCanvasGroup.alpha = toggledAlpha;

					// Reset the lerp value and set toggle in to false since the chat has just been disabled.
					fadeLerpValue = 0.0f;
					fadeIn = false;
				}
				// Else the user has actually set some disable duration...
				else
				{
					// Set toggleOut to true.
					fadeOut = true;

					// If the chat box is currently toggling in, then reset it.]
					if( fadeIn )
						fadeIn = false;
				}
			}

			// If the user wants to collapse the chat box when it's disabled...
			if( collapseWhenDisabled )
			{
				// If the collapse speed is set to zero, then the transition needs to be instant.
				if( collapseSpeed <= 0.0f || instant )
				{
					// Set the lerp value to zero so that the chat box is ready to expand properly when needed.
					collapseLerpValue = 0.0f;

					// Set the base and bounding box sizes to the calculated collapsed sizes.
					BaseTransform.sizeDelta = baseTransformCollapsedSize;
					visibleChatBoundingBox.sizeDelta = boundingBoxCollapsedSize;

					// Since the chat box has been disabled, force the position of the chat box back down to the bottom.
					chatContentBox.anchoredPosition = new Vector2( chatContentBox.anchoredPosition.x, chatContentBox.sizeDelta.y - visibleChatBoundingBox.sizeDelta.y );

					// Update the size of the components that move.
					UpdateChatBoxComponentSizes();

					// Since the size of the chat box has been modified, constrain the chat box content.
					ConstrainContentBox();
				}
				// Else the user has actually set some disable duration...
				else
				{
					// Set collapseChatBox to true so that it will collapse.
					collapseChatBox = true;

					// If the chat box is currently in the middle of expanding, then set expandChatBox to false to stop it from expanding.
					if( expandChatBox )
						expandChatBox = false;
				}
			}

			// If the user wants to use an input field, and the input field is currently enabled, then disable the input field.
			if( useInputField )
				DisableInputField();
		}

		/// <summary>
		/// Clears the whole chat window of all entries.
		/// </summary>
		public void ClearChat ()
		{
			// Loop through all ChatInformations.
			for( int i = 0; i < ChatInformations.Count; i++ )
			{
				// If the chat text is null, then just skip over this info.
				if( ChatInformations[ i ].chatText == null )
					continue;

				// Send the chat text to the pool.
				SendTextToPool( ChatInformations[ i ].chatText );

				// Reset it's position and unassign the text component from this info.
				ChatInformations[ i ].chatText.rectTransform.anchoredPosition = Vector2.zero;
				ChatInformations[ i ].chatText = null;
			}

			// Recreate the ChatInformations list.
			ChatInformations = new List<ChatInformation>();

			// Set the total content space to zero.
			totalContentSpace = 0.0f;

			// Set the custom chat box controller to false since the chat box is resetting.
			chatBoxPositionCustom = false;

			// Update the content area size for the next entry.
			UpdateChatBoxComponentSizes();
		}

		/// <summary>
		/// Enables the input field and makes sure the chat box is active.
		/// </summary>
		/// <param name="inputFieldValue">[OPTIONAL] The string to add in to the input field.</param>
		public void EnableInputField ( string inputFieldValue = "" )
		{
			// If the user doesn't want to have an input field OR the input field is assigned OR if the input field is already active, then just return.
			if( !useInputField || inputField == null || InputFieldEnabled )
				return;

			// Set InputFieldActive to true for reference.
			InputFieldEnabled = true;

			// Set the input field to interactable and activate the input field.
			inputField.enabled = true;
			inputField.interactable = true;
			inputField.ActivateInputField();

			// Enable the chat box if it isn't already active.
			Enable();

			// If the user wants to enable the input field with a string value present...
			if( inputFieldValue != string.Empty )
			{
				// Store the input field value as the provided string and update the input field.
				InputFieldValue += inputFieldValue;
				inputField.text = InputFieldValue;

				// Set the caret position of the input field to the end of the string.
				inputField.caretPosition = InputFieldValue.Length;
				inputField.selectionAnchorPosition = InputFieldValue.Length;
			}

			// Notify any subscribers that the input field has been enabled.
			OnInputFieldEnabled?.Invoke();
		}

		/// <summary>
		/// Toggles the current state of the input field.
		/// </summary>
		public void ToggleInputField ()
		{
			// If the input field is not currently active, then enable the input field.
			if( !InputFieldEnabled )
				EnableInputField();
			// Else disable the input field.
			else
				DisableInputField();
		}

		/// <summary>
		/// Disables the input field and disables the chat box if it is active.
		/// </summary>
		public void DisableInputField ()
		{
			// If the user doesn't want to have an input field OR the input field is assigned OR the input field is not even active, then just return.
			if( !useInputField || inputField == null || !InputFieldEnabled )
				return;

			// Since the input field is disabled now, set InputFieldActive to false for reference.
			InputFieldEnabled = false;

			// Loop through all the characters of the InputFieldValue...
			for( int i = 0; i < InputFieldValue.Length; i++ )
			{
				// If the current character is not just white space, then break the loop. This string has more than just spaces.
				if( !char.IsWhiteSpace( InputFieldValue[ i ] ) )
					break;

				// If the loop is at the end, then this string is only spaces, so nullify the string values to void this input.
				if( i == InputFieldValue.Length - 1 )
				{
					inputField.text = string.Empty;
					InputFieldValue = string.Empty;
				}
			}

			// If the chat box is enabled, the input field is also active, and the value of the input field is not empty...
			if( IsEnabled && InputFieldValue != string.Empty )
			{
				// If the input field value starts with a forward slash, then the string may actually be a command...
				if( InputFieldValue[ 0 ].ToString() == "/" )
				{
					// Set the InputFieldContainsCommandValue to true for outside reference.
					InputFieldContainsCommand = true;

					// By default, store the command as the input field value, and the follow up as an empty string.
					string command = InputFieldValue;
					string message = "";

					// If the input field value contains a space, then split the command from the follow up and store the values.
					if( InputFieldValue.Contains( " " ) )
					{
						command = InputFieldValue.Split( ' ' )[ 0 ];
						message = InputFieldValue.Split( ' ' )[ 1 ];
					}

					// Inform any subscribers about the potential command.
					OnInputFieldCommandSubmitted?.Invoke( command.ToLower(), message );
				}

				// Set chatBoxPositionCustom to false so that the chat content box will adjust to show the input field chat.
				chatBoxPositionCustom = false;

				// Inform any subscribers that the input field has been submitted.
				OnInputFieldSubmitted?.Invoke( InputFieldValue );

#if UNITY_EDITOR
				// If the OnInputFieldSubmitted callback is empty, then the user is likely just testing the chat box...
				if( OnInputFieldSubmitted == null )
				{
					// Log a warning that the user should actually be subscribing to this callback and sending the data across the server.
					Debug.LogWarning( FormatDebug( "No subscriptions to the OnInputFieldSubmitted callback. Local chat will be sent to the Ultimate Chat Box just so that you can see what it looks like but please make sure to subscribe your chat handler to the OnInputFieldSubmitted callback", "Subscribe your own chat handler function to the OnInputFieldSubmitted callback and send in chat using the RegisterChat function", "Ultimate Chat Box Internal" ) );

					// Register the local chat.
					RegisterChat( "Local Chat Test", InputFieldValue, UltimateChatBoxStyles.boldUsername );
				}
#endif

				// Reset the input field text, as well as the stored input field value.
				inputField.text = string.Empty;
				InputFieldValue = string.Empty;
			}

			// If the emoji window is enabled, disable it.
			if( EmojiWindowEnabled && emojiWindowCanvasGroup != null )
			{
				// Set the EmojiWindowEnabled controller to false for reference.
				EmojiWindowEnabled = false;

				// Set the alpha of the window to zero.
				emojiWindowCanvasGroup.alpha = 0.0f;
			}

			// Release the selection of the input field, and deactivate it.
			inputField.ReleaseSelection();
			inputField.DeactivateInputField();
			inputField.enabled = false;

			// Disable interaction to the input field so that the chat box can control interactions.
			inputField.interactable = false;

			// Reset the InputFieldContainsCommandValue controller since the input field has been disabled.
			InputFieldContainsCommand = false;

			// If the chat box is currently enabled, then disable it.
			if( IsEnabled )
				Disable();

			// Notify any subscribers that the input field has been disabled.
			OnInputFieldDisabled?.Invoke();
		}

		/// <summary>
		/// Sends custom input to the chat box to process.
		/// </summary>
		/// <param name="screenPosition">The position of the input on the screen.</param>
		/// <param name="getButtonDown">The state of the input being pressed this frame.</param>
		/// <param name="getButton">The state of the input being down.</param>
		public void SendCustomInput ( Vector2 screenPosition, bool getButtonDown, bool getButton )
		{
			customInputRecieved = true;
			customScreenPosition = screenPosition;
			customGetButtonDown = getButtonDown;
			customGetButton = getButton;
		}

		/// <summary>
		/// Returns all the registered chats from the targeted username.
		/// </summary>
		/// <param name="username">The username to find registered chats for.</param>
		public List<ChatInformation> FindChatsFromUser ( string username )
		{
			// If the username parameter is empty, inform the user and return an empty list to avoid errors.
			if( username == string.Empty )
			{
				Debug.LogError( FormatDebug( "The provided username is empty", "Please provide this function with the string value of the user", "Unknown (User Script)" ) );
				return new List<ChatInformation>();
			}

			// Create a temporary list to store all the chats from the targeted username.
			List<ChatInformation> chatsFromUser = new List<ChatInformation>();

			// Loop through all the registered chats.
			for( int i = 0; i < ChatInformations.Count; i++ )
			{
				// If the internal stored username matches the targeted username, add this chat to the list.
				if( ChatInformations[ i ].Username == username )
					chatsFromUser.Add( ChatInformations[ i ] );
			}

			// If no matching chats were found, inform the user.
			if( chatsFromUser.Count == 0 )
				Debug.LogWarning( FormatDebug( $"No chats were found from the provided username: {username}. Either their are no chats currently registered from this user, or the provided username is incorrect", "Please ensure that the provided username is correct", "Unknown (User Script)" ) );

			// Return the list of chats from the targeted username.
			return chatsFromUser;
		}

		/// <summary>
		/// Returns all the registered chats using the provided style.
		/// </summary>
		/// <param name="style">The style of the chats to return.</param>
		public List<ChatInformation> FindChatsOfStyle ( ChatStyle style )
		{
			// If the style parameter is null, inform the user and return an empty list to avoid errors.
			if( style == null )
			{
				Debug.LogError( FormatDebug( "The provided style is null", "Please provide this function with the style of the chats you want returned", "Unknown (User Script)" ) );
				return new List<ChatInformation>();
			}

			// Create a temporary list to store all the chats from the targeted style.
			List<ChatInformation> chatsWithStyle = new List<ChatInformation>();

			// Loop through all the registered chats.
			for( int i = 0; i < ChatInformations.Count; i++ )
			{
				// If the internal stored style matches the targeted style, add this chat to the list.
				if( ChatInformations[ i ].chatBoxStyle == style )
					chatsWithStyle.Add( ChatInformations[ i ] );
			}

			// If no matching chats were found, inform the user.
			if( chatsWithStyle.Count == 0 )
				Debug.LogWarning( FormatDebug( $"No chats were found that have been registered with the provided style", "Please ensure that the provided style is correct", "Unknown (User Script)" ) );

			// Return the list of chats from the targeted style.
			return chatsWithStyle;
		}
		// ---------------------- <END PUBLIC FUNCTIONS FOR THE USER> ---------------------- //
	}
}