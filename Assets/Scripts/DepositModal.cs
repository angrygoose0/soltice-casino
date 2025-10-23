using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class DepositModal : MonoBehaviour
{
    [SerializeField] private GameObject walletCard;
    [SerializeField] private GameObject ephemeralCard;

    // Wallet card components
    [SerializeField] private TMP_InputField walletInput;
    [SerializeField] private TMP_Dropdown walletDropdown;
    [SerializeField] private TMP_Text walletConverted;
    [SerializeField] private TMP_Text walletBalance;
    [SerializeField] private Button walletHalfButton;
    [SerializeField] private Button walletFullButton;

    // Ephemeral card components
    [SerializeField] private TMP_InputField ephemeralInput;
    [SerializeField] private TMP_Dropdown ephemeralDropdown;
    [SerializeField] private TMP_Text ephemeralConverted;
    [SerializeField] public TMP_Text ephemeralBalance;
    [SerializeField] private Button ephemeralHalfButton;
    [SerializeField] private Button ephemeralFullButton;

    private bool walletCardIsActive = true;
    private float currentInputValue = 0f;
    private const float multiplier = 1.5f;

    [SerializeField] private Button swapButton;
    [SerializeField] private Button sendButton;
    private TMP_Text sendButtonText;
    [SerializeField] private Button toggleModalButton;
    [SerializeField] private Button exitButton;
    
    public UnityEvent<ulong> onDeposit;
    public UnityEvent<ulong> onWithdraw;

    private void Start()
    {
        // Configure input fields to only accept numbers
        walletInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        ephemeralInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        
        sendButtonText = sendButton.GetComponentInChildren<TMP_Text>();
        
        UpdateCardStates();
        UpdateConvertedValues();
        
        // Listen to input changes on both fields
        walletInput.onValueChanged.AddListener(OnInputChanged);
        ephemeralInput.onValueChanged.AddListener(OnInputChanged);
        
        // Connect buttons
        swapButton.onClick.AddListener(Swap);
        sendButton.onClick.AddListener(Send);
        if (toggleModalButton != null)
            toggleModalButton.onClick.AddListener(ToggleModal);
        if (exitButton != null)
            exitButton.onClick.AddListener(() => UIFader.FadeOut(gameObject));
        
        // Initially hide everything since wallet isn't connected
        UIFader.HideImmediate(gameObject);
        if (toggleModalButton != null)
            UIFader.HideImmediate(toggleModalButton.gameObject);
    }

    private void OnInputChanged(string value)
    {
        if (float.TryParse(value, out float parsedValue))
        {
            currentInputValue = parsedValue;
        }
        else
        {
            currentInputValue = 0f;
        }
        
        UpdateInactiveCardPlaceholder();
        UpdateConvertedValues();
    }
    
    private void UpdateConvertedValues()
    {
        float convertedValue = currentInputValue * multiplier;
        walletConverted.text = convertedValue.ToString("F2");
        ephemeralConverted.text = convertedValue.ToString("F2");
    }

    private void UpdateCardStates()
    {
        if (walletCardIsActive)
        {
            walletCard.transform.SetAsFirstSibling();
            walletInput.interactable = true;
            UIFader.FadeIn(walletDropdown.gameObject);
            UIFader.FadeIn(walletHalfButton.gameObject);
            UIFader.FadeIn(walletFullButton.gameObject);
            UIFader.FadeIn(walletBalance.gameObject);
            
            ephemeralInput.interactable = false;
            UIFader.FadeOut(ephemeralDropdown.gameObject);
            UIFader.FadeOut(ephemeralHalfButton.gameObject);
            UIFader.FadeOut(ephemeralFullButton.gameObject);
            UIFader.FadeOut(ephemeralBalance.gameObject);
            
            if (sendButtonText != null)
                sendButtonText.text = "Deposit";
        }
        else
        {
            ephemeralCard.transform.SetAsFirstSibling();
            ephemeralInput.interactable = true;
            UIFader.FadeIn(ephemeralDropdown.gameObject);
            UIFader.FadeIn(ephemeralHalfButton.gameObject);
            UIFader.FadeIn(ephemeralFullButton.gameObject);
            UIFader.FadeIn(ephemeralBalance.gameObject);
            
            walletInput.interactable = false;
            UIFader.FadeOut(walletDropdown.gameObject);
            UIFader.FadeOut(walletHalfButton.gameObject);
            UIFader.FadeOut(walletFullButton.gameObject);
            UIFader.FadeOut(walletBalance.gameObject);
            
            if (sendButtonText != null)
                sendButtonText.text = "Withdraw";
        }
        
        UpdateInactiveCardPlaceholder();
    }

    private void UpdateInactiveCardPlaceholder()
    {
        string valueText = currentInputValue.ToString();
        
        if (walletCardIsActive)
        {
            ephemeralInput.text = valueText;
        }
        else
        {
            walletInput.text = valueText;
        }
    }

    public void Swap()
    {
        walletCardIsActive = !walletCardIsActive;
        UpdateCardStates();
    }

    public void Send()
    {
        ulong amount = (ulong)currentInputValue;
        
        if (walletCardIsActive)
        {
            onDeposit?.Invoke(amount);
        }
        else
        {
            onWithdraw?.Invoke(amount);
        }
    }
    
    public void Show()
    {
        UIFader.FadeIn(gameObject);
    }

    public void Hide()
    {
        UIFader.FadeOut(gameObject);
    }

    public void ToggleModal()
    {
        if (gameObject.activeSelf)
            UIFader.FadeOut(gameObject);
        else
            UIFader.FadeIn(gameObject);
    }
    
    public void ShowToggleButton()
    {
        if (toggleModalButton != null)
            UIFader.FadeIn(toggleModalButton.gameObject);
    }
    
    public void HideToggleButton()
    {
        if (toggleModalButton != null)
            UIFader.FadeOut(toggleModalButton.gameObject);
        UIFader.FadeOut(gameObject);
    }
}