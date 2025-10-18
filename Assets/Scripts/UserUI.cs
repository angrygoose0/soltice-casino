using UnityEngine;
using UnityEngine.UI;
using Crash.Accounts;
using Treasury.Accounts;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using TMPro;
using Coherence.Toolkit;

public class UserUI : MonoBehaviour
{
    [SerializeField] private TreasuryTransactionBuilder treasuryBuilder;
    [SerializeField] private CrashTransactionBuilder crashBuilder;
    [SerializeField] private SolanaManager solanaManager;
    [SerializeField] private CoherenceBridge coherenceBridge;

    private Game _gameCache;
    private PlayerBet _playerBetCache;
    private EphemeralBalance _ephemeralBalanceCache;

    // Account state tracking
    private bool _playerBetIsDelegated;
    private bool _playerBetIsInitialized;
    private bool _ephemeralBalanceIsDelegated;
    private bool _ephemeralBalanceIsInitialized;

    // Auto-tick coroutine tracking
    private Coroutine _tickCoroutine;

    [SerializeField] private GameObject beforeBettingGroup;
    [SerializeField] private GameObject afterBettingGroup;
    [SerializeField] private GameObject setupGroup;
    [SerializeField] private TextMeshProUGUI afterBettingText;
    [SerializeField] private Button claimButton;
    [SerializeField] private Button startButton;

    [SerializeField] private BalloonSimulator _balloonSimulator;

    // Button TMP fields
    [SerializeField] private Button btnSetupGame;
    [SerializeField] private Button btnSetupUser;
    [SerializeField] private Button btnDeposit;
    [SerializeField] private Button btnWithdraw;
    [SerializeField] private Button btnSetupRandomness;
    [SerializeField] private Button btnStartGame;
    [SerializeField] private Button btnPlaceBet;
    [SerializeField] private Button btnClaimBet;
    [SerializeField] private Button btnTick;
    [SerializeField] private Button btnCreateOrRefreshSession;

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
    [SerializeField] private TextMeshProUGUI ephemeralBalanceAccountTMP;

    [SerializeField] private TextMeshProUGUI playerTextTMP;


    private void Start()
    {
        SetupButtonListeners();
        SetupGameSubscription();
        beforeBettingGroup.SetActive(false);
        afterBettingGroup.SetActive(false);
        setupGroup.SetActive(false);
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
        StopAutoTick();
    }

    private async void OnWalletConnected(Account account)
    {
        await SetupUserAccountSubscriptions();
    }

    private void OnWalletDisconnected()
    {
        // Hide all UI groups when wallet disconnects
        beforeBettingGroup.SetActive(false);
        afterBettingGroup.SetActive(false);
        setupGroup.SetActive(false);
        
        // Reset account state
        _playerBetIsDelegated = false;
        _playerBetIsInitialized = false;
        _ephemeralBalanceIsDelegated = false;
        _ephemeralBalanceIsInitialized = false;
        
        Debug.Log("Wallet disconnected - UI reset");
    }

    private void SetupButtonListeners()
    {
        // Setup button listeners for all public void methods
        if (btnSetupGame != null)
            btnSetupGame.onClick.AddListener(() => SetupGame());
        
        if (btnSetupUser != null)
            btnSetupUser.onClick.AddListener(() => SetupUser());
        
        if (btnDeposit != null)
            btnDeposit.onClick.AddListener(() => Deposit(depositAmount));
        
        if (btnWithdraw != null)
            btnWithdraw.onClick.AddListener(() => Withdraw(withdrawAmount));
        
        if (btnSetupRandomness != null)
            btnSetupRandomness.onClick.AddListener(() => SetupRandomness(clientSeed));
        
        if (btnStartGame != null)
            btnStartGame.onClick.AddListener(() => StartGame());
        
        if (btnPlaceBet != null)
            btnPlaceBet.onClick.AddListener(() => PlaceBet());
        
        if (btnClaimBet != null)
            btnClaimBet.onClick.AddListener(() => ClaimBet());
        
        if (btnTick != null)
            btnTick.onClick.AddListener(() => Tick());

        if (btnCreateOrRefreshSession != null)
            btnCreateOrRefreshSession.onClick.AddListener(() => CreateOrRefreshSession());
    }

