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
using Treasury.Types;

namespace Treasury
{
    namespace Accounts
    {
        public partial class PlayerBalance
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 8055460702809784564UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{244, 128, 24, 93, 212, 190, 202, 111};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "htxg38No8W2";
            public ulong Balance { get; set; }

            public PublicKey Player { get; set; }

            public byte Bump { get; set; }

            public static PlayerBalance Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerBalance result = new PlayerBalance();
                result.Balance = _data.GetU64(offset);
                offset += 8;
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }

        public partial class Treasury
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 18277860573447974894UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{238, 239, 123, 238, 89, 1, 168, 253};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "gxyTsYaqFet";
            public byte Bump { get; set; }

            public byte TreasuryTokenAccountBump { get; set; }

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
                result.TreasuryTokenAccountBump = _data.GetU8(offset);
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
            InvalidOwner = 6002U
        }
    }

    namespace Types
    {
    }

    public partial class TreasuryClient : TransactionalBaseClient<TreasuryErrorKind>
    {
        public TreasuryClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(TreasuryProgram.ID))
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerBalance>>> GetPlayerBalancesAsync(string programAddress = TreasuryProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = PlayerBalance.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerBalance>>(res);
            List<PlayerBalance> resultingAccounts = new List<PlayerBalance>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerBalance.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerBalance>>(res, resultingAccounts);
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

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerBalance>> GetPlayerBalanceAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerBalance>(res);
            var resultingAccount = PlayerBalance.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerBalance>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Treasury.Accounts.Treasury>> GetTreasuryAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Treasury.Accounts.Treasury>(res);
            var resultingAccount = Treasury.Accounts.Treasury.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Treasury.Accounts.Treasury>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribePlayerBalanceAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerBalance> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerBalance parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerBalance.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
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

        protected override Dictionary<uint, ProgramError<TreasuryErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<TreasuryErrorKind>>{{6000U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.InsufficientBalance, "Insufficient balance")}, {6001U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.UnauthorizedCaller, "Unauthorized caller")}, {6002U, new ProgramError<TreasuryErrorKind>(TreasuryErrorKind.InvalidOwner, "Invalid owner for balance account")}, };
        }
    }

    namespace Program
    {
        public class CreditPlayerAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBalance { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class DebitPlayerAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBalance { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class DelegatePlayerBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferPlayerBalance { get; set; }

            public PublicKey DelegationRecordPlayerBalance { get; set; }

            public PublicKey DelegationMetadataPlayerBalance { get; set; }

            public PublicKey PlayerBalance { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("8Q2ChWpCZRicGnQogd4sQQ2zTT6kLmcyUPSUczaMMApv");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DepositAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBalance { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("So11111111111111111111111111111111111111112");
            public PublicKey UserTokenAccount { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializePlayerBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey PlayerBalance { get; set; }
        }

        public class InitializeTreasuryAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("So11111111111111111111111111111111111111112");
            public PublicKey TreasuryTokenAccount { get; set; }

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

        public class UndelegatePlayerBalanceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBalance { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class WithdrawAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBalance { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("So11111111111111111111111111111111111111112");
            public PublicKey UserTokenAccount { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public static class TreasuryProgram
        {
            public const string ID = "8Q2ChWpCZRicGnQogd4sQQ2zTT6kLmcyUPSUczaMMApv";
            public static Solana.Unity.Rpc.Models.TransactionInstruction CreditPlayer(CreditPlayerAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
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

            public static Solana.Unity.Rpc.Models.TransactionInstruction DebitPlayer(DebitPlayerAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(14427466938512022116UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegatePlayerBalance(DelegatePlayerBalanceAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferPlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordPlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataPlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15494976108273411052UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Deposit(DepositAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13182846803881894898UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayerBalance(InitializePlayerBalanceAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(6623160639733847745UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeTreasury(InitializeTreasuryAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
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

            public static Solana.Unity.Rpc.Models.TransactionInstruction UndelegatePlayerBalance(UndelegatePlayerBalanceAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7920962767755171480UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Withdraw(WithdrawAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBalance, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2495396153584390839UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}