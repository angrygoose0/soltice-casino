using UnityEngine;
using System.Threading.Tasks;
using Solana.Unity.SDK;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Wallet;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Programs;
using Blackjack;
using Blackjack.Program;
using Blackjack.Accounts;
using Blackjack.Types;
using Treasury.Program;
using System;
using System.Text;

public class BlackjackTransactionBuilder : MonoBehaviour
{
    public static readonly PublicKey SYSTEM_PROGRAM_ID = SystemProgram.ProgramIdKey;
    public static readonly PublicKey TOKEN_PROGRAM_ID = TokenProgram.ProgramIdKey;
    public static readonly PublicKey ASSOCIATED_TOKEN_PROGRAM_ID = AssociatedTokenAccountProgram.ProgramIdKey;

    [SerializeField] private SolanaManager solanaManager;

    private static PublicKey _programId = new PublicKey(BlackjackProgram.ID);
    private BlackjackClient Client => solanaManager?.BlackjackClient;

    private Account CurrentUser() => Web3.Account;
    private PublicKey CurrentUserPk() => CurrentUser()?.PublicKey;

    // PDA helpers
    public static PublicKey DeriveAuthorityAccount()
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("AUTHORITY") }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveBlackjackAccount(ulong gameId)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("BLACKJACK"), BitConverter.GetBytes(gameId) }, _programId, out PublicKey pda, out _);
        return pda;
    }

    public static PublicKey DeriveBlackjackHandAccount(PublicKey player, byte handId)
    {
        PublicKey.TryFindProgramAddress(new[] { Encoding.UTF8.GetBytes("BLACKJACKHAND"), player.KeyBytes, new[] { handId } }, _programId, out PublicKey pda, out _);
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

    // Fetch helpers
    public async Task<Authority> GetAuthorityData()
    {
        if (Client == null) return null;
        var authority = DeriveAuthorityAccount();
        var res = await Client.GetAuthorityAsync(authority.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<BlackJack> GetBlackjackData(ulong gameId)
    {
        if (Client == null) return null;
        var blackjack = DeriveBlackjackAccount(gameId);
        var res = await Client.GetBlackJackAsync(blackjack.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    public async Task<BlackJackHand> GetBlackjackHand(PublicKey player, byte handId)
    {
        if (Client == null) return null;
        var handPda = DeriveBlackjackHandAccount(player, handId);
        var res = await Client.GetBlackJackHandAsync(handPda.ToString(), Commitment.Confirmed);
        return res.ParsedResult;
    }

    private static DelegateParams BuildDelegateParams(uint commitFrequencyMs, PublicKey validator)
    {
        return new DelegateParams
        {
            CommitFrequencyMs = commitFrequencyMs,
            Validator = validator
        };
    }

    // Transaction builders
    public TransactionInstruction InitializeAuthority()
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var authority = DeriveAuthorityAccount();
        var accounts = new InitializeAuthorityAccounts
        {
            Signer = user,
            Authority = authority,
        };
        return BlackjackProgram.InitializeAuthority(accounts);
    }

    public TransactionInstruction InitializeBlackjack(ulong gameId, byte maxPlayers = 3)
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var authority = DeriveAuthorityAccount();
        var accounts = new InitializeBlackjackAccounts
        {
            Signer = user,
            Blackjack = blackjack,
            Authority = authority,
        };
        return BlackjackProgram.InitializeBlackjack(accounts, maxPlayers);
    }

    public TransactionInstruction InitializePlayerHand(byte handId)
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var blackjackHand = DeriveBlackjackHandAccount(user, handId);
        var accounts = new InitializePlayerHandAccounts
        {
            Signer = user,
            BlackjackHand = blackjackHand,
        };
        return BlackjackProgram.InitializePlayerHand(accounts, handId);
    }

    public TransactionInstruction DelegateAuthority(uint commitFrequencyMs = 1000, PublicKey validator = null)
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var authority = DeriveAuthorityAccount();
        var delegationMetadata = DeriveDelegationMetadataAccount(authority);
        var delegationRecord = DeriveDelegationRecordAccount(authority);
        var delegationBuffer = DeriveDelegationBufferAccount(authority);

        validator = new PublicKey("MAS1Dt9qreoRMQ14YQuhg8UTZMMzDdKhmkZMECCzk57");

        var accounts = new DelegateAuthorityAccounts
        {
            Signer = user,
            Authority = authority,
            DelegationMetadataAuthority = delegationMetadata,
            DelegationRecordAuthority = delegationRecord,
            BufferAuthority = delegationBuffer,
        };
        var @params = BuildDelegateParams(commitFrequencyMs, validator);
        return BlackjackProgram.DelegateAuthority(accounts, @params);
    }

    public TransactionInstruction DelegateBlackjack(ulong gameId, uint commitFrequencyMs = 1000, PublicKey validator = null)
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var delegationMetadata = DeriveDelegationMetadataAccount(blackjack);
        var delegationRecord = DeriveDelegationRecordAccount(blackjack);
        var delegationBuffer = DeriveDelegationBufferAccount(blackjack);

        validator = new PublicKey("MAS1Dt9qreoRMQ14YQuhg8UTZMMzDdKhmkZMECCzk57");

        var accounts = new DelegateBlackjackAccounts
        {
            Signer = user,
            Blackjack = blackjack,
            DelegationMetadataBlackjack = delegationMetadata,
            DelegationRecordBlackjack = delegationRecord,
            BufferBlackjack = delegationBuffer,
        };
        var @params = BuildDelegateParams(commitFrequencyMs, validator);
        return BlackjackProgram.DelegateBlackjack(accounts, @params);
    }

    public TransactionInstruction DelegateBlackjackHand(ulong gameId, byte handId, uint commitFrequencyMs = 1000, PublicKey validator = null)
    {
        if (CurrentUser() == null) return null;
        var user = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(user, handId);
        var delegationMetadata = DeriveDelegationMetadataAccount(blackjackHand);
        var delegationRecord = DeriveDelegationRecordAccount(blackjackHand);
        var delegationBuffer = DeriveDelegationBufferAccount(blackjackHand);

        validator = new PublicKey("MAS1Dt9qreoRMQ14YQuhg8UTZMMzDdKhmkZMECCzk57");

        var accounts = new DelegateBlackjackHandAccounts
        {
            Signer = user,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
            DelegationMetadataBlackjackHand = delegationMetadata,
            DelegationRecordBlackjackHand = delegationRecord,
            BufferBlackjackHand = delegationBuffer,
        };
        var @params = BuildDelegateParams(commitFrequencyMs, validator);
        return BlackjackProgram.DelegateBlackjackHand(accounts, @params);
    }

    public TransactionInstruction PlayerAnte(ulong gameId, byte handId, ulong betAmount)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, handId);
        var authority = DeriveAuthorityAccount();
        var userBalance = TreasuryTransactionBuilder.DeriveUserBalanceAccount(signer);
        var treasury = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();

        var accounts = new PlayerAnteAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
            Authority = authority,
            UserBalance = userBalance,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
        };

        ulong amountWithDecimals = (ulong)(betAmount * Math.Pow(10, solanaManager.TokenDecimals));
        return BlackjackProgram.PlayerAnte(accounts, handId, amountWithDecimals);
    }

    public TransactionInstruction PlayerDealCards(ulong gameId, byte handId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, handId);

        var accounts = new PlayerDealCardsAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
        };
        return BlackjackProgram.PlayerDealCards(accounts, handId, signer);
    }

    public TransactionInstruction PlayerHit(ulong gameId, byte handId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, handId);

        var accounts = new PlayerHitAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
        };
        return BlackjackProgram.PlayerHit(accounts, handId);
    }

    public TransactionInstruction PlayerStand(ulong gameId, byte handId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, handId);

        var accounts = new PlayerStandAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
        };
        return BlackjackProgram.PlayerStand(accounts, handId);
    }

    public TransactionInstruction PlayerDouble(ulong gameId, byte handId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, handId);
        var authority = DeriveAuthorityAccount();
        var userBalance = TreasuryTransactionBuilder.DeriveUserBalanceAccount(signer);
        var treasury = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();

        var accounts = new PlayerDoubleAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
            Authority = authority,
            UserBalance = userBalance,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
        };
        return BlackjackProgram.PlayerDouble(accounts);
    }

    public TransactionInstruction AcceptInsurance(ulong gameId, byte handId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, handId);
        var authority = DeriveAuthorityAccount();
        var userBalance = TreasuryTransactionBuilder.DeriveUserBalanceAccount(signer);
        var treasury = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();

        var accounts = new AcceptInsuranceAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
            Authority = authority,
            UserBalance = userBalance,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
        };
        return BlackjackProgram.AcceptInsurance(accounts);
    }

    public TransactionInstruction PlayerSplit(ulong gameId, byte existingHandId, byte newHandId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(signer, existingHandId);
        var newBlackjackHand = DeriveBlackjackHandAccount(signer, newHandId);
        var authority = DeriveAuthorityAccount();
        var userBalance = TreasuryTransactionBuilder.DeriveUserBalanceAccount(signer);
        var treasury = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();

        var accounts = new PlayerSplitAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
            NewBlackjackHand = newBlackjackHand,
            Authority = authority,
            UserBalance = userBalance,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
        };
        return BlackjackProgram.PlayerSplit(accounts, existingHandId, newHandId);
    }

    public TransactionInstruction DealerDealCard(ulong gameId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);

        var accounts = new DealerDealCardAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
        };
        return BlackjackProgram.DealerDealCard(accounts);
    }

    public TransactionInstruction DealerTurn(ulong gameId)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);

        var accounts = new DealerTurnAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
        };
        return BlackjackProgram.DealerTurn(accounts);
    }

    public TransactionInstruction SettleHand(ulong gameId, byte handId, PublicKey player)
    {
        if (CurrentUser() == null) return null;
        var signer = CurrentUserPk();
        var blackjack = DeriveBlackjackAccount(gameId);
        var blackjackHand = DeriveBlackjackHandAccount(player, handId);
        var authority = DeriveAuthorityAccount();
        var userBalance = TreasuryTransactionBuilder.DeriveUserBalanceAccount(player);
        var treasury = TreasuryTransactionBuilder.DeriveTreasuryAccount();
        var treasuryTokenAccount = TreasuryTransactionBuilder.DeriveTreasuryTokenAccount();

        var accounts = new SettleHandAccounts
        {
            Signer = signer,
            Blackjack = blackjack,
            BlackjackHand = blackjackHand,
            Authority = authority,
            UserBalance = userBalance,
            Treasury = treasury,
            TreasuryTokenAccount = treasuryTokenAccount,
        };
        return BlackjackProgram.SettleHand(accounts, handId, player);
    }
}

