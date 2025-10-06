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
    public static readonly PublicKey DELEGATION_PROGRAM_ID = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
    // References
    [SerializeField] private SolanaManager solanaManager;

    // Clients
    private static PublicKey _programId = new PublicKey(CrashProgram.ID);
    private CrashClient Client => solanaManager?.GetCrashClient();
    



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
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("delegation-metadata"), delegatedAccount.KeyBytes }, DELEGATION_PROGRAM_ID, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveDelegationRecordAccount(PublicKey delegatedAccount)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("delegation"), delegatedAccount.KeyBytes }, DELEGATION_PROGRAM_ID, out PublicKey pda, out _);
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
    public TransactionInstruction InitializeGame()
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var game = DeriveGameAccount();
        var accounts = new InitializeGameAccounts
        {
            Game = game,
            Signer = user,
            SystemProgram = SYSTEM_PROGRAM_ID
        };
        var ix = CrashProgram.InitializeGame(accounts);
        return ix;
    }

    public TransactionInstruction InitializeAuthority()
	{
        if (CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var authority = DeriveAuthorityAccount();
		var accounts = new InitializeAuthorityAccounts
		{
			Signer = user,
			Authority = authority,
			SystemProgram = SYSTEM_PROGRAM_ID
		};
        var ix = CrashProgram.InitializeAuthority(accounts);
        return ix;
	}

    public TransactionInstruction InitializePlayerBet()
	{
        if (CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var playerBet = DerivePlayerBetAccount(user);
		var game = DeriveGameAccount();
		var accounts = new InitializePlayerBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			Game = game,
			SystemProgram = SYSTEM_PROGRAM_ID
		};
        var ix = CrashProgram.InitializePlayerBet(accounts);
        return ix;
	}

    public TransactionInstruction StartGame()
	{
        if (CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var accounts = new StartGameAccounts
		{
			Signer = user,
			Game = game
		};
        var ix = CrashProgram.StartGame(accounts);
        return ix;
	}

	private static DelegateParams BuildDelegateParams(uint commitFrequencyMs, PublicKey validator)
	{
		return new DelegateParams
		{
			CommitFrequencyMs = commitFrequencyMs,
			Validator = validator
		};
	}

    public TransactionInstruction DelegateAuthority(uint commitFrequencyMs = 1000, PublicKey validator = null)
	{
        if (CurrentUser() == null) return null;
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
        return ix;
	}

    public TransactionInstruction DelegateGame(uint commitFrequencyMs = 1000, PublicKey validator = null)
	{
        if (CurrentUser() == null) return null;
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
        return ix;
	}

    public TransactionInstruction DelegatePlayerBet(uint commitFrequencyMs = 1000, PublicKey validator = null)
	{
        if (CurrentUser() == null) return null;
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
        return ix;
	}

    public TransactionInstruction Tick()
	{
        if (CurrentUser() == null) return null;
		var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var accounts = new TickAccounts
		{
			Signer = user,
			Game = game
		};
        var ix = CrashProgram.Tick(accounts);
        return ix;
	}

    public TransactionInstruction RequestRandomness(byte clientSeed)
	{
        if (CurrentUser() == null) return null;
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
        return ix;
	}

    public TransactionInstruction PlaceBet(ulong amountTokens)
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var playerBet = DerivePlayerBetAccount(user);
		var authority = DeriveAuthorityAccount();
        var ephemeralBalance = TreasuryTransactionBuilder.DeriveEphemeralBalanceAccount(user);

		var accounts = new PlaceBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			SessionToken = null,
			Game = game,
			Authority = authority,
			EphemeralBalance = ephemeralBalance,
			SystemProgram = SYSTEM_PROGRAM_ID
			// TreasuryProgram, MagicProgram, MagicContext use defaults from generated client
		};

        var ix = CrashProgram.PlaceBet(accounts, amountTokens);
        return ix;
    }

    public TransactionInstruction ClaimBet()
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
		var game = DeriveGameAccount();
		var playerBet = DerivePlayerBetAccount(user);
		var authority = DeriveAuthorityAccount();
        var ephemeralBalance = TreasuryTransactionBuilder.DeriveEphemeralBalanceAccount(user);

		var accounts = new ClaimBetAccounts
		{
			Signer = user,
			PlayerBet = playerBet,
			SessionToken = null,
			Game = game,
			Authority = authority,
			EphemeralBalance = ephemeralBalance,
			SystemProgram = SYSTEM_PROGRAM_ID
			// TreasuryProgram, MagicProgram, MagicContext use defaults from generated client
		};
        var ix = CrashProgram.ClaimBet(accounts);
        return ix;
    }
}

