using System.Diagnostics;
using System.IO;
using System.Numerics;
using LunarParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Emulator;
using Neo.VM;

[TestClass]
public class ContractTests
{
    private static NeoEmulator emulator;

    // load the .avm file before tests run
    [TestInitialize]
    public void Setup()
    {
        //var path = TestContext.CurrentContext.TestDirectory.Replace("ICO-Unit-Tests", "ICO-Template");
        string avmPath = @"C:\OguzhanE\projects\NepLend\NepLend\bin\Debug\NepLend.avm";
        var avmBytes = File.ReadAllBytes(avmPath);
        emulator = new NeoEmulator(avmBytes);
    }

    [TestMethod]
    public void PutSomething()
    {
        var inputs = MakeParameters("xx");
        emulator.Reset(inputs);
        emulator.Run();

        // obtain the smart contract output
        var result = emulator.GetOutput();

        // validate output
        var symbol = result.GetString();
        Debug.WriteLine(symbol);
    }

    [TestMethod]
    public void GetSomething()
    {
        var inputs = MakeParameters("yy");
        emulator.Reset(inputs);
        emulator.Run();

        // obtain the smart contract output
        var result = emulator.GetOutput();

        // validate output
        var symbol = result.GetString();
        Debug.WriteLine(symbol);
    }

    [TestMethod]
    public void TestSymbol()
    {
        // create the inputs to be passed to the NEO smart contract
        var inputs = MakeParameters("symbol");
        // reset the Emulator then run it
        emulator.Reset(inputs);
        emulator.Run();

        // obtain the smart contract output
        var result = emulator.GetOutput();

        // validate output
        var symbol = result.GetString();
        Debug.WriteLine(symbol);
        Assert.IsTrue(symbol.Equals("NPL"));
    }

    [TestMethod]
    public void TestMintTokens()
    {
        var inputs = MakeParameters("mintTokens");
        emulator.Reset(inputs);
        emulator.Run();
        var result = emulator.GetOutput();
        Debug.WriteLine(result);
    }

    [TestMethod]
    public void TestStorage()
    {
        string id = "HkBGPAhvG";
        string revBorrower = "8d0ed7fd5e3c18616e03d2f6afb06287046ae019";
        var inputs = MakeParameters("testMe", id, revBorrower);
        
        emulator.Reset(inputs);
        emulator.Run();

        var result = emulator.GetOutput();
        var hex = result.GetByteArray().AsString();
        Debug.WriteLine(result);
    }

    [TestMethod]
    public void CanCreateLoan()
    {
        string loanId = "536b7677677570508a";
        string revBorrower = "8d0ed7fd5e3c18616e03d2f6afb06287046ae019";
        string revBorrowerAsset = "9b7cffdaa674beae0f930ebe6085af9093e5fe56b34a5c220ccdcf6efc336fc5";
        BigInteger amountBorrowed = 12;
        BigInteger amountCollateral = 3000;
        BigInteger amountPremium = 2;
        BigInteger daysToLend = 6;
        var inputs = MakeParameters("createLoanRequest", loanId, revBorrower, revBorrowerAsset, amountBorrowed,  amountCollateral, amountPremium, daysToLend);
        //public static bool CreateLoanRequest(byte[] id, byte[] borrower, byte[] borrowedAsset, BigInteger amountBorrowed, 
        //BigInteger amountCollateral, BigInteger amountPremium, BigInteger daysToLend)
        emulator.Reset(inputs);
        emulator.Run();

        var output = emulator.GetOutput();
        var result = output.GetBoolean();
        Assert.IsTrue(result);
    }

    public DataNode MakeParameters(string operation, params object[] args)
    {
        var inputs = DataNode.CreateObject(null);
        inputs.AddField("operation", operation);
        DataNode argsNode = DataNode.CreateArray("args");
        foreach(object obj in args)
        {
            argsNode.AddValue(obj);
        }
        if (args.Length == 0) argsNode.AddValue("tmp");
        inputs.AddNode(argsNode);
        return inputs;
    }
}