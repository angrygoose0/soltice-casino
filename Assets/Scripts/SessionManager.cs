using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Solana.Unity.SessionKeys.GplSession.Accounts;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using UnityEngine;
using Crash;
using Crash.Program;
using Random = UnityEngine.Random;


public class SessionManager : MonoBehaviour
{
    // Session-related fields
    public static SessionToken SessionToken;
    public static SessionWallet SessionWallet;
    public static long SessionValidUntil;
    
    // Constants
    private const string SessionPwdPrefKey = nameof(SessionPwdPrefKey);
    
    
    /// <summary>
    /// Generates a random alphanumeric string of specified length
    /// </summary>
    private static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[Random.Range(0, s.Length)]).ToArray());
    }
    
    /// <summary>
    /// Refreshes the session wallet, creating a new password if needed
    /// </summary>
    private static async Task RefreshSessionWallet()
    {
        var password = PlayerPrefs.GetString(SessionPwdPrefKey, null);

        if (string.IsNullOrEmpty(password))
        {
            password = RandomString(10);
            PlayerPrefs.SetString(SessionPwdPrefKey, password);
        }

        SessionWallet = await SessionWallet.GetSessionWallet(new PublicKey(CrashProgram.ID),
            password,
            Web3.Wallet);
    }

    /// <summary>
    /// Updates and validates the current session token
    /// </summary>
    private static async Task<bool> UpdateSessionValid()
    {
        SessionToken = await RequestSessionToken();

        if (SessionToken == null) return false;

        Debug.Log("Session token valid until: " +
                    (new DateTime(1970, 1, 1)).AddSeconds(SessionToken.ValidUntil) +
                    " Now: " + DateTimeOffset.UtcNow);
        SessionValidUntil = SessionToken.ValidUntil;
        return IsSessionValid();
    }

    /// <summary>
    /// Requests a session token from the blockchain
    /// </summary>
    private static async Task<SessionToken> RequestSessionToken()
    {
        if (SessionWallet == null)
            await RefreshSessionWallet();

        var sessionTokenData =
            (await Web3.Rpc.GetAccountInfoAsync(SessionWallet.SessionTokenPDA, Commitment.Confirmed))
            .Result;

        if (sessionTokenData?.Value?.Data[0] == null)
            return null;

        var sessionToken = SessionToken.Deserialize(Convert.FromBase64String(sessionTokenData.Value.Data[0]));

        return sessionToken;
    }

    /// <summary>
    /// Checks if the current session is still valid with an optional buffer time
    /// </summary>
    /// <param name="buffer">Buffer time in seconds (default: 1 hour)</param>
    private static bool IsSessionValid(long buffer = 60 * 60)
    {
        return SessionValidUntil > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + buffer;
    }

    /// <summary>
    /// Creates a new session on the blockchain
    /// </summary>
    public static async Task CreateNewSession()
    {
        if (await UpdateSessionValid())
            return;

        if (SessionToken != null)
            await SessionWallet.CloseSession();

        var transaction = new Transaction
        {
            FeePayer = Web3.Account,
            Instructions = new List<TransactionInstruction>(),
            RecentBlockHash = await Web3.BlockHash(Commitment.Confirmed, false)
        };

        var sessionIx = SessionWallet.CreateSessionIX(
            true,
            DateTimeOffset.UtcNow.AddDays(6).ToUnixTimeSeconds(),
            1000000000);
        transaction.Add(sessionIx);
        transaction.PartialSign(new[] { Web3.Account, SessionWallet.Account });

        var res = await Web3.Wallet.SignAndSendTransaction(transaction, true, Commitment.Confirmed);

        Debug.Log("Create session wallet: " + res.RawRpcResponse);
        await Web3.Wallet.ActiveRpcClient.ConfirmTransaction(res.Result, Commitment.Confirmed);
        var sessionValid = await UpdateSessionValid();
        Debug.Log("After create session, the session is valid: " + sessionValid);
    }

    
}

//TO DO: COPY THE NEW METHOD IN SOLANA UNITY SDK AND MAKE MY OWN VERSION. MAYBE READ SOLANA UNITY CORE, BECAUSE THEY TREATE IT LIKE A SINGLETON AND DONT LET U HAVE MULTIPLE SESSIONS KEYS AT ONCE.


