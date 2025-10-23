using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Messages;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;

/// <summary>
/// Manages WebSocket subscriptions for Solana accounts with support for both mainnet and delegated (rollup) accounts.
/// </summary>
public class SubscriptionManager : MonoBehaviour
{
    private static SubscriptionManager _instance;
    public static SubscriptionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("SubscriptionManager");
                _instance = go.AddComponent<SubscriptionManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private readonly Dictionary<string, AccountSubscription> _subscriptions = new Dictionary<string, AccountSubscription>();
    private SolanaManager _solanaManager;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _solanaManager = FindObjectOfType<SolanaManager>();
    }

    /// <summary>
    /// Subscribe to a Solana account and receive live updates via callback.
    /// Automatically detects if the account is delegated and subscribes to the correct RPC endpoint.
    /// </summary>
    /// <typeparam name="T">The account data type to deserialize to</typeparam>
    /// <param name="accountAddress">The public key of the account to subscribe to</param>
    /// <param name="callback">Callback invoked when account data updates. Receives the deserialized data.</param>
    /// <param name="deserializer">Function to deserialize byte array into type T. If null, raw AccountInfo is passed.</param>
    /// <param name="commitment">Commitment level for the subscription (default: Processed for fast updates)</param>
    /// <param name="forceDelegated">Force subscription to delegated endpoint (optional)</param>
    /// <returns>Subscription ID for later unsubscription</returns>
    public async Task<string> Subscribe<T>(
        PublicKey accountAddress,
        Action<T> callback,
        Func<byte[], T> deserializer = null,
        Commitment commitment = Commitment.Processed,
        bool? forceDelegated = null)
    {
        if (callback == null)
        {
            Debug.LogError("Callback cannot be null");
            return null;
        }

        if (Web3.Instance == null)
        {
            Debug.LogError("Web3 instance not initialized");
            return null;
        }

        string subscriptionId = accountAddress.ToString();

        // Check if already subscribed
        if (_subscriptions.ContainsKey(subscriptionId))
        {
            Debug.LogWarning($"Already subscribed to {subscriptionId}. Unsubscribe first or use UpdateSubscription.");
            return subscriptionId;
        }

        // Determine if account is delegated
        bool isDelegated = forceDelegated ?? await _solanaManager.CheckIfDelegated(accountAddress);
        
        // If forceDelegated is true but account isn't actually delegated, fail
        if (forceDelegated == true && !isDelegated)
        {
            Debug.LogError($"Account {accountAddress} is not delegated but forceDelegated is true. Subscription failed.");
            return null;
        }

        // Create subscription object
        var subscription = new AccountSubscription
        {
            AccountAddress = accountAddress,
            IsDelegated = isDelegated,
            Commitment = commitment
        };

        // Get appropriate streaming client
        var streamingClient = await GetStreamingClient(isDelegated);
        if (streamingClient == null || streamingClient.State != WebSocketState.Open)
        {
            Debug.LogError($"WebSocket connection not available for {(isDelegated ? "delegated" : "mainnet")} endpoint");
            return null;
        }

        // Subscribe to account changes
        try
        {
            subscription.SubscriptionState = await streamingClient.SubscribeAccountInfoAsync(
                accountAddress.ToString(),
                (state, accountInfo) => HandleAccountUpdate(accountAddress.ToString(), accountInfo, callback, deserializer),
                commitment
            );

            _subscriptions[subscriptionId] = subscription;
            Debug.Log($"✓ Subscribed to account {subscriptionId} on {(isDelegated ? "delegated" : "mainnet")} endpoint");

            return subscriptionId;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to subscribe to {subscriptionId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Subscribe to account and automatically load initial data.
    /// </summary>
    public async Task<string> SubscribeAndLoad<T>(
        PublicKey accountAddress,
        Action<T> callback,
        Func<byte[], T> deserializer = null,
        Commitment commitment = Commitment.Processed,
        bool? forceDelegated = null)
    {
        // First load the data
        var initialData = await LoadAccountData(accountAddress, deserializer, forceDelegated);
        if (initialData != null && callback != null)
        {
            // Invoke callback with initial data on main thread
            UnityMainThreadDispatcher.Instance().Enqueue(() => callback(initialData));
        }

        // Then subscribe for updates
        return await Subscribe(accountAddress, callback, deserializer, commitment, forceDelegated);
    }

    /// <summary>
    /// Load account data once without subscribing.
    /// </summary>
    public async Task<T> LoadAccountData<T>(
        PublicKey accountAddress,
        Func<byte[], T> deserializer = null,
        bool? forceDelegated = null)
    {
        bool isDelegated = forceDelegated ?? await _solanaManager.CheckIfDelegated(accountAddress);
        
        // If forceDelegated is true but account isn't actually delegated, fail
        if (forceDelegated == true && !isDelegated)
        {
            Debug.LogError($"Account {accountAddress} is not delegated but forceDelegated is true. LoadAccountData failed.");
            return default;
        }
        
        var rpcClient = GetRpcClient(isDelegated);

        var result = await rpcClient.GetAccountInfoAsync(accountAddress, Commitment.Confirmed);
        if (!result.WasSuccessful || result.Result.Value == null)
        {
            Debug.LogWarning($"Failed to load account data for {accountAddress}");
            return default;
        }

        if (deserializer != null && result.Result.Value.Data?.Count > 0)
        {
            byte[] data = Convert.FromBase64String(result.Result.Value.Data[0]);
            return deserializer(data);
        }

        return default;
    }

    /// <summary>
    /// Unsubscribe from an account.
    /// </summary>
    public async Task<bool> Unsubscribe(string subscriptionId)
    {
        if (!_subscriptions.ContainsKey(subscriptionId))
        {
            Debug.LogWarning($"No subscription found for {subscriptionId}");
            return false;
        }

        var subscription = _subscriptions[subscriptionId];
        
        try
        {
            if (subscription.SubscriptionState != null)
            {
                await subscription.SubscriptionState.UnsubscribeAsync();
            }
            _subscriptions.Remove(subscriptionId);
            Debug.Log($"✓ Unsubscribed from {subscriptionId}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to unsubscribe from {subscriptionId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unsubscribe from all active subscriptions.
    /// </summary>
    public async Task UnsubscribeAll()
    {
        var subscriptionIds = new List<string>(_subscriptions.Keys);
        foreach (var id in subscriptionIds)
        {
            await Unsubscribe(id);
        }
        Debug.Log($"✓ Unsubscribed from all {subscriptionIds.Count} subscriptions");
    }

    /// <summary>
    /// Get information about a subscription.
    /// </summary>
    public AccountSubscription GetSubscriptionInfo(string subscriptionId)
    {
        if (!_subscriptions.ContainsKey(subscriptionId))
            return null;

        return _subscriptions[subscriptionId];
    }

    /// <summary>
    /// Get all active subscription IDs.
    /// </summary>
    public List<string> GetActiveSubscriptions()
    {
        return new List<string>(_subscriptions.Keys);
    }

    // Private helper methods

    private async Task<IStreamingRpcClient> GetStreamingClient(bool isDelegated)
    {
        var wallet = isDelegated ? SolanaManager.EphemeralWallet : Web3.Wallet;
        
        if (wallet == null)
        {
            Debug.LogError($"{(isDelegated ? "Ephemeral" : "Main")} wallet not initialized");
            return null;
        }

        if (wallet.ActiveStreamingRpcClient.State != WebSocketState.Open)
        {
            Debug.Log($"Waiting for {(isDelegated ? "ephemeral" : "main")} WebSocket connection...");
            await wallet.AwaitWsRpcConnection();
        }

        return wallet.ActiveStreamingRpcClient;
    }

    private IRpcClient GetRpcClient(bool isDelegated)
    {
        return isDelegated 
            ? SolanaManager.EphemeralWallet?.ActiveRpcClient 
            : Web3.Wallet?.ActiveRpcClient;
    }

    private async void HandleAccountUpdate<T>(
        string accountAddress,
        ResponseValue<AccountInfo> accountInfo,
        Action<T> callback,
        Func<byte[], T> deserializer)
    {
        try
        {
            Debug.Log($"📡 Account updated: {accountAddress}");

            // Switch to main thread for Unity operations
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(async () =>
            {
                T result = default;

                if (accountInfo.Value?.Data?.Count > 0 && deserializer != null)
                {
                    byte[] data = Convert.FromBase64String(accountInfo.Value.Data[0]);
                    result = deserializer(data);
                }
                else if (typeof(T) == typeof(AccountInfo))
                {
                    result = (T)(object)accountInfo.Value;
                }

                callback?.Invoke(result);
            });
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error handling account update for {accountAddress}: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        _ = UnsubscribeAll();
    }

    // Public data structures

    public class AccountSubscription
    {
        public PublicKey AccountAddress { get; set; }
        public bool IsDelegated { get; set; }
        public Commitment Commitment { get; set; }
        public bool IsActive => SubscriptionState != null;
        internal SubscriptionState SubscriptionState { get; set; }
    }
}

