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
using ScoogiBattle;
using ScoogiBattle.Program;
using ScoogiBattle.Errors;
using ScoogiBattle.Accounts;
using ScoogiBattle.Types;

namespace ScoogiBattle
{
    namespace Accounts
    {
        public partial class Admin
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 4684949812185702132UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{244, 158, 220, 65, 8, 73, 4, 65};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "hv8HzvNkkfr";
            public PublicKey AdminField { get; set; }

            public PublicKey Mint { get; set; }

            public ulong BurnFeeBps { get; set; }

            public ulong BattlePrice { get; set; }

            public static Admin Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Admin result = new Admin();
                result.AdminField = _data.GetPubKey(offset);
                offset += 32;
                result.Mint = _data.GetPubKey(offset);
                offset += 32;
                result.BurnFeeBps = _data.GetU64(offset);
                offset += 8;
                result.BattlePrice = _data.GetU64(offset);
                offset += 8;
                return result;
            }
        }

        public partial class Battle
        {
            public static ulong ACCOUNT_DISCRIMINATOR => 1762216144921007185UL;
            public static ReadOnlySpan<byte> ACCOUNT_DISCRIMINATOR_BYTES => new byte[]{81, 148, 121, 71, 63, 166, 116, 24};
            public static string ACCOUNT_DISCRIMINATOR_B58 => "EeRpdzahTuy";
            public ulong BattleId { get; set; }

            public PublicKey PlayerOne { get; set; }

            public PublicKey PlayerTwo { get; set; }

            public PublicKey Winner { get; set; }

            public BattleStatus BattleStatus { get; set; }

            public static Battle Deserialize(ReadOnlySpan<byte> _data)
            {
                int offset = 0;
                ulong accountHashValue = _data.GetU64(offset);
                offset += 8;
                if (accountHashValue != ACCOUNT_DISCRIMINATOR)
                {
                    return null;
                }

                Battle result = new Battle();
                result.BattleId = _data.GetU64(offset);
                offset += 8;
                result.PlayerOne = _data.GetPubKey(offset);
                offset += 32;
                result.PlayerTwo = _data.GetPubKey(offset);
                offset += 32;
                result.Winner = _data.GetPubKey(offset);
                offset += 32;
                result.BattleStatus = (BattleStatus)_data.GetU8(offset);
                offset += 1;
                return result;
            }
        }
    }

    namespace Errors
    {
        public enum ScoogiBattleErrorKind : uint
        {
            InvalidBattleResult = 6000U,
            InvalidBattleId = 6001U,
            InvalidBattleStatus = 6002U,
            InvalidWithdrawal = 6003U,
            InternalError = 6004U,
            Unauthorized = 6005U
        }
    }

    namespace Types
    {
        public enum BattleStatus : byte
        {
            Pending,
            InProgress,
            Completed
        }
    }

    public partial class ScoogiBattleClient : TransactionalBaseClient<ScoogiBattleErrorKind>
    {
        public ScoogiBattleClient(IRpcClient rpcClient, IStreamingRpcClient streamingRpcClient, PublicKey programId) : base(rpcClient, streamingRpcClient, programId)
        {
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Admin>>> GetAdminsAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Admin.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Admin>>(res);
            List<Admin> resultingAccounts = new List<Admin>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Admin.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Admin>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Battle>>> GetBattlesAsync(string programAddress, Commitment commitment = Commitment.Finalized)
        {
            var list = new List<Solana.Unity.Rpc.Models.MemCmp>{new Solana.Unity.Rpc.Models.MemCmp{Bytes = Battle.ACCOUNT_DISCRIMINATOR_B58, Offset = 0}};
            var res = await RpcClient.GetProgramAccountsAsync(programAddress, commitment, memCmpList: list);
            if (!res.WasSuccessful || !(res.Result?.Count > 0))
                return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Battle>>(res);
            List<Battle> resultingAccounts = new List<Battle>(res.Result.Count);
            resultingAccounts.AddRange(res.Result.Select(result => Battle.Deserialize(Convert.FromBase64String(result.Account.Data[0]))));
            return new Solana.Unity.Programs.Models.ProgramAccountsResultWrapper<List<Battle>>(res, resultingAccounts);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Admin>> GetAdminAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Admin>(res);
            var resultingAccount = Admin.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Admin>(res, resultingAccount);
        }

        public async Task<Solana.Unity.Programs.Models.AccountResultWrapper<Battle>> GetBattleAsync(string accountAddress, Commitment commitment = Commitment.Finalized)
        {
            var res = await RpcClient.GetAccountInfoAsync(accountAddress, commitment);
            if (!res.WasSuccessful)
                return new Solana.Unity.Programs.Models.AccountResultWrapper<Battle>(res);
            var resultingAccount = Battle.Deserialize(Convert.FromBase64String(res.Result.Value.Data[0]));
            return new Solana.Unity.Programs.Models.AccountResultWrapper<Battle>(res, resultingAccount);
        }

        public async Task<SubscriptionState> SubscribeAdminAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Admin> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Admin parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Admin.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<SubscriptionState> SubscribeBattleAsync(string accountAddress, Action<SubscriptionState, Solana.Unity.Rpc.Messages.ResponseValue<Solana.Unity.Rpc.Models.AccountInfo>, Battle> callback, Commitment commitment = Commitment.Finalized)
        {
            SubscriptionState res = await StreamingRpcClient.SubscribeAccountInfoAsync(accountAddress, (s, e) =>
            {
                Battle parsingResult = null;
                if (e.Value?.Data?.Count > 0)
                    parsingResult = Battle.Deserialize(Convert.FromBase64String(e.Value.Data[0]));
                callback(s, e, parsingResult);
            }, commitment);
            return res;
        }

        public async Task<RequestResult<string>> SendInitializeAsync(InitializeAccounts accounts, ulong burnFeeBps, ulong battlePrice, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.Initialize(accounts, burnFeeBps, battlePrice, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdateBurnFeeBpsAsync(UpdateBurnFeeBpsAccounts accounts, ulong burnFeeBps, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.UpdateBurnFeeBps(accounts, burnFeeBps, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdateBattlePriceAsync(UpdateBattlePriceAccounts accounts, ulong battlePrice, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.UpdateBattlePrice(accounts, battlePrice, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendUpdateMintAsync(UpdateMintAccounts accounts, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.UpdateMint(accounts, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendAdminWithdrawalAsync(AdminWithdrawalAccounts accounts, ulong battleId, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.AdminWithdrawal(accounts, battleId, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendCreateBattleAsync(CreateBattleAccounts accounts, ulong battleId, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.CreateBattle(accounts, battleId, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendJoinBattleAsync(JoinBattleAccounts accounts, ulong battleId, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.JoinBattle(accounts, battleId, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendRecordBattleResultAsync(RecordBattleResultAccounts accounts, byte battleResult, ulong battleId, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.RecordBattleResult(accounts, battleResult, battleId, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        public async Task<RequestResult<string>> SendWithdrawFromBattleAsync(WithdrawFromBattleAccounts accounts, ulong battleId, PublicKey feePayer, Func<byte[], PublicKey, byte[]> signingCallback, PublicKey programId)
        {
            Solana.Unity.Rpc.Models.TransactionInstruction instr = Program.ScoogiBattleProgram.WithdrawFromBattle(accounts, battleId, programId);
            return await SignAndSendTransaction(instr, feePayer, signingCallback);
        }

        protected override Dictionary<uint, ProgramError<ScoogiBattleErrorKind>> BuildErrorsDictionary()
        {
            return new Dictionary<uint, ProgramError<ScoogiBattleErrorKind>>{{6000U, new ProgramError<ScoogiBattleErrorKind>(ScoogiBattleErrorKind.InvalidBattleResult, "Invalid battle result")}, {6001U, new ProgramError<ScoogiBattleErrorKind>(ScoogiBattleErrorKind.InvalidBattleId, "Invalid battle id")}, {6002U, new ProgramError<ScoogiBattleErrorKind>(ScoogiBattleErrorKind.InvalidBattleStatus, "Invalid battle status")}, {6003U, new ProgramError<ScoogiBattleErrorKind>(ScoogiBattleErrorKind.InvalidWithdrawal, "Invalid withdrawal")}, {6004U, new ProgramError<ScoogiBattleErrorKind>(ScoogiBattleErrorKind.InternalError, "Internal error")}, {6005U, new ProgramError<ScoogiBattleErrorKind>(ScoogiBattleErrorKind.Unauthorized, "Unauthorized")}, };
        }
    }

    namespace Program
    {
        public class InitializeAccounts
        {
            public PublicKey Admin { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class UpdateBurnFeeBpsAccounts
        {
            public PublicKey Admin { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey SystemProgram { get; set; }
        }

        public class UpdateBattlePriceAccounts
        {
            public PublicKey Admin { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey Mint { get; set; }
        }

        public class UpdateMintAccounts
        {
            public PublicKey Admin { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey Mint { get; set; }
        }

        public class AdminWithdrawalAccounts
        {
            public PublicKey Admin { get; set; }

            public PublicKey PlayerOne { get; set; }

            public PublicKey PlayerTwo { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey BattleAccount { get; set; }

            public PublicKey BattleTokenAccount { get; set; }

            public PublicKey PlayerOneTokenAccount { get; set; }

            public PublicKey PlayerTwoTokenAccount { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey TokenProgram { get; set; }
        }

        public class CreateBattleAccounts
        {
            public PublicKey PlayerOne { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey BattleAccount { get; set; }

            public PublicKey PlayerOneTokenAccount { get; set; }

            public PublicKey BattleTokenAccount { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey TokenProgram { get; set; }
        }

        public class JoinBattleAccounts
        {
            public PublicKey PlayerTwo { get; set; }

            public PublicKey PlayerOne { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey BattleAccount { get; set; }

            public PublicKey PlayerTwoTokenAccount { get; set; }

            public PublicKey BattleTokenAccount { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey TokenProgram { get; set; }
        }

        public class RecordBattleResultAccounts
        {
            public PublicKey Winner { get; set; }

            public PublicKey PlayerOne { get; set; }

            public PublicKey PlayerTwo { get; set; }

            public PublicKey Admin { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey AdminTokenAccount { get; set; }

            public PublicKey BattleAccount { get; set; }

            public PublicKey BattleTokenAccount { get; set; }

            public PublicKey WinnerTokenAccount { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey TokenProgram { get; set; }
        }

        public class WithdrawFromBattleAccounts
        {
            public PublicKey PlayerOne { get; set; }

            public PublicKey Admin { get; set; }

            public PublicKey AdminAccount { get; set; }

            public PublicKey BattleAccount { get; set; }

            public PublicKey BattleTokenAccount { get; set; }

            public PublicKey PlayerOneTokenAccount { get; set; }

            public PublicKey Mint { get; set; }

            public PublicKey SystemProgram { get; set; }

            public PublicKey AssociatedTokenProgram { get; set; }

            public PublicKey TokenProgram { get; set; }
        }

        public static class ScoogiBattleProgram
        {
            public static Solana.Unity.Rpc.Models.TransactionInstruction Initialize(InitializeAccounts accounts, ulong burnFeeBps, ulong battlePrice, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Admin, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(17121445590508351407UL, offset);
                offset += 8;
                _data.WriteU64(burnFeeBps, offset);
                offset += 8;
                _data.WriteU64(battlePrice, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdateBurnFeeBps(UpdateBurnFeeBpsAccounts accounts, ulong burnFeeBps, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Admin, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(3150535225490043282UL, offset);
                offset += 8;
                _data.WriteU64(burnFeeBps, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdateBattlePrice(UpdateBattlePriceAccounts accounts, ulong battlePrice, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Admin, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7564932090806838066UL, offset);
                offset += 8;
                _data.WriteU64(battlePrice, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction UpdateMint(UpdateMintAccounts accounts, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Admin, true), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(423045118803168212UL, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction AdminWithdrawal(AdminWithdrawalAccounts accounts, ulong battleId, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Admin, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.PlayerOne, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.PlayerTwo, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerOneTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerTwoTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(10791029715833818358UL, offset);
                offset += 8;
                _data.WriteU64(battleId, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction CreateBattle(CreateBattleAccounts accounts, ulong battleId, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerOne, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerOneTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7402619448180799746UL, offset);
                offset += 8;
                _data.WriteU64(battleId, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction JoinBattle(JoinBattleAccounts accounts, ulong battleId, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerTwo, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.PlayerOne, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerTwoTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(7221119029367603326UL, offset);
                offset += 8;
                _data.WriteU64(battleId, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction RecordBattleResult(RecordBattleResultAccounts accounts, byte battleResult, ulong battleId, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.Winner, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.PlayerOne, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.PlayerTwo, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Admin, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.AdminTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.WinnerTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(15787151554137610846UL, offset);
                offset += 8;
                _data.WriteU8(battleResult, offset);
                offset += 1;
                _data.WriteU64(battleId, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }

            public static Solana.Unity.Rpc.Models.TransactionInstruction WithdrawFromBattle(WithdrawFromBattleAccounts accounts, ulong battleId, PublicKey programId)
            {
                List<Solana.Unity.Rpc.Models.AccountMeta> keys = new()
                {Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerOne, true), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Admin, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AdminAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.BattleTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.Writable(accounts.PlayerOneTokenAccount, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.Mint, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.SystemProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.AssociatedTokenProgram, false), Solana.Unity.Rpc.Models.AccountMeta.ReadOnly(accounts.TokenProgram, false)};
                byte[] _data = new byte[1200];
                int offset = 0;
                _data.WriteU64(10978602512387980774UL, offset);
                offset += 8;
                _data.WriteU64(battleId, offset);
                offset += 8;
                byte[] resultData = new byte[offset];
                Array.Copy(_data, resultData, offset);
                return new Solana.Unity.Rpc.Models.TransactionInstruction{Keys = keys, ProgramId = programId.KeyBytes, Data = resultData};
            }
        }
    }
}