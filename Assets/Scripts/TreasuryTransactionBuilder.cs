using UnityEngine;
using System.Threading.Tasks;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using Treasury;
using Treasury.Program;
using Treasury.Accounts;
using Treasury.Types;
using System;
using System.Text;
using System.Collections.Generic;

public class TreasuryTransactionBuilder : MonoBehaviour
{
    // Common Solana Program IDs
    public static readonly PublicKey SYSTEM_PROGRAM_ID = SystemProgram.ProgramIdKey;
    public static readonly PublicKey TOKEN_PROGRAM_ID = TokenProgram.ProgramIdKey;
    public static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = AssociatedTokenAccountProgram.ProgramIdKey;
    public static readonly PublicKey DELEGATION_PROGRAM_ID = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");

    // References
    [SerializeField] private SolanaManager solanaManager;

    // Clients
    private static PublicKey _programId = new PublicKey(TreasuryProgram.ID);
    private TreasuryClient Client => solanaManager?.GetTreasuryClient();
    


    private Account CurrentUser() => Web3.Account;
    private PublicKey CurrentUserPk() => CurrentUser()?.PublicKey;

    // PDA helpers (match Treasury IDL)
    public static PublicKey DeriveSolanaBalanceAccount(PublicKey player)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("SOLANA_BALANCE"), player.KeyBytes }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveEphemeralBalanceAccount(PublicKey player)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("EPHEMERAL_BALANCE"), player.KeyBytes }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveTreasuryAccount()
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("TREASURY") }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveTreasuryTokenAccount()
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

    private static DelegateParams BuildDelegateParams(uint commitFrequencyMs, PublicKey validator)
    {
        return new DelegateParams
        {
            CommitFrequencyMs = commitFrequencyMs,
            Validator = validator
        };
    }

    // Fetch helpers
    public async Task<SolanaBalance> GetSolanaBalance(PublicKey player)
    {
        if (Client == null) return null;
        var pda = DeriveSolanaBalanceAccount(player);
        var res = await Client.GetSolanaBalanceAsync(pda.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<EphemeralBalance> GetEphemeralBalance(PublicKey player)
    {
        if (Client == null) return null;
        var pda = DeriveEphemeralBalanceAccount(player);
        var res = await Client.GetEphemeralBalanceAsync(pda.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<Treasury.Accounts.Treasury> GetTreasuryData()
    {
        if (Client == null) return null;
        var treasury = DeriveTreasuryAccount();
        var res = await Client.GetTreasuryAsync(treasury.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    // High-level actions (Treasury IDL)
    public TransactionInstruction InitializeTreasury()
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var treasury = DeriveTreasuryAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new InitializeTreasuryAccounts
        {
            Signer = signer,
            Treasury = treasury,
            TokenMint = solanaManager.GetMintPublicKey(),
            TreasuryTokenAccount = treasuryTokenAccount,
            TokenProgram = TOKEN_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        var ix = TreasuryProgram.InitializeTreasury(accounts);
        return ix;
    }

    public TransactionInstruction InitializePlayerBalance()
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var solanaBalance = DeriveSolanaBalanceAccount(signer);
        var ephemeralBalance = DeriveEphemeralBalanceAccount(signer);

        var accounts = new InitializeBalanceAccounts
        {
            Signer = signer,
            SystemProgram = SYSTEM_PROGRAM_ID,
            EphemeralBalance = ephemeralBalance,
            SolanaBalance = solanaBalance
        };

        var ix = TreasuryProgram.InitializeBalance(accounts);
        return ix;
    }

    public TransactionInstruction DelegatePlayerBalance(uint commitFrequencyMs = 1000, PublicKey validator = null)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var ephemeralBalance = DeriveEphemeralBalanceAccount(signer);
        var delegationMetadata = DeriveDelegationMetadataAccount(ephemeralBalance);
        var delegationRecord = DeriveDelegationRecordAccount(ephemeralBalance);
        var delegationBuffer = DeriveDelegationBufferAccount(ephemeralBalance);
        
        var accounts = new DelegateEphemeralBalanceAccounts
        {
            Signer = signer,
            BufferEphemeralBalance = delegationBuffer,
            DelegationRecordEphemeralBalance = delegationRecord,
            DelegationMetadataEphemeralBalance = delegationMetadata,
            EphemeralBalance = ephemeralBalance,
            OwnerProgram = _programId,
            DelegationProgram = DELEGATION_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        var @params = BuildDelegateParams(commitFrequencyMs, validator);
        var ix = TreasuryProgram.DelegateEphemeralBalance(accounts, @params);
        return ix;
    }

    public TransactionInstruction Deposit(ulong amount)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var solanaBalance = DeriveSolanaBalanceAccount(signer);
        var userAta = DeriveUserTokenAccount(signer, solanaManager.GetMintPublicKey());
        var treasury = DeriveTreasuryAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new UserDepositAccounts
        {
            Signer = signer,
            SolanaBalance = solanaBalance,
            TokenMint = solanaManager.GetMintPublicKey(),
            UserTokenAccount = userAta,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
            AssociatedTokenProgram = ASSOCIATED_TOKEN_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        var ix = TreasuryProgram.UserDeposit(accounts, amount);
        return ix;
    }

    public TransactionInstruction Withdraw(ulong amount)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var solanaBalance = DeriveSolanaBalanceAccount(signer);
        var ephemeralBalance = DeriveEphemeralBalanceAccount(signer);
        var userAta = DeriveUserTokenAccount(signer, solanaManager.GetMintPublicKey());
        var treasury = DeriveTreasuryAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new UserWithdrawAccounts
        {
            Signer = signer,
            EphemeralBalance = ephemeralBalance,
            SolanaBalance = solanaBalance,
            TokenMint = solanaManager.GetMintPublicKey(),
            UserTokenAccount = userAta,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
            AssociatedTokenProgram = ASSOCIATED_TOKEN_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        // amount is ignored for UserWithdraw in updated IDL
        var ix = TreasuryProgram.UserWithdraw(accounts);
        return ix;
    }

    public TransactionInstruction CreditPlayer(ulong amount, PublicKey authority)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var ephemeralBalance = DeriveEphemeralBalanceAccount(signer);

        var accounts = new CreditPlayerAccounts
        {
            Signer = signer,
            EphemeralBalance = ephemeralBalance,
            Authority = authority
            // MagicProgram and MagicContext use defaults from generated client
        };

        var ix = TreasuryProgram.CreditPlayer(accounts, amount);
        return ix;
    }

    public TransactionInstruction DebitPlayer(ulong amount, PublicKey authority)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var ephemeralBalance = DeriveEphemeralBalanceAccount(signer);

        var accounts = new DebitPlayerAccounts
        {
            Signer = signer,
            EphemeralBalance = ephemeralBalance,
            Authority = authority
            // MagicProgram and MagicContext use defaults from generated client
        };

        var ix = TreasuryProgram.DebitPlayer(accounts, amount);
        return ix;
    }
}

