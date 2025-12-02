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
using Blackjack;
using Blackjack.Program;
using Blackjack.Errors;
using Blackjack.Accounts;
using Blackjack.Types;

namespace Blackjack
{
    namespace Accounts
    {
        public partial class Authority
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 2601832256989195300UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{36, 108, 254, 18, 167, 144, 27, 36};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "76NmvGVe5LX";
            public PublicKey Admin { get; set; }

            public ulong GameCounter { get; set; }

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
                result.GameCounter = _data.GetU64(offset);
                offset += 8;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }

        public partial class BlackJack
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 15552040555559140499UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{147, 60, 146, 174, 124, 242, 211, 215};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "RdP1vPK7e3Y";
            public ulong GameId { get; set; }

            public byte[] DealerCards { get; set; }

            public byte DealerCardCount { get; set; }

            public long LastAnteTime { get; set; }

            public long LastPlayerActionTime { get; set; }

            public ulong GameNo { get; set; }

            public byte ActiveHands { get; set; }

            public byte HandsFinished { get; set; }

            public byte MaxPlayers { get; set; }

            public PublicKey[] Players { get; set; }

            public ulong[] AntedGameNo { get; set; }

            public byte Bump { get; set; }

            public static BlackJack Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                BlackJack result = new BlackJack();
                result.GameId = _data.GetU64(offset);
                offset += 8;
                result.DealerCards = _data.GetBytes(offset, 21);
                offset += 21;
                result.DealerCardCount = _data.GetU8(offset);
                offset += 1;
                result.LastAnteTime = _data.GetS64(offset);
                offset += 8;
                result.LastPlayerActionTime = _data.GetS64(offset);
                offset += 8;
                result.GameNo = _data.GetU64(offset);
                offset += 8;
                result.ActiveHands = _data.GetU8(offset);
                offset += 1;
                result.HandsFinished = _data.GetU8(offset);
                offset += 1;
                result.MaxPlayers = _data.GetU8(offset);
                offset += 1;
                result.Players = new PublicKey[3];
                for (uint resultPlayersIdx = 0; resultPlayersIdx < 3; resultPlayersIdx++)
                {
                    result.Players[resultPlayersIdx] = _data.GetPubKey(offset);
                    offset += 32;
                }

