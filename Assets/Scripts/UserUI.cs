using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Crash.Accounts;
using Treasury.Accounts;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using TMPro;
using Coherence.Toolkit;

public class UserUI : MonoBehaviour
{
    [SerializeField] private TreasuryTransactionBuilder treasuryBuilder;
    [SerializeField] private CrashTransactionBuilder crashBuilder;
    [SerializeField] private SolanaManager solanaManager;
    [SerializeField] private CoherenceBridge coherenceBridge;
    [SerializeField] private DepositModal depositModal;

    private Game _gameCache;
    private PlayerBet _playerBetCache;
    private UserBalance _userBalanceCache;

    // Subscription tracking
    private string _playerBetSubscriptionId;
    private string _userBalanceSubscriptionId;

    [SerializeField] private GameObject beforeBettingGroup;
    [SerializeField] private GameObject afterBettingGroup;
    [SerializeField] private TextMeshProUGUI afterBettingText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button tickButton;
    [SerializeField] private Button setupGameButton;

    [SerializeField] private BalloonSimulator _balloonSimulator;


    // Public fields for input values (editable in Inspector)
    [Header("Input Values")]
    public ulong depositAmount = 1000;
    public ulong withdrawAmount = 1000;
    public ulong betAmount = 0;
    public byte clientSeed = 42;

    // Account Data Display
    [Header("Account Data Display")]
    [SerializeField] private TextMeshProUGUI gameAccountTMP;
    [SerializeField] private TextMeshProUGUI playerBetAccountTMP;
    [SerializeField] private TextMeshProUGUI userBalanceAccountTMP;

    [SerializeField] private TextMeshProUGUI playerTextTMP;


    private void Start()
    {
        SetupGameSubscription();
        SetupDepositModalListeners();
        SetupTickButton();
        SetupSetupGameButton();
        UIFader.HideImmediate(beforeBettingGroup);
        UIFader.HideImmediate(afterBettingGroup);
    }

    private void SetupTickButton()
    {
        if (tickButton != null)
        {
            tickButton.onClick.AddListener(() => Tick());
        }
    }

    private void SetupSetupGameButton()
    {
        if (setupGameButton != null)
        {
            setupGameButton.onClick.AddListener(() => SetupGame());
        }
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
    
    private void SetupDepositModalListeners()
    {
        if (depositModal != null)
        {
            depositModal.onDeposit.AddListener(DepositAsync);
            depositModal.onWithdraw.AddListener(Withdraw);
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
        if (depositModal != null)
            depositModal.ShowToggleButton();
        
        await SetupUserAccountSubscriptions();
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
        
        // Hide all UI groups when wallet disconnects
        UIFader.FadeOut(beforeBettingGroup);
        UIFader.FadeOut(afterBettingGroup);
        
        if (depositModal != null)
            depositModal.HideToggleButton();
        
        Debug.Log("Wallet disconnected - UI reset");
    }


    //ENSURE user_balance delegated
    public async void DepositAsync(ulong amount) //user
    {
        var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);
        bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);
        
        // 1. Undelegate on ER if currently delegated
        if (userBalanceIsDelegated)
        {
            var undelegateIx = treasuryBuilder.UndelegateUserBalance();
            await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, undelegateIx);
        }
        
