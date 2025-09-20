using UnityEngine;
using System.Threading.Tasks;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using Crash;
using Crash.Program;
using Crash.Accounts;
using System;
using System.Text;
using System.Collections.Generic;

public class CrashTransactionBuilder : MonoBehaviour
{
    // Common Solana Program IDs
    public static readonly PublicKey SYSTEM_PROGRAM_ID = SystemProgram.ProgramIdKey;
    public static readonly PublicKey TOKEN_PROGRAM_ID = TokenProgram.ProgramIdKey;
    public static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = AssociatedTokenAccountProgram.ProgramIdKey;
    public static readonly PublicKey NATIVE_MINT = new PublicKey("So11111111111111111111111111111111111111112");

    // References
    [SerializeField] private SolanaManager solanaManager;

    // Clients
    private CrashClient _client;
    private static PublicKey _programId = new PublicKey(CrashProgram.ID);


    private void Awake()
    {
        if (solanaManager == null)
        {
            solanaManager = FindObjectOfType<SolanaManager>();
        }
    }

    private void OnEnable()
    {
        TryInitClient();
        Web3.OnLogin += _ => TryInitClient();
    }

    private void OnDisable()
    {
        Web3.OnLogin -= _ => TryInitClient();
    }

    private void TryInitClient()
    {
        try
        {
            _client = solanaManager?.GetCrashClient();
        }
        catch (Exception ex)
        {
            Debug.LogError($"CrashTransactionBuilder: failed to init client: {ex.Message}");
        }
    }

    private async Task<bool> EnsureClientReady()
    {
        if (_client != null) return true;

        // First attempt: fetch existing client reference
        TryInitClient();
        if (_client != null) return true;

        // If still null and wallet is connected, ask SolanaManager to initialize
        if (solanaManager != null && Web3.Instance?.WalletBase != null)
        {
            solanaManager.InitializeClients();
            await Task.Delay(200);
            TryInitClient();
            if (_client != null) return true;
        }

        Debug.LogWarning("CrashTransactionBuilder: client not initialized yet; join will be ignored");
        return false;
    }

    private Account CurrentUser() => Web3.Account;
    private PublicKey CurrentUserPk() => CurrentUser()?.PublicKey;

    // PDA helpers
    public static PublicKey DeriveGameAccount()
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("GAME") }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DerivePlayerBetAccount(PublicKey player)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("PLAYER_BET"), player.KeyBytes }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveTokenTreasuryAccount()
    {
        byte[] seed1 = Encoding.UTF8.GetBytes("TOKEN");
        byte[] seed2 = Encoding.UTF8.GetBytes("f2b1af7922723e1636a0614e0c24a5eb");
        PublicKey.TryFindProgramAddress(new[] { seed1, seed2 }, _programId, out PublicKey pda, out _);
        return pda;
    }


    public static PublicKey DeriveUserTokenAccount(PublicKey user, PublicKey mint)
    {
        return AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(user, mint);
    }

    private PublicKey GetTokenMint() => solanaManager != null ? solanaManager.GetMintPublicKey() : null;

    // Fetch helpers
    public async Task<Game> GetGameData(PublicKey game)
    {
        if (_client == null) return null;
        var res = await _client.GetGameAsync(game.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<PlayerBet> GetPlayerBet(PublicKey player)
    {
        if (_client == null) return null;
        var res = await _client.GetPlayerBetAsync(player.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    // Transaction sending via SolanaManager
    private Task<string> SendTx(uint cuLimit, ulong cuPrice, params TransactionInstruction[] ix)
    {
        if (solanaManager == null)
        {
            Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
            return Task.FromResult<string>(null);
        }
        return solanaManager.SendAndConfirmTransaction(false, cuLimit, cuPrice, ix);
    }

    // High-level actions
    public async Task<string> InitializeGame()
    {
        if (CurrentUser() == null) return null;
        if (!await EnsureClientReady()) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var accounts = new InitializeAccounts
        {
            Game = game,
            Signer = user,
            SystemProgram = SYSTEM_PROGRAM_ID
        };
        var ix = CrashProgram.Initialize(accounts);
        return await SendTx(0u, 0ul, ix);
    }

    public async Task<string> InitializeTreasury()
    {
        if (_client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var treasury = DeriveTokenTreasuryAccount();
        var tokenMint = GetTokenMint()

        var accounts = new InitializeTreasuryAccounts
        {
            Signer = user,
            TokenMint = tokenMint,
            Game = game,
            Treasury = treasury,
            SystemProgram = SYSTEM_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID
        };
        var ix = CrashProgram.InitializeTreasury(accounts);
        return await SendTx(0u, 0ul, ix);
    }

    public async Task<string> SetupTick(PublicKey randomnessAccount)
    {
        if (_client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var accounts = new SetupTickAccounts
        {
            Signer = user,
            Game = game,
            RandomnessAccountData = randomnessAccount
        };
        var ix = CrashProgram.SetupTick(accounts, randomnessAccount);
        return await SendTx(0u, 0ul, ix);
    }

    public async Task<string> AdvanceTick(PublicKey randomnessAccount)
    {
        if (_client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var accounts = new AdvanceTickAccounts
        {
            Signer = user,
            Game = game,
            RandomnessAccountData = randomnessAccount
        };
        var ix = CrashProgram.AdvanceTick(accounts);
        return await SendTx(0u, 0ul, ix);
    }

    public async Task<string> PlaceBet(ulong amountTokens)
    {
        if (_client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var tokenMint = GetTokenMint();
        var userAta = DeriveUserTokenAccount(user, tokenMint);
        var playerBet = DerivePlayerBetAccount(user);
        var treasury = DeriveTokenTreasuryAccount();

        var accounts = new PlaceBetAccounts
        {
            Signer = user,
            Game = game,
            TokenMint = tokenMint,
            UserTokenAccount = userAta,
            PlayerBet = playerBet,
            Treasury = treasury,
            SystemProgram = SYSTEM_PROGRAM_ID,
            AssociatedTokenProgram = ASSOCIATED_TOKEN_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID
        };

        var ix = CrashProgram.PlaceBet(accounts, amountTokens);
        return await SendTx(0u, 0ul, ix);
    }

    public async Task<string> ClaimBet()
    {
        if (_client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var playerBet = DerivePlayerBetAccount(user);
        var treasury = DeriveTokenTreasuryAccount();
        var tokenMint = GetTokenMint();
        var userAta = DeriveUserTokenAccount(user, tokenMint);

        var accounts = new ClaimBetAccounts
        {
            Signer = user,
            Game = game,
            PlayerBet = playerBet,
            Treasury = treasury,
            TokenMint = tokenMint,
            UserTokenAccount = userAta,
            SystemProgram = SYSTEM_PROGRAM_ID,
            AssociatedTokenProgram = ASSOCIATED_TOKEN_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID
        };
        var ix = CrashProgram.ClaimBet(accounts);
        return await SendTx(0u, 0ul, ix);
    }
}

