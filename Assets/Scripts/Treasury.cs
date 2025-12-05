using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Solana.Unity;
using Solana.Unity.Programs.Abstract;
using Solana.Unity.Programs.Utilities;
using Solana.Unity.Rpc;
using Solana.Unity.Rpc.Builders;
using Solana.Unity.Rpc.Core.Http;
using Solana.Unity.Rpc.Core.Sockets;
using Solana.Unity.Rpc.Types;
using Solana.Unity.Wallet;
using Treasury;
using Treasury.Program;
using Treasury.Errors;
using Treasury.Accounts;
using Treasury.Events;
using Treasury.Types;

namespace Treasury
{
    namespace Accounts
    {
        public partial class Treasury
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 18277860573447974894UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{238, 239, 123, 238, 89, 1, 168, 253};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "gxyTsYaqFet";
            public byte Bump { get; set; }

            public ulong BufferAmount { get; set; }

            public long UserOwnedAmount { get; set; }

            public static Treasury Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Treasury result = new Treasury();
                result.Bump = _data.GetU8(offset);
                offset += 1;
                result.BufferAmount = _data.GetU64(offset);
                offset += 8;
                result.UserOwnedAmount = _data.GetS64(offset);
                offset += 8;
                return result;
            }
        }

        public partial class TreasuryConfig
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 3001857877990454908UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{124, 54, 212, 227, 213, 189, 168, 41};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "Mn2vgr9jecQ";
            public byte Bump { get; set; }

            public byte TreasuryBump { get; set; }

            public byte TreasuryTokenAccountBump { get; set; }

            public byte StakingTokenMintBump { get; set; }

            public long UserOwnedAmount { get; set; }

            public static TreasuryConfig Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                TreasuryConfig result = new TreasuryConfig();
                result.Bump = _data.GetU8(offset);
                offset += 1;
                result.TreasuryBump = _data.GetU8(offset);
                offset += 1;
                result.TreasuryTokenAccountBump = _data.GetU8(offset);
                offset += 1;
                result.StakingTokenMintBump = _data.GetU8(offset);
                offset += 1;
                result.UserOwnedAmount = _data.GetS64(offset);
                offset += 8;
                return result;
            }
        }

        public partial class UserBalance
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 13771308742934064571UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{187, 237, 208, 146, 86, 132, 29, 191};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "YS9VMFZ1i6S";
            public PublicKey User { get; set; }

            public ulong Balance { get; set; }

            public ulong BufferAmount { get; set; }

            public ulong DepositBuffer { get; set; }

            public ulong WithdrawBuffer { get; set; }

            public ulong PendingStakeWithdraw { get; set; }

            public byte Bump { get; set; }

            public static UserBalance Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                UserBalance result = new UserBalance();
                result.User = _data.GetPubKey(offset);
                offset += 32;
                result.Balance = _data.GetU64(offset);
                offset += 8;
                result.BufferAmount = _data.GetU64(offset);
                offset += 8;
                result.DepositBuffer = _data.GetU64(offset);
                offset += 8;
                result.WithdrawBuffer = _data.GetU64(offset);
                offset += 8;
                result.PendingStakeWithdraw = _data.GetU64(offset);
                offset += 8;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum TreasuryErrorKind : uint
        {
            InsufficientBalance = 6000U,
            UnauthorizedCaller = 6001U,
            InvalidAmount = 6002U,
            InvalidStakeAmount = 6003U,
            ActiveBetInProgress = 6004U,
            InsufficientTreasuryFunds = 6005U,
            UnauthorizedGame = 6006U
        }
    }

    namespace Events
    {
    }

    namespace Types
    {
        public partial class DelegateParams
        {
            public uint CommitFrequencyMs { get; set; }

            public PublicKey Validator { get; set; }

            public int Serialize(byte[] _data, int initialOffset)
            {
                int offset = initialOffset;
                _data.WriteU32(CommitFrequencyMs, offset);
                offset += 4;
                if (Validator != null)
                {
                    _data.WriteU8(1, offset);
                    offset += 1;
                    _data.WritePubKey(Validator, offset);
                    offset += 32;
                }
                else
                {
                    _data.WriteU8(0, offset);
                    offset += 1;
                }

                return offset - initialOffset;
            }

            public static int Deserialize(ReadOnlySpan<byte> _data, int initialOffset, out DelegateParams result)
            {
                int offset = initialOffset;
                result = new DelegateParams();
                result.CommitFrequencyMs = _data.GetU32(offset);
                offset += 4;
                if (_data.GetBool(offset++))
                {
                    result.Validator = _data.GetPubKey(offset);
                    offset += 32;
                }

                return offset - initialOffset;
            }
        }
    }

    public partial class TreasuryClient : TransactionalBaseClient<TreasuryErrorKind>
    {
        public TreasuryClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(TreasuryProgram.ID))
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Treasury.Accounts.Treasury>>> GetTreasurysAsync(string programAddress = TreasuryProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Treasury.Accounts.Treasury.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Treasury.Accounts.Treasury>>(res);
            List<Treasury.Accounts.Treasury> resultingAccounts = new List<Treasury.Accounts.Treasury>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Treasury.Accounts.Treasury.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Treasury.Accounts.Treasury>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<TreasuryConfig>>> GetTreasuryConfigsAsync(string programAddress = TreasuryProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = TreasuryConfig.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<TreasuryConfig>>(res);
            List<TreasuryConfig> resultingAccounts = new List<TreasuryConfig>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => TreasuryConfig.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<TreasuryConfig>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<UserBalance>>> GetUserBalancesAsync(string programAddress = TreasuryProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = UserBalance.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<UserBalance>>(res);
            List<UserBalance> resultingAccounts = new List<UserBalance>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => UserBalance.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<UserBalance>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Treasury.Accounts.Treasury>> GetTreasuryAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Treasury.Accounts.Treasury>(res);
            var resultingAccount = Treasury.Accounts.Treasury.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Treasury.Accounts.Treasury>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<TreasuryConfig>> GetTreasuryConfigAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<TreasuryConfig>(res);
            var resultingAccount = TreasuryConfig.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<TreasuryConfig>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<UserBalance>> GetUserBalanceAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<UserBalance>(res);
            var resultingAccount = UserBalance.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<UserBalance>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeTreasuryAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Treasury.Accounts.Treasury> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Treasury.Accounts.Treasury parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Treasury.Accounts.Treasury.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeTreasuryConfigAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, TreasuryConfig> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                TreasuryConfig parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = TreasuryConfig.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeUserBalanceAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, UserBalance> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                UserBalance parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = UserBalance.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        protected override Dictionary<uint, ProgramError<TreasuryErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<TreasuryErrorKind>>{{6000U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.InsufficientBalance, "Insufficient balance")}, {6001U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.UnauthorizedCaller, "Unauthorized caller")}, {6002U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.InvalidAmount, "Invalid amount")}, {6003U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.InvalidStakeAmount, "Invalid stake amount")}, {6004U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.ActiveBetInProgress, "Active bet in progress")}, {6005U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.InsufficientTreasuryFunds, "Insufficient treasury funds")}, {6006U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.UnauthorizedGame, "Unauthorized game program")}, };
        }
    }

    namespace Program
    {
        public class ApplyDepositAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }
        }

        public class ApplyStakeWithdrawAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }
        }

        public class BurnStakeToBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey StakingTokenMint { get; set; }

            public PublicKey UserStakeTokenAccount { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TokenProgram { get; set; }
        }

        public class CreditPlayerAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey Authority { get; set; }
        }

        public class DebitPlayerAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey Authority { get; set; }
        }

        public class DelegateTreasuryAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferTreasury { get; set; }

            public PublicKey DelegationRecordTreasury { get; set; }

            public PublicKey DelegationMetadataTreasury { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("4vPABcv4v2KTm5L3iur8tFVg5CLjymPJfwb8zWHeXXCh");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DelegateUserBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferUserBalance { get; set; }

            public PublicKey DelegationRecordUserBalance { get; set; }

            public PublicKey DelegationMetadataUserBalance { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("4vPABcv4v2KTm5L3iur8tFVg5CLjymPJfwb8zWHeXXCh");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DepositTokensAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("CYPCGLgf2r6TA53y14YpR4RzAL9i2JfGTEZYEeabyh6z");
            public PublicKey UserTokenAccount { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeTreasuryAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("CYPCGLgf2r6TA53y14YpR4RzAL9i2JfGTEZYEeabyh6z");
            public PublicKey StakingTokenMint { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryStakeTokenAccount { get; set; }

            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class ProcessUndelegationAccounts
        {
            public PublicKey BaseAccount { get; set; }

            public PublicKey Buffer { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class RequestWithdrawAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }
        }

        public class UndelegateTreasuryAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class UndelegateUserBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class UserStakeAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("CYPCGLgf2r6TA53y14YpR4RzAL9i2JfGTEZYEeabyh6z");
            public PublicKey StakingTokenMint { get; set; }

            public PublicKey UserTokenAccount { get; set; }

            public PublicKey UserStakeTokenAccount { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class WithdrawTokensAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("CYPCGLgf2r6TA53y14YpR4RzAL9i2JfGTEZYEeabyh6z");
            public PublicKey UserTokenAccount { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryConfig { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public static class TreasuryProgram
        {
            public const string ID = "4vPABcv4v2KTm5L3iur8tFVg5CLjymPJfwb8zWHeXXCh";
            public static Solana.Unity.Rpc.Models.TransactionInstruction ApplyDeposit(ApplyDepositAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(5151566404731654447UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ApplyStakeWithdraw(ApplyStakeWithdrawAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryTokenAccount, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13738030120677033441UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction BurnStakeToBalance(BurnStakeToBalanceAccounts accounts, ulong share_amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.StakingTokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserStakeTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(11461961541829520467UL, offset);
                offset += 8;
                _data.WriteU64(share_amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction CreditPlayer(CreditPlayerAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(8012807824712303984UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DebitPlayer(DebitPlayerAccounts accounts, ulong amount, ulong possible_payout, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, true)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(14427466938512022116UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                _data.WriteU64(possible_payout, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegateTreasury(DelegateTreasuryAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferTreasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordTreasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataTreasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12223577925809465052UL, offset);
                offset += 8;
                offset += @params.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegateUserBalance(DelegateUserBalanceAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferUserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordUserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataUserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(11607102496156307682UL, offset);
                offset += 8;
                offset += @params.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DepositTokens(DepositTokensAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(10858336752408810416UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeBalance(InitializeBalanceAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(10500426380888413483UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeTreasury(InitializeTreasuryAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.StakingTokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryStakeTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(11998052670067948156UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ProcessUndelegation(ProcessUndelegationAccounts accounts, byte[][] account_seeds, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BaseAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Buffer, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Payer, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12048014319693667524UL, offset);
                offset += 8;
                _data.WriteS32(account_seeds.Length, offset);
                offset += 4;
                foreach (var account_seedsElement in account_seeds)
                {
                    _data.WriteS32(account_seedsElement.Length, offset);
                    offset += 4;
                    _data.WriteSpan(account_seedsElement, offset);
                    offset += account_seedsElement.Length;
                }

                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction RequestWithdraw(RequestWithdrawAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13123360647150264201UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UndelegateTreasury(UndelegateTreasuryAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3081456972430884043UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UndelegateUserBalance(UndelegateUserBalanceAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15378193751178868651UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UserStake(UserStakeAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.StakingTokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserStakeTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7167166551223355506UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction WithdrawTokens(WithdrawTokensAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryConfig, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12279827527737869314UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}