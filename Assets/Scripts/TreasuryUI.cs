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
    [SerializeField] private Button undelegatePlayerBalanceButton;

    [Header("Funds")]
    [SerializeField] private Button depositButton;
    [SerializeField] private Button withdrawButton;

    [Header("Fetch")]
    [SerializeField] private Button getPlayerBalanceButton;
    [SerializeField] private Button getTreasuryDataButton;

    [Header("Feedback (optional)")]
    [SerializeField] private TextMeshProUGUI statusTMPText;

    // Cached data from fetches
    private Treasury.Accounts.PlayerBalance _cachedPlayerBalance;
    private Treasury.Accounts.Treasury _cachedTreasury;


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

        InitializeButtonTexts();
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

    private void InitializeButtonTexts()
    {
        SetButtonText(initializeTreasuryButton, "Initialize Treasury");
        SetButtonText(initializePlayerBalanceButton, "Initialize Player Balance");
        SetButtonText(delegatePlayerBalanceButton, "Delegate Player Balance");
        SetButtonText(undelegatePlayerBalanceButton, "Undelegate Player Balance");
        SetButtonText(depositButton, "Deposit");
        SetButtonText(withdrawButton, "Withdraw");
        SetButtonText(getPlayerBalanceButton, "Get Player Balance");
        SetButtonText(getTreasuryDataButton, "Get Treasury Data");
    }

    public void RefreshButtons()
    {
        if (initializeTreasuryButton != null) initializeTreasuryButton.interactable = true;
        if (initializePlayerBalanceButton != null) initializePlayerBalanceButton.interactable = true;
        if (delegatePlayerBalanceButton != null) delegatePlayerBalanceButton.interactable = true;
        if (undelegatePlayerBalanceButton != null) undelegatePlayerBalanceButton.interactable = true;
        if (depositButton != null) depositButton.interactable = true;
        if (withdrawButton != null) withdrawButton.interactable = true;
        if (getPlayerBalanceButton != null) getPlayerBalanceButton.interactable = true;
        if (getTreasuryDataButton != null) getTreasuryDataButton.interactable = true;
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
        // Hardcoded delegate parameters
        const uint commitMs = 30000u; // 30 seconds commit frequency
        const string validatorPubkeyString = "mAGicPQYBMvcYveUZA5F5UNNwyHvfYh5xkLS2Fr1mev"; // Oracle queue validator
        var validator = new PublicKey(validatorPubkeyString);
        
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
            return await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, ix);
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
            if (statusTMPText != null) statusTMPText.text = "Wallet not connected";
            else Debug.Log("TreasuryUI: Wallet not connected");
            return;
        }
        var userPk = Web3.Account.PublicKey;
        var playerBalancePda = TreasuryTransactionBuilder.DerivePlayerBalanceAccount(userPk);
        await RunFetchAsync(getPlayerBalanceButton, async () => await builder.GetPlayerBalance(userPk), result =>
        {
            _cachedPlayerBalance = result;
            if (result != null)
            {
                Debug.Log($"TreasuryUI: Fetched PlayerBalance:");
                Debug.Log($"  - PubKey: {playerBalancePda}");
                Debug.Log($"  - Balance: {result.Balance}");
                Debug.Log($"  - Player: {result.Player}");
                Debug.Log($"  - Bump: {result.Bump}");
            }
            else
            {
                Debug.Log($"TreasuryUI: PlayerBalance not found (PubKey: {playerBalancePda})");
            }
            return result != null ? "Fetched PlayerBalance" : "PlayerBalance not found";
        });
    }

    private async void OnClickGetTreasuryData()
    {
        var treasuryPda = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        await RunFetchAsync(getTreasuryDataButton, async () => await builder.GetTreasuryData(), result =>
        {
            _cachedTreasury = result;
            if (result != null)
            {
                Debug.Log($"TreasuryUI: Fetched Treasury:");
                Debug.Log($"  - PubKey: {treasuryPda}");
                Debug.Log($"  - Bump: {result.Bump}");
                Debug.Log($"  - TreasuryTokenAccountBump: {result.TreasuryTokenAccountBump}");
            }
            else
            {
                Debug.Log($"TreasuryUI: Treasury not found (PubKey: {treasuryPda})");
            }
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
            if (statusTMPText != null) statusTMPText.text = message;
            else Debug.Log($"TreasuryUI: {message}");
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
            if (statusTMPText != null) statusTMPText.text = message;
            else Debug.Log($"TreasuryUI: {message}");
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
        if (delegatePlayerBalanceButton != null) delegatePlayerBalanceButton.interactable = interactable;
        if (undelegatePlayerBalanceButton != null) undelegatePlayerBalanceButton.interactable = interactable;
        if (depositButton != null) depositButton.interactable = interactable;
        if (withdrawButton != null) withdrawButton.interactable = interactable;
        if (getPlayerBalanceButton != null) getPlayerBalanceButton.interactable = interactable;
        if (getTreasuryDataButton != null) getTreasuryDataButton.interactable = interactable;
    }


}


