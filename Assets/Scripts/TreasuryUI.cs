using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using TMPro;
using Solana.Unity.SDK;

public class TreasuryUI : MonoBehaviour
{
    /*
    [Header("Builder")]
    [SerializeField] private TreasuryTransactionBuilder builder;
    [SerializeField] private SolanaManager solanaManager;

    [Header("Initialize")]
    [SerializeField] private Button initializeTreasuryButton;
    [SerializeField] private Button initializePlayerBalanceButton;

    [Header("Funds")]
    [SerializeField] private Button depositButton;
    [SerializeField] private Button withdrawButton;


    private void OnEnable()
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.onClick.AddListener(OnClickInitializeTreasury);
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.onClick.AddListener(OnClickInitializePlayerBalance);

        if (depositButton != null) depositButton.onClick.AddListener(OnClickDeposit);
        if (withdrawButton != null) withdrawButton.onClick.AddListener(OnClickWithdraw);

        InitializeButtonTexts();
        CacheOriginalButtonTexts();
        RefreshButtons();
    }

    private void OnDisable()
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.onClick.RemoveListener(OnClickInitializeTreasury);
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.onClick.RemoveListener(OnClickInitializePlayerBalance);

        if (depositButton != null) depositButton.onClick.RemoveListener(OnClickDeposit);
        if (withdrawButton != null) withdrawButton.onClick.RemoveListener(OnClickWithdraw);
    }

    private void InitializeButtonTexts()
    {
        SetButtonText(initializeTreasuryButton, "Initialize Treasury");
        SetButtonText(initializePlayerBalanceButton, "Initialize Player Balance");
        SetButtonText(depositButton, "Deposit");
        SetButtonText(withdrawButton, "Withdraw");
    }

    public void RefreshButtons()
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.interactable = true;
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.interactable = true;
        if (depositButton != null) depositButton.interactable = true;
        if (withdrawButton != null) withdrawButton.interactable = true;
    }

    private async void OnClickInitializeTreasury()
    {
        await RunAsync(initializeTreasuryButton, async () =>
        {
            var ix = builder.InitializeTreasury();
            if (ix == null) return null;
            return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
        });
    }

    private async void OnClickInitializePlayerBalance()
    {
        await RunAsync(initializePlayerBalanceButton, async () =>
        {
            var ix = builder.InitializePlayerBalance();
            if (ix == null) return null;
            return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
        });
        
        // Automatically delegate after initialization
        await DelegatePlayerBalance();
    }

    private async Task DelegatePlayerBalance()
    {
        // Hardcoded delegate parameters
        const uint commitMs = 30000u; // 30 seconds commit frequency
        const string validatorPubkeyString = "mAGicPQYBMvcYveUZA5F5UNNwyHvfYh5xkLS2Fr1mev"; // Oracle queue validator
        var validator = new PublicKey(validatorPubkeyString);
        
        await RunAsync(initializePlayerBalanceButton, async () =>
        {
            var ix = builder.DelegatePlayerBalance(commitMs, validator);
            if (ix == null) return null;
            return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
        });
    }

    private async void OnClickDeposit()
    {
        const ulong amount = 1000000000ul; // 1 billion hard-coded
        await RunAsync(depositButton, async () =>
        {
            var ix = builder.Deposit(amount);
            if (ix == null) return null;
            return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
        });
    }

    private async void OnClickWithdraw()
    {
        const ulong amount = 1000000000ul; // 1 billion hard-coded
        await RunAsync(withdrawButton, async () =>
        {
            var ix = builder.Withdraw(amount);
            if (ix == null) return null;
            return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
        });
    }

    private readonly Dictionary<Button, string> _originalButtonText = new Dictionary<Button, string>();

    private void CacheOriginalButtonTexts()
    {
        CacheOriginalText(initializeTreasuryButton);
        CacheOriginalText(initializePlayerBalanceButton);
        CacheOriginalText(depositButton);
        CacheOriginalText(withdrawButton);
    }

    private void CacheOriginalText(Button button)
    {
        if (button == null) return;
        if (_originalButtonText.ContainsKey(button)) return;
        var textComp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            _originalButtonText[button] = textComp.text;
        }
    }

    private void SetButtonText(Button button, string newText)
    {
        if (button == null) return;
        var textComp = button.GetComponentInChildren<TextMeshProUGUI>();
        if (textComp != null)
        {
            textComp.text = newText;
        }
    }

    private async Task RunAsync(Button contextButton, System.Func<Task<string>> func)
    {
        string originalText = null;
        if (contextButton != null)
        {
            CacheOriginalText(contextButton);
            _originalButtonText.TryGetValue(contextButton, out originalText);
            SetButtonText(contextButton, "Processing...");
        }
        SetAllButtonsInteractable(false);
        try
        {
            string signature = await func();
            var message = string.IsNullOrEmpty(signature) ? "Transaction failed" : $"Tx: {signature}";
            Debug.Log($"TreasuryUI: {message}");
        }
        finally
        {
            if (contextButton != null)
            {
                if (!string.IsNullOrEmpty(originalText))
                {
                    SetButtonText(contextButton, originalText);
                }
            }
            SetAllButtonsInteractable(true);
        }
    }

    private void SetAllButtonsInteractable(bool interactable)
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.interactable = interactable;
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.interactable = interactable;
        if (depositButton != null) depositButton.interactable = interactable;
        if (withdrawButton != null) withdrawButton.interactable = interactable;
    }
    */


}


