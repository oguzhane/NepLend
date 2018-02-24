using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System;
using System.ComponentModel;
using System.Numerics;

namespace NepLend
{
    public class NepLendContract : SmartContract
    {
        // Token Settings
        public static string Name() => "NepLend";
        public static string Symbol() => "NPL";
        public static readonly byte[] Owner = "AK2nJJpJr6o664CWJKi1QRXjqeic2zRp8y".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong factor = 100000000;
        private const ulong neo_decimals = 1000000;
        private static readonly byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };

        public static readonly char POSTFIX_BORROWER = 'A';
        public static readonly char POSTFIX_LENDER = 'B';
        public static readonly char POSTFIX_ASSET = 'C';
        public static readonly char POSTFIX_ASSET_AMOUNT = 'D';
        public static readonly char POSTFIX_COLLATERAL_AMOUNT = 'E';
        public static readonly char POSTFIX_PREMIUM_AMOUNT = 'F';
        public static readonly char POSTFIX_DAYS_TO_LEND = 'G';
        public static readonly char POSTFIX_FUND_DATE = 'H';

        public static readonly byte[] REGISTERED = { 31, 32 };
        public static readonly byte[] COLLATERAL_LOCKED = { 32, 33 };
        public static readonly byte[] CANCELLED = { 33, 34 };
        public static readonly byte[] DONE = { 34, 35 };

        //[DisplayName("transfer")]
        //public static event Action<byte[], byte[], BigInteger> Transferred;

        public static Object Main(string operation, params object[] args)
        {
            Runtime.Log("#ctor 1.1");
            Runtime.Log("Operation:" + operation);
            Storage.Put(Storage.CurrentContext, "operationKey", operation.AsByteArray());
            if (Runtime.Trigger == TriggerType.VerificationR) // X->SC
            {
                Runtime.Log("Trigger = VerificationR");
            }
            if (Runtime.Trigger == TriggerType.Verification) // SC->X
            {
                Runtime.Log("Trigger = Verification");
            }
            if (Runtime.Trigger == TriggerType.Application)
            {
                Runtime.Log("Trigger = Application");
            }
            if (Runtime.Trigger == TriggerType.ApplicationR)
            {
                Runtime.Log("Trigger = ApplicationR");
            }
            return (bool)args[0];
            //if (Runtime.Trigger == TriggerType.Verification)
            //{
            //    Runtime.Log("->Verif");
            //}else if(Runtime.Trigger == TriggerType.Application)
            //{
            //    Runtime.Log("->App");
            //}
            //return false;





            //if (Owner.Length == 20)
            //{
            //    // if param Owner is script hash
            //    return Runtime.CheckWitness(Owner);
            //}
            //else if (Owner.Length == 33)
            //{
            //    // if param Owner is public key
            //    byte[] signature = operation.AsByteArray();
            //    return VerifySignature(signature, Owner);
            //}

            /*else if (Runtime.Trigger == TriggerType.Application)
            {
                // dApp methods
                if (operation == "createLoanRequest")
                {
                    return CreateLoanRequest((byte[])args[0], (byte[])args[1], (byte[])args[2], (BigInteger)args[3], (BigInteger)args[4], (BigInteger)args[5], (BigInteger)args[6]);
                }
                if (operation == "lockCollateralForLoan")
                {
                    return LockCollateralForLoan((byte[])args[0], (byte[])args[1]);
                }
                if (operation == "deposit")
                {

                }
                if (operation == "fundLoan")
                {
                    return FundLoan((byte[])args[0], (byte[])args[1]);
                }
                if (operation == "testMe")
                {
                    Runtime.Log("ContractScriptHash:");
                    Runtime.Log(GetContractScriptHash().AsString());
                    //------------------------------------>
                    Runtime.Log("GetReferences#Start");
                    Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
                    TransactionOutput[] reference = tx.GetReferences();
                    foreach (TransactionOutput output in reference)
                    {
                        Runtime.Log("AssetId:");
                        Runtime.Log(output.AssetId.AsString());
                        Runtime.Log("ScriptHash:");
                        Runtime.Log(output.ScriptHash.AsString());
                        Runtime.Log("Value:");
                        BigInteger bigInteger = output.Value;
                        Runtime.Log(bigInteger.AsByteArray().AsString());
                    }
                    Runtime.Log("GetReferences#End");
                    //------------------------------------>
                    Runtime.Log("GetOutputs#Start");
                    TransactionOutput[] outputs = tx.GetOutputs();
                    foreach (TransactionOutput output in outputs)
                    {
                        Runtime.Log("AssetId:");
                        Runtime.Log(output.AssetId.AsString());
                        Runtime.Log("ScriptHash:");
                        Runtime.Log(output.ScriptHash.AsString());
                        Runtime.Log("Value:");
                        BigInteger bigInteger = output.Value;
                        Runtime.Log(bigInteger.AsByteArray().AsString());
                    }
                    Runtime.Log("GetOutputs#End");
                    //------------------------------------>
                    return false;
                }
                // nep-5 related functions
                if (operation == "mintTokens") return MintTokens();
                if (operation == "totalSupply") return TotalSupply();
                if (operation == "decimals") return Decimals();
                if (operation == "name") return Name();
                if (operation == "symbol") return Symbol();
                if (operation == "transfer")
                {
                    if (args.Length != 3) return false;
                    byte[] from = (byte[])args[0];
                    byte[] to = (byte[])args[1];
                    BigInteger value = (BigInteger)args[2];
                    return Transfer(from, to, value);
                }
                if (operation == "balanceOf")
                {
                    if (args.Length != 1) return 0;
                    byte[] account = (byte[])args[0];
                    return BalanceOf(account);
                }
            }*/
            //return false;
        }

