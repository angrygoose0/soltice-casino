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
using TreasuryAccount = Treasury.Accounts.Treasury;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using TMPro;
using System.Linq;
using Coherence.Toolkit;

public class UserUI : MonoBehaviour
{
    [SerializeField] private OnChainAccountManager accountManager;
    [SerializeField] private TreasuryTransactionBuilder treasuryBuilder;
    [SerializeField] private CrashTransactionBuilder crashBuilder;
    [SerializeField] private BlackjackTransactionBuilder blackjackBuilder;
    [SerializeField] private SolanaManager solanaManager;
    [SerializeField] private CoherenceBridge coherenceBridge;
    [SerializeField] private FeedbackManager feedbackManager;
    [SerializeField] private Button withdrawButton;
    [SerializeField] private PlayerManager playerManager; 

    [SerializeField] private GameObject beforeBettingGroup;
    [SerializeField] private GameObject afterBettingGroup;
    [SerializeField] private TextMeshProUGUI afterBettingText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TextMeshProUGUI countdownText;

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

    [Header("Account Data Display")]
    [SerializeField] private TextMeshProUGUI userBalanceAccountTMP;
    [SerializeField] private TextMeshProUGUI playerTextTMP;
    [SerializeField] private TextMeshProUGUI maxBetTextTMP;

    public ulong maxBetAmount { get; private set; } = 0;
    private string maxBetReason = "";

