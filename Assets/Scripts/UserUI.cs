using UnityEngine;
using UnityEngine.UI;
using Crash.Accounts;
using Treasury.Accounts;
using Solana.Unity.SDK;
using TMPro;

public class UserUI : MonoBehaviour
{
    [SerializeField] private TreasuryTransactionBuilder treasuryBuilder;
    [SerializeField] private CrashTransactionBuilder crashBuilder;
    [SerializeField] private SolanaManager solanaManager;

    private Game _gameCache;
    private PlayerBet _playerBetCache;
    private EphemeralBalance _ephemeralBalanceCache;

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
    [SerializeField] private Button btnEnsureDelegation;
    [SerializeField] private Button btnUponUserJoin;
    [SerializeField] private Button btnSetupGameSubscription;
    [SerializeField] private Button btnSetupPlayerBetSubscription;
    [SerializeField] private Button btnSetupEphemeralBalanceSubscription;

    // Public fields for input values (editable in Inspector)
    [Header("Input Values")]
    public ulong depositAmount = 1000;
    public ulong withdrawAmount = 1000;
    public ulong betAmount = 1000;
    public byte clientSeed = 42;

    // Account Data Display
    [Header("Account Data Display")]
    [SerializeField] private TextMeshProUGUI gameAccountTMP;
    [SerializeField] private TextMeshProUGUI playerBetAccountTMP;
    [SerializeField] private TextMeshProUGUI ephemeralBalanceAccountTMP;

    private void Start()
    {
        SetupButtonListeners();
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
            btnPlaceBet.onClick.AddListener(() => PlaceBet(betAmount));
        
        if (btnClaimBet != null)
            btnClaimBet.onClick.AddListener(() => ClaimBet());
        
        if (btnTick != null)
            btnTick.onClick.AddListener(() => Tick());
        
        if (btnEnsureDelegation != null)
            btnEnsureDelegation.onClick.AddListener(() => EnsureDelegation());
        
        if (btnUponUserJoin != null)
            btnUponUserJoin.onClick.AddListener(() => UponUserJoin());
        
        if (btnSetupGameSubscription != null)
            btnSetupGameSubscription.onClick.AddListener(() => SetupGameSubscription());
        
        if (btnSetupPlayerBetSubscription != null)
            btnSetupPlayerBetSubscription.onClick.AddListener(() => SetupPlayerBetSubscription());
        
        if (btnSetupEphemeralBalanceSubscription != null)
            btnSetupEphemeralBalanceSubscription.onClick.AddListener(() => SetupEphemeralBalanceSubscription());
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
    public async void PlaceBet(ulong amount) //user
    {
        var placeBetIx = crashBuilder.PlaceBet(amount);
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

    public async void EnsureDelegation() //prevents soft lock. input: pubkey + account type.
    {
        /*
        checks account's owner. if delegation program, return true.
        if not, delegate it and then return true.
        */
    }
    //maybe we do this when subscribing to the account?
    


    public async void UponUserJoin()
    {
        /*
        ensure game is setup.
            setup subscription for game.
        ensure user is setup.
            setup ephemeral balance subscription.
            setup player bet subscription.
        */

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

    public async void SetupPlayerBetSubscription()
    {
        var playerBetPk = CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey);
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<PlayerBet>(
            playerBetPk,
            OnPlayerBetUpdate,
            data => PlayerBet.Deserialize(data),
            forceDelegated: true
        );
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("Failed to subscribe to delegated player bet account");
        }
    }

    public async void SetupEphemeralBalanceSubscription()
    {
        var ephemeralBalancePk = TreasuryTransactionBuilder.DeriveEphemeralBalanceAccount(Web3.Account.PublicKey);
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<EphemeralBalance>(
            ephemeralBalancePk,
            OnEphemeralBalanceUpdate,
            data => EphemeralBalance.Deserialize(data),
            forceDelegated: true
        );
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("Failed to subscribe to delegated ephemeral balance account");
        }
    }

    private void OnPlayerBetUpdate(PlayerBet newData)
    {
        _playerBetCache = newData;
        if (playerBetAccountTMP != null)
        {
            playerBetAccountTMP.text = newData != null ? $"PLAYERBET\nAmount: {newData.Amount}, GameNo: {newData.GameNo}" : "No PlayerBet Data";
        }
        if (newData == null)
        {
            Debug.Log("Player bet account update: null");
            return;
        }
        Debug.Log($"Game: {newData.Game}, Amount: {newData.Amount}, Player: {newData.Player}");
    }

    private void OnEphemeralBalanceUpdate(EphemeralBalance newData)
    {
        _ephemeralBalanceCache = newData;
        if (ephemeralBalanceAccountTMP != null)
        {
            ephemeralBalanceAccountTMP.text = newData != null ? $"EPHEMERALBALANCE\nBalance: {newData.Balance}" : "No EphemeralBalance Data";
        }
        if (newData == null)
        {
            Debug.Log("Ephemeral balance account update: null");
            return;
        }
        Debug.Log($"Balance: {newData.Balance}, Withdraw: {newData.Withdraw}, Deposit: {newData.Deposit}");
    }

    private void OnGameUpdate(Game newData)
    {
        _gameCache = newData;
        if (gameAccountTMP != null)
        {
            gameAccountTMP.text = newData != null ? $"GAME\nState: {newData.State}, Tick: {newData.Tick}, GameNo: {newData.GameNo}" : "No Game Data";
        }
        _balloonSimulator.UpdateBalloon(newData);
        if (newData == null)
        {
            Debug.Log("Game account update: null");
            return;
        }
        Debug.Log($"State: {newData.State}, Tick: {newData.Tick}, CrashTick: {newData.CrashTick}, GameNo: {newData.GameNo}");
    }
}