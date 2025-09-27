using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using TMPro;
using Solana.Unity.SDK;

public class TreasuryUI : MonoBehaviour
{
    [Header("Builder")]
    [SerializeField] private TreasuryTransactionBuilder builder;
    [SerializeField] private SolanaManager solanaManager;

    [Header("Initialize")]
    [SerializeField] private Button initializeTreasuryButton;
    [SerializeField] private Button initializePlayerBalanceButton;

    [Header("Delegation")]
    [SerializeField] private Button delegatePlayerBalanceButton;
    [SerializeField] private InputField delegateCommitFrequencyMsInput; // optional
    [SerializeField] private InputField delegateValidatorPubkeyInput;   // optional
    [SerializeField] private Button undelegatePlayerBalanceButton;

    [Header("Funds")]
    [SerializeField] private Button depositButton;
    [SerializeField] private Button withdrawButton;

    [Header("Fetch")]
    [SerializeField] private Button getPlayerBalanceButton;
    [SerializeField] private Button getTreasuryDataButton;

    [Header("Feedback (optional)")]
    [SerializeField] private TMP_Text statusTMPText;

    // Cached data from fetches
    private Treasury.Accounts.PlayerBalance _cachedPlayerBalance;
    private Treasury.Accounts.Treasury _cachedTreasury;

    private void Awake()
    {
        if (builder == null)
        {
            builder = FindObjectOfType<TreasuryTransactionBuilder>();
        }
        if (solanaManager == null)
        {
            solanaManager = FindObjectOfType<SolanaManager>();
        }
        if (builder == null)
        {
            Debug.LogError("TreasuryUI: TreasuryTransactionBuilder reference is missing; disabling UI");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.onClick.AddListener(OnClickInitializeTreasury);
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.onClick.AddListener(OnClickInitializePlayerBalance);

        if (delegatePlayerBalanceButton != null) delegatePlayerBalanceButton.onClick.AddListener(OnClickDelegatePlayerBalance);
        if (undelegatePlayerBalanceButton != null) undelegatePlayerBalanceButton.onClick.AddListener(OnClickUndelegatePlayerBalance);

        if (depositButton != null) depositButton.onClick.AddListener(OnClickDeposit);
        if (withdrawButton != null) withdrawButton.onClick.AddListener(OnClickWithdraw);

        if (getPlayerBalanceButton != null) getPlayerBalanceButton.onClick.AddListener(OnClickGetPlayerBalance);
        if (getTreasuryDataButton != null) getTreasuryDataButton.onClick.AddListener(OnClickGetTreasuryData);

        CacheOriginalButtonTexts();
        RefreshButtons();
    }

    private void OnDisable()
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.onClick.RemoveListener(OnClickInitializeTreasury);
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.onClick.RemoveListener(OnClickInitializePlayerBalance);

        if (delegatePlayerBalanceButton != null) delegatePlayerBalanceButton.onClick.RemoveListener(OnClickDelegatePlayerBalance);
        if (undelegatePlayerBalanceButton != null) undelegatePlayerBalanceButton.onClick.RemoveListener(OnClickUndelegatePlayerBalance);

        if (depositButton != null) depositButton.onClick.RemoveListener(OnClickDeposit);
        if (withdrawButton != null) withdrawButton.onClick.RemoveListener(OnClickWithdraw);

        if (getPlayerBalanceButton != null) getPlayerBalanceButton.onClick.RemoveListener(OnClickGetPlayerBalance);
        if (getTreasuryDataButton != null) getTreasuryDataButton.onClick.RemoveListener(OnClickGetTreasuryData);
    }

    public void SetBuilder(TreasuryTransactionBuilder newBuilder)
    {
        builder = newBuilder;
        RefreshButtons();
    }