    public async void SetupGame() //admin
    {
        var initTreasuryIx = treasuryBuilder.InitializeTreasury();
        var initCrashAuthorityIx = crashBuilder.InitializeAuthority();
        var delegateCrashAuthorityIx = crashBuilder.DelegateAuthority();
        var initGameIx = crashBuilder.InitializeGame();
        var delegateGameIx = crashBuilder.DelegateGame();

        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, 
            initTreasuryIx, 
            initCrashAuthorityIx, 
            delegateCrashAuthorityIx, 
            initGameIx, 
            delegateGameIx
        );  
    }

    public async void SetupUser() //user
    {
        var initBalanceIx = treasuryBuilder.InitializeBalance();
        var delegateEphemeralBalanceIx = treasuryBuilder.DelegateEphemeralBalance();
        var initPlayerBetIx = crashBuilder.InitializePlayerBet();
        var delegatePlayerBetIx = crashBuilder.DelegatePlayerBet();

        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, 
            initBalanceIx, delegateEphemeralBalanceIx, initPlayerBetIx, delegatePlayerBetIx
        );
    }

    public async void SetupUserAccountsFromState()
    {
        var instructions = new System.Collections.Generic.List<Solana.Unity.Rpc.Models.TransactionInstruction>();

        // Handle Ephemeral Balance setup
        if (!_ephemeralBalanceIsInitialized)
        {
            Debug.Log("Adding InitializeBalance instruction");
            instructions.Add(treasuryBuilder.InitializeBalance());
        }
        
        if (_ephemeralBalanceIsInitialized && !_ephemeralBalanceIsDelegated)
        {
            Debug.Log("Adding DelegateEphemeralBalance instruction");
            instructions.Add(treasuryBuilder.DelegateEphemeralBalance());
        }

        // Handle Player Bet setup
        if (!_playerBetIsInitialized)
        {
            Debug.Log("Adding InitializePlayerBet instruction");
            instructions.Add(crashBuilder.InitializePlayerBet());
        }
        
        if (_playerBetIsInitialized && !_playerBetIsDelegated)
        {
            Debug.Log("Adding DelegatePlayerBet instruction");
            instructions.Add(crashBuilder.DelegatePlayerBet());
        }

        if (instructions.Count == 0)
        {
            Debug.Log("All accounts already set up - no transaction needed");
            return;
        }

        Debug.Log($"Sending transaction with {instructions.Count} instruction(s)");
        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, instructions.ToArray());
        
        // After successful transaction, refresh subscription states
        await SetupUserAccountSubscriptions();
    }

    //ENSURE ephemeral_balance delegated
    public async void Deposit(ulong amount) //user
    {
        var userDepositIx = treasuryBuilder.UserDeposit(amount);
        var ephemeralDepositIx = treasuryBuilder.EphemeralDeposit();

        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, userDepositIx);
        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, ephemeralDepositIx);
    }

    //ENSURE ephemeral_balance delegated
    public async void Withdraw(ulong amount) //user
    {
        var ephemeralWithdrawIx = treasuryBuilder.EphemeralWithdraw(amount);
        var userWithdrawIx = treasuryBuilder.UserWithdraw();

        await solanaManager.SendAndConfirmTransaction(true, 0u, 0ul, ephemeralWithdrawIx);
        await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, userWithdrawIx);
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

    private System.Collections.IEnumerator AutoTickCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(3f);
            Tick();
        }
    }

    private void StartAutoTick()
    {
        if (_tickCoroutine == null && coherenceBridge != null && coherenceBridge.IsSimulatorOrHost)
        {
            _tickCoroutine = StartCoroutine(AutoTickCoroutine());
            Debug.Log("Auto-tick started (Host)");
        }
    }

    private void StopAutoTick()
    {
        if (_tickCoroutine != null)
        {
            StopCoroutine(_tickCoroutine);
            _tickCoroutine = null;
            Debug.Log("Auto-tick stopped");
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

    private async System.Threading.Tasks.Task SetupUserAccountSubscriptions()
    {
        // Setup both accounts
        await SetupAccountSubscription(
            "PlayerBet",
            CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey),
            OnPlayerBetUpdate,
            data => PlayerBet.Deserialize(data),
            (isDelegated, isInitialized) => {
                _playerBetIsDelegated = isDelegated;
                _playerBetIsInitialized = isInitialized;
            }
        );

        await SetupAccountSubscription(
            "EphemeralBalance",
            TreasuryTransactionBuilder.DeriveEphemeralBalanceAccount(Web3.Account.PublicKey),
            OnEphemeralBalanceUpdate,
            data => EphemeralBalance.Deserialize(data),
            (isDelegated, isInitialized) => {
                _ephemeralBalanceIsDelegated = isDelegated;
                _ephemeralBalanceIsInitialized = isInitialized;
            }
        );

        UpdateUIGroupsBasedOnAccountState();
    }

    private async System.Threading.Tasks.Task SetupAccountSubscription<T>(
        string accountName,
        PublicKey accountPk,
        System.Action<T> callback,
        System.Func<byte[], T> deserializer,
        System.Action<bool, bool> setState)
    {
        // Try loading account using ER
        var initialData = await SubscriptionManager.Instance.LoadAccountData<T>(
            accountPk, deserializer, forceDelegated: true
        );

        if (initialData != null)
        {
            // Account is delegated and ready
            Debug.Log($"{accountName} loaded via ER - setting up subscription");
            setState(true, true);
            await SubscriptionManager.Instance.SubscribeAndLoad<T>(
                accountPk, callback, deserializer, forceDelegated: true
            );
            return;
        }

        // Check if delegated but not ready
        bool isDelegated = await SubscriptionManager.Instance.CheckIfDelegated(accountPk);
        
        if (isDelegated)
        {
            // Delegated but not ready - load via normal RPC and subscribe via ER
            Debug.Log($"{accountName} delegated but not ready - loading via RPC and subscribing via ER");
            setState(true, true);
            
            var normalData = await SubscriptionManager.Instance.LoadAccountData<T>(
                accountPk, deserializer, forceDelegated: false
            );
            
            if (normalData != null)
            {
                callback(normalData);
            }
            
            await SubscriptionManager.Instance.Subscribe<T>(
                accountPk, callback, deserializer, forceDelegated: true
            );
        }
        else
        {
            // Not delegated - check if exists on base layer
            var baseLayerData = await SubscriptionManager.Instance.LoadAccountData<T>(
                accountPk, deserializer, forceDelegated: false
            );
            
            setState(false, baseLayerData != null);
            Debug.Log($"{accountName} state: delegated=false, initialized={baseLayerData != null}");
        }
    }

    private bool AreAccountsReady() => _playerBetIsDelegated && _playerBetIsInitialized 
                                      && _ephemeralBalanceIsDelegated && _ephemeralBalanceIsInitialized;

    private void UpdateUIGroupsBasedOnAccountState()
    {
        bool ready = AreAccountsReady();
        
        setupGroup.SetActive(!ready);
        
        if (!ready)
        {
            beforeBettingGroup.SetActive(false);
            afterBettingGroup.SetActive(false);
            Debug.Log($"Setup required - PlayerBet(d={_playerBetIsDelegated}, i={_playerBetIsInitialized}), EphemeralBalance(d={_ephemeralBalanceIsDelegated}, i={_ephemeralBalanceIsInitialized})");
        }
        else
        {
            Debug.Log("All accounts ready");
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
        if (!AreAccountsReady()) return;

        if (playerBet.Amount == 0 || playerBet.GameNo < game.GameNo || playerBet.Claimed) { //means player hasnt bet.
            playerTextTMP.enabled = false;
            beforeBettingGroup.SetActive(true);
            afterBettingGroup.SetActive(false);
            return;
        }
        
        if (playerBet.GameNo > game.GameNo) {
            playerTextTMP.enabled = true;
            beforeBettingGroup.SetActive(false);
            afterBettingGroup.SetActive(true);

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
            claimButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(game.State == 0);
        } 
        else if (playerBet.GameNo == game.GameNo) {
            playerTextTMP.enabled = true;
            beforeBettingGroup.SetActive(false);
            afterBettingGroup.SetActive(true);
            
            ulong currentValue = (ulong)(playerBet.Amount * System.Math.Pow(1.11, game.Tick));
            playerTextTMP.text = $"Bet: {currentValue}";
            playerTextTMP.color = new Color(1f, 1f, 1f, 1f);
            
            afterBettingText.text = $"Your bet: {currentValue}";
            claimButton.gameObject.SetActive(true);
            startButton.gameObject.SetActive(false);
        }
    }


    private void OnEphemeralBalanceUpdate(EphemeralBalance newData)
    {
        _ephemeralBalanceCache = newData;
        if (ephemeralBalanceAccountTMP != null && TextAnimationManager.Instance != null)
        {
            ulong lastBalance = TextAnimationManager.Instance.GetLastRenderedAmount(ephemeralBalanceAccountTMP);
            if (lastBalance != newData.Balance)
            {
                TextAnimationManager.Instance.AnimateNumber(
                    ephemeralBalanceAccountTMP,
                    lastBalance,
                    newData.Balance,
                    prefix: "Balance: "
                );
            }
            else
            {
                ephemeralBalanceAccountTMP.text = $"Balance: {newData.Balance}";
            }
        }
        Debug.Log($"Balance: {newData.Balance}, Withdraw: {newData.Withdraw}, Deposit: {newData.Deposit}");
    }

    
    private void OnGameUpdate(Game newData)
    {
        _gameCache = newData;
        if (gameAccountTMP != null)
        {
            gameAccountTMP.text = $"GAME\nState: {newData.State}, Tick: {newData.Tick}, GameNo: {newData.GameNo}";
        }
        _balloonSimulator.UpdateBalloon(newData);
        UpdateUserBetText(_playerBetCache, newData);
        Debug.Log($"State: {newData.State}, Tick: {newData.Tick}, CrashTick: {newData.CrashTick}, GameNo: {newData.GameNo}");

        // Auto-tick management (host only)
        // State 1 = game running, State 0 = game not running/waiting
        if (newData.State == 1)
        {
            StartAutoTick();
        }
        else
        {
            StopAutoTick();
        }
    }

    /// <summary>
    /// Calls SessionManager to create or refresh a session.
    /// </summary>
    public async void CreateOrRefreshSession()
    {
        await SessionManager.CreateNewSession();
        Debug.Log("Session created or refreshed.");
    }
}