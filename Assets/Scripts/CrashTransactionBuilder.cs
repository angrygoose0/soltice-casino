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
using Crash.Types;
using Treasury.Program;
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
    private static PublicKey _programId = new PublicKey(CrashProgram.ID);
    private CrashClient Client => solanaManager?.GetCrashClient();


    private void Awake()
    {
        if (solanaManager == null)
        {
            solanaManager = FindObjectOfType<SolanaManager>();
        }
    }

    // Builders do not initialize clients; SolanaManager owns lifecycle

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

	public static PublicKey DeriveAuthorityAccount()
	{
		PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("AUTHORITY") }, _programId, out PublicKey pda, out _);
		return pda;
	}

	// Program identity PDA for VRF requests (seed: "identity")
	public static PublicKey DeriveProgramIdentity()
	{
		PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("identity") }, _programId, out PublicKey pda, out _);
		return pda;
	}

    public static PublicKey DeriveDelegationMetadataAccount(PublicKey delegatedAccount)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("delegation_metadata"), delegatedAccount.KeyBytes }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveDelegationRecordAccount(PublicKey delegatedAccount)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("delegation"), delegatedAccount.KeyBytes }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveDelegationBufferAccount(PublicKey delegatedAccount)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("buffer"), delegatedAccount.KeyBytes }, _programId, out PublicKey pda, out _);
        return pda;
    }

    // Fetch helpers
    public async Task<Game> GetGameData(PublicKey game)
    {
        if (Client == null) return null;
        var res = await Client.GetGameAsync(game.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<PlayerBet> GetPlayerBet(PublicKey player)
    {
        if (Client == null) return null;
        var playerBetPda = DerivePlayerBetAccount(player);
        var res = await Client.GetPlayerBetAsync(playerBetPda.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    // High-level actions
    public async Task<string> InitializeGame()
    {
        if (CurrentUser() == null || Client == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var accounts = new InitializeGameAccounts
        {
            Game = game,
            Signer = user,
            SystemProgram = SYSTEM_PROGRAM_ID
        };
        var ix = CrashProgram.InitializeGame(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

	public async Task<string> InitializeAuthority()
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var authority = DeriveAuthorityAccount();
		var accounts = new InitializeAuthorityAccounts
		{
			Signer = user,
			Authority = authority,
			SystemProgram = SYSTEM_PROGRAM_ID
		};
		var ix = CrashProgram.InitializeAuthority(accounts);
		if (solanaManager == null)
		{
			Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
			return null;
		}
		return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	public async Task<string> InitializePlayerBet()
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var playerBet = DerivePlayerBetAccount(user);
		var accounts = new InitializePlayerBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			SystemProgram = SYSTEM_PROGRAM_ID
		};
		var ix = CrashProgram.InitializePlayerBet(accounts);
		if (solanaManager == null)
		{
			Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
			return null;
		}
		return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	public async Task<string> StartGame()
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var accounts = new StartGameAccounts
		{
			Signer = user,
			Game = game
		};
        var ix = CrashProgram.StartGame(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	private static DelegateParams BuildDelegateParams(uint commitFrequencyMs, PublicKey validator)
	{
		return new DelegateParams
		{
			CommitFrequencyMs = commitFrequencyMs,
			Validator = validator
		};
	}

	public async Task<string> DelegateAuthority(uint commitFrequencyMs = 1000, PublicKey validator = null)
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var authority = DeriveAuthorityAccount();
		var delegationMetadata = DeriveDelegationMetadataAccount(authority);
		var delegationRecord = DeriveDelegationRecordAccount(authority);
		var delegationBuffer = DeriveDelegationBufferAccount(authority);
		var accounts = new DelegateAuthorityAccounts
		{
			Signer = user,
			Authority = authority,
			DelegationMetadataAuthority = delegationMetadata,
			DelegationRecordAuthority = delegationRecord,
			BufferAuthority = delegationBuffer
		};
		var @params = BuildDelegateParams(commitFrequencyMs, validator);
		var ix = CrashProgram.DelegateAuthority(accounts, @params);
		if (solanaManager == null)
		{
			Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
			return null;
		}
		return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	public async Task<string> DelegateGame(uint commitFrequencyMs = 1000, PublicKey validator = null)
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var delegationMetadata = DeriveDelegationMetadataAccount(game);
		var delegationRecord = DeriveDelegationRecordAccount(game);
		var delegationBuffer = DeriveDelegationBufferAccount(game);
		var accounts = new DelegateGameAccounts
		{
			Signer = user,
			Game = game,
			DelegationMetadataGame = delegationMetadata,
			DelegationRecordGame = delegationRecord,
			BufferGame = delegationBuffer
		};
		var @params = BuildDelegateParams(commitFrequencyMs, validator);
		var ix = CrashProgram.DelegateGame(accounts, @params);
		if (solanaManager == null)
		{
			Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
			return null;
		}
		return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	public async Task<string> DelegatePlayerBet(uint commitFrequencyMs = 1000, PublicKey validator = null)
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var playerBet = DerivePlayerBetAccount(user);
		var delegationMetadata = DeriveDelegationMetadataAccount(playerBet);
		var delegationRecord = DeriveDelegationRecordAccount(playerBet);
		var delegationBuffer = DeriveDelegationBufferAccount(playerBet);
		var accounts = new DelegatePlayerBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			DelegationMetadataPlayerBet = delegationMetadata,
			DelegationRecordPlayerBet = delegationRecord,
			BufferPlayerBet = delegationBuffer
		};
		var @params = BuildDelegateParams(commitFrequencyMs, validator);
		var ix = CrashProgram.DelegatePlayerBet(accounts, @params);
		if (solanaManager == null)
		{
			Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
			return null;
		}
		return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	public async Task<string> Tick()
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var accounts = new TickAccounts
		{
			Signer = user,
			Game = game
		};
        var ix = CrashProgram.Tick(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

	public async Task<string> RequestRandomness(byte clientSeed)
	{
		if (Client == null || CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var programIdentity = DeriveProgramIdentity();
		var accounts = new RequestRandomnessAccounts
		{
			Signer = user,
			Game = game,
			ProgramIdentity = programIdentity
		};
		var ix = CrashProgram.RequestRandomness(accounts, clientSeed);
		if (solanaManager == null)
		{
			Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
			return null;
		}
		return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
	}

    public async Task<string> PlaceBet(ulong amountTokens)
    {
        if (Client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var playerBet = DerivePlayerBetAccount(user);
		var authority = DeriveAuthorityAccount();
		var playerBalance = TreasuryTransactionBuilder.DerivePlayerBalanceAccount(user);

		var accounts = new PlaceBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			SessionToken = null,
			Game = game,
			Authority = authority,
			PlayerBalance = playerBalance,
			SystemProgram = SYSTEM_PROGRAM_ID
		};

		var ix = CrashProgram.PlaceBet(accounts, amountTokens);
        if (solanaManager == null)
        {
            Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> ClaimBet()
    {
        if (Client == null || CurrentUser() == null) return null;
        var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var playerBet = DerivePlayerBetAccount(user);
		var authority = DeriveAuthorityAccount();
		var playerBalance = TreasuryTransactionBuilder.DerivePlayerBalanceAccount(user);

		var accounts = new ClaimBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			SessionToken = null,
			Game = game,
			Authority = authority,
			PlayerBalance = playerBalance,
			SystemProgram = SYSTEM_PROGRAM_ID
		};
		var ix = CrashProgram.ClaimBet(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("CrashTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }
}

