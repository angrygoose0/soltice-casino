using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Crash.Accounts;
using Treasury.Accounts;
using Blackjack.Accounts;
using BlackJackGame = Blackjack.Accounts.BlackJack;
using TreasuryAccount = Treasury.Accounts.Treasury;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using System.Linq;

/// <summary>
/// Manages subscriptions and caching for all on-chain accounts.
/// Exposes events for UI and simulators to react to data changes.
/// </summary>
public class OnChainAccountManager : MonoBehaviour
{
    [SerializeField] private SolanaManager solanaManager;

    // Cached account data
    public Game GameCache { get; private set; }
    public PlayerBet PlayerBetCache { get; private set; }
    public UserBalance UserBalanceCache { get; private set; }
    public TreasuryAccount TreasuryCache { get; private set; }
    public BlackJackGame BlackjackGameCache { get; private set; }
    public PublicKey SubscribedBlackjackPk { get; private set; }

    // Blackjack hands
    public IReadOnlyDictionary<PublicKey, BlackJackHand> BlackjackHands => _blackjackHandsCache;
    public IReadOnlyList<PublicKey> UserBlackjackHands => _userBlackjackHands;

    private Dictionary<PublicKey, BlackJackHand> _blackjackHandsCache = new();
    private Dictionary<PublicKey, string> _blackjackHandSubscriptionIds = new();
    private List<PublicKey> _userBlackjackHands = new();

    // Subscription IDs
    private string _playerBetSubscriptionId;
    private string _userBalanceSubscriptionId;

    private static readonly PublicKey DEFAULT_PUBKEY = new PublicKey("11111111111111111111111111111111");

    // Events for data updates
    public event Action<Game, Game> OnGameUpdated;           // (oldData, newData)
    public event Action<PlayerBet, PlayerBet> OnPlayerBetUpdated; // (oldData, newData)
    public event Action<UserBalance> OnUserBalanceUpdated;
    public event Action<TreasuryAccount> OnTreasuryUpdated;
    public event Action<BlackJackGame> OnBlackjackGameUpdated;
    public event Action<PublicKey, BlackJackHand, bool> OnBlackjackHandUpdated; // (handPk, hand, isNew)
    public event Action<PublicKey> OnBlackjackHandRemoved;

    private void Start()
    {
        SetupGameSubscription();
        SetupBlackjackGameSubscription();
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
        try
        {
            await SetupTreasurySubscription();
            await SetupUserAccountSubscriptions();
            
            _userBlackjackHands = await GetBlackjackHands(Web3.Account.PublicKey, 40);
            Debug.Log($"Found {_userBlackjackHands.Count} blackjack hands owned by player");
            await SubscribeToBlackjackHands(_userBlackjackHands);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Subscription setup failed: {ex.Message}");
        }
        finally
        {
            solanaManager?.HideConnectionLoadingOverlay();
        }
    }

    private async void OnWalletDisconnected()
    {
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
        
        PlayerBetCache = null;
        UserBalanceCache = null;
    }

    public async void SetupGameSubscription()
    {
        var gamePk = CrashTransactionBuilder.DeriveGameAccount();
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<Game>(
            gamePk,
            HandleGameUpdate,
            data => Game.Deserialize(data),
            forceDelegated: SolanaManager.USE_EPHEMERAL_ROLLUPS
        );

        if (string.IsNullOrEmpty(id))
            Debug.LogError("Failed to subscribe to game account");
    }

    public async void SetupBlackjackGameSubscription()
    {
        var blackjackPk = BlackjackTransactionBuilder.DeriveBlackjackAccount(1);
        SubscribedBlackjackPk = blackjackPk;
        
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<BlackJackGame>(
            blackjackPk,
            HandleBlackjackGameUpdate,
            data => BlackJackGame.Deserialize(data),
            forceDelegated: SolanaManager.USE_EPHEMERAL_ROLLUPS
        );

        if (string.IsNullOrEmpty(id))
            Debug.LogError("Failed to subscribe to blackjack game account");
        
        var gameHands = await GetBlackjackHands(blackjackPk, 8);
        Debug.Log($"Found {gameHands.Count} blackjack hands for game {blackjackPk}");
        await SubscribeToBlackjackHands(gameHands);
    }

