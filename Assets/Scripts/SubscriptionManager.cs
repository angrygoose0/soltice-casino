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
/// Supports subscriptions before wallet connection using a standalone streaming client.
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
    private SolanaManager _solanaManagerCache;
    private SolanaManager SolanaManagerInstance => _solanaManagerCache ??= FindObjectOfType<SolanaManager>();
    
    // Standalone streaming client for pre-wallet subscriptions
    private IStreamingRpcClient _standaloneStreamingClient;
    private bool _standaloneClientConnecting;

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
            GameLogger.LogError("Callback cannot be null");
            return null;
        }

        if (Web3.Instance == null)
        {
            GameLogger.LogError("Web3 instance not initialized");
            return null;
        }

        string subscriptionId = accountAddress.ToString();

        // Check if already subscribed
        if (_subscriptions.ContainsKey(subscriptionId))
        {
            GameLogger.LogWarning($"Already subscribed to {subscriptionId}. Unsubscribe first or use UpdateSubscription.");
            return subscriptionId;
        }

        // Determine if account is delegated
        bool isDelegated = forceDelegated ?? await SolanaManagerInstance.CheckIfDelegated(accountAddress);
        
        // If forceDelegated is true but account isn't actually delegated, fail
        if (forceDelegated == true && !isDelegated)
        {
            GameLogger.LogError($"Account {accountAddress} is not delegated but forceDelegated is true. Subscription failed.");
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
            return null; // Already logged in GetStreamingClient

        // Subscribe to account changes
        try
        {
            subscription.SubscriptionState = await streamingClient.SubscribeAccountInfoAsync(
                accountAddress.ToString(),
                (state, accountInfo) => HandleAccountUpdate(accountAddress.ToString(), accountInfo, callback, deserializer),
                commitment
            );

            _subscriptions[subscriptionId] = subscription;
            GameLogger.Log($"✓ Subscribed to account {subscriptionId} on {(isDelegated ? "delegated" : "mainnet")} endpoint");

            return subscriptionId;
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Failed to subscribe to {subscriptionId}: {ex.Message}");
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
        bool isDelegated = forceDelegated ?? await SolanaManagerInstance.CheckIfDelegated(accountAddress);
        
        // If forceDelegated is true but account isn't actually delegated, fail
        if (forceDelegated == true && !isDelegated)
        {
            GameLogger.LogError($"Account {accountAddress} is not delegated but forceDelegated is true. LoadAccountData failed.");
            return default;
        }
        
        var rpcClient = GetRpcClient(isDelegated);
        if (rpcClient == null)
        {
            GameLogger.LogError($"RPC client not available for {(isDelegated ? "delegated" : "mainnet")} endpoint");
            return default;
        }

        var result = await rpcClient.GetAccountInfoAsync(accountAddress, Commitment.Confirmed);
        if (!result.WasSuccessful || result.Result.Value == null)
        {
            GameLogger.LogWarning($"Failed to load account data for {accountAddress}");
            return default;
        }

        if (deserializer != null && result.Result.Value.Data?.Count > 0)
        {
            try
            {
                byte[] data = Convert.FromBase64String(result.Result.Value.Data[0]);
                return deserializer(data);
            }
            catch (Exception ex)
            {
                GameLogger.LogWarning($"Failed to deserialize account data for {accountAddress}: {ex.Message}");
                return default;
            }
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
            GameLogger.LogWarning($"No subscription found for {subscriptionId}");
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
            GameLogger.Log($"✓ Unsubscribed from {subscriptionId}");
            return true;
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Failed to unsubscribe from {subscriptionId}: {ex.Message}");
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
        GameLogger.Log($"✓ Unsubscribed from all {subscriptionIds.Count} subscriptions");
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
        // For delegated accounts, use ephemeral wallet's streaming client
        if (isDelegated)
        {
            var ephemeralWallet = SolanaManager.EphemeralWallet;
            if (ephemeralWallet == null)
            {
                GameLogger.Log("Skipping subscription: ephemeral wallet not available");
                return null;
            }
            if (ephemeralWallet.ActiveStreamingRpcClient.State != WebSocketState.Open)
            {
                GameLogger.Log("Waiting for ephemeral WebSocket connection...");
                await ephemeralWallet.AwaitWsRpcConnection();
            }
            return ephemeralWallet.ActiveStreamingRpcClient;
        }

        // For mainnet - always use standalone streaming client (never user's wallet)
        return await GetOrCreateStandaloneStreamingClient();
    }

    private async Task<IStreamingRpcClient> GetOrCreateStandaloneStreamingClient()
    {
        // Already connected
        if (_standaloneStreamingClient?.State == WebSocketState.Open)
            return _standaloneStreamingClient;

        // Already connecting - wait for it
        if (_standaloneClientConnecting)
        {
            while (_standaloneClientConnecting)
                await Task.Delay(50);
            return _standaloneStreamingClient?.State == WebSocketState.Open ? _standaloneStreamingClient : null;
        }

        // Get WebSocket URL from Web3 configuration
        if (Web3.Instance == null)
        {
            GameLogger.LogError("Web3.Instance not initialized - cannot create standalone streaming client");
            return null;
        }

        // Derive WebSocket URL from RPC URL
        string wsUrl = GetWebSocketUrl();
        if (string.IsNullOrEmpty(wsUrl))
        {
            GameLogger.LogError("Could not determine WebSocket URL for standalone streaming client");
            return null;
        }

        _standaloneClientConnecting = true;
        try
        {
            GameLogger.Log($"Creating standalone streaming client: {wsUrl}");
            _standaloneStreamingClient = ClientFactory.GetStreamingClient(wsUrl);
            await _standaloneStreamingClient.ConnectAsync();
            GameLogger.Log("✓ Standalone streaming client connected");
            return _standaloneStreamingClient;
        }
        catch (Exception ex)
        {
            GameLogger.LogError($"Failed to connect standalone streaming client: {ex.Message}");
            _standaloneStreamingClient = null;
            return null;
        }
        finally
        {
            _standaloneClientConnecting = false;
        }
    }

    private string GetWebSocketUrl()
    {
        // Try to get the WebSocket URL from Web3's RPC configuration
        // The RPC URL is typically https://... and WS is wss://...
        string rpcUrl = Web3.Rpc?.NodeAddress?.AbsoluteUri;
        if (string.IsNullOrEmpty(rpcUrl))
            return null;

        // Convert HTTP(S) to WS(S)
        if (rpcUrl.StartsWith("https://"))
            return "wss://" + rpcUrl.Substring(8).TrimEnd('/');
        if (rpcUrl.StartsWith("http://"))
            return "ws://" + rpcUrl.Substring(7).TrimEnd('/');

        return null;
    }

    private IRpcClient GetRpcClient(bool isDelegated)
    {
        if (isDelegated)
            return SolanaManager.EphemeralWallet?.ActiveRpcClient;
        
        // Web3.Rpc is available before wallet connection, Web3.Wallet requires login
        return Web3.Wallet?.ActiveRpcClient ?? Web3.Rpc;
    }

    private async void HandleAccountUpdate<T>(
        string accountAddress,
        ResponseValue<AccountInfo> accountInfo,
        Action<T> callback,
        Func<byte[], T> deserializer)
    {
        try
        {
            GameLogger.Log($"📡 Account updated: {accountAddress}");

            // Switch to main thread for Unity operations
            await UnityMainThreadDispatcher.Instance().EnqueueAsync(async () =>
            {
                T result = default;

                if (accountInfo.Value?.Data?.Count > 0 && deserializer != null)
                {
                    try
                    {
                        byte[] data = Convert.FromBase64String(accountInfo.Value.Data[0]);
                        result = deserializer(data);
                    }
                    catch (Exception ex)
                    {
                        GameLogger.LogWarning($"Failed to deserialize account update for {accountAddress}: {ex.Message}");
                        return;
                    }
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
            GameLogger.LogError($"Error handling account update for {accountAddress}: {ex.Message}");
        }
    }

    private async void OnDestroy()
    {
        await UnsubscribeAll();
        
        // Cleanup standalone streaming client
        if (_standaloneStreamingClient != null)
        {
            try
            {
                await _standaloneStreamingClient.DisconnectAsync();
            }
            catch { /* ignore cleanup errors */ }
            _standaloneStreamingClient = null;
        }
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

