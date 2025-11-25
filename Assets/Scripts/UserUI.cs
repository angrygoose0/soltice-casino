using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Crash.Accounts;
using Treasury.Accounts;
using Blackjack.Accounts;
using BlackJackGame = Blackjack.Accounts.BlackJack;
using TreasuryAccount = Treasury.Accounts.Treasury;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using TMPro;
using System.Linq;
using Coherence.Toolkit;

public class UserUI : MonoBehaviour
{
    [SerializeField] private TreasuryTransactionBuilder treasuryBuilder;
    [SerializeField] private CrashTransactionBuilder crashBuilder;
    [SerializeField] private BlackjackTransactionBuilder blackjackBuilder;
    [SerializeField] private SolanaManager solanaManager;
    [SerializeField] private CoherenceBridge coherenceBridge;
    [SerializeField] private FeedbackManager feedbackManager;
    [SerializeField] private Button withdrawButton;
    [SerializeField] private PlayerManager playerManager;

    private Game _gameCache;
    private PlayerBet _playerBetCache;
    private UserBalance _userBalanceCache;
    private TreasuryAccount _treasuryCache;
    private BlackJackGame _blackjackGameCache;

    // Subscription tracking
    private string _playerBetSubscriptionId;
    private string _userBalanceSubscriptionId;
    
    // Blackjack hands tracking
    private Dictionary<PublicKey, BlackJackHand> _blackjackHandsCache = new Dictionary<PublicKey, BlackJackHand>();
    private Dictionary<PublicKey, string> _blackjackHandSubscriptionIds = new Dictionary<PublicKey, string>();
    private PublicKey _subscribedBlackjackPk;
    
    // Count of blackjack hands owned by user's pool (hand_ids are 0 to count-1)
    private byte _userBlackjackHandCount = 0;

    [SerializeField] private GameObject beforeBettingGroup;
    [SerializeField] private GameObject afterBettingGroup;
    [SerializeField] private TextMeshProUGUI afterBettingText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI countdownText;

    [SerializeField] private BalloonSimulator _balloonSimulator;

    private Coroutine _countdownCoroutine;

    private ulong _betAmount = 0;
    public ulong betAmount
    {
        get => _betAmount;
        set
        {
            _betAmount = value;
            UpdateMaxBetUI();
        }
    }
    public byte clientSeed = 42;

    // Account Data Display
    [Header("Account Data Display")]
    [SerializeField] private TextMeshProUGUI userBalanceAccountTMP;

    [SerializeField] private TextMeshProUGUI playerTextTMP;
    [SerializeField] private TextMeshProUGUI maxBetTextTMP;

    // Cached max bet amount (updated dynamically)
    public ulong maxBetAmount { get; private set; } = 0;
    private string maxBetReason = "";

    private async void UpdateMaxBet()
    {
        // Default to 0 if wallet not connected or data not available
        if (Web3.Account == null || solanaManager == null)
        {
            maxBetAmount = 0;
            return;
        }

        try
        {
            // Limit 1: wallet token balance + ephemeral balance
            double walletBalanceDouble = await solanaManager.GetSPLTokenBalance();
            ulong walletBalance = (ulong)walletBalanceDouble;
            ulong ephemeralBalance = _userBalanceCache?.Balance ?? 0;
            Debug.Log($"Ephemeral balance: {ephemeralBalance}");
            Debug.Log($"Wallet balance: {walletBalance}");
            ulong userLimit = walletBalance + ephemeralBalance;

            // Limit 2: (treasury token account balance - userOwnedAmount - buffer_amount) * 0.5
            var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();
            Debug.Log($"Treasury token account: {treasuryTokenAccount}");
            ulong treasuryTokenBalance = await solanaManager.GetTokenAccountBalance(treasuryTokenAccount);
            Debug.Log($"Treasury token balance: {treasuryTokenBalance}");
            
            ulong houseLimit = 0;
            if (_treasuryCache != null)
            {
                ulong availableHouseFunds = treasuryTokenBalance;
                
                Debug.Log($"Available house funds: {availableHouseFunds}");
                Debug.Log($"Buffer amount: {_treasuryCache.BufferAmount}");
                Debug.Log($"User owned amount: {_treasuryCache.UserOwnedAmount}");
                if (availableHouseFunds >= _treasuryCache.BufferAmount)
                    availableHouseFunds -= _treasuryCache.BufferAmount;
                else
                    availableHouseFunds = 0;
                
                if (availableHouseFunds >= _treasuryCache.UserOwnedAmount)
                    availableHouseFunds -= _treasuryCache.UserOwnedAmount;
                else
                    availableHouseFunds = 0;
                
                houseLimit = availableHouseFunds / 2; // 50% of available house funds
            }
            
            Debug.Log($"userLimit: {userLimit}, houseLimit: {houseLimit}");

            // Set to the minimum of the two limits and track the reason
            if (userLimit <= houseLimit)
            {
                maxBetAmount = userLimit;
                maxBetReason = "user balance";
            }
            else
            {
                maxBetAmount = houseLimit;
                maxBetReason = "house limit";
            }
            
            // Clamp current bet amount if it exceeds the new limit
            if (betAmount > maxBetAmount)
            {
                betAmount = maxBetAmount;
                Debug.Log($"Bet amount clamped to new limit: {maxBetAmount}");
            }
            
            UpdateMaxBetUI();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to calculate max bet: {ex.Message}");
            maxBetAmount = 0;
            betAmount = 0;
            maxBetReason = "";
            UpdateMaxBetUI();
        }
    }