        // loan isteği oluşturur
        public static bool CreateLoanRequest(byte[] id, byte[] borrower, byte[] borrowedAsset, BigInteger amountBorrowed, 
            BigInteger amountCollateral, BigInteger amountPremium, BigInteger daysToLend)
        {
            // borç isteyen kişinin kontratı invoke ettiğine emin ol
            if (!Runtime.CheckWitness(borrower)) return false;
            Runtime.Notify("Passed check for borrower");
            // isteğin bu id ile birlikte kullanılmadıgına kontrol et
            if (Storage.Get(Storage.CurrentContext, id).Length != 0)
                return false;

            // Register Loan Request
            Storage.Put(Storage.CurrentContext, id, REGISTERED);
            PutOnPostfix(id, borrower, POSTFIX_BORROWER);
            PutOnPostfix(id, borrowedAsset, POSTFIX_ASSET);
            PutOnPostfix(id, amountBorrowed.AsByteArray(), POSTFIX_ASSET_AMOUNT);
            PutOnPostfix(id, amountCollateral.AsByteArray(), POSTFIX_COLLATERAL_AMOUNT);
            PutOnPostfix(id, amountPremium.AsByteArray(), POSTFIX_PREMIUM_AMOUNT);
            PutOnPostfix(id, daysToLend.AsByteArray(), POSTFIX_DAYS_TO_LEND);

            Runtime.Notify("REG", id, borrower, borrowedAsset, amountBorrowed, amountCollateral, amountPremium, daysToLend);
            return true;
        }

        public static bool LockCollateralForLoan(byte[] id, byte[] borrower)
        {
            if (Runtime.CheckWitness(borrower)) return false;
            if (Storage.Get(Storage.CurrentContext, id) != REGISTERED) return false;// invalid state transition
            if (GetOnPostfix(id, POSTFIX_BORROWER) != borrower) return false; // invalid borrower

            BigInteger collateralAmount = GetOnPostfix(id, POSTFIX_COLLATERAL_AMOUNT).AsBigInteger();
            BigInteger balance = BalanceOf(borrower);
            if (collateralAmount > balance) {
                Runtime.Notify("insufficient balance");
                return false;
            }
            BigInteger finalBalance = balance - collateralAmount;// decrease borrowers balance
            Storage.Put(Storage.CurrentContext, id, finalBalance);// update borrowers balance
            Storage.Put(Storage.CurrentContext, id, COLLATERAL_LOCKED);// update loan request status
            return true;
        }
        
