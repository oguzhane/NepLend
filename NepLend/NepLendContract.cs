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
        public static readonly byte[] Owner = "AUdihuUDFA5LTPCaztZHsT5L9nTVrnsiHq".ToScriptHash();
        public static byte Decimals() => 8;
        private const ulong neo_decimals = 100000000;
        private const ulong factor = 100000000;
        private const ulong basic_rate = 100 * factor;
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
        public static readonly byte[] FUNDED = { 33, 34 };
        public static readonly byte[] CANCELLED = { 34, 35 };
        public static readonly byte[] COLLATERAL_CLAIMED = { 35, 36 };
        public static readonly byte[] LOAN_REPAID = { 36, 37 };
        public static readonly int LOAN_ID_VALUE_LEN = 2;

        [DisplayName("transfer")]
        public static event Action<byte[], byte[], BigInteger> Transferred;

        public static object Main(string operation, params object[] args)
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
            if (operation == "fundLoan")
            {
                return FundLoan((byte[])args[0], (byte[])args[1]);
            }
            if (operation == "cancelLoan")
            {
                return CancelLoan((byte[])args[0], (byte[])args[1]);
            }
            if (operation == "claimCollateral")
            {
                return ClaimCollateral((byte[])args[0], (byte[])args[1]);
            }
            if (operation == "repayLoan")
            {
                return RepayLoan((byte[])args[0], (byte[])args[1]);
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
                return BalanceOf((byte[])args[0]);
            }
            return false;
        }

        // Creates Loan Request
        public static bool CreateLoanRequest(byte[] id, byte[] borrower, byte[] borrowedAsset, BigInteger amountBorrowed, 
            BigInteger amountCollateral, BigInteger amountPremium, BigInteger daysToLend)
        {
            // ensure invoker is borrower?
            if (!Runtime.CheckWitness(borrower)) return false;

            var stat = Storage.Get(Storage.CurrentContext, id);
            if (stat.Length == LOAN_ID_VALUE_LEN)
            {
                if (stat == null)
                {
                    Runtime.Notify("LENGTH", "Eq", "Null");
                }
                return false;
            }


            // Register Loan Request
            Storage.Put(Storage.CurrentContext, id, REGISTERED);
            Runtime.Log("id REGISTERED");

            PutOnPostfix(id, borrower, POSTFIX_BORROWER);
            PutOnPostfix(id, borrowedAsset, POSTFIX_ASSET);
            PutOnPostfix(id, amountBorrowed.AsByteArray(), POSTFIX_ASSET_AMOUNT);
            PutOnPostfix(id, amountCollateral.AsByteArray(), POSTFIX_COLLATERAL_AMOUNT);
            PutOnPostfix(id, amountPremium.AsByteArray(), POSTFIX_PREMIUM_AMOUNT);
            PutOnPostfix(id, daysToLend.AsByteArray(), POSTFIX_DAYS_TO_LEND);

            Runtime.Notify("REG", id, borrower, borrowedAsset, amountBorrowed, amountCollateral, amountPremium, daysToLend);
            return true;
        }

        // locks collateral for created loan
        public static bool LockCollateralForLoan(byte[] id, byte[] borrower)
        {
            if (!Runtime.CheckWitness(borrower)) return false;
            if (Storage.Get(Storage.CurrentContext, id) != REGISTERED) return false;// invalid state transition
            if (GetOnPostfix(id, POSTFIX_BORROWER) != borrower) return false; // invalid borrower

            BigInteger collateralAmount = GetOnPostfix(id, POSTFIX_COLLATERAL_AMOUNT).AsBigInteger();
            Runtime.Notify(nameof(collateralAmount), collateralAmount);

            BigInteger balance = BalanceOf(borrower);
            Runtime.Notify(nameof(balance), balance);

            if (collateralAmount > balance) {
                Runtime.Notify("insufficient balance");
                return false;
            }
            BigInteger finalBalance = balance - collateralAmount;// decrease borrowers balance
            Storage.Put(Storage.CurrentContext, borrower, finalBalance);// update borrowers balance
            Storage.Put(Storage.CurrentContext, id, COLLATERAL_LOCKED);// update loan request status
            Transferred(borrower, null, collateralAmount);
            return true;
        }

        // add collateral to borrower
        private static bool unlockCollateralForLoan(byte[] id, byte[] borrower)
        {
            //if (Runtime.CheckWitness(borrower)) return false;
            if (GetOnPostfix(id, POSTFIX_BORROWER) != borrower) return false; // invalid borrower
            
            BigInteger collateralAmount = GetOnPostfix(id, POSTFIX_COLLATERAL_AMOUNT).AsBigInteger();
            BigInteger balance = BalanceOf(borrower);
            BigInteger finalBalance = balance + collateralAmount;// increase borrowers balance
            Storage.Put(Storage.CurrentContext, borrower, finalBalance);// update borrowers balance
            BigInteger zero = 0;
            PutOnPostfix(id, zero.AsByteArray(), POSTFIX_COLLATERAL_AMOUNT);
            Transferred(null, borrower, collateralAmount);
            return true;
        }

        // lender funds a loan
        public static bool FundLoan(byte[] id, byte[] lender)
        {
            if (!Runtime.CheckWitness(lender)) return false;
            if (Storage.Get(Storage.CurrentContext, id) != COLLATERAL_LOCKED) return false;// invalid state transition

            byte[] borrower = GetOnPostfix(id, POSTFIX_BORROWER);
            Runtime.Notify(nameof(borrower), borrower);
            if (borrower == lender) return false;// ignore same lender and borrower

            byte[] assetScriptHash = GetOnPostfix(id, POSTFIX_ASSET);
            Runtime.Notify(nameof(assetScriptHash), assetScriptHash);
            if (GetSenderByAssetScriptHash(assetScriptHash) != lender) return false;

            BigInteger assetAmount = GetOnPostfix(id, POSTFIX_ASSET_AMOUNT).AsBigInteger();
            BigInteger loanAmountOutput = GetTxOutputTotalAmount(borrower, assetScriptHash);

            loanAmountOutput = loanAmountOutput / neo_decimals;

            Runtime.Notify(nameof(assetAmount), assetAmount);
            Runtime.Notify(nameof(loanAmountOutput), loanAmountOutput);

            if (loanAmountOutput == 0 || loanAmountOutput < assetAmount) return false;

            PutOnPostfix(id, lender, POSTFIX_LENDER);
            PutOnPostfix(id, Now().AsByteArray(), POSTFIX_FUND_DATE);
            Storage.Put(Storage.CurrentContext, id, FUNDED); // update loan request status
            Runtime.Notify("LoanFUNDED");
            return true;
        }


        public static bool CancelLoan(byte[] id, byte[] canceller)
        {
            if (!Runtime.CheckWitness(canceller)) return false;
            Runtime.Log("pass checkwitness");
            byte[] borrower = GetOnPostfix(id, POSTFIX_BORROWER);
            if (borrower != canceller) return false;
            Runtime.Log("pass borrower is noteq canceller");

            byte[] loanStatus = Storage.Get(Storage.CurrentContext, id);
            if (loanStatus == REGISTERED || loanStatus == COLLATERAL_LOCKED)
            {
                Runtime.Log("can be cancancelable");
                Storage.Put(Storage.CurrentContext, id, CANCELLED);
                if (loanStatus == COLLATERAL_LOCKED)
                {
                    Runtime.Log("unlock loan");
                    unlockCollateralForLoan(id, borrower);
                }
                return true;
            }
            return false;
        }

        // lender claim collateral, if borrower doesnt repay loan in time period
        public static bool ClaimCollateral(byte[] id, byte[] lender)
        {
            if (!Runtime.CheckWitness(lender)) return false;
            if (Storage.Get(Storage.CurrentContext, id) != FUNDED) return false;// invalid state transition
            if (GetOnPostfix(id, POSTFIX_LENDER) != lender) return false;

            BigInteger daysToLend = GetOnPostfix(id, POSTFIX_DAYS_TO_LEND).AsBigInteger();
            BigInteger fundDate = GetOnPostfix(id, POSTFIX_FUND_DATE).AsBigInteger();
            BigInteger deadLine = fundDate + daysToLend;

            Runtime.Notify(nameof(daysToLend), daysToLend);
            Runtime.Notify(nameof(fundDate), fundDate);
            Runtime.Notify(nameof(deadLine), deadLine);

            if (deadLine > Now()) return false;

            // can claimable
            BigInteger collateralAmount = GetOnPostfix(id, POSTFIX_COLLATERAL_AMOUNT).AsBigInteger();
            BigInteger balance = BalanceOf(lender);
            BigInteger finalBalance = balance + collateralAmount;// increase lendes balance

            Runtime.Notify(nameof(collateralAmount), collateralAmount);
            Runtime.Notify(nameof(balance), balance);
            Runtime.Notify(nameof(finalBalance), finalBalance);

            Storage.Put(Storage.CurrentContext, lender, finalBalance);// update lenders balance
            BigInteger zero = 0;
            PutOnPostfix(id, zero.AsByteArray(), POSTFIX_COLLATERAL_AMOUNT);

            Storage.Put(Storage.CurrentContext, id, COLLATERAL_CLAIMED);// update loan status
            Transferred(null, lender, collateralAmount);

            Runtime.Notify("CollateralClaimed");

            return true;
        }

        // borrows repay loan with premium amount
        public static bool RepayLoan(byte[] id, byte[] borrower)
        {
            if (!Runtime.CheckWitness(borrower)) return false;
            if (Storage.Get(Storage.CurrentContext, id) != FUNDED) return false;// invalid state transition
            if (GetOnPostfix(id, POSTFIX_BORROWER) != borrower) return false;
            
            byte[] assetScriptHash = GetOnPostfix(id, POSTFIX_ASSET);
            if (borrower != GetSenderByAssetScriptHash(assetScriptHash)) return false;

            byte[] lender = GetOnPostfix(id, POSTFIX_LENDER);

            BigInteger assetAmount = GetOnPostfix(id, POSTFIX_ASSET_AMOUNT).AsBigInteger();
            BigInteger premiumAmount = GetOnPostfix(id, POSTFIX_PREMIUM_AMOUNT).AsBigInteger();
            BigInteger totalRequiredAmount = assetAmount + premiumAmount;
            BigInteger loanAmountOutput = GetTxOutputTotalAmount(lender, assetScriptHash);
            loanAmountOutput = loanAmountOutput / neo_decimals;

            Runtime.Notify(nameof(assetAmount), assetAmount);
            Runtime.Notify(nameof(premiumAmount), premiumAmount);
            Runtime.Notify(nameof(totalRequiredAmount), totalRequiredAmount);
            Runtime.Notify(nameof(loanAmountOutput), loanAmountOutput);

            if (loanAmountOutput < totalRequiredAmount) return false;

            unlockCollateralForLoan(id, borrower);
            Storage.Put(Storage.CurrentContext, id, LOAN_REPAID); // update loan request status
            Runtime.Notify("Repaid Successfully");
            return true;
        }



        // gets available balance
        public static BigInteger BalanceOf(byte[] scriptHash)
        {
            BigInteger currentBalance = Storage.Get(Storage.CurrentContext, scriptHash).AsBigInteger();
            return currentBalance;
        }

        public static bool MintTokens()
        {
            Runtime.Log("called mintTokenz");
            //getting script hash who sent tokens
            byte[] sender = GetSenderByAssetScriptHash(neo_asset_id);
            Runtime.Log("SENDER:" + sender);
            //check for references (if no outputs in invocation transaction's inputs)
            if (sender == null || sender.Length == 0)
            {
                Runtime.Log("SenderLengthEq0");
                return false;
            }

            //getting amount contributed in neo and set the balance
            BigInteger value = GetTxOutputTotalAmount(GetContractScriptHash(), neo_asset_id);
            Storage.Put(Storage.CurrentContext, "outputTotalAmount", value);// todo:remove me
            Runtime.Log("AMOUNT:"+ value);

            if (value == 0)
                return false;
            value = value / neo_decimals * basic_rate;
            BigInteger balance = BalanceOf(sender);
            Storage.Put(Storage.CurrentContext, sender, value + balance);
            AddSupply(value);
            Transferred(null, sender, value);
            Runtime.Notify("Minted Successfully");
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
            if (balance < value || balance == null || balance.AsByteArray().Length == 0)
                return false;

            Runtime.Notify("Transfer validated");

            //decreasing balance 
            Storage.Put(Storage.CurrentContext, from, balance - value);
            balance = 0;
            balance = BalanceOf(to);
            //increasing balance and notify
            Storage.Put(Storage.CurrentContext, to, balance + value);
            Transferred(from, to, value);
            Runtime.Notify("Transferred:", "from", from, "to", to, "value", value);
            return true;
        }

        private static void PutOnPostfix(byte[] key, byte[] value, char postfix)
        {
            string k = key.AsString() + postfix;
            Storage.Put(Storage.CurrentContext, k, value);
            Runtime.Notify("PUT", k, ":", value);
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

        // gets sender with scriot hash
        private static byte[] GetSenderByAssetScriptHash(byte[] assetId)
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] reference = tx.GetReferences();
            foreach (TransactionOutput output in reference)
            {
                if (output.AssetId == assetId) return output.ScriptHash;
            }
            return new byte[0] { };
        }

        // get smart contract script hash
        private static byte[] GetContractScriptHash()
        {
            return ExecutionEngine.ExecutingScriptHash;
        }

        // total asset amount that is sent to receiver
        private static BigInteger GetTxOutputTotalAmount(byte[] receiver, byte[] assetScriptHash)
        {
            Transaction tx = (Transaction)ExecutionEngine.ScriptContainer;
            TransactionOutput[] outputs = tx.GetOutputs();
            BigInteger value = 0;
            foreach (TransactionOutput output in outputs)
            {
                if (output.ScriptHash == receiver && output.AssetId == assetScriptHash)
                {
                    value += (BigInteger)output.Value;
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
            var supp = Storage.Get(Storage.CurrentContext, "totalSupply");
            if (supp == null || supp.Length == 0)
            {
                BigInteger zero = 0;
                return zero;
            }
            return supp.AsBigInteger();
        }
    }
}