        // 2. Deposit + Re-delegate on base layer (together)
        var userDepositIx = treasuryBuilder.UserDeposit(amount);
        var delegateIx = treasuryBuilder.DelegateUserBalance();
        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, userDepositIx, delegateIx);
    }

    //ENSURE user_balance delegated
    public async void Withdraw(ulong amount) //user
    {
        // 1. Undelegate on ER
        var undelegateIx = treasuryBuilder.UndelegateUserBalance();
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, undelegateIx);
        
        // 2. Withdraw + Re-delegate on base layer (together)
        var userWithdrawIx = treasuryBuilder.UserWithdraw(amount);
        var delegateIx = treasuryBuilder.DelegateUserBalance();
        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, userWithdrawIx, delegateIx);
    }

    //ENSURE game delegated
    public async void SetupRandomness(byte clientSeed) //user
    {
        var requestRandomnessIx = crashBuilder.RequestRandomness(clientSeed);
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, requestRandomnessIx);
    }

    public async void StartGame() //user
    {
        var startGameIx = crashBuilder.StartGame();
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, startGameIx);
    }

    //ENSURE player_bet + ephemeral_balance delegated + user setup + game delegated
    public async void PlaceBet() //user
    {
        var instructions = new List<TransactionInstruction>();

        var playerBetPk = CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey);
        var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);

        // Step 1: Initialize any uninitialized accounts
        var playerBetData = await SubscriptionManager.Instance.LoadAccountData<PlayerBet>(
            playerBetPk, data => PlayerBet.Deserialize(data), forceDelegated: false
        );
        
        if (playerBetData == null)
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

        if (instructions.Count > 0)
        {
            Debug.Log($"Sending initialization transaction with {instructions.Count} instruction(s)");
            await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, instructions.ToArray());
            instructions.Clear();
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
        
        if (depositAmount > 0)
        {
            bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);
            
            if (userBalanceIsDelegated)
            {
                var undelegateIx = treasuryBuilder.UndelegateUserBalance();
                await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, undelegateIx);
            }
            
            var userDepositIx = treasuryBuilder.UserDeposit(depositAmount);
            var delegateIx = treasuryBuilder.DelegateUserBalance();
            instructions.Add(userDepositIx);
            instructions.Add(delegateIx);
        }

        // Step 4: Delegate PlayerBet if it wasn't
        bool playerBetIsDelegated = await solanaManager.CheckIfDelegated(playerBetPk);
        
        if (!playerBetIsDelegated)
        {
            Debug.Log("Delegating PlayerBet");
            instructions.Add(crashBuilder.DelegatePlayerBet());
        }

        if (instructions.Count > 0)
        {
            Debug.Log($"Sending deposit and delegation transaction with {instructions.Count} instruction(s)");
            await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, instructions.ToArray());
        }

        // Step 5: Send place_bet transaction on ER layer
        Debug.Log($"Placing bet of {betAmount}");
        var placeBetIx = crashBuilder.PlaceBet(betAmount);
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, placeBetIx);
    }

    //ENSURE player_bet + ephemeral_balance delegated + user setup + game delegated
    public async void ClaimBet() //user
    {
        var claimBetIx = crashBuilder.ClaimBet();
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, claimBetIx);
    }

    //ENSURE game delegated
    public async void Tick() //admin? 
    {
        var tickIx = crashBuilder.Tick();
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, tickIx);
    }

    public async void SetupGame() //admin? 
    {
        var instructions = new List<TransactionInstruction>();
        instructions.Add(crashBuilder.InitializeGame());
        instructions.Add(crashBuilder.InitializeAuthority());
        instructions.Add(treasuryBuilder.InitializeTreasury());
        instructions.Add(crashBuilder.DelegateGame());
        instructions.Add(crashBuilder.DelegateAuthority());

        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, instructions.ToArray());
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

    private async Task SetupUserAccountSubscriptions()
    {
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
        _playerBetCache = newData;
        if (playerBetAccountTMP != null)
        {
            playerBetAccountTMP.text = $"Game: {newData.Game}, Amount: {newData.Amount}, Player: {newData.Player}";
        }
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
            if (lastAmount != playerBet.Amount)
            {
                TextAnimationManager.Instance.AnimateNumber(
                    playerTextTMP,
                    lastAmount,
                    playerBet.Amount,
                    prefix: "Bet: "
                );
            }

            playerTextTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            
            afterBettingText.text = $"Your bet: {playerBet.Amount} (next round)";
            UIFader.FadeOut(claimButton.gameObject);
            if (game.State == 0)
                UIFader.FadeIn(startButton.gameObject);
            else
                UIFader.FadeOut(startButton.gameObject);
        } 
        else if (playerBet.GameNo == game.GameNo) {
            playerTextTMP.enabled = true;
            UIFader.FadeOut(beforeBettingGroup);
            UIFader.FadeIn(afterBettingGroup);
            
            ulong currentValue = (ulong)(playerBet.Amount * Math.Pow(1.11, game.Tick));
            playerTextTMP.text = $"Bet: {currentValue}";
            playerTextTMP.color = new Color(1f, 1f, 1f, 1f);
            
            afterBettingText.text = $"Your bet: {currentValue}";
            UIFader.FadeIn(claimButton.gameObject);
            UIFader.FadeOut(startButton.gameObject);
        }
    }


    private void OnUserBalanceUpdate(UserBalance newData)
    {
        _userBalanceCache = newData;
        if (userBalanceAccountTMP != null && TextAnimationManager.Instance != null)
        {
            ulong lastBalance = TextAnimationManager.Instance.GetLastRenderedAmount(userBalanceAccountTMP);
            if (lastBalance != newData.Balance)
            {
                TextAnimationManager.Instance.AnimateNumber(
                    userBalanceAccountTMP,
                    lastBalance,
                    newData.Balance,
                    prefix: "Balance: "
                );
            }
            else
            {
                userBalanceAccountTMP.text = $"Balance: {newData.Balance}";
            }
        }
        
        if (depositModal != null && depositModal.ephemeralBalance != null)
        {
            depositModal.ephemeralBalance.text = newData.Balance.ToString();
        }
        
        Debug.Log($"Balance: {newData.Balance}, User: {newData.User}");
    }

    
    private void OnGameUpdate(Game newData)
    {
        _gameCache = newData;
        if (gameAccountTMP != null)
        {
            gameAccountTMP.text = $"GAME\nState: {newData.State}, Tick: {newData.Tick}, GameNo: {newData.GameNo}";
        }
        if (_balloonSimulator != null)
            _balloonSimulator.UpdateBalloon(newData);
        UpdateUserBetText(_playerBetCache, newData);
        Debug.Log($"State: {newData.State}, Tick: {newData.Tick}, CrashTick: {newData.CrashTick}, GameNo: {newData.GameNo}");
    }
}