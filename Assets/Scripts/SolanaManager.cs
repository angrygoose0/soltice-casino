using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Linq;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Rpc.Builders;
using System.Threading.Tasks;
using System;
using Solana.Unity.Programs;
using Solana.Unity.Programs.Models;
using Solana.Unity.Rpc;
using Newtonsoft.Json;
using Crash.Program;
using Crash;

public class SolanaManager : MonoBehaviour
{
    [Header("Wallet Buttons")]
    [SerializeField] private List<Button> connectButtons;
    [SerializeField] private List<Button> disconnectButtons;

    [Header("Mint")]
    [SerializeField] private string mintAddress;
    [SerializeField] private int tokenDecimals = 6; // Default to 6 decimals (millions)
    
    [Header("UI Elements")]
    [SerializeField] private List<TextMeshProUGUI> publicKeyTexts;
    [SerializeField] private List<TextMeshProUGUI> balanceTexts;
    [SerializeField] private List<TextMeshProUGUI> tokenBalanceTexts;

    // Crash Client properties
    private CrashClient _crashClient;

    private static PublicKey _programId = new PublicKey(CrashProgram.ID);
    private IRpcClient _rpcClient;


    public CrashClient GetCrashClient() => _crashClient;

    public PublicKey GetMintPublicKey()
    {
        if (string.IsNullOrWhiteSpace(mintAddress))
        {
            return null;
        }
        return new PublicKey(mintAddress);
    }


