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
using Treasury.Program;
using Treasury;

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
    public CrashClient CrashClient { get; private set; }
    public CrashClient CrashClientEphemeral { get; private set; }
    public TreasuryClient TreasuryClient { get; private set; }
    
    private static PublicKey _crashProgramId = new PublicKey(CrashProgram.ID);
    private static PublicKey _treasuryProgramId = new PublicKey(TreasuryProgram.ID);

    private IRpcClient _rpcClient;
    private IRpcClient _ephemeralRpcClient;

    private WalletBase _walletBase;
    private WalletBase _ephemeralWalletBase;

    public static readonly InGameWallet EphemeralWallet = new(RpcCluster.DevNet, "https://devnet.magicblock.app", "wss://devnet.magicblock.app", true);

    public PublicKey MintPublicKey => string.IsNullOrWhiteSpace(mintAddress) ? null : new PublicKey(mintAddress);

    

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
            btn.onClick.AddListener(() => Web3.Instance?.LoginWithWalletAdapter());
            
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
            _ephemeralRpcClient = EphemeralWallet?.ActiveRpcClient;

            _walletBase = Web3.Instance.WalletBase;
            _ephemeralWalletBase = EphemeralWallet;

            var streamingClient = Web3.Instance.WalletBase.ActiveStreamingRpcClient;
            if (streamingClient == null)
            {
                Debug.LogWarning("ActiveStreamingRpcClient is null");
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
            btn.onClick.AddListener(() => Web3.Instance?.Logout());
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
            await Web3.UpdateBalance();
            if (Web3.Instance?.WalletBase != null)
            {
                var sol = await Web3.Instance.WalletBase.GetBalance();
                foreach (var txt in balanceTexts)
                {
                    txt.text = FormatAmount(sol);
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
            btn.onClick.AddListener(() => Web3.Instance?.LoginWithWalletAdapter());
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
            txt.text = FormatAmount(amount);
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


    public async Task<string> SendAndConfirmTransaction(
        bool ephemeralFlag = false, // if true, tx is happening on ER.
        uint computeUnitLimit = 0,
        ulong computeUnitPrice = 0,
        params TransactionInstruction[] additionalInstructions)
    {
        try
        {
            var baseWallet = SessionManager.SessionToken == null
                ? Web3.Wallet
                : SessionManager.SessionWallet;

            var rpcClient = ephemeralFlag ? _ephemeralRpcClient : _rpcClient;

            var blockHashResult = await rpcClient.GetLatestBlockHashAsync(Commitment.Confirmed);

            // Create a list to store our instructions
            var instructions = new List<TransactionInstruction>();

            // Add compute unit limit and price if specified
            if (computeUnitLimit > 0)
            {
                var computeLimitInstruction = ComputeBudgetProgram.SetComputeUnitLimit(computeUnitLimit);
                instructions.Add(computeLimitInstruction);
            }
            if (computeUnitPrice > 0)
            {
                var computePriceInstruction = ComputeBudgetProgram.SetComputeUnitPrice(computeUnitPrice);
                instructions.Add(computePriceInstruction);
            }

            // Add any additional instructions
            if (additionalInstructions != null)
            {
                foreach (var instruction in additionalInstructions)
                {
                    instructions.Add(instruction);
                }
            }

            // Create the transaction object
            var transaction = new Transaction();

            transaction.FeePayer = baseWallet.Account.PublicKey;
            transaction.RecentBlockHash = blockHashResult.Result.Value.Blockhash;
            transaction.Signatures = new List<SignaturePubKeyPair>();
            transaction.Instructions = new List<TransactionInstruction>();

            foreach (var instruction in instructions)
            {
                transaction.Instructions.Add(instruction);
            }
            
            var signedTransaction = await baseWallet.SignTransaction(transaction);

            // Convert signed transaction to byte array for sending
            var serializedTransaction = signedTransaction.Serialize();

            var simulationResult = await rpcClient.SimulateTransactionAsync(
                serializedTransaction,
                commitment: Commitment.Confirmed
            );

            Debug.Log($"Full simulation result: {JsonConvert.SerializeObject(simulationResult.Result, Formatting.Indented)}");
            
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

            return result.Result;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error in SendAndConfirmTransaction: {ex.Message}");
            throw;
        }
    }

} 