    private void UpdateMaxBetUI()
    {
        if (maxBetTextTMP == null)
            return;
        
        // Show max bet UI when bet amount equals max bet
        if (betAmount >= maxBetAmount)
        {
            ulong displayAmount = ConvertToDisplayAmount(maxBetAmount);
            maxBetTextTMP.text = $"max bet: {displayAmount} ({maxBetReason})";
            UIFader.FadeIn(maxBetTextTMP.gameObject);
        }
        else
        {
            UIFader.FadeOut(maxBetTextTMP.gameObject);
        }
    }

    private void UpdateWithdrawButtonVisibility()
    {
        if (withdrawButton == null)
            return;
        
        ulong ephemeralBalance = _userBalanceCache?.Balance ?? 0;
        
        if (Web3.Account != null)
        {
            UIFader.FadeIn(withdrawButton.gameObject);
            withdrawButton.interactable = ephemeralBalance > 0;
        }
        else
        {
            UIFader.FadeOut(withdrawButton.gameObject);
        }
    }

    private void Start()
    {
        SetupGameSubscription();
        SetupBlackjackGameSubscription();
        
        if (withdrawButton != null)
        {
            withdrawButton.onClick.AddListener(() => Withdraw(0));
            UIFader.HideImmediate(withdrawButton.gameObject);
        }
        
        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
        
        if (maxBetTextTMP != null)
            UIFader.HideImmediate(maxBetTextTMP.gameObject);
        
        if (userBalanceAccountTMP != null)
            userBalanceAccountTMP.gameObject.SetActive(false);
        
        UIFader.HideImmediate(beforeBettingGroup);
        UIFader.HideImmediate(afterBettingGroup);
    }

    private void Update()
    {
        if (Keyboard.current != null && 
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            if (startButton != null && startButton.gameObject.activeInHierarchy && startButton.interactable)
            {
                StartGame();
            }
        }
    }

    public void SetPlayerBetText(TextMeshProUGUI betText)
    {
        playerTextTMP = betText;
    }
    
    private ulong ConvertToDisplayAmount(ulong rawAmount)
    {
        return (ulong)(rawAmount / Math.Pow(10, solanaManager.TokenDecimals));
    }