        public static bool FundLoan(byte[] id, byte[] lender)
        {
            if (Runtime.CheckWitness(lender)) return false;
            if (Storage.Get(Storage.CurrentContext, id) != COLLATERAL_LOCKED) return false;// invalid state transition
            if (GetOnPostfix(id, POSTFIX_BORROWER) == lender) return false;// ignore same lender and borrower

            byte[] assetScriptHash = GetOnPostfix(id, POSTFIX_ASSET);
            BigInteger assetAmount = GetOnPostfix(id, POSTFIX_ASSET_AMOUNT).AsBigInteger();
            return false;
        }

        // gets available balance
        public static BigInteger BalanceOf(byte[] address)
        {
            BigInteger currentBalance = Storage.Get(Storage.CurrentContext, address).AsBigInteger();
            Runtime.Notify("BALANCE", address, currentBalance);
            return currentBalance;
        }

        public static bool MintTokens()
        {
            //getting script hash who sent tokens
            byte[] id = GetSenderByAssetId(neo_asset_id);

            //check for references (if no outputs in invocation transaction's inputs)
            if (id.Length == 0)
                return false;

            //getting amount contributed in neo and set the balance
            BigInteger value = GetContributeValue();
            if (value == 0)
                return false;
            value = value / neo_decimals;
            BigInteger balance = BalanceOf(id);
            Storage.Put(Storage.CurrentContext, id, value + balance);
            AddSupply(value);

            //Transferred(null, id, value);
            return true;
        }

        public static bool Transfer(byte[] from, byte[] to, BigInteger value)
        {
            if (value <= 0) return false;
            //check scripthash
            if (!Runtime.CheckWitness(from))
                return false;
            if (from == to)
                return true;
            //check if the balance is enough to make a transfer
            BigInteger balance = BalanceOf(from);
            if (balance < value || balance.AsByteArray().Length == 0)
                return false;

            Runtime.Notify("Transfer validated");

            //decreasing balance 
            Storage.Put(Storage.CurrentContext, from, balance - value);
            balance = 0;
            balance = BalanceOf(to);
            //increasing balance and notify
            Storage.Put(Storage.CurrentContext, to, balance + value);
            //Transferred(from, to, value);
            return true;
        }

        private static void PutOnPostfix(byte[] key, byte[] value, char postfix)
        {
            string k = key.AsString() + postfix;
            Storage.Put(Storage.CurrentContext, k, value);
            Runtime.Notify("PUT", value);
        }

        private static byte[] GetOnPostfix(byte[] key, char postfix)
        {
            string k = key.AsString() + postfix;
            return Storage.Get(Storage.CurrentContext, k);
        }

        private static void DeleteOnPostfix(byte[] key, char postfix)
        {
            string k = key.AsString() + postfix;
            Storage.Delete(Storage.CurrentContext, k);
            Runtime.Notify("DELETE", key);
        }

        private static BigInteger Now()
        {
            uint height = Blockchain.GetHeight();
            Header header = Blockchain.GetHeader(height);
            uint res = header.Timestamp + 10;
            Runtime.Notify("NOW", res);
            return header.Timestamp + 10;
        }

        // check whether asset is neo and get sender script hash
        private static byte[] GetSenderByAssetId(byte[] assetId)
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            // you can choice refund or not refund
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == assetId) return output.ScriptHash;
            }
            return new byte[] { };
        }

        // get smart contract script hash
        private static byte[] GetContractScriptHash()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        // get all you contribute neo amount
        private static ulong GetContributeValue()
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            ulong value = 0;
            // get the total amount of Neo
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == GetContractScriptHash() && output.AssetId == neo_asset_id)
                {
                    value += (ulong)output.Value;
                }
            }
            return value;
        }

        //Inreasing the total supply to call when someone mint tokens
        private static void AddSupply(BigInteger amount)
        {
            BigInteger current = TotalSupply();
            BigInteger add = current + amount;
            Storage.Put(Storage.CurrentContext, "totalSupply", add);
            Runtime.Notify("ADDTOTAL", add);
        }
        // Total supply of token minted
        public static BigInteger TotalSupply()
        {
            BigInteger totalSupply = 2200000000;
            return totalSupply;
            //return Storage.Get(Storage.CurrentContext, "totalSupply").AsBigInteger();
        }
    }
}