    public async Task SetupTreasurySubscription()
    {
        var treasuryPk = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var id = await SubscriptionManager.Instance.SubscribeAndLoad<TreasuryAccount>(
            treasuryPk,
            HandleTreasuryUpdate,
            data => TreasuryAccount.Deserialize(data),
            forceDelegated: false
        );

        if (string.IsNullOrEmpty(id))
            Debug.LogError("Failed to subscribe to treasury account");
    }

    public async Task SetupUserAccountSubscriptions()
    {
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
        
        PlayerBetCache = null;
        UserBalanceCache = null;

        _playerBetSubscriptionId = await SetupAccountSubscription(
            "PlayerBet",
            CrashTransactionBuilder.DerivePlayerBetAccount(Web3.Account.PublicKey),
            HandlePlayerBetUpdate,
            data => PlayerBet.Deserialize(data)
        );

        _userBalanceSubscriptionId = await SetupAccountSubscription(
            "UserBalance",
            TreasuryTransactionBuilder.DeriveUserBalanceAccount(Web3.Account.PublicKey),
            HandleUserBalanceUpdate,
            data => UserBalance.Deserialize(data)
        );
    }

    private async Task<string> SetupAccountSubscription<T>(
        string accountName,
        PublicKey accountPk,
        Action<T> callback,
        Func<byte[], T> deserializer)
    {
        if (!SolanaManager.USE_EPHEMERAL_ROLLUPS)
        {
            // Non-ephemeral mode: just load and subscribe via mainnet
            return await SubscriptionManager.Instance.SubscribeAndLoad<T>(
                accountPk, callback, deserializer, forceDelegated: false
            );
        }
        
        var initialData = await SubscriptionManager.Instance.LoadAccountData<T>(
            accountPk, deserializer, forceDelegated: true
        );

        if (initialData != null)
        {
            Debug.Log($"{accountName} loaded via ER - setting up subscription");
            return await SubscriptionManager.Instance.SubscribeAndLoad<T>(
                accountPk, callback, deserializer, forceDelegated: true
            );
        }

        bool isDelegated = await solanaManager.CheckIfDelegated(accountPk);
        
        if (isDelegated)
        {
            Debug.Log($"{accountName} delegated but not ready - loading via RPC and subscribing via ER");
            
            var normalData = await SubscriptionManager.Instance.LoadAccountData<T>(
                accountPk, deserializer, forceDelegated: false
            );
            
            if (normalData != null)
                callback(normalData);
            
            return await SubscriptionManager.Instance.Subscribe<T>(
                accountPk, callback, deserializer, forceDelegated: true
            );
        }
        else
        {
            var baseLayerData = await SubscriptionManager.Instance.LoadAccountData<T>(
                accountPk, deserializer, forceDelegated: false
            );
            
            Debug.Log($"{accountName} state: delegated={isDelegated}, initialized={baseLayerData != null}");
            return null;
        }
    }

