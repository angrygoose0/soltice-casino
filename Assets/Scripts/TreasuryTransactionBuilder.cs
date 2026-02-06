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
    public static readonly PublicKey TOKEN_2022_PROGRAM_ID = new PublicKey("TokenzQdBNbLqP5VEhdkAS6EPFLC1PHnBqCXEpPxuEb");
    public static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = AssociatedTokenAccountProgram.ProgramIdKey;

    // References
    [SerializeField] private SolanaManager solanaManager;

    // Clients
    private static PublicKey _programId = new PublicKey(TreasuryProgram.ID);
    private TreasuryClient Client => solanaManager?.TreasuryClient;
    


    private Account CurrentUser() => Web3.Account;
    private PublicKey CurrentUserPk() => CurrentUser()?.PublicKey;

    // PDA helpers (match Treasury IDL)
    public static PublicKey DeriveUserBalanceAccount(PublicKey player)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("USER_BALANCE"), player.KeyBytes }, _programId, out PublicKey pda, out _);
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
        // Secondary seed derived from 17293822569102704606u64.to_le_bytes() to get PDA starting with "BANK"
        byte[] seed2 = new byte[] { 0xde, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef };
        PublicKey.TryFindProgramAddress(new[] { seed1, seed2 }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveTreasuryConfigAccount()
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("TREASURY_CONFIG") }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveUserTokenAccount(PublicKey user, PublicKey mint)
    {
        // Derive ATA for Token-2022 (uses Token-2022 program ID in seeds)
        PublicKey.TryFindProgramAddress(
            new[] { user.KeyBytes, TOKEN_2022_PROGRAM_ID.KeyBytes, mint.KeyBytes },
            ASSOCIATED_TOKEN_PROGRAM_ID,
            out PublicKey pda,
            out _
        );
        return pda;
    }

    public static PublicKey DeriveDelegationMetadataAccount(PublicKey delegatedAccount)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("delegation-metadata"), delegatedAccount.KeyBytes }, SolanaManager.DELEGATION_PROGRAM_ID, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveDelegationRecordAccount(PublicKey delegatedAccount)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("delegation"), delegatedAccount.KeyBytes }, SolanaManager.DELEGATION_PROGRAM_ID, out PublicKey pda, out _);
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
    public async Task<UserBalance> GetUserBalance(PublicKey player)
    {
        if (Client == null) return null;
        var pda = DeriveUserBalanceAccount(player);
        var res = await Client.GetUserBalanceAsync(pda.ToString(), Commitment.Confirmed);
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
        var treasuryConfig = DeriveTreasuryConfigAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new InitializeTreasuryAccounts
        {
            Signer = signer,
            Treasury = treasury,
            TreasuryConfig = treasuryConfig,
            TreasuryTokenAccount = treasuryTokenAccount,
            TokenProgram = TOKEN_2022_PROGRAM_ID,
        };

        var ix = TreasuryProgram.InitializeTreasury(accounts);
        return ix;
    }

    public TransactionInstruction InitializeBalance()
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);

        var accounts = new InitializeBalanceAccounts
        {
            Signer = signer,
            UserBalance = userBalance
        };

        var ix = TreasuryProgram.InitializeBalance(accounts);
        return ix;
    }

    public TransactionInstruction DelegateUserBalance(uint commitFrequencyMs = 1000, PublicKey validator = null)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);
        var delegationMetadata = DeriveDelegationMetadataAccount(userBalance);
        var delegationRecord = DeriveDelegationRecordAccount(userBalance);
        var delegationBuffer = DeriveDelegationBufferAccount(userBalance);
        
        // Hardcode validator address
        validator = new PublicKey("MAS1Dt9qreoRMQ14YQuhg8UTZMMzDdKhmkZMECCzk57");
        
        var accounts = new DelegateUserBalanceAccounts
        {
            Signer = signer,
            BufferUserBalance = delegationBuffer,
            DelegationRecordUserBalance = delegationRecord,
            DelegationMetadataUserBalance = delegationMetadata,
            UserBalance = userBalance,
        };

        var @params = BuildDelegateParams(commitFrequencyMs, validator);
        var ix = TreasuryProgram.DelegateUserBalance(accounts, @params);
        return ix;
    }

    public TransactionInstruction UndelegateUserBalance()
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);
        
        var accounts = new UndelegateUserBalanceAccounts
        {
            Signer = signer,
            UserBalance = userBalance,
        };

        var ix = TreasuryProgram.UndelegateUserBalance(accounts);
        return ix;
    }

    public TransactionInstruction DepositTokens(ulong amount)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);
        var userAta = DeriveUserTokenAccount(signer, solanaManager.MintPublicKey);
        var treasuryConfig = DeriveTreasuryConfigAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new DepositTokensAccounts
        {
            Signer = signer,
            UserBalance = userBalance,
            UserTokenAccount = userAta,
            TreasuryConfig = treasuryConfig,
            TreasuryTokenAccount = treasuryTokenAccount,
            TokenProgram = TOKEN_2022_PROGRAM_ID,
        };

        // amount is already in lamports
        var ix = TreasuryProgram.DepositTokens(accounts, amount);
        return ix;
    }

    public TransactionInstruction WithdrawTokens()
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);
        var userAta = DeriveUserTokenAccount(signer, solanaManager.MintPublicKey);
        var treasury = DeriveTreasuryAccount();
        var treasuryConfig = DeriveTreasuryConfigAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new WithdrawTokensAccounts
        {
            Signer = signer,
            UserBalance = userBalance,
            UserTokenAccount = userAta,
            Treasury = treasury,
            TreasuryConfig = treasuryConfig,
            TreasuryTokenAccount = treasuryTokenAccount,
            TokenProgram = TOKEN_2022_PROGRAM_ID,
        };

        var ix = TreasuryProgram.WithdrawTokens(accounts);
        return ix;
    }

    public TransactionInstruction RequestWithdraw(ulong amount)
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);
        var treasury = DeriveTreasuryAccount();

        var accounts = new RequestWithdrawAccounts
        {
            Signer = signer,
            UserBalance = userBalance,
            Treasury = treasury,
        };

        // amount is already in lamports
        var ix = TreasuryProgram.RequestWithdraw(accounts, amount);
        return ix;
    }

    public TransactionInstruction ApplyDeposit()
    {
        if (CurrentUser() == null) return null;

        var signer = CurrentUserPk();
        var userBalance = DeriveUserBalanceAccount(signer);
        var treasury = DeriveTreasuryAccount();

        var accounts = new ApplyDepositAccounts
        {
            Signer = signer,
            UserBalance = userBalance,
            Treasury = treasury,
        };

        var ix = TreasuryProgram.ApplyDeposit(accounts);
        return ix;
    }
}