    public void RefreshButtons()
    {
        SetInteractable(initializeTreasuryButton, true);
        SetInteractable(initializePlayerBalanceButton, true);
        SetInteractable(delegatePlayerBalanceButton, true);
        SetInteractable(undelegatePlayerBalanceButton, true);
        SetInteractable(depositButton, true);
        SetInteractable(withdrawButton, true);
        SetInteractable(getPlayerBalanceButton, true);
        SetInteractable(getTreasuryDataButton, true);
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null) button.interactable = interactable;
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
    }

    private async void OnClickDelegatePlayerBalance()
    {
        uint commitMs = ParseUint(delegateCommitFrequencyMsInput, 1000u);
        PublicKey validator = ParsePublicKey(delegateValidatorPubkeyInput);
        await RunAsync(delegatePlayerBalanceButton, async () =>
        {
            var ix = builder.DelegatePlayerBalance(commitMs, validator);
            if (ix == null) return null;
            return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
        });
    }

    private async void OnClickUndelegatePlayerBalance()
    {
        await RunAsync(undelegatePlayerBalanceButton, async () =>
        {
            var ix = builder.UndelegatePlayerBalance();
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

    private async void OnClickGetPlayerBalance()
    {
        if (Web3.Account == null)
        {
            SetStatus("Wallet not connected");
            return;
        }
        var userPk = Web3.Account.PublicKey;
        await RunFetchAsync(getPlayerBalanceButton, async () => await builder.GetPlayerBalance(userPk), result =>
        {
            _cachedPlayerBalance = result;
            return result != null ? "Fetched PlayerBalance" : "PlayerBalance not found";
        });
    }

    private async void OnClickGetTreasuryData()
    {
        await RunFetchAsync(getTreasuryDataButton, async () => await builder.GetTreasuryData(), result =>
        {
            _cachedTreasury = result;
            return result != null ? "Fetched Treasury" : "Treasury not found";
        });
    }

    private readonly Dictionary<Button, string> _originalButtonText = new Dictionary<Button, string>();

    private void CacheOriginalButtonTexts()
    {
        CacheOriginalText(initializeTreasuryButton);
        CacheOriginalText(initializePlayerBalanceButton);
        CacheOriginalText(delegatePlayerBalanceButton);
        CacheOriginalText(undelegatePlayerBalanceButton);
        CacheOriginalText(depositButton);
        CacheOriginalText(withdrawButton);
        CacheOriginalText(getPlayerBalanceButton);
        CacheOriginalText(getTreasuryDataButton);
    }

    private void CacheOriginalText(Button button)
    {
        if (button == null) return;
        if (_originalButtonText.ContainsKey(button)) return;
        var textComp = FindChildTMPText(button);
        if (textComp != null)
        {
            _originalButtonText[button] = textComp.text;
        }
    }

    private TMP_Text FindChildTMPText(Button button)
    {
        if (button == null) return null;
        return button.GetComponentInChildren<TMP_Text>();
    }

    private string GetCurrentButtonText(Button button)
    {
        var tmp = FindChildTMPText(button);
        if (tmp != null) return tmp.text;
        return null;
    }

    private void SetButtonText(Button button, string newText)
    {
        if (button == null) return;
        var textComp = FindChildTMPText(button);
        if (textComp == null) return;
        textComp.text = newText;
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
            SetStatus(message);
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

    private async Task RunFetchAsync<T>(Button contextButton, System.Func<Task<T>> fetch, System.Func<T, string> makeMessage)
    {
        string originalText = null;
        if (contextButton != null)
        {
            CacheOriginalText(contextButton);
            _originalButtonText.TryGetValue(contextButton, out originalText);
            SetButtonText(contextButton, "Loading...");
        }
        SetAllButtonsInteractable(false);
        try
        {
            T result = await fetch();
            string message = makeMessage != null ? makeMessage(result) : (result != null ? "Fetch succeeded" : "Fetch failed");
            SetStatus(message);
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
        SetInteractable(initializeTreasuryButton, interactable);
        SetInteractable(initializePlayerBalanceButton, interactable);
        SetInteractable(delegatePlayerBalanceButton, interactable);
        SetInteractable(undelegatePlayerBalanceButton, interactable);
        SetInteractable(depositButton, interactable);
        SetInteractable(withdrawButton, interactable);
        SetInteractable(getPlayerBalanceButton, interactable);
        SetInteractable(getTreasuryDataButton, interactable);
    }

    private void SetStatus(string message)
    {
        if (statusTMPText != null) statusTMPText.text = message;
        else Debug.Log($"TreasuryUI: {message}");
    }


    private static uint ParseUint(InputField input, uint defaultValue)
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text)) return defaultValue;
        if (uint.TryParse(input.text, out var result)) return result;
        return defaultValue;
    }

    private static PublicKey ParsePublicKey(InputField input)
    {
        if (input == null) return null;
        var value = input.text;
        if (string.IsNullOrWhiteSpace(value)) return null;
        try
        {
            return new PublicKey(value.Trim());
        }
        catch
        {
            return null;
        }
    }
}


