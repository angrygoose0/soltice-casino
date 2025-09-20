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
using Crash;
using Crash.Program;
using Crash.Errors;
using Crash.Accounts;
using Crash.Types;

namespace Crash
{
    namespace Accounts
    {
        public partial class Game
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 1331205435963103771UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{27, 90, 166, 125, 74, 100, 121, 18};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "5aNQXizG8jB";
            public ulong Rconstant { get; set; }

            public byte State { get; set; }

            public ulong Round { get; set; }

            public ulong GameNo { get; set; }

            public PublicKey RandomnessAccount { get; set; }

            public ulong CommitSlot { get; set; }

            public byte Bump { get; set; }

            public byte TreasuryBump { get; set; }

            public static Game Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Game result = new Game();
                result.Rconstant = _data.GetU64(offset);
                offset += 8;
                result.State = _data.GetU8(offset);
                offset += 1;
                result.Round = _data.GetU64(offset);
                offset += 8;
                result.GameNo = _data.GetU64(offset);
                offset += 8;
                result.RandomnessAccount = _data.GetPubKey(offset);
                offset += 32;
                result.CommitSlot = _data.GetU64(offset);
                offset += 8;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                result.TreasuryBump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }

        public partial class PlayerBet
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 43393042262853108UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{244, 5, 205, 245, 189, 41, 154, 0};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "hpKtMur2mxb";
            public ulong Amount { get; set; }

            public ulong GameNo { get; set; }

            public PublicKey Player { get; set; }

            public byte Bump { get; set; }

            public static PlayerBet Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                PlayerBet result = new PlayerBet();
                result.Amount = _data.GetU64(offset);
                offset += 8;
                result.GameNo = _data.GetU64(offset);
                offset += 8;
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum CrashErrorKind : uint
        {
            RandomnessAlreadyRevealed = 6000U,
            InvalidAmount = 6001U,
            GameCrashed = 6002U,
            InvalidGame = 6003U,
            GameHasNotStarted = 6004U,
            RandomnessExpired = 6005U,
            RandomnessNotResolved = 6006U
        }
    }

    namespace Types
    {
    }

    public partial class CrashClient : TransactionalBaseClient<CrashErrorKind>
    {
        public CrashClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(CrashProgram.ID))
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Game>>> GetGamesAsync(string programAddress = CrashProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Game.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Game>>(res);
            List<Game> resultingAccounts = new List<Game>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Game.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Game>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerBet>>> GetPlayerBetsAsync(string programAddress = CrashProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = PlayerBet.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerBet>>(res);
            List<PlayerBet> resultingAccounts = new List<PlayerBet>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => PlayerBet.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<PlayerBet>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Game>> GetGameAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Game>(res);
            var resultingAccount = Game.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Game>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<PlayerBet>> GetPlayerBetAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerBet>(res);
            var resultingAccount = PlayerBet.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<PlayerBet>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeGameAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Game> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Game parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Game.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribePlayerBetAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, PlayerBet> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                PlayerBet parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = PlayerBet.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        protected override Dictionary<uint, ProgramError<CrashErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<CrashErrorKind>>{{6000U, new ProgramError<CrashErrorKind>(CrashErrorKind.RandomnessAlreadyRevealed, "Randomness already revealed for previous slot")}, {6001U, new ProgramError<CrashErrorKind>(CrashErrorKind.InvalidAmount, "Invalid bet amount")}, {6002U, new ProgramError<CrashErrorKind>(CrashErrorKind.GameCrashed, "Game crashed")}, {6003U, new ProgramError<CrashErrorKind>(CrashErrorKind.InvalidGame, "Invalid game")}, {6004U, new ProgramError<CrashErrorKind>(CrashErrorKind.GameHasNotStarted, "Game has not started")}, {6005U, new ProgramError<CrashErrorKind>(CrashErrorKind.RandomnessExpired, "Randomness expired")}, {6006U, new ProgramError<CrashErrorKind>(CrashErrorKind.RandomnessNotResolved, "Randomness not resolved")}, };
        }
    }

    namespace Program
    {
        public class AdvanceTickAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey RandomnessAccountData { get; set; }
        }

        public class ClaimBetAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey PlayerBet { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("So11111111111111111111111111111111111111112");
            public PublicKey UserTokenAccount { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }
        }

        public class InitializeAccounts
        {
            public PublicKey Game { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeTreasuryAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("So11111111111111111111111111111111111111112");
            public PublicKey Game { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey TokenProgram { get; set; }
        }

        public class PlaceBetAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey TokenMint { get; set; } = new PublicKey("So11111111111111111111111111111111111111112");
            public PublicKey UserTokenAccount { get; set; }

            public PublicKey PlayerBet { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey AssociatedTokenProgram { get; set; } = new PublicKey("ATokenGPvbdGVxr1b2hvZbsiqW5xWH25efTNsLJA8knL");
            public PublicKey TokenProgram { get; set; }
        }

        public class SetupTickAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey RandomnessAccountData { get; set; }
        }

        public static class CrashProgram
        {
            public const string ID = "B7pQKs9hCqpamjHBnJZY2w2oAKjMLy8XXSdovSwrsZxN";
            public static Solana.Unity.Rpc.Models.TransactionInstruction AdvanceTick(AdvanceTickAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.RandomnessAccountData, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(6590003246110228109UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ClaimBet(ClaimBetAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9128365113323633980UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeTreasury(InitializeTreasuryAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(11998052670067948156UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlaceBet(PlaceBetAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenMint, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2413549243525709534UL, offset);
                offset += 8;
                _data.WriteU64(amount, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction SetupTick(SetupTickAccounts accounts, PublicKey randomness_account, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.RandomnessAccountData, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(14541262023699551989UL, offset);
                offset += 8;
                _data.WritePubKey(randomness_account, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}