                result.AntedGameNo = new ulong[3];
                for (uint resultAntedGameNoIdx = 0; resultAntedGameNoIdx < 3; resultAntedGameNoIdx++)
                {
                    result.AntedGameNo[resultAntedGameNoIdx] = _data.GetU64(offset);
                    offset += 8;
                }

                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }

        public partial class BlackJackHand
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 18304055888329210149UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{37, 121, 194, 244, 217, 17, 5, 254};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "7GZWjyFkNVT";
            public PublicKey Blackjack { get; set; }

            public PublicKey Player { get; set; }

            public byte HandId { get; set; }

            public byte State { get; set; }

            public ulong CurrentBet { get; set; }

            public ulong OriginalBet { get; set; }

            public bool Insured { get; set; }

            public byte[] PlayerCards { get; set; }

            public byte CardCount { get; set; }

            public ulong GameNo { get; set; }

            public byte Bump { get; set; }

            public static BlackJackHand Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                BlackJackHand result = new BlackJackHand();
                result.Blackjack = _data.GetPubKey(offset);
                offset += 32;
                result.Player = _data.GetPubKey(offset);
                offset += 32;
                result.HandId = _data.GetU8(offset);
                offset += 1;
                result.State = _data.GetU8(offset);
                offset += 1;
                result.CurrentBet = _data.GetU64(offset);
                offset += 8;
                result.OriginalBet = _data.GetU64(offset);
                offset += 8;
                result.Insured = _data.GetBool(offset);
                offset += 1;
                result.PlayerCards = _data.GetBytes(offset, 21);
                offset += 21;
                result.CardCount = _data.GetU8(offset);
                offset += 1;
                result.GameNo = _data.GetU64(offset);
                offset += 8;
                result.Bump = _data.GetU8(offset);
                offset += 1;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum BlackjackErrorKind : uint
        {
            Unauthorized = 6000U,
            ActionTooSoon = 6001U,
            HandAlreadyInUse = 6002U,
            HandNotInThisGame = 6003U,
            ArithmeticOverflow = 6004U,
            CannotSplitUnlikeCards = 6005U,
            CardsNotDealt = 6006U,
            GameNotActive = 6007U,
            DealerNotFinished = 6008U,
            GameStillInProgress = 6009U,
            HandsNotSettled = 6010U,
            MaxPlayersReached = 6011U,
            InvalidMaxPlayers = 6012U,
            NoPlayersAnted = 6013U,
            InvalidSeatId = 6014U,
            SeatOccupied = 6015U
        }
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

    public partial class BlackjackClient : TransactionalBaseClient<BlackjackErrorKind>
    {
        public BlackjackClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId = null) : base(rpcClient, streamingRpcClient, programId ?? new PublicKey(BlackjackProgram.ID))
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Authority>>> GetAuthoritysAsync(string programAddress = BlackjackProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Authority.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Authority>>(res);
            List<Authority> resultingAccounts = new List<Authority>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Authority.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Authority>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BlackJack>>> GetBlackJacksAsync(string programAddress = BlackjackProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = BlackJack.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BlackJack>>(res);
            List<BlackJack> resultingAccounts = new List<BlackJack>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => BlackJack.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BlackJack>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BlackJackHand>>> GetBlackJackHandsAsync(string programAddress = BlackjackProgram.ID, Commitment commitment = Commitment.Confirmed)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = BlackJackHand.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BlackJackHand>>(res);
            List<BlackJackHand> resultingAccounts = new List<BlackJackHand>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => BlackJackHand.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<BlackJackHand>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Authority>> GetAuthorityAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Authority>(res);
            var resultingAccount = Authority.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Authority>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<BlackJack>> GetBlackJackAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<BlackJack>(res);
            var resultingAccount = BlackJack.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<BlackJack>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<BlackJackHand>> GetBlackJackHandAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<BlackJackHand>(res);
            var resultingAccount = BlackJackHand.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<BlackJackHand>(res, resultingAccount);
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

        public async Task<SubscriptionState> SubscribeBlackJackAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, BlackJack> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                BlackJack parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = BlackJack.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeBlackJackHandAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, BlackJackHand> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                BlackJackHand parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = BlackJackHand.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        protected override Dictionary<uint, ProgramError<BlackjackErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<BlackjackErrorKind>>{{6000U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.Unauthorized, "Unauthorized")}, {6001U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.ActionTooSoon, "Too soon to perform dealer action")}, {6002U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.HandAlreadyInUse, "Hand is already in use")}, {6003U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.HandNotInThisGame, "Hand does not belong to this game")}, {6004U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.ArithmeticOverflow, "Arithmetic overflow in calculation")}, {6005U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.CannotSplitUnlikeCards, "Cannot split unlike cards")}, {6006U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.CardsNotDealt, "Cards not dealt yet")}, {6007U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.GameNotActive, "Game is not active or dealer turn already completed")}, {6008U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.DealerNotFinished, "Dealer has not finished their turn yet")}, {6009U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.GameStillInProgress, "Previous game still in progress")}, {6010U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.HandsNotSettled, "All hands must be settled before starting new game")}, {6011U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.MaxPlayersReached, "Maximum number of players reached")}, {6012U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.InvalidMaxPlayers, "Invalid max players (must be 1-3)")}, {6013U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.NoPlayersAnted, "At least one player must ante before dealing")}, {6014U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.InvalidSeatId, "Invalid seat ID")}, {6015U, new ProgramError<BlackjackErrorKind>(BlackjackErrorKind.SeatOccupied, "Seat is occupied by an active player")}, };
        }
    }

    namespace Program
    {
        public class AcceptInsuranceAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("8UMkGYdEDoQL8dcQvPjLhM6tcUM3zFD6reGzJerugWif");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class DealerDealCardAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class DealerTurnAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

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

            public PublicKey OwnerProgram { get; set; } = new PublicKey("ER2g3hnznLMag4pYu4Nv5UAjw7EwcAr8WSibjXfpuB3m");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DelegateBlackjackAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferBlackjack { get; set; }

            public PublicKey DelegationRecordBlackjack { get; set; }

            public PublicKey DelegationMetadataBlackjack { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("ER2g3hnznLMag4pYu4Nv5UAjw7EwcAr8WSibjXfpuB3m");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class DelegateBlackjackHandAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BufferBlackjackHand { get; set; }

            public PublicKey DelegationRecordBlackjackHand { get; set; }

            public PublicKey DelegationMetadataBlackjackHand { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey OwnerProgram { get; set; } = new PublicKey("ER2g3hnznLMag4pYu4Nv5UAjw7EwcAr8WSibjXfpuB3m");
            public PublicKey DelegationProgram { get; set; } = new PublicKey("DELeGGvXpWV2fqJUhqcF5ZSYMS4JTLjteaAMARRSaeSh");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeAuthorityAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializeBlackjackAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class InitializePlayerHandAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
        }

        public class PlayerAnteAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("8UMkGYdEDoQL8dcQvPjLhM6tcUM3zFD6reGzJerugWif");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class PlayerDealCardsAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class PlayerDoubleAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("8UMkGYdEDoQL8dcQvPjLhM6tcUM3zFD6reGzJerugWif");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class PlayerHitAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class PlayerSplitAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey NewBlackjackHand { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("8UMkGYdEDoQL8dcQvPjLhM6tcUM3zFD6reGzJerugWif");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public class PlayerStandAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

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

        public class SettleHandAccounts
        {
            public PublicKey Signer { get; set; }

            public PublicKey Blackjack { get; set; }

            public PublicKey BlackjackHand { get; set; }

            public PublicKey Authority { get; set; }

            public PublicKey UserBalance { get; set; }

            public PublicKey Treasury { get; set; }

            public PublicKey TreasuryTokenAccount { get; set; }

            public PublicKey TreasuryProgram { get; set; } = new PublicKey("8UMkGYdEDoQL8dcQvPjLhM6tcUM3zFD6reGzJerugWif");
            public PublicKey SystemProgram { get; set; } = new PublicKey("11111111111111111111111111111111");
            public PublicKey MagicProgram { get; set; } = new PublicKey("Magic11111111111111111111111111111111111111");
            public PublicKey MagicContext { get; set; } = new PublicKey("MagicContext1111111111111111111111111111111");
        }

        public static class BlackjackProgram
        {
            public const string ID = "ER2g3hnznLMag4pYu4Nv5UAjw7EwcAr8WSibjXfpuB3m";
            public static Solana.Unity.Rpc.Models.TransactionInstruction AcceptInsurance(AcceptInsuranceAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(5070812016613705564UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DealerDealCard(DealerDealCardAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13099160151344458123UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DealerTurn(DealerTurnAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(1264071378159152280UL, offset);
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

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegateBlackjack(DelegateBlackjackAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferBlackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordBlackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataBlackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13280223928771545364UL, offset);
                offset += 8;
                offset += @params.Serialize(_data, offset);
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction DelegateBlackjackHand(DelegateBlackjackHandAccounts accounts, DelegateParams @params, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BufferBlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationRecordBlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.DelegationMetadataBlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.OwnerProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.DelegationProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(8577237152105393028UL, offset);
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

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializeBlackjack(InitializeBlackjackAccounts accounts, byte max_players, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15421721827742565211UL, offset);
                offset += 8;
                _data.WriteU8(max_players, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction InitializePlayerHand(InitializePlayerHandAccounts accounts, byte hand_id, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12604088471169429360UL, offset);
                offset += 8;
                _data.WriteU8(hand_id, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlayerAnte(PlayerAnteAccounts accounts, byte hand_id, byte seat_id, ulong player_bet, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(13066935305136486753UL, offset);
                offset += 8;
                _data.WriteU8(hand_id, offset);
                offset += 1;
                _data.WriteU8(seat_id, offset);
                offset += 1;
                _data.WriteU64(player_bet, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlayerDealCards(PlayerDealCardsAccounts accounts, byte _hand_id, PublicKey _player, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17862690385178829778UL, offset);
                offset += 8;
                _data.WriteU8(_hand_id, offset);
                offset += 1;
                _data.WritePubKey(_player, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlayerDouble(PlayerDoubleAccounts accounts, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(16233891453206625025UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlayerHit(PlayerHitAccounts accounts, byte _hand_id, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(2507777151813813021UL, offset);
                offset += 8;
                _data.WriteU8(_hand_id, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlayerSplit(PlayerSplitAccounts accounts, byte _existing_hand_id, byte new_hand_id, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.NewBlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(12046340378111106225UL, offset);
                offset += 8;
                _data.WriteU8(_existing_hand_id, offset);
                offset += 1;
                _data.WriteU8(new_hand_id, offset);
                offset += 1;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction PlayerStand(PlayerStandAccounts accounts, byte _hand_id, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(18399413966457782912UL, offset);
                offset += 8;
                _data.WriteU8(_hand_id, offset);
                offset += 1;
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

            public static Solana.Unity.Rpc.Models.TransactionInstruction SettleHand(SettleHandAccounts accounts, byte _hand_id, PublicKey _player, PublicKey programId = null)
            {
                programId ??= new(ID);
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Signer, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Blackjack, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BlackjackHand, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Authority, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.UserBalance, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Treasury, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.TreasuryTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TreasuryProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.MagicProgram, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.MagicContext, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3144721542130864098UL, offset);
                offset += 8;
                _data.WriteU8(_hand_id, offset);
                offset += 1;
                _data.WritePubKey(_player, offset);
                offset += 32;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}