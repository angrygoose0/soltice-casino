using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private TMP_Text ephemeralBalance;
    [SerializeField] private Button ephemeralHalfButton;
    [SerializeField] private Button ephemeralFullButton;

    private bool walletCardIsActive = true;
    private float currentInputValue = 0f;
    private const float multiplier = 1.5f;

    [SerializeField] private Button swapButton;
    [SerializeField] private Button sendButton;

    private void Start()
    {
        // Configure input fields to only accept numbers
        walletInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        ephemeralInput.contentType = TMP_InputField.ContentType.DecimalNumber;
        
        UpdateCardStates();
        UpdateConvertedValues();
        
        // Listen to input changes on both fields
        walletInput.onValueChanged.AddListener(OnInputChanged);
        ephemeralInput.onValueChanged.AddListener(OnInputChanged);
        
        // Connect button
        swapButton.onClick.AddListener(Swap);
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
            walletDropdown.gameObject.SetActive(true);
            walletHalfButton.gameObject.SetActive(true);
            walletFullButton.gameObject.SetActive(true);
            walletBalance.gameObject.SetActive(true);
            
            ephemeralInput.interactable = false;
            ephemeralDropdown.gameObject.SetActive(false);
            ephemeralHalfButton.gameObject.SetActive(false);
            ephemeralFullButton.gameObject.SetActive(false);
            ephemeralBalance.gameObject.SetActive(false);
        }
        else
        {
            ephemeralCard.transform.SetAsFirstSibling();
            ephemeralInput.interactable = true;
            ephemeralDropdown.gameObject.SetActive(true);
            ephemeralHalfButton.gameObject.SetActive(true);
            ephemeralFullButton.gameObject.SetActive(true);
            ephemeralBalance.gameObject.SetActive(true);
            
            walletInput.interactable = false;
            walletDropdown.gameObject.SetActive(false);
            walletHalfButton.gameObject.SetActive(false);
            walletFullButton.gameObject.SetActive(false);
            walletBalance.gameObject.SetActive(false);
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
        if (walletCardIsActive)
        {
            // send to wallet
        }
        else
        {
            // send to ephemeral
        }
    }
}