    private void Start()
    {
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

    private void OnEnable()
    {
        Web3.OnLogin += OnWalletConnected;
        Web3.OnLogout += OnWalletDisconnected;
        
        if (accountManager != null)
        {
            accountManager.OnGameUpdated += HandleGameUpdate;
            accountManager.OnPlayerBetUpdated += HandlePlayerBetUpdate;
            accountManager.OnUserBalanceUpdated += HandleUserBalanceUpdate;
            accountManager.OnTreasuryUpdated += HandleTreasuryUpdate;
        }
    }

    private void OnDisable()
    {
        Web3.OnLogin -= OnWalletConnected;
        Web3.OnLogout -= OnWalletDisconnected;
        
        if (accountManager != null)
        {
            accountManager.OnGameUpdated -= HandleGameUpdate;
            accountManager.OnPlayerBetUpdated -= HandlePlayerBetUpdate;
            accountManager.OnUserBalanceUpdated -= HandleUserBalanceUpdate;
            accountManager.OnTreasuryUpdated -= HandleTreasuryUpdate;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && 
            (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame))
        {
            if (startButton != null && startButton.gameObject.activeInHierarchy && startButton.interactable)
                StartGame();
        }
    }

    public void SetPlayerBetText(TextMeshProUGUI betText) => playerTextTMP = betText;

    private ulong ConvertToDisplayAmount(ulong rawAmount) =>
        (ulong)(rawAmount / Math.Pow(10, solanaManager.TokenDecimals));

    #region Wallet Events
    private void OnWalletConnected(Account account)
    {
        if (userBalanceAccountTMP != null)
            userBalanceAccountTMP.gameObject.SetActive(true);
        
        UpdateMaxBet();
        UpdateWithdrawButtonVisibility();
        
        if (accountManager.enableCrash)
            UpdateUserBetText(accountManager.PlayerBetCache, accountManager.GameCache);
    }

    private void OnWalletDisconnected()
    {
        maxBetAmount = 0;
        betAmount = 0;
        
        UIFader.FadeOut(beforeBettingGroup);
        UIFader.FadeOut(afterBettingGroup);
        
        if (withdrawButton != null)
            UIFader.FadeOut(withdrawButton.gameObject);
        
        if (maxBetTextTMP != null)
            UIFader.FadeOut(maxBetTextTMP.gameObject);
        
        if (userBalanceAccountTMP != null)
            userBalanceAccountTMP.gameObject.SetActive(false);
        
        GameLogger.Log("Wallet disconnected - UI reset");
    }
    #endregion

    #region Account Event Handlers
    private void HandleGameUpdate(Game oldData, Game newData)
    {
        if (!accountManager.enableCrash) return;
        DetectAndAnnounceCrashEvents(oldData, newData);
        UpdateUserBetText(accountManager.PlayerBetCache, newData);
    }

    private void HandlePlayerBetUpdate(PlayerBet oldData, PlayerBet newData)
    {
        if (!accountManager.enableCrash) return;
        DetectAndAnnounceBetEvents(oldData, newData);
        UpdateUserBetText(newData, accountManager.GameCache);
    }

    private void HandleUserBalanceUpdate(UserBalance newData)
    {
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
    }

    private void HandleTreasuryUpdate(TreasuryAccount newData) => UpdateMaxBet();
    #endregion

    #region Chat Announcements
    private void DetectAndAnnounceCrashEvents(Game oldData, Game newData)
    {
        var playerBet = accountManager.PlayerBetCache;
        
        // Game start
        if (oldData != null && oldData.State == 0 && newData.State == 1)
        {
            bool playerHasBet = playerBet != null && playerBet.GameNo == newData.GameNo && playerBet.Amount > 0;
            ChatUI.Instance?.DisplayBetAnnouncement($"Game #{newData.GameNo} started", bold: playerHasBet);
        }
        
        // Tick increase
        if (oldData != null && newData.State == 1 && newData.Tick > oldData.Tick)
        {
            double multiplier = Math.Pow(1.11, newData.Tick);
            bool playerHasBet = playerBet != null && playerBet.GameNo == newData.GameNo && playerBet.Amount > 0 && !playerBet.Claimed;
            string tickMessage = $"Balloon at {multiplier:0.00}x";
            if (playerHasBet)
                tickMessage += $", your balloon at: {ConvertToDisplayAmount((ulong)(playerBet.Amount * multiplier))}";
            ChatUI.Instance?.DisplayTickAnnouncement(tickMessage, bold: playerHasBet);
        }
        
        // Crash
        if (oldData != null && oldData.State == 1 && newData.State == 0)
        {
            double multiplier = Math.Pow(1.11, oldData.Tick + 1);
            bool playerHadBet = playerBet != null && playerBet.GameNo == newData.GameNo && playerBet.Amount > 0 && !playerBet.Claimed;
            string crashMessage = $"Balloon popped at {multiplier:0.00}x";
            if (playerHadBet)
                crashMessage += $", you lost: {ConvertToDisplayAmount(playerBet.Amount)}";
            ChatUI.Instance?.DisplayCrashAnnouncement(crashMessage, bold: playerHadBet);
        }
    }

    private void DetectAndAnnounceBetEvents(PlayerBet oldData, PlayerBet newData)
    {
        // New bet placed
        if (oldData != null && newData.Amount > 0 && newData.GameNo > oldData.GameNo)
        {
            NetworkedPlayer localPlayer = playerManager?.GetLocalPlayer();
            if (localPlayer != null)
            {
                ulong displayAmount = ConvertToDisplayAmount(newData.Amount);
                ChatUI.Instance?.DisplayBetAnnouncement($"{localPlayer.playerUsername} bet: {displayAmount}", bold: true);
                localPlayer.GetComponent<NetworkedChat>()?.BroadcastBet(displayAmount);
            }
        }
        
        // Win claimed
        if (oldData != null && !oldData.Claimed && newData.Claimed && newData.Amount > 0)
        {
            NetworkedPlayer localPlayer = playerManager?.GetLocalPlayer();
            if (localPlayer != null)
            {
                var game = accountManager.GameCache;
                double multiplier = game != null ? Math.Pow(1.11, game.Tick) : 1.0;
                ulong winAmountRaw = (ulong)(newData.Amount * multiplier);
                ulong displayAmount = ConvertToDisplayAmount(winAmountRaw);
                ChatUI.Instance?.DisplayWinAnnouncement($"{localPlayer.playerUsername} won: {displayAmount}", bold: true);
                localPlayer.GetComponent<NetworkedChat>()?.BroadcastWin(displayAmount);
            }
        }
    }
    #endregion

    #region Max Bet Calculation
    private async void UpdateMaxBet()
    {
        if (Web3.Account == null || solanaManager == null)
        {
            maxBetAmount = 0;
            return;
        }

        try
        {
            double walletBalanceDouble = await solanaManager.GetSPLTokenBalance();
            ulong walletBalance = (ulong)walletBalanceDouble;
            ulong ephemeralBalance = accountManager.UserBalanceCache?.Balance ?? 0;
            ulong userLimit = walletBalance + ephemeralBalance;

            var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();
            ulong treasuryTokenBalance = await solanaManager.GetTokenAccountBalance(treasuryTokenAccount);
            
            ulong houseLimit = 0;
            if (accountManager.TreasuryCache != null)
            {
                ulong availableHouseFunds = treasuryTokenBalance;
                
                if (availableHouseFunds >= accountManager.TreasuryCache.BufferAmount)
                    availableHouseFunds -= accountManager.TreasuryCache.BufferAmount;
                else
                    availableHouseFunds = 0;
                
                ulong userOwned = accountManager.TreasuryCache.UserOwnedAmount > 0 ? (ulong)accountManager.TreasuryCache.UserOwnedAmount : 0;
                if (availableHouseFunds >= userOwned)
                    availableHouseFunds -= userOwned;
                else
                    availableHouseFunds = 0;
                
                houseLimit = availableHouseFunds / 2;
            }

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
            
            if (betAmount > maxBetAmount)
                betAmount = maxBetAmount;
            
            UpdateMaxBetUI();
        }
        catch (Exception ex)
        {
            GameLogger.LogWarning($"Failed to calculate max bet: {ex.Message}");
            maxBetAmount = 0;
            betAmount = 0;
            maxBetReason = "";
            UpdateMaxBetUI();
        }
    }

    private void UpdateMaxBetUI()
    {
        if (maxBetTextTMP == null) return;
        
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
        if (withdrawButton == null) return;
        
        ulong ephemeralBalance = accountManager.UserBalanceCache?.Balance ?? 0;
        
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
    #endregion

    #region Betting UI State
    private void UpdateUserBetText(PlayerBet playerBet, Game game)
    {
        if (Web3.Account == null)
        {
            if (playerTextTMP != null) playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeOut(beforeBettingGroup);
            return;
        }
        
        if (playerBet == null || game == null)
        {
            if (playerTextTMP != null) playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeIn(beforeBettingGroup);
            return;
        }
        
        if (playerTextTMP == null || afterBettingText == null) return;
            
        // No active bet
        if (playerBet.Amount == 0 || playerBet.GameNo < game.GameNo || playerBet.Claimed)
        {
            playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeIn(beforeBettingGroup);
            return;
        }
        
        // Bet for next round
        if (playerBet.GameNo > game.GameNo)
        {
            ShowBetForNextRound(playerBet, game);
        }
        // Bet in current running game
        else if (playerBet.GameNo == game.GameNo && game.State == 1)
        {
            ShowBetInRunningGame(playerBet, game);
        }
        // Game crashed but bet not claimed
        else if (playerBet.GameNo == game.GameNo && game.State != 1)
        {
            playerTextTMP.enabled = false;
            UIFader.FadeOut(afterBettingGroup);
            UIFader.FadeIn(beforeBettingGroup);
        }
    }

    private void ShowBetForNextRound(PlayerBet playerBet, Game game)
    {
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
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (game.NextActionTime > currentTime)
            {
                if (_countdownCoroutine != null) StopCoroutine(_countdownCoroutine);
                _countdownCoroutine = StartCoroutine(CountdownToStartAvailable(game.NextActionTime));
            }
            else
            {
                StopCountdown(true);
            }
        }
        else
        {
            StopCountdown(false);
        }
    }

    private void ShowBetInRunningGame(PlayerBet playerBet, Game game)
    {
        playerTextTMP.enabled = true;
        UIFader.FadeOut(beforeBettingGroup);
        UIFader.FadeIn(afterBettingGroup);
        
        ulong currentValueRaw = (ulong)(playerBet.Amount * Math.Pow(1.11, game.Tick));
        ulong currentValue = ConvertToDisplayAmount(currentValueRaw);
        playerTextTMP.text = $"Bet: {currentValue}";
        playerTextTMP.color = Color.white;
        
        afterBettingText.text = $"Your bet: {currentValue}";
        UIFader.FadeIn(claimButton.gameObject);
        StopCountdown(false);
    }

    private void StopCountdown(bool showStart)
    {
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (showStart) UIFader.FadeIn(startButton.gameObject);
        else UIFader.FadeOut(startButton.gameObject);
    }

    private IEnumerator CountdownToStartAvailable(long nextActionTime)
    {
        if (startButton != null) startButton.gameObject.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(true);
        
        while (true)
        {
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long timeRemaining = nextActionTime - currentTime;
            
            if (timeRemaining <= 0)
            {
                if (countdownText != null) countdownText.gameObject.SetActive(false);
                if (startButton != null) startButton.gameObject.SetActive(true);
                yield break;
            }
            
            if (countdownText != null) countdownText.text = timeRemaining.ToString();
            yield return new WaitForSeconds(1f);
        }
    }
    #endregion

    #region Transactions
    private async Task EnsureDepositAndDelegation(ulong requiredAmount, List<TransactionInstruction> additionalSetup = null)
    {
        var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);
        ulong existingBalance = accountManager.UserBalanceCache?.Balance ?? 0;
        ulong depositAmount = requiredAmount > existingBalance ? requiredAmount - existingBalance : 0;
        
        var setupInstructions = additionalSetup ?? new List<TransactionInstruction>();
        
        if (solanaManager.useEphemeralRollups)
        {
            bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);
            
            if (depositAmount > 0 && userBalanceIsDelegated)
            {
                await solanaManager.SendAndConfirmTransaction(true, 150000u, 200000ul, "undelegating balance...", treasuryBuilder.UndelegateUserBalance());
                userBalanceIsDelegated = false;
            }
            
            if (depositAmount > 0)
                setupInstructions.Insert(0, treasuryBuilder.DepositTokens(depositAmount));
            
            if (!userBalanceIsDelegated)
                setupInstructions.Add(treasuryBuilder.DelegateUserBalance());
            
            if (setupInstructions.Count > 0)
                await solanaManager.SendAndConfirmTransaction(false, 200000u, 200000ul, "setting up...", setupInstructions.ToArray());
            
            if (depositAmount > 0)
                await solanaManager.SendAndConfirmTransaction(true, 100000u, 200000ul, "applying deposit...", treasuryBuilder.ApplyDeposit());
        }
        else
        {
            if (depositAmount > 0)
            {
                setupInstructions.Insert(0, treasuryBuilder.DepositTokens(depositAmount));
                setupInstructions.Insert(1, treasuryBuilder.ApplyDeposit());
            }
            
            if (setupInstructions.Count > 0)
                await solanaManager.SendAndConfirmTransaction(false, 200000u, 200000ul, "setting up...", setupInstructions.ToArray());
        }
    }

    public async void DepositAsync(ulong amount)
    {
        try
        {
            var depositTokensIx = treasuryBuilder.DepositTokens(amount);
            var applyDepositIx = treasuryBuilder.ApplyDeposit();
            
            if (solanaManager.useEphemeralRollups)
            {
                var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);
                bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);
                
                if (userBalanceIsDelegated)
                {
                    var undelegateIx = treasuryBuilder.UndelegateUserBalance();
                    await solanaManager.SendAndConfirmTransaction(true, 150000u, 200000ul, "undelegating balance...", undelegateIx);
                }
                
                var delegateIx = treasuryBuilder.DelegateUserBalance();
                await solanaManager.SendAndConfirmTransaction(false, 150000u, 200000ul, $"depositing {amount}...", depositTokensIx, delegateIx);
                await solanaManager.SendAndConfirmTransaction(true, 100000u, 200000ul, "applying deposit...", applyDepositIx);
            }
            else
            {
                await solanaManager.SendAndConfirmTransaction(false, 150000u, 200000ul, $"depositing {amount}...", depositTokensIx, applyDepositIx);
            }
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Deposit failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void Withdraw(ulong amount)
    {
        try
        {
            var requestWithdrawIx = treasuryBuilder.RequestWithdraw(amount);
            var withdrawTokensIx = treasuryBuilder.WithdrawTokens();
            
            if (solanaManager.useEphemeralRollups)
            {
                var undelegateIx = treasuryBuilder.UndelegateUserBalance();
                await solanaManager.SendAndConfirmTransaction(true, 150000u, 200000ul, $"requesting withdraw {amount}...", requestWithdrawIx, undelegateIx);
                
                var delegateIx = treasuryBuilder.DelegateUserBalance();
                await solanaManager.SendAndConfirmTransaction(false, 150000u, 200000ul, "withdrawing...", withdrawTokensIx, delegateIx);
            }
            else
            {
                await solanaManager.SendAndConfirmTransaction(false, 150000u, 200000ul, $"withdrawing {amount}...", requestWithdrawIx, withdrawTokensIx);
            }
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Withdraw failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void RequestTick(byte clientSeed)
    {
        if (!accountManager.enableCrash) return;
        
        try
        {
            var requestTickIx = crashBuilder.RequestTick(clientSeed);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 100000u, 200000ul, "requesting tick...", requestTickIx);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Request tick failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void StartGame()
    {
        if (!accountManager.enableCrash) return;
        
        try
        {
            var startGameIx = crashBuilder.StartGame();
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 100000u, 200000ul, "starting game...", startGameIx);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Start game failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void PlaceBet()
    {
        if (!accountManager.enableCrash) return;
        
        try
        {
            var instructions = new List<TransactionInstruction>();

            var playerBetPk = CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey);
            var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);

            var playerBetData = await SubscriptionManager.Instance.LoadAccountData<PlayerBet>(
                playerBetPk, data => PlayerBet.Deserialize(data), forceDelegated: false
            );
            
            bool isInitializingPlayerBet = playerBetData == null;
            if (isInitializingPlayerBet)
                instructions.Add(crashBuilder.InitializePlayerBet());
            
            var userBalanceData = await SubscriptionManager.Instance.LoadAccountData<UserBalance>(
                userBalancePk, data => UserBalance.Deserialize(data), forceDelegated: false
            );
            
            bool isInitializingUserBalance = userBalanceData == null;
            if (isInitializingUserBalance)
                instructions.Add(treasuryBuilder.InitializeBalance());

            ulong depositAmount = betAmount;
            if (!isInitializingUserBalance && accountManager.UserBalanceCache != null && accountManager.UserBalanceCache.Balance > 0)
            {
                if (accountManager.UserBalanceCache.Balance >= betAmount)
                    depositAmount = 0;
                else
                    depositAmount = betAmount - accountManager.UserBalanceCache.Balance;
            }
            
            if (solanaManager.useEphemeralRollups)
            {
                bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);

                if (depositAmount > 0)
                {
                    if (userBalanceIsDelegated)
                    {
                        var undelegateIx = treasuryBuilder.UndelegateUserBalance();
                        await solanaManager.SendAndConfirmTransaction(true, 150000u, 200000ul, "undelegating balance...", undelegateIx);
                        userBalanceIsDelegated = false;
                    }
                    instructions.Add(treasuryBuilder.DepositTokens(depositAmount));
                }

                if (!userBalanceIsDelegated)
                    instructions.Add(treasuryBuilder.DelegateUserBalance());

                bool playerBetIsDelegated = await solanaManager.CheckIfDelegated(playerBetPk);
                if (!playerBetIsDelegated)
                    instructions.Add(crashBuilder.DelegatePlayerBet());

                bool needsSubscriptionSetup = isInitializingPlayerBet || isInitializingUserBalance;

                if (instructions.Count > 0)
                {
                    await solanaManager.SendAndConfirmTransaction(false, 200000u, 200000ul, "initializing and depositing...", instructions.ToArray());
                    
                    if (needsSubscriptionSetup)
                        await accountManager.SetupUserAccountSubscriptions();
                }

                if (depositAmount > 0)
                {
                    var applyDepositIx = treasuryBuilder.ApplyDeposit();
                    await solanaManager.SendAndConfirmTransaction(true, 100000u, 200000ul, "applying deposit...", applyDepositIx);
                }

                var placeBetIx = crashBuilder.PlaceBet(betAmount);
                await solanaManager.SendAndConfirmTransaction(true, 100000u, 200000ul, $"placing bet {ConvertToDisplayAmount(betAmount)}...", placeBetIx);
            }
            else
            {
                if (depositAmount > 0)
                {
                    instructions.Add(treasuryBuilder.DepositTokens(depositAmount));
                    instructions.Add(treasuryBuilder.ApplyDeposit());
                }

                bool needsSubscriptionSetup = isInitializingPlayerBet || isInitializingUserBalance;

                if (instructions.Count > 0)
                {
                    await solanaManager.SendAndConfirmTransaction(false, 200000u, 200000ul, "initializing and depositing...", instructions.ToArray());
                    
                    if (needsSubscriptionSetup)
                        await accountManager.SetupUserAccountSubscriptions();
                }

                var placeBetIx = crashBuilder.PlaceBet(betAmount);
                await solanaManager.SendAndConfirmTransaction(false, 100000u, 200000ul, $"placing bet {ConvertToDisplayAmount(betAmount)}...", placeBetIx);
            }
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Place bet failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void ClaimBet()
    {
        if (!accountManager.enableCrash) return;
        
        try
        {
            var claimBetIx = crashBuilder.ClaimBet();
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 100000u, 200000ul, "claiming bet...", claimBetIx);
            
            feedbackManager?.PlaySuccessSound();
            feedbackManager?.PlayCashFountainSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Claim bet failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void Ante(byte seatId, ulong[] betAmounts)
    {
        if (!accountManager.enableBlackjack || betAmounts == null || betAmounts.Length == 0) return;
        
        try
        {
            ulong totalBet = betAmounts.Aggregate(0UL, (sum, bet) => sum + bet);
            var setupInstructions = new List<TransactionInstruction>();
            
            var availableHands = accountManager.GetAvailableHandIds();
            
            var handsToUse = new List<byte>();
            var handsToInit = new List<byte>();
            
            for (int i = 0; i < betAmounts.Length; i++)
            {
                if (i < availableHands.Count)
                {
                    handsToUse.Add(availableHands[i]);
                }
                else
                {
                    byte newHandId = accountManager.GetNextUnusedHandId();
                    handsToUse.Add(newHandId);
                    handsToInit.Add(newHandId);
                }
            }
            
            var userBalancePk = TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey);
            var userBalanceData = await SubscriptionManager.Instance.LoadAccountData<UserBalance>(
                userBalancePk, data => UserBalance.Deserialize(data), forceDelegated: false
            );
            if (userBalanceData == null)
                setupInstructions.Add(treasuryBuilder.InitializeBalance());
            
            ulong existingBalance = accountManager.UserBalanceCache?.Balance ?? 0;
            ulong depositAmount = totalBet > existingBalance ? totalBet - existingBalance : 0;
            
            if (solanaManager.useEphemeralRollups)
            {
                var handsToDelegate = new List<byte>(handsToInit);
                
                foreach (var handId in handsToUse.Except(handsToInit))
                {
                    var handPk = BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, handId);
                    if (!await solanaManager.CheckIfDelegated(handPk))
                        handsToDelegate.Add(handId);
                }
                
                bool userBalanceIsDelegated = await solanaManager.CheckIfDelegated(userBalancePk);
                
                if (depositAmount > 0)
                {
                    if (userBalanceIsDelegated)
                    {
                        await solanaManager.SendAndConfirmTransaction(true, 150000u, 200000ul, "undelegating balance...", treasuryBuilder.UndelegateUserBalance());
                        userBalanceIsDelegated = false;
                    }
                    setupInstructions.Add(treasuryBuilder.DepositTokens(depositAmount));
                }
                
                if (!userBalanceIsDelegated)
                    setupInstructions.Add(treasuryBuilder.DelegateUserBalance());
                
                foreach (var handId in handsToInit)
                {
                    setupInstructions.Add(blackjackBuilder.InitializePlayerHand(handId));
                    setupInstructions.Add(blackjackBuilder.DelegateBlackjackHand(handId));
                }
                
                var existingHandsToDelegate = handsToDelegate.Except(handsToInit).ToList();
                foreach (var handId in existingHandsToDelegate)
                    setupInstructions.Add(blackjackBuilder.DelegateBlackjackHand(handId));
                
                if (setupInstructions.Count > 0)
                    await solanaManager.SendAndConfirmTransaction(false, 200000u, 200000ul, "setting up...", setupInstructions.ToArray());
                
                if (depositAmount > 0)
                {
                    var applyDepositIx = treasuryBuilder.ApplyDeposit();
                    await solanaManager.SendAndConfirmTransaction(true, 100000u, 200000ul, "applying deposit...", applyDepositIx);
                }
                
                var anteIxs = new List<TransactionInstruction>();
                for (int i = 0; i < betAmounts.Length; i++)
                {
                    if (betAmounts[i] > 0)
                        anteIxs.Add(blackjackBuilder.PlayerAnte(1, handsToUse[i], seatId, betAmounts[i]));
                }
                
                if (anteIxs.Count > 0)
                    await solanaManager.SendAndConfirmTransaction(true, 150000u, 200000ul, "placing ante...", anteIxs.ToArray());
            }
            else
            {
                if (depositAmount > 0)
                {
                    setupInstructions.Add(treasuryBuilder.DepositTokens(depositAmount));
                    setupInstructions.Add(treasuryBuilder.ApplyDeposit());
                }
                
                foreach (var handId in handsToInit)
                    setupInstructions.Add(blackjackBuilder.InitializePlayerHand(handId));
                
                if (setupInstructions.Count > 0)
                    await solanaManager.SendAndConfirmTransaction(false, 200000u, 200000ul, "setting up...", setupInstructions.ToArray());
                
                var anteIxs = new List<TransactionInstruction>();
                for (int i = 0; i < betAmounts.Length; i++)
                {
                    if (betAmounts[i] > 0)
                        anteIxs.Add(blackjackBuilder.PlayerAnte(1, handsToUse[i], seatId, betAmounts[i]));
                }
                
                if (anteIxs.Count > 0)
                    await solanaManager.SendAndConfirmTransaction(false, 150000u, 200000ul, "placing ante...", anteIxs.ToArray());
            }
            
            foreach (var handId in handsToInit)
            {
                var handPk = BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, handId);
                accountManager.TrackUserHand(handPk);
            }
            
            var newHandPks = handsToInit.Select(h => BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, h)).ToList();
            await accountManager.SubscribeToBlackjackHands(newHandPks);
            
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Ante failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void BlackjackHit(byte handId)
    {
        if (!accountManager.enableBlackjack) return;
        
        try
        {
            var ix = blackjackBuilder.PlayerHit(1, handId);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 150000u, 200000ul, "hitting...", ix);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Hit failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void BlackjackStand(byte handId)
    {
        if (!accountManager.enableBlackjack) return;
        
        try
        {
            var ix = blackjackBuilder.PlayerStand(1, handId);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 150000u, 200000ul, "standing...", ix);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Stand failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void BlackjackDouble(byte handId)
    {
        if (!accountManager.enableBlackjack) return;
        
        try
        {
            var handPk = BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, handId);
            if (!accountManager.BlackjackHands.TryGetValue(handPk, out var hand))
            {
                GameLogger.LogError("Hand not found in cache");
                return;
            }
            
            await EnsureDepositAndDelegation(hand.OriginalBet);
            
            var ix = blackjackBuilder.PlayerDouble(1, handId);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 150000u, 200000ul, "doubling...", ix);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Double failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void BlackjackSplit(byte handId)
    {
        if (!accountManager.enableBlackjack) return;
        
        try
        {
            var existingHandPk = BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, handId);
            if (!accountManager.BlackjackHands.TryGetValue(existingHandPk, out var hand))
            {
                GameLogger.LogError("Hand not found in cache");
                return;
            }
            
            byte newHandId = accountManager.GetNextUnusedHandId();
            var handSetup = new List<TransactionInstruction> { blackjackBuilder.InitializePlayerHand(newHandId) };
            if (solanaManager.useEphemeralRollups)
                handSetup.Add(blackjackBuilder.DelegateBlackjackHand(newHandId));
            
            await EnsureDepositAndDelegation(hand.OriginalBet, handSetup);
            
            var splitIx = blackjackBuilder.PlayerSplit(1, handId, newHandId);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 150000u, 200000ul, "splitting...", splitIx);
            
            var newHandPk = BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, newHandId);
            accountManager.TrackUserHand(newHandPk);
            await accountManager.SubscribeToBlackjackHands(new List<Solana.Unity.Wallet.PublicKey> { newHandPk });
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Split failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void BlackjackAcceptInsurance(byte handId)
    {
        if (!accountManager.enableBlackjack) return;
        
        try
        {
            var handPk = BlackjackTransactionBuilder.DeriveBlackjackHandAccount(Web3.Account.PublicKey, handId);
            if (!accountManager.BlackjackHands.TryGetValue(handPk, out var hand))
            {
                GameLogger.LogError("Hand not found in cache");
                return;
            }
            
            await EnsureDepositAndDelegation(hand.CurrentBet / 2);
            
            var ix = blackjackBuilder.AcceptInsurance(1, handId);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 150000u, 200000ul, "accepting insurance...", ix);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Accept insurance failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }

    public async void BlackjackSettleHand(byte handId)
    {
        if (!accountManager.enableBlackjack) return;
        
        try
        {
            var ix = blackjackBuilder.SettleHand(1, handId, Web3.Account.PublicKey);
            await solanaManager.SendAndConfirmTransaction(solanaManager.useEphemeralRollups, 150000u, 200000ul, "settling...", ix);
            feedbackManager?.PlaySuccessSound();
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Settle failed: {ex.Message}");
            feedbackManager?.PlayErrorSound();
        }
    }
    #endregion
}
