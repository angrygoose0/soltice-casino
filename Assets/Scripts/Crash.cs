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
using Crash.Events;
using Crash.Types;

namespace Crash
{
    namespace Accounts
    {
        public partial class Authority
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 2601832256989195300UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{36, 108, 254, 18, 167, 144, 27, 36};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "76NmvGVe5LX";
            public PublicKey Admin { get; set; }

            public bool Paused { get; set; }

            public byte Bump { get; set; }

            public static Authority Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Authority result = new Authority();
                result.Admin = _data.GetPubKey(offset);
                offset += 32;
                result.Paused = _data.GetBool(offset);
                offset += 1;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }

        public partial class Game
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 1331205435963103771UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{27, 90, 166, 125, 74, 100, 121, 18};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "5aNQXizG8jB";
            public byte State { get; set; }

            public ulong Tick { get; set; }

            public ulong CrashTick { get; set; }

            public ulong GameNo { get; set; }

            public byte Bump { get; set; }

            public uint CurrentPlayers { get; set; }

            public ulong CurrentTotalBet { get; set; }

            public uint NextPlayers { get; set; }

            public ulong NextTotalBet { get; set; }

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
                result.State = _data.GetU8(offset);
                offset += 1;
                result.Tick = _data.GetU64(offset);
                offset += 8;
                result.CrashTick = _data.GetU64(offset);
                offset += 8;
                result.GameNo = _data.GetU64(offset);
                offset += 8;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                result.CurrentPlayers = _data.GetU32(offset);
                offset += 4;
                result.CurrentTotalBet = _data.GetU64(offset);
                offset += 8;
                result.NextPlayers = _data.GetU32(offset);
                offset += 4;
                result.NextTotalBet = _data.GetU64(offset);
                offset += 8;
                return result;
            }
        }

        public partial class PlayerBet
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 43393042262853108UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{244, 5, 205, 245, 189, 41, 154, 0};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "hpKtMur2mxb";
            public PublicKey Game { get; set; }

            public ulong Amount { get; set; }

            public ulong GameNo { get; set; }

            public PublicKey Player { get; set; }

            public byte Bump { get; set; }

            public bool Claimed { get; set; }

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
                result.Game = _data.GetPubKey(offset);
                offset += 32;
                result.Amount = _data.GetU64(offset);
                offset += 8;
                result.GameNo = _data.GetU64(offset);
                offset += 8;
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                result.Claimed = _data.GetBool(offset);
                offset += 1;
                return result;
            }
        }

        public partial class SessionToken
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 1081168673100727529UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{233, 4, 115, 14, 46, 21, 1, 15};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "fyZWTdUu1pS";
            public PublicKey Authority { get; set; }

            public PublicKey TargetProgram { get; set; }

            public PublicKey SessionSigner { get; set; }

            public long ValidUntil { get; set; }

            public static SessionToken Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                SessionToken result = new SessionToken();
                result.Authority = _data.GetPubKey(offset);
                offset += 32;
                result.TargetProgram = _data.GetPubKey(offset);
                offset += 32;
                result.SessionSigner = _data.GetPubKey(offset);
                offset += 32;
                result.ValidUntil = _data.GetS64(offset);
                offset += 8;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum CrashErrorKind : uint
        {
            InvalidAmount = 6000U,
            GameCrashed = 6001U,
            InvalidGame = 6002U,
            GameHasNotStarted = 6003U,
            GameAlreadyStarted = 6004U,
            InsufficientBalance = 6005U,
            ActiveBetInProgress = 6006U,
            RandomnessNotResolved = 6007U,
            AlreadyClaimed = 6008U
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

    public partial class CrashClient : TransactionalBaseClient<CrashErrorKind>
    {
        public CrashClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(CrashProgram.ID))
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Authority>>> GetAuthoritysAsync(string programAddress = CrashProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Authority.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Authority>>(res);
            List<Authority> resultingAccounts = new List<Authority>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Authority.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Authority>>(res, resultingAccounts);
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

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<SessionToken>>> GetSessionTokensAsync(string programAddress = CrashProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = SessionToken.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<SessionToken>>(res);
            List<SessionToken> resultingAccounts = new List<SessionToken>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => SessionToken.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<SessionToken>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Authority>> GetAuthorityAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Authority>(res);
            var resultingAccount = Authority.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Authority>(res, resultingAccount);
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

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<SessionToken>> GetSessionTokenAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<SessionToken>(res);
            var resultingAccount = SessionToken.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<SessionToken>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeAuthorityAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Authority> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Authority parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Authority.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
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

        public async Task<SubscriptionState> SubscribeSessionTokenAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, SessionToken> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                SessionToken parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = SessionToken.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        protected override Dictionary<uint, ProgramError<CrashErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<CrashErrorKind>>{{6000U, new ProgramError<CrashErrorKind>(CrashErrorKind.InvalidAmount, "Invalid bet amount")}, {6001U, new ProgramError<CrashErrorKind>(CrashErrorKind.GameCrashed, "Game crashed")}, {6002U, new ProgramError<CrashErrorKind>(CrashErrorKind.InvalidGame, "Invalid game")}, {6003U, new ProgramError<CrashErrorKind>(CrashErrorKind.GameHasNotStarted, "Game has not started")}, {6004U, new ProgramError<CrashErrorKind>(CrashErrorKind.GameAlreadyStarted, "Game already started")}, {6005U, new ProgramError<CrashErrorKind>(CrashErrorKind.InsufficientBalance, "Insufficient balance")}, {6006U, new ProgramError<CrashErrorKind>(CrashErrorKind.ActiveBetInProgress, "Active bet in progress for current game")}, {6007U, new ProgramError<CrashErrorKind>(CrashErrorKind.RandomnessNotResolved, "Randomness not resolved")}, {6008U, new ProgramError<CrashErrorKind>(CrashErrorKind.AlreadyClaimed, "Bet already claimed")}, };
        }
    }

    namespace Program
    {
        public class CallbackRandomnessAccounts
        {
            public PublicKey VrfProgramIdentity { get; set; } = new PublicKey("9irBy75QS2BN81FUgXuHcjqceJJRuc9oDkAe8TKVvvAw");
            public PublicKey Game { get; set; }
        }

        public class ClaimBetAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBet { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("GWKspieMW3131AAkCVvoVTUDwkdfoume4dNuwnzw8i6J");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class DelegateAuthorityAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferAuthority { get; set; }

            public PublicKey DelegationRecordAuthority { get; set; }

            public PublicKey DelegationMetadataAuthority { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("He8a1qcRE5zQo5ZYsKoj9CtVdAXRhS14jPxAmHCpEJW3");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DelegateGameAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferGame { get; set; }

            public PublicKey DelegationRecordGame { get; set; }

            public PublicKey DelegationMetadataGame { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("He8a1qcRE5zQo5ZYsKoj9CtVdAXRhS14jPxAmHCpEJW3");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DelegatePlayerBetAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferPlayerBet { get; set; }

            public PublicKey DelegationRecordPlayerBet { get; set; }

            public PublicKey DelegationMetadataPlayerBet { get; set; }

            public PublicKey PlayerBet { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("He8a1qcRE5zQo5ZYsKoj9CtVdAXRhS14jPxAmHCpEJW3");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeAuthorityAccounts
        {
            public PublicKey Signer { get; set; } = new PublicKey("Fn1XAMy3qdqxkyTViJca2gBqTDjXJH6GYsthQggjweCP");
            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeGameAccounts
        {
            public PublicKey Game { get; set; }

            public PublicKey Signer { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializePlayerBetAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBet { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class PlaceBetAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey PlayerBet { get; set; }

            public PublicKey SessionToken { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("GWKspieMW3131AAkCVvoVTUDwkdfoume4dNuwnzw8i6J");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class ProcessUndelegationAccounts
        {
            public PublicKey BaseAccount { get; set; }

            public PublicKey Buffer { get; set; }

            public PublicKey Payer { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class RequestRandomnessAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey OracleQueue { get; set; } = new PublicKey("5hBR571xnXppuCPveTrctfTU7tJLSN94nq7kv7FRK5Tc");
            public PublicKey Game { get; set; }

            public PublicKey ProgramIdentity { get; set; }

            public PublicKey VrfProgram { get; set; } = new PublicKey("Vrf1RNUjXmQGjmQrQLvJHs9SNkvDJEsRVFPkfSQUwGz");
            public PublicKey SlotHashes { get; set; } = new PublicKey("SysvarS1otHashes111111111111111111111111111");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class StartGameAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class TickAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Game { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public static class CrashProgram
        {
            public const string ID = "He8a1qcRE5zQo5ZYsKoj9CtVdAXRhS14jPxAmHCpEJW3";
            public static Solana.Unity.Rpc.Models.TransactionInstruction CallbackRandomness(CallbackRandomnessAccounts accounts, byte[] randomness, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VrfProgramIdentity, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3935360171388153382UL, offset);
                offset += 8;
                _data.WriteSpan(randomness, offset);
                offset += randomness.Length;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction ClaimBet(ClaimBetAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(9128365113323633980UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegateAuthority(DelegateAuthorityAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferAuthority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordAuthority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataAuthority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(14854150585004134884UL, offset);
                offset += 8;
                offset += @params.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegateGame(DelegateGameAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferGame, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordGame, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataGame, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15166680369052694388UL, offset);
                offset += 8;
                offset += @params.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegatePlayerBet(DelegatePlayerBetAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferPlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordPlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataPlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7436920703093002937UL, offset);
                offset += 8;
                offset += @params.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeAuthority(InitializeAuthorityAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(97425363375340045UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeGame(InitializeGameAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15529203708862021164UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayerBet(InitializePlayerBetAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17266688202344431813UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlaceBet(PlaceBetAccounts accounts, ulong amount, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerBet, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SessionToken == null ? programId : accounts.SessionToken, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
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

            public static Solana.Unity.Rpc.Models.TransactionInstruction RequestRandomness(RequestRandomnessAccounts accounts, byte client_seed, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.OracleQueue, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.ProgramIdentity, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.VrfProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SlotHashes, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1306022063415035349UL, offset);
                offset += 8;
                _data.WriteU8(client_seed, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction StartGame(StartGameAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1077946599884992505UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction Tick(TickAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Game, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1098685228960730972UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}