    private void OnEnable()
    {
        // Check if Web3 is available and user is logged in
        if (Web3.Instance != null && Web3.Account != null)
        {
            OnLogin(Web3.Account);
        }
        else
        {
            OnLogout();
        }

        // Set button texts and listeners
        foreach (var btn in connectButtons)
        {
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = "Connect";
            }
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                // Play button click feedback
                //feedbackManager?.PlayButtonClickFeedback();
                
                // Call the handler
                ConnectWallet();
            });
            
            // Add hover effect with EventTrigger
            AddHoverEffect(btn);
        }

        foreach (var btn in disconnectButtons)
        {
            var btnText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = "Disconnect";
            }
            
            // Add hover effect with EventTrigger
            AddHoverEffect(btn);
        }


        Web3.OnLogin += OnLogin;
        Web3.OnLogout += OnLogout;
        Web3.OnBalanceChange += OnBalanceChange;
    }

    public void InitializeClients()
    {
        if (Web3.Instance?.WalletBase == null)
        {
            Debug.LogWarning("Web3 instance or WalletBase not initialized yet");
            return;
        }

        try
        {
            // Initialize main client
            _rpcClient = Web3.Instance.WalletBase.ActiveRpcClient;
            if (_rpcClient == null)
            {
                Debug.LogWarning("ActiveRpcClient is null");
                return;
            }

            var streamingClient = Web3.Instance.WalletBase.ActiveStreamingRpcClient;
            if (streamingClient == null)
            {
                Debug.LogWarning("ActiveStreamingRpcClient is null");
                return;
            }


            _crashClient = new CrashClient(
                _rpcClient,
                streamingClient,
                _programId
            );

            
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error initializing Clients: {ex.Message}");
        }
    }

    private void AddHoverEffect(Button button)
    {
        // Add hover effect with EventTrigger
        EventTrigger eventTrigger = button.gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = button.gameObject.AddComponent<EventTrigger>();
        }
        
        // Clear any existing triggers
        if (eventTrigger.triggers != null)
        {
            eventTrigger.triggers.Clear();
        }
        else
        {
            eventTrigger.triggers = new List<EventTrigger.Entry>();
        }
        
        // Add pointer enter (hover) event
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => {
            // Play button hover feedback
            //feedbackManager?.PlayButtonClickFeedback();
        });
        eventTrigger.triggers.Add(enterEntry);
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnLogin;
        Web3.OnLogout -= OnLogout;
        Web3.OnBalanceChange -= OnBalanceChange;
    }

    private async void OnLogin(Account account)
    {
        // Initialize clients when user logs in
        InitializeClients();

        // Update connect/disconnect buttons
        foreach (var btn in connectButtons)
        {
            btn.gameObject.SetActive(false);
        }
        foreach (var btn in disconnectButtons)
        {
            btn.gameObject.SetActive(true);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                // Play button click feedback
                //feedbackManager?.PlayButtonClickFeedback();
                
                // Call the handler
                DisconnectWallet();
            });
        }

        // Update public key texts
        foreach (var txt in publicKeyTexts)
        {
            txt.gameObject.SetActive(true);
            txt.text = account.PublicKey;
        }

        // Show balance texts
        foreach (var txt in balanceTexts)
        {
            txt.gameObject.SetActive(true);
        }

        // Show and update token balance texts
        foreach (var txt in tokenBalanceTexts)
        {
            txt.gameObject.SetActive(true);
        }
        
        // Immediately refresh SOL balance and update UI once
        await System.Threading.Tasks.Task.Delay(300);
        try
        {
            Web3.UpdateBalance();
            if (Web3.Instance?.WalletBase != null)
            {
                var sol = await Web3.Instance.WalletBase.GetBalance();
                foreach (var txt in balanceTexts)
                {
                    txt.text = "SOL: " + FormatSol(sol);
                }
            }
        }
        catch (Exception) { }

        // Update token balance
        UpdateTokenBalance();

    }

    private void OnLogout()
    {
        // Update connect/disconnect buttons
        foreach (var btn in connectButtons)
        {
            btn.gameObject.SetActive(true);
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                // Play button click feedback
                //feedbackManager?.PlayButtonClickFeedback();
                
                // Call the handler
                ConnectWallet();
            });
        }
        foreach (var btn in disconnectButtons)
        {
            btn.gameObject.SetActive(false);
        }

        // Hide and clear public key texts
        foreach (var txt in publicKeyTexts)
        {
            txt.gameObject.SetActive(false);
            txt.text = string.Empty;
        }

        // Hide balance texts
        foreach (var txt in balanceTexts)
        {
            txt.gameObject.SetActive(false);
            txt.text = string.Empty;
        }

        // Hide and clear token balance texts
        foreach (var txt in tokenBalanceTexts)
        {
            txt.gameObject.SetActive(false);
            txt.text = string.Empty;
        }
    }

    private void OnBalanceChange(double amount)
    {
        // Update all balance texts
        foreach (var txt in balanceTexts)
        {
            txt.text = "SOL: " + FormatSol(amount);
        }
    }

    private string FormatBalance(double amount)
    {
        // Convert raw amount to actual token amount based on decimals
        double actualAmount = amount / Math.Pow(10, tokenDecimals);

        if (actualAmount >= 1000000)
        {
            return (actualAmount / 1000000).ToString("F2", CultureInfo.InvariantCulture) + "m";
        }
        else if (actualAmount >= 1000)
        {
            return (actualAmount / 1000).ToString("F2", CultureInfo.InvariantCulture) + "k";
        }
        else
        {
            return actualAmount.ToString("F2", CultureInfo.InvariantCulture);
        }
    }

    private string FormatSol(double amount)
    {
        // Amount is expected to be in SOL units already
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

    private async void UpdateTokenBalance()
    {
        if (string.IsNullOrEmpty(mintAddress))
        {
            return;
        }

        if (Web3.Account == null)
        {
            return;
        }

        try
        {
            double tokenBalance = await GetSPLTokenBalance();
            
            // Update all token balance texts
            foreach (var txt in tokenBalanceTexts)
            {
                txt.text = "$BLKJAK: " + FormatBalance(tokenBalance);
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

    private async Task<double> GetSPLTokenBalance()
    {
        if (Web3.Account == null || string.IsNullOrEmpty(mintAddress))
        {
            return 0.0;
        }

        try
        {
            // Get all token accounts for the current wallet
            var tokenAccounts = await Web3.Wallet.GetTokenAccounts(Commitment.Confirmed);
            
            if (tokenAccounts != null)
            {
                // Find the token account that matches our mint address
                var matchingAccount = tokenAccounts.FirstOrDefault(t => 
                    t.Account.Data.Parsed.Info.Mint == mintAddress);
                
                if (matchingAccount != null)
                {
                    var tokenAmount = matchingAccount.Account.Data.Parsed.Info.TokenAmount;
                    
                    // Try to get the UI amount (properly decimalized)
                    if (!string.IsNullOrEmpty(tokenAmount.UiAmountString))
                    {
                        if (double.TryParse(tokenAmount.UiAmountString, NumberStyles.Any, CultureInfo.InvariantCulture, out double balance))
                        {
                            // Convert to raw amount based on decimals
                            return balance * Math.Pow(10, tokenDecimals);
                        }
                    }
                    
                    // Fallback to raw amount if UiAmountString is not available
                    if (ulong.TryParse(tokenAmount.Amount, out ulong rawAmount))
                    {
                        return (double)rawAmount;
                    }
                }
            }
            
            return 0.0;
        }
        catch (System.Exception)
        {
            return 0.0;
        }
    }

    // Public methods for button callbacks
    public void ConnectWallet()
    {
        if (Web3.Instance == null)
        {
            return;
        }
        Web3.Instance.LoginWithWalletAdapter();
    }

    public void DisconnectWallet()
    {
        if (Web3.Instance == null)
        {
            return;
        }
        Web3.Instance.Logout();
    }

    // Public method to refresh token balance
    public void RefreshTokenBalance()
    {
        UpdateTokenBalance();
    }

    public async Task<string> SendAndConfirmTransaction(
        bool reservedFlag = false,
        uint computeUnitLimit = 0,
        ulong computeUnitPrice = 0,
        params TransactionInstruction[] additionalInstructions)
    {
        try
        {
            var primaryEndpoint = Web3.Instance != null ? Web3.Instance.customRpc : null;
            Debug.Log($"SendAndConfirmTransaction using RPC: {primaryEndpoint ?? "<unknown>"}");
            var blockHashResult = await Web3.Rpc.GetLatestBlockHashAsync(Commitment.Confirmed);
            if (blockHashResult.Result == null)
            {
                throw new Exception("Failed to get recent block hash");
            }

            // Build the transaction
            var transaction = new TransactionBuilder()
                .SetRecentBlockHash(blockHashResult.Result.Value.Blockhash)
                .SetFeePayer(Web3.Account);

            // Create a list to store our instructions
            var instructions = new List<TransactionInstruction>();

            // Add compute unit limit and price if specified
            if (computeUnitLimit > 0)
            {
                var computeLimitInstruction = ComputeBudgetProgram.SetComputeUnitLimit(computeUnitLimit);
                transaction.AddInstruction(computeLimitInstruction);
                instructions.Add(computeLimitInstruction);
            }
            if (computeUnitPrice > 0)
            {
                var computePriceInstruction = ComputeBudgetProgram.SetComputeUnitPrice(computeUnitPrice);
                transaction.AddInstruction(computePriceInstruction);
                instructions.Add(computePriceInstruction);
            }

            // Add any additional instructions
            if (additionalInstructions != null)
            {
                foreach (var instruction in additionalInstructions)
                {
                    transaction.AddInstruction(instruction);
                    instructions.Add(instruction);
                }
            }

            // Create the transaction object
            var unsignedTransaction = new Transaction
            {
                RecentBlockHash = blockHashResult.Result.Value.Blockhash,
                FeePayer = Web3.Account.PublicKey,
                Instructions = instructions,
                Signatures = new List<SignaturePubKeyPair>()
            };
            
            // Sign the transaction using the wallet
            var signedTransaction = await Web3.Instance.WalletBase.SignTransaction(unsignedTransaction);

            // Convert signed transaction to byte array for sending
            var serializedTransaction = signedTransaction.Serialize();

            // Simulate the transaction first
            var rpcClient = Web3.Rpc;
            var simulationResult = await rpcClient.SimulateTransactionAsync(
                serializedTransaction,
                commitment: Commitment.Confirmed
            );

            Debug.Log($"Full simulation result: {JsonConvert.SerializeObject(simulationResult.Result, Formatting.Indented)}");

            // Send and confirm the transaction
            var result = await rpcClient.SendTransactionAsync(
                serializedTransaction,
                commitment: Commitment.Confirmed,
                skipPreflight: false
            );

            if (result.Result == null)
            {
                var reason = result.Reason ?? "<no reason>";
                Debug.LogWarning($"Primary RPC send failed. Reason: {reason}");

                // If RPC returned an invalid JSON error (often proxy/HTML), retry against official devnet as fallback
                if (reason.IndexOf("Unable to parse json", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    var fallbackUrl = "https://api.devnet.solana.com";
                    Debug.LogWarning($"Retrying SendTransaction against fallback RPC: {fallbackUrl}");
                    var fallbackClient = ClientFactory.GetClient(fallbackUrl);

                    var fallbackSend = await fallbackClient.SendTransactionAsync(
                        serializedTransaction,
                        commitment: Commitment.Confirmed,
                        skipPreflight: false
                    );

                    if (fallbackSend.Result == null)
                    {
                        throw new Exception($"Transaction failed on fallback as well: {fallbackSend.Reason}");
                    }

                    bool confirmedFallback = await fallbackClient.ConfirmTransaction(
                        fallbackSend.Result,
                        Commitment.Confirmed
                    );

                    if (!confirmedFallback)
                    {
                        throw new Exception("Transaction confirmation failed on fallback RPC");
                    }

                    return fallbackSend.Result;
                }

                throw new Exception($"Transaction failed: {reason}");
            }

            // Wait for confirmation
            bool confirmed = await rpcClient.ConfirmTransaction(
                result.Result,
                Commitment.Confirmed
            );

            if (!confirmed)
            {
                throw new Exception("Transaction confirmation failed");
            }

            return result.Result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in SendAndConfirmTransaction: {ex.Message}");
            throw;
        }
    }

} 