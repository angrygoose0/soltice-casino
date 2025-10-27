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
    [Header("Dependencies")]
    [SerializeField] private InteractableObjects interactableObjects;

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
    public int TokenDecimals => tokenDecimals;
    
    private static PublicKey _crashProgramId = new PublicKey(CrashProgram.ID);
    private static PublicKey _treasuryProgramId = new PublicKey(TreasuryProgram.ID);
    public static readonly PublicKey DELEGATION_PROGRAM_ID = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");

    private IRpcClient _rpcClient;
    private IRpcClient _ephemeralRpcClient;

    private WalletBase _walletBase;
    private WalletBase _ephemeralWalletBase;
    private bool _gameJoined = false;

    public static readonly InGameWallet EphemeralWallet = new(RpcCluster.DevNet, "https://devnet.magicblock.app", "wss://devnet.magicblock.app", true);

    public PublicKey MintPublicKey => string.IsNullOrWhiteSpace(mintAddress) ? null : new PublicKey(mintAddress);

    

    private void OnEnable()
    {
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

    private void OnDisable()
    {
        SimpleWorldJoin.OnGameJoined -= OnGameJoined;
        Web3.OnLogin -= OnLogin;
        Web3.OnLogout -= OnLogout;
        Web3.OnBalanceChange -= OnBalanceChange;
    }

    private void OnGameJoined()
    {
        _gameJoined = true;
        Debug.Log("Game joined - enabling wallet buttons");
        
        // Show appropriate buttons based on wallet state
        if (interactableObjects != null)
        {
            if (Web3.Instance != null && Web3.Account != null)
            {
                interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.DisconnectWallet, true);
            }
            else
            {
                interactableObjects.SetInteractableHardEnabledByAction(InteractableObjects.ActionType.ConnectWallet, true);
            }
        }
    }

    private async void OnLogin(Account account)
    {
        // Initialize clients when user logs in
        InitializeClients();
        Debug.Log("Login successful");

        // Update connect/disconnect buttons
        if (interactableObjects != null)
        {
            Debug.Log("Setting interactable objects to hard disable connect wallet");
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

    /// <summary>
    /// Check if an account is delegated to the rollup.
    /// </summary>
    public async Task<bool> CheckIfDelegated(PublicKey accountAddress)
    {
        try
        {
            var accountInfo = await Web3.Wallet.ActiveRpcClient.GetAccountInfoAsync(
                accountAddress,
                Commitment.Processed
            );

            return accountInfo.Result?.Value?.Owner?.Equals(DELEGATION_PROGRAM_ID.Key) ?? false;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to check delegation status for {accountAddress}: {ex.Message}");
            return false;
        }
    }

} 