using UnityEngine;
using TMPro;
using System.Globalization;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using System.Linq;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Rpc.Builders;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Text;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Crash.Program;
using Crash;
using Treasury.Program;
using Treasury;
using Blackjack.Program;
using Blackjack;

public class SolanaManager : MonoBehaviour
{
    [Header("Ephemeral Rollups")]
    // Set to false to disable ephemeral rollups (no delegation/undelegation)
    public bool useEphemeralRollups = true;
    [SerializeField] private string ephemeralRpcUrl;
    [SerializeField] private string ephemeralWsUrl;

    [Header("Priority Fees")]
    [SerializeField] private bool useDynamicPriorityFees = true;
    [SerializeField] private string heliusRpcUrl; // e.g., https://mainnet.helius-rpc.com/?api-key=YOUR_KEY
    [SerializeField] private string priorityLevel = "Medium"; // Low, Medium, High, VeryHigh
    [SerializeField] private ulong fallbackPriorityFee = 100000; // Fallback if API fails

    [Header("Dependencies")]
    [SerializeField] private InteractableObjects interactableObjects;
    [SerializeField] private FeedbackManager feedbackManager;

    [Header("Mint")]
    [SerializeField] private string mintAddress;
    [SerializeField] private int tokenDecimals = 9; // Default to 6 decimals (millions)
    
    [Header("UI Elements")]
    [SerializeField] private List<TextMeshProUGUI> publicKeyTexts;
    [SerializeField] private List<TextMeshProUGUI> balanceTexts;
    [SerializeField] private List<TextMeshProUGUI> tokenBalanceTexts;
    
    [Header("Wallet Connection UI")]
    [SerializeField] private GameObject loadingCircle;
    [SerializeField] private GameObject darkOverlay;
    [SerializeField] private GameObject loadingText;
    private TextMeshProUGUI loadingTextComponent;

    // Client properties
    public CrashClient CrashClient { get; private set; }
    public CrashClient CrashClientEphemeral { get; private set; }
    public TreasuryClient TreasuryClient { get; private set; }
    public BlackjackClient BlackjackClient { get; private set; }
    public int TokenDecimals => tokenDecimals;
    
    private static PublicKey _crashProgramId = new PublicKey(CrashProgram.ID);
    private static PublicKey _treasuryProgramId = new PublicKey(TreasuryProgram.ID);
    private static PublicKey _blackjackProgramId = new PublicKey(BlackjackProgram.ID);
    public static readonly PublicKey DELEGATION_PROGRAM_ID = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");

    private IRpcClient _rpcClient;
    private IRpcClient _ephemeralRpcClient;
    private bool _gameJoined = false;
    private bool _isConnectingWallet = false;
    private bool _isWalletConnected = false;
    private static readonly HttpClient _httpClient = new HttpClient();
    
    // Wallet adapter UI monitoring
    private GameObject _walletAdapterUI;
    private bool _wasWalletAdapterActive;

    public static InGameWallet EphemeralWallet { get; private set; }

    public PublicKey MintPublicKey => string.IsNullOrWhiteSpace(mintAddress) ? null : new PublicKey(mintAddress);

    

    private void Awake()
    {
        // Initialize EphemeralWallet if ephemeral rollups are enabled
        if (useEphemeralRollups)
        {
            if (string.IsNullOrEmpty(ephemeralRpcUrl) || string.IsNullOrEmpty(ephemeralWsUrl))
            {
                GameLogger.LogError("Ephemeral RPC URL and WebSocket URL must be configured when useEphemeralRollups is enabled");
                throw new System.InvalidOperationException("Ephemeral RPC URL and WebSocket URL are required");
            }
            EphemeralWallet = new InGameWallet(RpcCluster.MainNet, ephemeralRpcUrl, ephemeralWsUrl, true);
        }
    }

