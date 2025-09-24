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
using System;
using System.Text;
using System.Collections.Generic;

public class TreasuryTransactionBuilder : MonoBehaviour
{
    // Common Solana Program IDs
    public static readonly PublicKey SYSTEM_PROGRAM_ID = SystemProgram.ProgramIdKey;
    public static readonly PublicKey TOKEN_PROGRAM_ID = TokenProgram.ProgramIdKey;
    public static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = AssociatedTokenAccountProgram.ProgramIdKey;
    public static readonly PublicKey NATIVE_MINT = new PublicKey("So11111111111111111111111111111111111111112");

    // References
    [SerializeField] private SolanaManager solanaManager;

    // Clients
    private static PublicKey _programId = new PublicKey(TreasuryProgram.ID);
    private TreasuryClient Client => solanaManager?.GetTreasuryClient();

    private void Awake()
    {
        if (solanaManager == null)
        {
            solanaManager = FindObjectOfType<SolanaManager>();
        }
    }

    private Account CurrentUser() => Web3.Account;
    private PublicKey CurrentUserPk() => CurrentUser()?.PublicKey;

    // PDA helpers (match Treasury IDL)
    public static PublicKey DerivePlayerBalanceAccount(PublicKey player)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("PLAYER_BALANCE"), player.KeyBytes }, _programId, out PublicKey pda, out _);
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
    public async Task<PlayerBalance> GetPlayerBalance(PublicKey player)
    {
        if (Client == null) return null;
        var pda = DerivePlayerBalanceAccount(player);
        var res = await Client.GetPlayerBalanceAsync(pda.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<Treasury.Accounts.Treasury> GetTreasuryData()
    {
        if (Client == null) return null;
        var treasury = DeriveTreasuryAccount();
        var res = await Client.GetTreasuryAsync(treasury.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    // No local send wrapper; call SolanaManager directly

    // High-level actions (Treasury IDL)
    public async Task<string> InitializeTreasury()
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var treasury = DeriveTreasuryAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new InitializeTreasuryAccounts
        {
            Signer = signer,
            Treasury = treasury,
            TokenMint = NATIVE_MINT,
            TreasuryTokenAccount = treasuryTokenAccount,
            TokenProgram = TOKEN_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        var ix = TreasuryProgram.InitializeTreasury(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> InitializePlayerBalance()
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);

        var accounts = new InitializePlayerBalanceAccounts
        {
            Signer = signer,
            SystemProgram = SYSTEM_PROGRAM_ID,
            PlayerBalance = playerBalance
        };

        var ix = TreasuryProgram.InitializePlayerBalance(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> DelegatePlayerBalance()
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);
        var delegationMetadata = DeriveDelegationMetadataAccount(playerBalance);
        var delegationRecord = DeriveDelegationRecordAccount(playerBalance);
        var delegationBuffer = DeriveDelegationBufferAccount(playerBalance);
        var accounts = new DelegatePlayerBalanceAccounts
        {
            Signer = signer,
            PlayerBalance = playerBalance,
            DelegationMetadataPlayerBalance = delegationMetadata,
            DelegationRecordPlayerBalance = delegationRecord,
            BufferPlayerBalance = delegationBuffer
        };

        var ix = TreasuryProgram.DelegatePlayerBalance(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> UndelegatePlayerBalance()
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);

        var accounts = new UndelegatePlayerBalanceAccounts
        {
            Signer = signer,
            PlayerBalance = playerBalance
        };

        var ix = TreasuryProgram.UndelegatePlayerBalance(accounts);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> Deposit(ulong amount)
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);
        var userAta = DeriveUserTokenAccount(signer, NATIVE_MINT);
        var treasury = DeriveTreasuryAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new DepositAccounts
        {
            Signer = signer,
            PlayerBalance = playerBalance,
            TokenMint = NATIVE_MINT,
            UserTokenAccount = userAta,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
            AssociatedTokenProgram = ASSOCIATED_TOKEN_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        var ix = TreasuryProgram.Deposit(accounts, amount);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> Withdraw(ulong amount)
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);
        var userAta = DeriveUserTokenAccount(signer, NATIVE_MINT);
        var treasury = DeriveTreasuryAccount();
        var treasuryTokenAccount = DeriveTreasuryTokenAccount();

        var accounts = new WithdrawAccounts
        {
            Signer = signer,
            PlayerBalance = playerBalance,
            TokenMint = NATIVE_MINT,
            UserTokenAccount = userAta,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
            AssociatedTokenProgram = ASSOCIATED_TOKEN_PROGRAM_ID,
            TokenProgram = TOKEN_PROGRAM_ID,
            SystemProgram = SYSTEM_PROGRAM_ID
        };

        var ix = TreasuryProgram.Withdraw(accounts, amount);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> CreditPlayer(ulong amount, PublicKey authority)
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);

        var accounts = new CreditPlayerAccounts
        {
            Signer = signer,
            PlayerBalance = playerBalance,
            Authority = authority
            // MagicProgram and MagicContext use defaults from generated client
        };

        var ix = TreasuryProgram.CreditPlayer(accounts, amount);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }

    public async Task<string> DebitPlayer(ulong amount, PublicKey authority)
    {
        if (CurrentUser() == null || Client == null) return null;

        var signer = CurrentUserPk();
        var playerBalance = DerivePlayerBalanceAccount(signer);

        var accounts = new DebitPlayerAccounts
        {
            Signer = signer,
            PlayerBalance = playerBalance,
            Authority = authority
            // MagicProgram and MagicContext use defaults from generated client
        };

        var ix = TreasuryProgram.DebitPlayer(accounts, amount);
        if (solanaManager == null)
        {
            Debug.LogError("TreasuryTransactionBuilder: SolanaManager reference is missing");
            return null;
        }
        return await solanaManager.SendAndConfirmTransaction(false, 0u, 0ul, ix);
    }
}