    private IEnumerator CountdownToStartAvailable(long nextActionTime)
    {
        // Hide start button and show countdown text
        if (startButton != null)
            startButton.gameObject.SetActive(false);
        if (countdownText != null)
            countdownText.gameObject.SetActive(true);
        
        while (true)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long timeRemaining = nextActionTime - currentTime;
            
            if (timeRemaining <= 0)
            {
                // Hide countdown text and show start button
                if (countdownText != null)
                    countdownText.gameObject.SetActive(false);
                if (startButton != null)
                    startButton.gameObject.SetActive(true);
                yield break;
            }
            
            if (countdownText != null)
                countdownText.text = timeRemaining.ToString();
            
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnEnable()
    {
        Web3.OnLogin += OnWalletConnected;
        Web3.OnLogout += OnWalletDisconnected;
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnWalletConnected;
        Web3.OnLogout -= OnWalletDisconnected;
    }

    private async void OnWalletConnected(Account account)
    {
        if (userBalanceAccountTMP != null)
            userBalanceAccountTMP.gameObject.SetActive(true);
        
        try
        {
        await SetupTreasurySubscription();
        await SetupUserAccountSubscriptions();
        await RefreshUserBlackjackHandCount();
        UpdateMaxBet();
        UpdateWithdrawButtonVisibility();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Subscription setup failed: {ex.Message}");
        }
        finally
        {
            // Always hide the loading overlay, even if subscriptions failed
            solanaManager?.HideConnectionLoadingOverlay();
        }
    }

    private async void OnWalletDisconnected()
    {
        // Unsubscribe from user-specific accounts
        if (!string.IsNullOrEmpty(_playerBetSubscriptionId))
        {
            await SubscriptionManager.Instance.Unsubscribe(_playerBetSubscriptionId);
            _playerBetSubscriptionId = null;
        }
        
        if (!string.IsNullOrEmpty(_userBalanceSubscriptionId))
        {
            await SubscriptionManager.Instance.Unsubscribe(_userBalanceSubscriptionId);
            _userBalanceSubscriptionId = null;
        }
        
        // Reset cache for next connection
        _playerBetCache = null;
        _userBalanceCache = null;
        
        // Reset max bet
        maxBetAmount = 0;
        betAmount = 0;
        
        // Hide all UI groups when wallet disconnects
        UIFader.FadeOut(beforeBettingGroup);
        UIFader.FadeOut(afterBettingGroup);
        
        if (withdrawButton != null)
            UIFader.FadeOut(withdrawButton.gameObject);
        
        if (maxBetTextTMP != null)
            UIFader.FadeOut(maxBetTextTMP.gameObject);
        
        if (userBalanceAccountTMP != null)
            userBalanceAccountTMP.gameObject.SetActive(false);
        
        Debug.Log("Wallet disconnected - UI reset");
    }


    //ENSURE user_balance delegated
    public async void DepositAsync(ulong amount) //user
    {
        try
        {
            var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);
            bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);
            
            // 1. Undelegate on ER if currently delegated
            if (userBalanceIsDelegated)
            {
                var undelegateIx = treasuryBuilder.UndelegateUserBalance();
                await solanaManager.SendAndConfirmTransaction(true, 400000u, 20000ul, "undelegating balance...", undelegateIx);
            }
            
            // 2. Deposit + Re-delegate on base layer (together)
            var userDepositIx = treasuryBuilder.UserDeposit(amount);
            var delegateIx = treasuryBuilder.DelegateUserBalance();
            await solanaManager.SendAndConfirmTransaction(false, 400000u, 20000ul, $"depositing {amount}...", userDepositIx, delegateIx);
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Deposit failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    //ENSURE user_balance delegated
    public async void Withdraw(ulong amount) //user
    {
        try
        {
            // 1. Undelegate on ER
            var undelegateIx = treasuryBuilder.UndelegateUserBalance();
            await solanaManager.SendAndConfirmTransaction(true, 400000u, 20000ul, "undelegating balance...", undelegateIx);
            
            // 2. Withdraw + Re-delegate on base layer (together)
            var userWithdrawIx = treasuryBuilder.UserWithdraw(amount);
            var delegateIx = treasuryBuilder.DelegateUserBalance();
            await solanaManager.SendAndConfirmTransaction(false, 400000u, 20000ul, $"withdrawing {amount}...", userWithdrawIx, delegateIx);
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Withdraw failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    //ENSURE game delegated
    public async void SetupRandomness(byte clientSeed) //user
    {
        try
        {
            var requestRandomnessIx = crashBuilder.RequestRandomness(clientSeed);
            await solanaManager.SendAndConfirmTransaction(true, 300000u, 20000ul, "requesting randomness...", requestRandomnessIx);
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Setup randomness failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void StartGame() //user
    {
        try
        {
            var startGameIx = crashBuilder.StartGame();
            await solanaManager.SendAndConfirmTransaction(true, 300000u, 20000ul, "starting game...", startGameIx);
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Start game failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    //ENSURE player_bet + ephemeral_balance delegated + user setup + game delegated
    public async void PlaceBet() //user
    {
        try
        {
            var instructions = new List<TransactionInstruction>();

            var playerBetPk = CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey);
            var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);

            // Step 1: Initialize any uninitialized accounts
            var playerBetData = await SubscriptionManager.Instance.LoadAccountData<PlayerBet>(
                playerBetPk, data => PlayerBet.Deserialize(data), forceDelegated: false
            );
            
            bool isInitializingPlayerBet = playerBetData == null;
            if (isInitializingPlayerBet)
            {
                Debug.Log("Initializing PlayerBet account");
                instructions.Add(crashBuilder.InitializePlayerBet());
            }
            
            var userBalanceData = await SubscriptionManager.Instance.LoadAccountData<UserBalance>(
                userBalancePk, data => UserBalance.Deserialize(data), forceDelegated: false
            );
            
            bool isInitializingUserBalance = userBalanceData == null;
            if (isInitializingUserBalance)
            {
                Debug.Log("Initializing UserBalance account");
                instructions.Add(treasuryBuilder.InitializeBalance());
            }

            // Step 2 & 3: Calculate how much deposit is needed based on existing balance
            ulong depositAmount = betAmount;
            if (!isInitializingUserBalance && _userBalanceCache != null && _userBalanceCache.Balance > 0)
            {
                if (_userBalanceCache.Balance >= betAmount)
                {
                    Debug.Log($"Sufficient balance available ({_userBalanceCache.Balance} >= {betAmount}), skipping deposit");
                    depositAmount = 0;
                }
                else
                {
                    depositAmount = betAmount - _userBalanceCache.Balance;
                    Debug.Log($"Partial balance available ({_userBalanceCache.Balance}), depositing remaining {depositAmount}");
                }
            }
            
            bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);

            if (depositAmount > 0)
            {
                if (userBalanceIsDelegated)
                {
                    var undelegateIx = treasuryBuilder.UndelegateUserBalance();
                    await solanaManager.SendAndConfirmTransaction(true, 400000u, 20000ul, "undelegating balance...", undelegateIx);
                    userBalanceIsDelegated = false;
                }
                
                var userDepositIx = treasuryBuilder.UserDeposit(depositAmount);
                instructions.Add(userDepositIx);
            }

            if (!userBalanceIsDelegated)
            {
                Debug.Log("Delegating UserBalance");
                instructions.Add(treasuryBuilder.DelegateUserBalance());
            }

            // Step 4: Delegate PlayerBet if it wasn't
            bool playerBetIsDelegated = await solanaManager.CheckIfDelegated(playerBetPk);
            
            if (!playerBetIsDelegated)
            {
                Debug.Log("Delegating PlayerBet");
                instructions.Add(crashBuilder.DelegatePlayerBet());
            }

            bool needsSubscriptionSetup = isInitializingPlayerBet || isInitializingUserBalance;

            if (instructions.Count > 0)
            {
                Debug.Log($"Sending combined transaction with {instructions.Count} instruction(s)");
                await solanaManager.SendAndConfirmTransaction(false, 500000u, 20000ul, "initializing and depositing...", instructions.ToArray());
                
                // If we initialized accounts, setup subscriptions after delegation
                if (needsSubscriptionSetup)
                {
                    Debug.Log("Accounts initialized - setting up subscriptions");
                    await SetupUserAccountSubscriptions();
                }
            }

            // Step 5: Send place_bet transaction on ER layer
            Debug.Log($"Placing bet of {betAmount}");
            var placeBetIx = crashBuilder.PlaceBet(betAmount);
            await solanaManager.SendAndConfirmTransaction(true, 300000u, 20000ul, $"placing bet {betAmount}...", placeBetIx);
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Place bet failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    //ENSURE player_bet + ephemeral_balance delegated + user setup + game delegated
    public async void ClaimBet() //user
    {
        try
        {
            var claimBetIx = crashBuilder.ClaimBet();
            await solanaManager.SendAndConfirmTransaction(true, 300000u, 20000ul, "claiming bet...", claimBetIx);
            
            feedbackManager?.PlaySuccessSound();
            feedbackManager?.PlayCashFountainSound();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Claim bet failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }


    public async void SetupGameSubscription()
    {
        var gamePk = CrashTransactionBuilder.DeriveGameAccount();
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<Game>(
            gamePk,
            OnGameUpdate,
            data => Game.Deserialize(data),
            forceDelegated: true
        );

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("Failed to subscribe to delegated game account");
        }
    }

    public async void SetupBlackjackGameSubscription()
    {
        var blackjackPk = BlackjackTransactionBuilder.DeriveBlackjackAccount(1);
        _subscribedBlackjackPk = blackjackPk;
        
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<BlackJackGame>(
            blackjackPk,
            OnBlackjackGameUpdate,
            data => BlackJackGame.Deserialize(data),
            forceDelegated: true
        );

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("Failed to subscribe to delegated blackjack game account");
        }
        
        // Also subscribe to all hands for this blackjack game
        await SetupBlackjackHandsSubscription(blackjackPk);
    }

    private async Task<List<PublicKey>> GetBlackjackHandsByGame(PublicKey blackjackAccount)
    {
        var rpcClient = SolanaManager.EphemeralWallet?.ActiveRpcClient;
        if (rpcClient == null) return new List<PublicKey>();
        
        try
        {
            // BlackJackHand structure:
            // - 8 bytes: discriminator
            // - 32 bytes: Blackjack pubkey (offset 8)
            var memCmpList = new List<MemCmp>
            {
                new MemCmp
                {
                    Bytes = BlackJackHand.ACCOUNT_DISCRIMINATOR_B58,
                    Offset = 0
                },
                new MemCmp
                {
                    Bytes = blackjackAccount.Key,
                    Offset = 8
                }
            };
            
            var result = await rpcClient.GetProgramAccountsAsync(
                Blackjack.Program.BlackjackProgram.ID,
                Commitment.Confirmed,
                memCmpList: memCmpList
            );
            
            return result.Result?.Select(a => new PublicKey(a.PublicKey)).ToList() ?? new List<PublicKey>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting blackjack hands: {ex.Message}");
            return new List<PublicKey>();
        }
    }

    /// <summary>
    /// Refreshes the count of blackjack hands owned by the current user's pool.
    /// </summary>
    public async Task RefreshUserBlackjackHandCount()
    {
        if (Web3.Account == null) return;
        
        var rpcClient = SolanaManager.EphemeralWallet?.ActiveRpcClient;
        if (rpcClient == null) return;
        
        var player = Web3.Account.PublicKey;
        
        try
        {
            // BlackJackHand structure:
            // - 8 bytes: discriminator
            // - 32 bytes: Blackjack pubkey (offset 8)
            // - 32 bytes: Player pubkey (offset 40)
            var memCmpList = new List<MemCmp>
            {
                new MemCmp { Bytes = BlackJackHand.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 },
                new MemCmp { Bytes = player.Key, Offset = 40 }
            };
            
            var result = await rpcClient.GetProgramAccountsAsync(
                Blackjack.Program.BlackjackProgram.ID,
                Commitment.Confirmed,
                memCmpList: memCmpList
            );
            
            _userBlackjackHandCount = (byte)(result.Result?.Count ?? 0);
            Debug.Log($"User has {_userBlackjackHandCount} blackjack hands");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error refreshing user blackjack hand count: {ex.Message}");
        }
    }

    public byte GetNextHandId() => _userBlackjackHandCount;

    public PublicKey GetUserHandPubkey(byte handId)
    {
        if (Web3.Account == null) return null;
        return BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, handId);
    }

    private async Task SetupBlackjackHandsSubscription(PublicKey blackjackAccount)
    {
        var handPubkeys = await GetBlackjackHandsByGame(blackjackAccount);
        Debug.Log($"Found {handPubkeys.Count} blackjack hands for game {blackjackAccount}");
        
        foreach (var handPk in handPubkeys)
        {
            
            // Skip if already subscribed
            if (_blackjackHandSubscriptionIds.ContainsKey(handPk))
                continue;
            
            var subId = await SubscriptionManager.Instance.SubscribeAndLoad<BlackJackHand>(
                handPk,
                data => OnBlackjackHandUpdate(handPk, data),
                data => BlackJackHand.Deserialize(data),
                forceDelegated: true
            );
            
            if (!string.IsNullOrEmpty(subId))
            {
                _blackjackHandSubscriptionIds[handPk] = subId;
                Debug.Log($"Subscribed to blackjack hand: {handPk}");
            }
        }
    }

    public async Task SetupTreasurySubscription()
    {
        var treasuryPk = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<TreasuryAccount>(
            treasuryPk,
            OnTreasuryUpdate,
            data => TreasuryAccount.Deserialize(data),
            forceDelegated: false
        );

        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("Failed to subscribe to treasury account");
        }
    }

    private async Task SetupUserAccountSubscriptions()
    {
        // Unsubscribe from existing subscriptions if they exist
        if (!string.IsNullOrEmpty(_playerBetSubscriptionId))
        {
            await SubscriptionManager.Instance.Unsubscribe(_playerBetSubscriptionId);
            _playerBetSubscriptionId = null;
        }
        
        if (!string.IsNullOrEmpty(_userBalanceSubscriptionId))
        {
            await SubscriptionManager.Instance.Unsubscribe(_userBalanceSubscriptionId);
            _userBalanceSubscriptionId = null;
        }
        
        // Reset cache when setting up new subscriptions
        _playerBetCache = null;
        _userBalanceCache = null;

        // Setup both accounts and store subscription IDs
        _playerBetSubscriptionId = await SetupAccountSubscription(
            "PlayerBet",
            CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey),
            OnPlayerBetUpdate,
            data => PlayerBet.Deserialize(data)
        );

        _userBalanceSubscriptionId = await SetupAccountSubscription(
            "UserBalance",
            TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey),
            OnUserBalanceUpdate,
            data => UserBalance.Deserialize(data)
        );

        // Let UpdateUserBetText determine the correct initial UI state
        UpdateUserBetText(_playerBetCache, _gameCache);
    }

    private async Task<string> SetupAccountSubscription<T>(
        string accountName,
        PublicKey accountPk,
        Action<T> callback,
        Func<byte[], T> deserializer)
    {
        // Try loading account using ER
        var initialData = await SubscriptionManager.Instance.LoadAccountData<T>(
            accountPk, deserializer, forceDelegated: true
        );

        if (initialData != null)
        {
            // Account is delegated and ready
            Debug.Log($"{accountName} loaded via ER - setting up subscription");
            return await SubscriptionManager.Instance.SubscribeAndLoad<T>(
                accountPk, callback, deserializer, forceDelegated: true
            );
        }

        // Check if delegated but not ready
        bool isDelegated = await solanaManager.CheckIfDelegated(accountPk);
        
        if (isDelegated)
        {
            // Delegated but not ready - load via normal RPC and subscribe via ER
            Debug.Log($"{accountName} delegated but not ready - loading via RPC and subscribing via ER");
            
            var normalData = await SubscriptionManager.Instance.LoadAccountData<T>(
                accountPk, deserializer, forceDelegated: false
            );
            
            if (normalData != null)
            {
                callback(normalData);
            }
            
            return await SubscriptionManager.Instance.Subscribe<T>(
                accountPk, callback, deserializer, forceDelegated: true
            );
        }
        else
        {
            // Not delegated - check if exists on base layer
            var baseLayerData = await SubscriptionManager.Instance.LoadAccountData<T>(
                accountPk, deserializer, forceDelegated: false
            );
            
            Debug.Log($"{accountName} state: delegated={isDelegated}, initialized={baseLayerData != null}");
            return null;
        }
    }

    private void OnPlayerBetUpdate(PlayerBet newData)
    {
        
        // Detect new bet placement (new game number means new bet)
        // Skip announcement if _playerBetCache is null (initial load)
        bool isNewBet = _playerBetCache != null && 
                       newData.Amount > 0 && 
                       newData.GameNo > _playerBetCache.GameNo;
        
        if (isNewBet)
        {
            // Announce bet to chat
            NetworkedPlayer localPlayer = playerManager?.GetLocalPlayer();
            if (localPlayer != null)
            {
                ulong displayAmount = ConvertToDisplayAmount(newData.Amount);
                string username = localPlayer.playerUsername;
                string betMessage = $"{username} bet: {displayAmount}";
                
                // Display locally in bold
                ChatUI.Instance?.DisplayBetAnnouncement(betMessage, bold: true);
                
                // Broadcast to others (they'll see it non-bold)
                NetworkedChat networkedChat = localPlayer.GetComponent<NetworkedChat>();
                networkedChat?.BroadcastBet(displayAmount);
            }
        }
        
        // Detect claim/win (claimed changed from false to true)
        bool isWin = _playerBetCache != null && 
                    !_playerBetCache.Claimed && 
                    newData.Claimed && 
                    newData.Amount > 0;
        
        if (isWin)
        {
            // Announce win to chat
            NetworkedPlayer localPlayer = playerManager?.GetLocalPlayer();
            if (localPlayer != null)
            {
                ulong displayAmount = ConvertToDisplayAmount(newData.Amount);
                string username = localPlayer.playerUsername;
                string winMessage = $"{username} won: {displayAmount}";
                
                // Display locally in bold
                ChatUI.Instance?.DisplayWinAnnouncement(winMessage, bold: true);
                
                // Broadcast to others (they'll see it non-bold)
                NetworkedChat networkedChat = localPlayer.GetComponent<NetworkedChat>();
                networkedChat?.BroadcastWin(displayAmount);
            }
        }
        
        // Update cache
        _playerBetCache = newData;
        
        UpdateUserBetText(newData, _gameCache);
        Debug.Log($"Game: {newData.Game}, Amount: {newData.Amount}, Player: {newData.Player}");
    }

    private void UpdateUserBetText(PlayerBet playerBet, Game game)
    {
        // Don't show any betting UI if wallet isn't connected
        if (Web3.Account == null)
        {
            if (playerTextTMP != null)
                playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeOut(beforeBettingGroup);
            return;
        }
        
        // If data isn't ready yet, show the before betting UI as default
        if (playerBet == null || game == null)
        {
            if (playerTextTMP != null)
                playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeIn(beforeBettingGroup);
            return;
        }
        
        if (playerTextTMP == null || afterBettingText == null)
            return;
            
        if (playerBet.Amount == 0 || playerBet.GameNo < game.GameNo || playerBet.Claimed) { //means player hasnt bet.
            playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeIn(beforeBettingGroup);
            return;
        }
        
        if (playerBet.GameNo > game.GameNo) {
            playerTextTMP.enabled = true;
            UIFader.FadeOut(beforeBettingGroup);
            UIFader.FadeIn(afterBettingGroup);

            ulong lastAmount = TextAnimationManager.Instance.GetLastRenderedAmount(playerTextTMP);
            ulong displayAmount = ConvertToDisplayAmount(playerBet.Amount);
            ulong lastDisplayAmount = ConvertToDisplayAmount(lastAmount);
            
            if (lastDisplayAmount != displayAmount)
            {
                TextAnimationManager.Instance.AnimateNumber(
                    playerTextTMP,
                    lastDisplayAmount,
                    displayAmount,
                    prefix: "Bet: "
                );
            }

            playerTextTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            afterBettingText.text = $"Your bet: {displayAmount} (next round)";
            UIFader.FadeOut(claimButton.gameObject);
            if (game.State == 0)
            {
                // Check if start is available or needs countdown
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (game.NextActionTime > currentTime)
                {
                    // Start countdown
                    if (_countdownCoroutine != null)
                        StopCoroutine(_countdownCoroutine);
                    _countdownCoroutine = StartCoroutine(CountdownToStartAvailable(game.NextActionTime));
                }
                else
                {
                    // Start is available
                    if (_countdownCoroutine != null)
                    {
                        StopCoroutine(_countdownCoroutine);
                        _countdownCoroutine = null;
                    }
                    if (countdownText != null)
                        countdownText.gameObject.SetActive(false);
                    UIFader.FadeIn(startButton.gameObject);
                }
            }
            else
            {
                if (_countdownCoroutine != null)
                {
                    StopCoroutine(_countdownCoroutine);
                    _countdownCoroutine = null;
                }
                if (countdownText != null)
                    countdownText.gameObject.SetActive(false);
                UIFader.FadeOut(startButton.gameObject);
            }
        } 
        else if (playerBet.GameNo == game.GameNo && game.State == 1) {
            playerTextTMP.enabled = true;
            UIFader.FadeOut(beforeBettingGroup);
            UIFader.FadeIn(afterBettingGroup);
            
            ulong currentValueRaw = (ulong)(playerBet.Amount * Math.Pow(1.11, game.Tick));
            ulong currentValue = ConvertToDisplayAmount(currentValueRaw);
            playerTextTMP.text = $"Bet: {currentValue}";
            playerTextTMP.color = new Color(1f, 1f, 1f, 1f);
            
            afterBettingText.text = $"Your bet: {currentValue}";
            UIFader.FadeIn(claimButton.gameObject);
            UIFader.FadeOut(startButton.gameObject);
            
            if (_countdownCoroutine != null)
            {
                StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = null;
            }
            if (countdownText != null)
                countdownText.gameObject.SetActive(false);
        }
        else if (playerBet.GameNo == game.GameNo && game.State != 1) {
            // Game has crashed or not started yet, show betting UI even though bet hasn't been claimed
            playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeIn(beforeBettingGroup);
        }
    }


    private void OnUserBalanceUpdate(UserBalance newData)
    {
        _userBalanceCache = newData;
        
        if (Web3.Account == null)
        {
            if (userBalanceAccountTMP != null)
                userBalanceAccountTMP.gameObject.SetActive(false);
            return;
        }
        
        if (userBalanceAccountTMP != null && TextAnimationManager.Instance != null)
        {
            ulong lastBalance = TextAnimationManager.Instance.GetLastRenderedAmount(userBalanceAccountTMP);
            ulong displayBalance = ConvertToDisplayAmount(newData.Balance);
            ulong lastDisplayBalance = ConvertToDisplayAmount(lastBalance);
            
            if (lastDisplayBalance != displayBalance)
            {
                TextAnimationManager.Instance.AnimateNumber(
                    userBalanceAccountTMP,
                    lastDisplayBalance,
                    displayBalance,
                    prefix: "Balance: "
                );
            }
            else
            {
                userBalanceAccountTMP.text = $"Balance: {displayBalance}";
            }
        }
        
        UpdateMaxBet();
        UpdateWithdrawButtonVisibility();
        
        Debug.Log($"Balance: {newData.Balance}, User: {newData.User}");
    }

    private void OnTreasuryUpdate(TreasuryAccount newData)
    {
        _treasuryCache = newData;
        
        UpdateMaxBet();
        
        Debug.Log($"TREASURY UPDATE:\n" +
                  $"  Bump: {newData.Bump}\n" +
                  $"  FeePercentage: {newData.FeePercentage}\n" +
                  $"  BufferAmount: {newData.BufferAmount}\n" +
                  $"  UserOwnedAmount: {newData.UserOwnedAmount}\n" +
                  $"  TreasuryTokenAccountBump: {newData.TreasuryTokenAccountBump}\n" +
                  $"  TreasuryStakeTokenAccountBump: {newData.TreasuryStakeTokenAccountBump}\n" +
                  $"  StakingTokenMintBump: {newData.StakingTokenMintBump}\n" +
                  $"  WithdrawalTime: {newData.WithdrawalTime}");
    }

    // Calculate available house funds using the same logic as the Rust contract
    // Available house funds = total tokens - buffer amount - user owned amount
    private ulong CalculateAvailableHouseFunds(ulong totalTokens)
    {
        if (_treasuryCache == null)
        {
            Debug.LogWarning("Treasury cache is null, cannot calculate available house funds");
            return 0;
        }

        // Calculate available house funds: total - buffer - user_owned
        ulong availableHouseFunds = totalTokens;
        
        if (availableHouseFunds >= _treasuryCache.BufferAmount)
        {
            availableHouseFunds -= _treasuryCache.BufferAmount;
        }
        else
        {
            availableHouseFunds = 0;
        }
        
        if (availableHouseFunds >= _treasuryCache.UserOwnedAmount)
        {
            availableHouseFunds -= _treasuryCache.UserOwnedAmount;
        }
        else
        {
            availableHouseFunds = 0;
        }
        
        return availableHouseFunds;
    }

    
    private void OnGameUpdate(Game newData)
    {
        // Detect game start (state changes from 0 to 1)
        bool isGameStart = _gameCache != null && _gameCache.State == 0 && newData.State == 1;
        
        if (isGameStart)
        {
            // Check if player has a bet in this game
            bool playerHasBet = _playerBetCache != null && 
                               _playerBetCache.GameNo == newData.GameNo && 
                               _playerBetCache.Amount > 0;
            
            string startMessage = $"Game #{newData.GameNo} started";
            
            ChatUI.Instance?.DisplayBetAnnouncement(startMessage, bold: playerHasBet);
        }
        
        // Detect tick increase (game is running and tick increased)
        bool isTick = _gameCache != null && 
                     newData.State == 1 && 
                     newData.Tick > _gameCache.Tick;
        
        if (isTick)
        {
            // Calculate multiplier from new tick
            double multiplier = System.Math.Pow(1.11, newData.Tick);
            string multiplierText = $"{multiplier:0.00}x";
            
            // Check if player has a bet in this game
            bool playerHasBet = _playerBetCache != null && 
                               _playerBetCache.GameNo == newData.GameNo && 
                               _playerBetCache.Amount > 0 &&
                               !_playerBetCache.Claimed;
            
            string tickMessage = $"Balloon at {multiplierText}";
            
            if (playerHasBet)
            {
                ulong betAmount = ConvertToDisplayAmount(_playerBetCache.Amount);
                tickMessage += $", your bet: {betAmount}";
            }
            
            ChatUI.Instance?.DisplayTickAnnouncement(tickMessage, bold: playerHasBet);
        }
        
        // Detect crash (state changes from 1 to 0)
        bool isCrash = _gameCache != null && _gameCache.State == 1 && newData.State == 0;
        
        if (isCrash)
        {
            // Calculate multiplier from crash tick
            double multiplier = System.Math.Pow(1.11, newData.CrashTick);
            string multiplierText = $"{multiplier:0.00}x";
            
            // Check if player had a bet in this crashed game
            bool playerHadBet = _playerBetCache != null && 
                               _playerBetCache.GameNo == newData.GameNo && 
                               _playerBetCache.Amount > 0 &&
                               !_playerBetCache.Claimed;
            
            string crashMessage = $"Balloon popped at {multiplierText}";
            
            if (playerHadBet)
            {
                ulong lossAmount = ConvertToDisplayAmount(_playerBetCache.Amount);
                crashMessage += $", you lost: {lossAmount}";
            }
            
            ChatUI.Instance?.DisplayCrashAnnouncement(crashMessage, bold: playerHadBet);
        }
        
        _gameCache = newData;
        if (_balloonSimulator != null)
            _balloonSimulator.UpdateBalloon(newData);
        UpdateUserBetText(_playerBetCache, newData);
        Debug.Log($"State: {newData.State}, Tick: {newData.Tick}, CrashTick: {newData.CrashTick}, GameNo: {newData.GameNo}");
    }

    private async void OnBlackjackGameUpdate(BlackJackGame newData)
    {
        _blackjackGameCache = newData;
        
        // Check for new hands that belong to this blackjack game
        if (_subscribedBlackjackPk != null)
        {
            await SetupBlackjackHandsSubscription(_subscribedBlackjackPk);
        }
        
        Debug.Log($"BLACKJACK GAME UPDATE:\n" +
                  $"  GameId: {newData.GameId}\n" +
                  $"  DealerCardCount: {newData.DealerCardCount}\n" +
                  $"  NextActionTime: {newData.NextActionTime}\n" +
                  $"  GameNo: {newData.GameNo}\n" +
                  $"  ActiveHands: {newData.ActiveHands}");
    }

    private async void OnBlackjackHandUpdate(PublicKey handPk, BlackJackHand newData)
    {
        // Check if this hand now belongs to a different blackjack game
        if (_subscribedBlackjackPk != null && !newData.Blackjack.Equals(_subscribedBlackjackPk))
        {
            Debug.Log($"Hand {handPk} blackjack changed from {_subscribedBlackjackPk} to {newData.Blackjack} - unsubscribing");
            
            if (_blackjackHandSubscriptionIds.TryGetValue(handPk, out string subId))
            {
                await SubscriptionManager.Instance.Unsubscribe(subId);
                _blackjackHandSubscriptionIds.Remove(handPk);
            }
            _blackjackHandsCache.Remove(handPk);
            return;
        }
        
        _blackjackHandsCache[handPk] = newData;
        
        Debug.Log($"BLACKJACK HAND UPDATE ({handPk}):\n" +
                  $"  Player: {newData.Player}\n" +
                  $"  HandId: {newData.HandId}\n" +
                  $"  State: {newData.State}\n" +
                  $"  CurrentBet: {newData.CurrentBet}\n" +
                  $"  CardCount: {newData.CardCount}\n" +
                  $"  GameNo: {newData.GameNo}");
    }
}