    private void OnEnable()
    {
        // Initialize loading text component
        if (loadingText != null)
        {
            loadingTextComponent = loadingText.GetComponent<TextMeshProUGUI>();
        }

        // Hide wallet buttons until game is joined
        if (interactableObjects != null)
        {
            interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.ConnectWallet, false);
            interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.DisconnectWallet, false);
        }

        // Subscribe to game join event
        SimpleWorldJoin.OnGameJoined += OnGameJoined;

        // Check if Web3 is available and user is logged in
        if (Web3.Instance != null && Web3.Account != null)
        {
            OnLogin(Web3.Account);
        }
        else
        {
            OnLogout();
        }

        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout;
        Web3.OnBalanceChange += OnBalanceChange;
    }

    public void InitializeClients()
    {
        if (Web3.Instance?.WalletBase == null)
        {
            GameLogger.LogWarning("Web3 instance or WalletBase not initialized yet");
            return;
        }

        try
        {
            _rpcClient = Web3.Instance.WalletBase.ActiveRpcClient;
            _ephemeralRpcClient = EphemeralWallet?.ActiveRpcClient;

            var streamingClient = Web3.Instance.WalletBase.ActiveStreamingRpcClient;
            if (streamingClient == null)
            {
                GameLogger.LogWarning("ActiveStreamingRpcClient is null");
                return;
            }

            CrashClient = new CrashClient(
                _rpcClient,
                streamingClient,
                _crashProgramId
            );

            CrashClientEphemeral = new CrashClient(
                _ephemeralRpcClient,
                streamingClient,
                _crashProgramId
            );

            TreasuryClient = new TreasuryClient(
                _rpcClient,
                streamingClient,
                _treasuryProgramId
            );

            BlackjackClient = new BlackjackClient(
                _rpcClient,
                streamingClient,
                _blackjackProgramId
            );
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Error initializing Clients: {ex.Message}");
        }
    }

    private void OnDisable()
    {
        SimpleWorldJoin.OnGameJoined -= OnGameJoined;
        Web3.OnLogin -= OnLogin;
        Web3.OnLogout -= OnLogout;
        Web3.OnBalanceChange -= OnBalanceChange;
    }

    private void Update()
    {
        // Monitor WalletAdapterUI only when we're connecting
        if (!_isConnectingWallet) return;

        // Ensure loading overlay stays visible while connecting
        EnsureLoadingVisible();

        // Find the WalletAdapterUI if we don't have a reference yet
        if (_walletAdapterUI == null)
        {
            _walletAdapterUI = GameObject.Find("WalletAdapterUI(Clone)");
            if (_walletAdapterUI != null)
            {
                _wasWalletAdapterActive = _walletAdapterUI.activeSelf;
            }
            return;
        }

        // Check if the UI was just deactivated
        bool isCurrentlyActive = _walletAdapterUI.activeSelf;
        
        if (_wasWalletAdapterActive && !isCurrentlyActive)
        {
            // UI was closed - wait briefly to see if OnLogin gets called
            // If user selected a wallet, OnLogin will be called soon and will set _isConnectingWallet to false
            // If user clicked exit, OnLogin won't be called, so we'll cancel after the delay
            StartCoroutine(CheckForCancellation());
            _walletAdapterUI = null;
        }
        
        _wasWalletAdapterActive = isCurrentlyActive;
    }

    private void EnsureLoadingVisible()
    {
        // Keep loading overlay visible while connecting
        if (darkOverlay != null && !darkOverlay.activeSelf)
        {
            UIFader.ShowImmediate(darkOverlay);
        }
        
        if (loadingCircle != null && !loadingCircle.activeSelf)
        {
            UIFader.ShowImmediate(loadingCircle);
        }
        
        if (loadingText != null && !loadingText.activeSelf)
        {
            UIFader.ShowImmediate(loadingText);
        }
    }

    private System.Collections.IEnumerator CheckForCancellation()
    {
        // Wait a brief moment to give OnLogin a chance to be called
        yield return new WaitForSeconds(0.3f);
        
        // If we're still in connecting state, user must have clicked exit
        if (_isConnectingWallet)
        {
            CancelWalletConnection();
        }
    }

    private void OnGameJoined()
    {
        _gameJoined = true;
        GameLogger.Log("Game joined - enabling wallet buttons");
        
        // Show appropriate buttons based on wallet state
        if (interactableObjects != null)
        {
            // Only enable disconnect button if wallet is connected
            if (_isWalletConnected)
            {
                interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.DisconnectWallet, true);
            }
            // Only enable connect button if wallet is NOT connected and we're not in the middle of connecting
            else if (!_isConnectingWallet)
            {
                interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.ConnectWallet, true);
            }
        }
    }

    private async void OnLogin(Account account)
    {
        // Mark wallet as connected
        _isWalletConnected = true;
        
        // If we were connecting, update loading text to show subscription setup
        if (_isConnectingWallet)
        {
            _isConnectingWallet = false;
            _walletAdapterUI = null; // Clean up reference since login succeeded
            
            // Keep loading visible but update text
            if (loadingText != null && loadingTextComponent != null)
            {
                loadingTextComponent.text = "setting up subscriptions...";
                loadingTextComponent.color = Color.white;
            }
        }

        // Initialize clients when user logs in
        InitializeClients();
        GameLogger.Log("Login successful");
        
        feedbackManager?.PlaySuccessSound();

        // Update connect/disconnect buttons
        if (interactableObjects != null)
        {
            GameLogger.Log("Setting interactable objects to hard disable connect wallet");
            interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.ConnectWallet, false);
            if (_gameJoined)
            {
                interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.DisconnectWallet, true);
            }
        }

        // Update public key texts
        foreach (var txt in publicKeyTexts)
        {
            UIFader.FadeIn(txt.gameObject);
            txt.text = account.PublicKey;
        }

        // Show balance texts
        foreach (var txt in balanceTexts)
        {
            UIFader.FadeIn(txt.gameObject);
        }

        // Show and update token balance texts
        foreach (var txt in tokenBalanceTexts)
        {
            UIFader.FadeIn(txt.gameObject);
        }

        // Update token balance
        await UpdateTokenBalance();

    }

    private void OnLogout()
    {
        // Mark wallet as disconnected
        _isWalletConnected = false;
        
        // Update connect/disconnect buttons
        if (interactableObjects != null)
        {
            interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.DisconnectWallet, false);
            if (_gameJoined)
            {
                interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.ConnectWallet, true);
            }
        }

        // Hide and clear public key texts
        foreach (var txt in publicKeyTexts)
        {
            UIFader.FadeOut(txt.gameObject);
            txt.text = string.Empty;
        }

        // Hide balance texts
        foreach (var txt in balanceTexts)
        {
            UIFader.FadeOut(txt.gameObject);
            txt.text = string.Empty;
        }

        // Hide and clear token balance texts
        foreach (var txt in tokenBalanceTexts)
        {
            UIFader.FadeOut(txt.gameObject);
            txt.text = string.Empty;
        }
    }

    private void OnBalanceChange(double amount)
    {
        // Update all balance texts
        foreach (var txt in balanceTexts)
        {
            txt.text = "Wallet balance: " + FormatAmount(amount);
        }
    }

    private string FormatAmount(double amount, bool applyTokenDecimals = false)
    {
        if (applyTokenDecimals)
        {
            amount = amount / Math.Pow(10, tokenDecimals);
        }

        if (amount >= 1000000)
        {
            return (amount / 1000000).ToString("F2", CultureInfo.InvariantCulture) + "m";
        }
        else if (amount >= 1000)
        {
            return (amount / 1000).ToString("F2", CultureInfo.InvariantCulture) + "k";
        }
        else
        {
            return amount.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    private async Task UpdateTokenBalance()
    {
        GameLogger.Log($"Updating token balance: {mintAddress}, {Web3.Account}");
        if (string.IsNullOrEmpty(mintAddress) || Web3.Account == null)
        {
            GameLogger.LogWarning($"Cannot update token balance - mintAddress: {mintAddress}, Web3.Account: {Web3.Account}");
            return;
        }

        try
        {
            double tokenBalance = await GetSPLTokenBalance();
            GameLogger.Log($"Token balance: {tokenBalance}");
            
            // Update all token balance texts
            foreach (var txt in tokenBalanceTexts)
            {
                GameLogger.Log($"Token balance text: {txt.text}");
                txt.text = FormatAmount(tokenBalance, applyTokenDecimals: true);
            }
        }
        catch (System.Exception)
        {
            // Set error text for token balance
            foreach (var txt in tokenBalanceTexts)
            {
                txt.text = "Error";
            }
        }
    }

    private static readonly PublicKey TOKEN_2022_PROGRAM_ID = new PublicKey("TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb");
    private static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");

    private static PublicKey DeriveToken2022ATA(PublicKey owner, PublicKey mint)
    {
        PublicKey.TryFindProgramAddress(
            new[]
            {
                owner.KeyBytes,
                TOKEN_2022_PROGRAM_ID.KeyBytes,
                mint.KeyBytes
            },
            ASSOCIATED_TOKEN_PROGRAM_ID,
            out var ata,
            out _
        );
        return ata;
    }

    public async Task<double> GetSPLTokenBalance()
    {
        try
        {
            // First try regular SPL Token accounts
            var tokenAccounts = await Web3.Wallet.GetTokenAccounts(Commitment.Confirmed);
            var matchingAccount = tokenAccounts?.FirstOrDefault(t => 
                t.Account.Data.Parsed.Info.Mint == mintAddress);
            
            if (matchingAccount != null)
            {
                var tokenAmount = matchingAccount.Account.Data.Parsed.Info.TokenAmount;
                if (!string.IsNullOrEmpty(tokenAmount.UiAmountString) &&
                    double.TryParse(tokenAmount.UiAmountString, NumberStyles.Any, CultureInfo.InvariantCulture, out double balance))
                {
                    return balance * Math.Pow(10, tokenDecimals);
                }
                return ulong.TryParse(tokenAmount.Amount, out ulong rawAmount) ? (double)rawAmount : 0.0;
            }
            
            // If not found, try Token-2022 ATA directly
            var mint = new PublicKey(mintAddress);
            var ata = DeriveToken2022ATA(Web3.Account.PublicKey, mint);
            return (double)await GetTokenAccountBalance(ata);
        }
        catch (System.Exception ex)
        {
            GameLogger.LogWarning($"Failed to get SPL token balance: {ex.Message}");
            return 0.0;
        }
    }

    public async Task<ulong> GetTokenAccountBalance(PublicKey tokenAccount)
    {
        try
        {
            var accountInfo = await _rpcClient.GetTokenAccountBalanceAsync(tokenAccount, Commitment.Confirmed);
            if (accountInfo.Result?.Value?.Amount != null)
            {
                return ulong.TryParse(accountInfo.Result.Value.Amount, out ulong amount) ? amount : 0;
            }
            return 0;
        }
        catch (Exception)
        {
            return 0;
        }
    }


    /// <summary>
    /// Get priority fee estimate from Helius API using serialized transaction.
    /// Returns the recommended priority fee in microlamports per compute unit.
    /// </summary>
    public async Task<ulong> GetPriorityFeeEstimate(string serializedTransaction)
    {
        if (!useDynamicPriorityFees || string.IsNullOrEmpty(heliusRpcUrl))
        {
            return fallbackPriorityFee;
        }

        try
        {
            var requestBody = new
            {
                jsonrpc = "2.0",
                id = "1",
                method = "getPriorityFeeEstimate",
                @params = new object[]
                {
                    new
                    {
                        transaction = serializedTransaction,
                        options = new
                        {
                            priorityLevel = priorityLevel,
                            recommended = true
                        }
                    }
                }
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.PostAsync(heliusRpcUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(responseString);

            if (result["error"] != null)
            {
                GameLogger.LogWarning($"Helius API error: {result["error"]}");
                return fallbackPriorityFee;
            }

            var fee = result["result"]?["priorityFeeEstimate"]?.Value<ulong>() ?? fallbackPriorityFee;
            GameLogger.Log($"Dynamic priority fee estimate: {fee} microlamports/CU ({priorityLevel})");
            return fee;
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning($"Failed to get priority fee estimate: {ex.Message}");
            return fallbackPriorityFee;
        }
    }

    public async Task<string> SendAndConfirmTransaction(
        bool ephemeralFlag = false, // if true, tx is happening on ER.
        uint computeUnitLimit = 0,
        ulong computeUnitPrice = 0,
        string loadingMessage = null,
        params TransactionInstruction[] additionalInstructions)
    {
        bool showedLoading = !string.IsNullOrEmpty(loadingMessage);
        try
        {
            if (showedLoading)
                ShowLoadingOverlay(loadingMessage);

            var baseWallet = SessionManager.SessionToken == null
                ? Web3.Wallet
                : SessionManager.SessionWallet;

            var rpcClient = ephemeralFlag ? _ephemeralRpcClient : _rpcClient;

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync(Commitment.Confirmed);

            // Step 1: Build transaction with just business logic for fee estimation
            var transaction = new Transaction
            {
                FeePayer = baseWallet.Account.PublicKey,
                RecentBlockHash = blockHashResult.Result.Value.Blockhash,
                Signatures = new List<SignaturePubKeyPair>(),
                Instructions = new List<TransactionInstruction>()
            };

            // Add business logic instructions first (for fee estimation)
            if (additionalInstructions != null)
                transaction.Instructions.AddRange(additionalInstructions);

            // Step 2: Get dynamic priority fee estimate (only for mainnet, not ER)
            ulong finalPriorityFee = computeUnitPrice;
            if (!ephemeralFlag && useDynamicPriorityFees && !string.IsNullOrEmpty(heliusRpcUrl))
            {
                // Serialize for fee estimation (without compute budget instructions)
                var estimationTx = await baseWallet.SignTransaction(transaction);
                var serializedForEstimate = Convert.ToBase64String(estimationTx.Serialize());
                finalPriorityFee = await GetPriorityFeeEstimate(serializedForEstimate);
            }
            else if (computeUnitPrice == 0)
            {
                finalPriorityFee = fallbackPriorityFee;
            }

            // Step 3: Rebuild transaction with compute budget instructions at the front
            transaction.Instructions.Clear();
            transaction.Signatures.Clear();
            
            if (computeUnitLimit > 0)
                transaction.Instructions.Add(ComputeBudgetProgram.SetComputeUnitLimit(computeUnitLimit));
            
            if (finalPriorityFee > 0)
                transaction.Instructions.Add(ComputeBudgetProgram.SetComputeUnitPrice(finalPriorityFee));

            // Re-add business logic instructions
            if (additionalInstructions != null)
                transaction.Instructions.AddRange(additionalInstructions);

            // Get fresh blockhash before signing
            blockHashResult = await rpcClient.GetLatestBlockHashAsync(Commitment.Confirmed);
            transaction.RecentBlockHash = blockHashResult.Result.Value.Blockhash;
            
            var signedTransaction = await baseWallet.SignTransaction(transaction);

            // Convert signed transaction to byte array for sending
            var serializedTransaction = signedTransaction.Serialize();

            var simulationResult = await rpcClient.SimulateTransactionAsync(
                serializedTransaction,
                commitment: Commitment.Confirmed
            );

            GameLogger.Log($"Full simulation result: {JsonConvert.SerializeObject(simulationResult.Result, Formatting.Indented)}");
            
            var result = await rpcClient.SendTransactionAsync(
                Convert.ToBase64String(serializedTransaction),
                true,
                Commitment.Confirmed
            );

            if (result.Result == null)
            {
                throw new Exception($"Transaction sending failed: {result.Reason}");
            }

            // Wait for confirmation using the appropriate RPC client
            var confirmed = await rpcClient.ConfirmTransaction(
                result.Result,
                Commitment.Confirmed
            );

            if (!confirmed)
            {
                throw new Exception("Transaction confirmation failed");
            }

            if (showedLoading)
                HideLoadingOverlay(0f);
            
            feedbackManager?.PlaySuccessSound();

            return result.Result;
        }
        catch (Exception ex)
        {
            // Show error and fade out after delay
            if (showedLoading)
            {
                if (loadingText != null && loadingTextComponent != null)
                {
                    loadingTextComponent.text = "error";
                    loadingTextComponent.color = Color.red;
                }
                
                StartCoroutine(FadeOutError());
            }
            
            feedbackManager?.PlayErrorSound();
            
            GameLogger.LogError($"Error in SendAndConfirmTransaction: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Check if an account is delegated to the rollup.
    /// </summary>
    public async Task<bool> CheckIfDelegated(PublicKey accountAddress)
    {
        try
        {
            var accountInfo = await _rpcClient.GetAccountInfoAsync(
                accountAddress,
                Commitment.Processed
            );

            return accountInfo.Result?.Value?.Owner?.Equals(DELEGATION_PROGRAM_ID.Key) ?? false;
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning($"Failed to check delegation status for {accountAddress}: {ex.Message}");
            return false;
        }
    }

    public void StartWalletConnection()
    {
        if (_isConnectingWallet) return;
        
        _isConnectingWallet = true;
        ShowLoadingOverlay("connecting wallet...");
        
        try
        {
            Web3.Instance.LoginWithWalletAdapter();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Wallet connection failed: {ex.Message}");
            CancelWalletConnection();
            
            if (loadingTextComponent != null)
            {
                loadingTextComponent.text = "connection error";
                loadingTextComponent.color = Color.red;
            }
            
            feedbackManager?.PlayErrorSound();
            StartCoroutine(FadeOutError());
        }
    }

    public void CancelWalletConnection()
    {
        _isConnectingWallet = false;
        HideLoadingOverlay(0f);
    }

    public void HideConnectionLoadingOverlay()
    {
        HideLoadingOverlay(0.3f);
    }
    
    private void ShowLoadingOverlay(string message)
    {
        UIFader.ShowImmediate(darkOverlay);
        UIFader.ShowImmediate(loadingCircle);
        
        if (loadingText != null && loadingTextComponent != null)
        {
            loadingTextComponent.text = message;
            loadingTextComponent.color = Color.white;
            UIFader.ShowImmediate(loadingText);
        }
    }
    
    private void HideLoadingOverlay(float duration)
    {
        if (duration == 0f)
        {
            UIFader.HideImmediate(darkOverlay);
            UIFader.HideImmediate(loadingCircle);
            if (loadingText != null)
                UIFader.HideImmediate(loadingText);
        }
        else
        {
            UIFader.FadeOut(darkOverlay, duration);
            UIFader.FadeOut(loadingCircle, duration);
            if (loadingText != null)
                UIFader.FadeOut(loadingText, duration);
        }
    }
    
    private System.Collections.IEnumerator FadeOutError()
    {
        HideLoadingOverlay(3f);
        yield break;
    }
} 