    // BlackJackHand offsets: discriminator=0, blackjack=8, player=40
    public async Task<List<PublicKey>> GetBlackjackHands(PublicKey filterKey, ulong offset)
    {
        var rpcClient = SolanaManager.USE_EPHEMERAL_ROLLUPS 
            ? SolanaManager.EphemeralWallet?.ActiveRpcClient 
            : Web3.Instance?.WalletBase?.ActiveRpcClient;
        if (rpcClient == null) return new List<PublicKey>();
        
        try
        {
            var memCmpList = new List<MemCmp>
            {
                new MemCmp { Bytes = BlackJackHand.ACCOUNT_DISCRIMINATOR_B58, Offset = 0 },
                new MemCmp { Bytes = filterKey.Key, Offset = (int)offset }
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

    public async Task SubscribeToBlackjackHands(List<PublicKey> handPubkeys)
    {
        foreach (var handPk in handPubkeys)
        {
            if (_blackjackHandSubscriptionIds.ContainsKey(handPk))
                continue;
            
            var subId = await SubscriptionManager.Instance.SubscribeAndLoad<BlackJackHand>(
                handPk,
                data => HandleBlackjackHandUpdate(handPk, data),
                data => BlackJackHand.Deserialize(data),
                forceDelegated: SolanaManager.USE_EPHEMERAL_ROLLUPS
            );
            
            if (!string.IsNullOrEmpty(subId))
            {
                _blackjackHandSubscriptionIds[handPk] = subId;
                Debug.Log($"Subscribed to blackjack hand: {handPk}");
            }
        }
    }

    public void TrackUserHand(PublicKey handPk)
    {
        if (!_userBlackjackHands.Contains(handPk))
            _userBlackjackHands.Add(handPk);
    }

    // Get hands available for betting in the subscribed game
    public List<byte> GetAvailableHandIds()
    {
        var availableHands = new List<byte>();
        foreach (var handPk in _userBlackjackHands)
        {
            if (_blackjackHandsCache.TryGetValue(handPk, out var hand) &&
                hand.Blackjack.Equals(SubscribedBlackjackPk) &&
                hand.CurrentBet == 0)
            {
                availableHands.Add(hand.HandId);
            }
        }
        return availableHands;
    }

    public byte GetNextUnusedHandId()
    {
        byte newHandId = (byte)_userBlackjackHands.Count;
        while (_userBlackjackHands.Any(pk => 
            _blackjackHandsCache.TryGetValue(pk, out var h) && h.HandId == newHandId))
            newHandId++;
        return newHandId;
    }

    // Internal handlers that update cache and fire events
    private void HandleGameUpdate(Game newData)
    {
        var oldData = GameCache;
        GameCache = newData;
        OnGameUpdated?.Invoke(oldData, newData);
        Debug.Log($"State: {newData.State}, Tick: {newData.Tick}, CrashTick: {newData.CrashTick}, GameNo: {newData.GameNo}");
    }

    private void HandlePlayerBetUpdate(PlayerBet newData)
    {
        var oldData = PlayerBetCache;
        PlayerBetCache = newData;
        OnPlayerBetUpdated?.Invoke(oldData, newData);
        Debug.Log($"Game: {newData.Game}, Amount: {newData.Amount}, Player: {newData.Player}");
    }

    private void HandleUserBalanceUpdate(UserBalance newData)
    {
        UserBalanceCache = newData;
        OnUserBalanceUpdated?.Invoke(newData);
        Debug.Log($"Balance: {newData.Balance}, User: {newData.User}");
    }

    private void HandleTreasuryUpdate(TreasuryAccount newData)
    {
        TreasuryCache = newData;
        OnTreasuryUpdated?.Invoke(newData);
        Debug.Log($"Treasury BufferAmount: {newData.BufferAmount}, UserOwnedAmount: {newData.UserOwnedAmount}");
    }

    private async void HandleBlackjackGameUpdate(BlackJackGame newData)
    {
        BlackjackGameCache = newData;
        OnBlackjackGameUpdated?.Invoke(newData);
        
        if (SubscribedBlackjackPk != null)
        {
            var gameHands = await GetBlackjackHands(SubscribedBlackjackPk, 8);
            await SubscribeToBlackjackHands(gameHands);
        }
        
        Debug.Log($"Blackjack GameId: {newData.GameId}, ActiveHands: {newData.ActiveHands}");
    }

    private async void HandleBlackjackHandUpdate(PublicKey handPk, BlackJackHand newData)
    {
        bool isInSubscribedGame = SubscribedBlackjackPk != null 
            && newData.Blackjack.Equals(SubscribedBlackjackPk)
            && (BlackjackGameCache == null || newData.GameNo >= BlackjackGameCache.GameNo);
        bool isPlayerOwnHand = Web3.Account != null && newData.Player.Equals(Web3.Account.PublicKey);
        
        if (!isInSubscribedGame && !isPlayerOwnHand)
        {
            Debug.Log($"Hand {handPk} not in subscribed game - unsubscribing");
            
            if (_blackjackHandSubscriptionIds.TryGetValue(handPk, out string subId))
            {
                await SubscriptionManager.Instance.Unsubscribe(subId);
                _blackjackHandSubscriptionIds.Remove(handPk);
            }
            _blackjackHandsCache.Remove(handPk);
            OnBlackjackHandRemoved?.Invoke(handPk);
            return;
        }
        
        bool isNew = !_blackjackHandsCache.ContainsKey(handPk);
        _blackjackHandsCache[handPk] = newData;
        
        if (isNew && isPlayerOwnHand && !_userBlackjackHands.Contains(handPk))
            _userBlackjackHands.Add(handPk);
        
        OnBlackjackHandUpdated?.Invoke(handPk, newData, isNew);
        
        Debug.Log($"Blackjack Hand {handPk}: State={newData.State}, CurrentBet={newData.CurrentBet}");
    }
}

