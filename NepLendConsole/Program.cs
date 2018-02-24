using Neo.JsonRpc.Client;
using Neo.RPC.Helpers;
using Neo.RPC.Services;
using Neo.RPC.Services.Contract;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NepLendConsole
{
    class Program
    {
        static void PrintAddr(string hex)
        {
            Console.WriteLine(hex.HexToBytes().Reverse().ToHexString());
        }
        static void Main(string[] args)
        {
            byte[] by = { 31, 32 };
            Console.WriteLine(by.Length);
            //byte[] REGISTERED = { 32, 31 };
            //Console.WriteLine(REGISTERED.ToHexString());
            //var encoded = System.Text.Encoding.UTF8.GetString(REGISTERED);
            //Console.WriteLine(encoded);
            //return;
            //PrintAddr("9b7cffdaa674beae0f930ebe6085af9093e5fe56b34a5c220ccdcf6efc336fc5");
            //PrintAddr("8d0ed7fd5e3c18616e03d2f6afb06287046ae019");
            //PrintAddr("23ba2703c53263e8d6e522dc32203339dcd8eee9");
            //PrintAddr("AJ8hNruhB2FVeEsUchsx6NFP7wvmJUeXkg");
            //byte[] neo_asset_id = { 155, 124, 255, 218, 166, 116, 190, 174, 15, 147, 14, 190, 96, 133, 175, 144, 147, 229, 254, 86, 179, 74, 92, 34, 12, 205, 207, 110, 252, 51, 111, 197 };
            //Console.WriteLine("c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b".HexToBytes().Reverse().ToHexString());
            //Console.WriteLine(Helper.ToHexString(neo_asset_id));
            //Console.WriteLine("9b7cffdaa674beae0f930ebe6085af9093e5fe56b34a5c220ccdcf6efc336fc5".HexToBytes().Reverse().ToHexString());
            //Console.WriteLine("c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b");
            //var v1 = "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b".
            //Console.WriteLine(Helper.ToHexString(neo_asset_id));
            //var b = Encoding.Default.GetBytes("c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b");
            //Console.WriteLine(Helper.ToHexString(b));
            //foreach (var item in "c56f33fc6ecfcd0c225c4ab356fee59390af8560be0e930faebe74a6daff7c9b".HexToBytes().Reverse())
            //{
            //    Console.Write($"{item}, ");
            //}
            //Console.WriteLine(Helper.HextoString("23ba2703c53263e8d6e522dc32203339dcd8eee9"));
            MainAsync(args).GetAwaiter().GetResult();
            Console.ReadLine();
        }
        
        static async Task MainAsync(string[] args)
        {
            var seedUrl = "http://127.0.0.1:30333";//http://seed5.neo.org:10332;
            var scHash = "23c5ab74cc4bbed00da03ca4c347b83f8db67b35";
            var client = new RpcClient(new Uri(seedUrl));
            var contractState = new NeoGetStorage(client);
            string[] keys = new string[] { "42796a4c673973764d"+'A', "42796a4c673973764dB", "42796a4c673973764dC", "42796a4c673973764dD", "42796a4c673973764dE" };
            foreach(var key in keys)
            {
                //var hexKey = Helper.ToHexString(Encoding.Default.GetBytes(key));
                //var val = await contractState.SendRequestAsync(scHash, hexKey);
                try
                {
                    var val = await contractState.SendRequestAsync(scHash, key);
                    Console.WriteLine($"->{key}:{(val == null ? "null" : Helper.HextoString(val))}");
                }
                catch (Exception)
                {

                }
            }
        }
        //var scriptHash = "08e8c4400f1af2c20c28e0018f29535eb85d15b6"; //TNC token
        //var nep5Service = new NeoNep5Service(client, scriptHash);
        //var name = await nep5Service.GetName();
        //var decimals = await nep5Service.GetDecimals();
        //var totalsupply = await nep5Service.GetTotalSupply(decimals);
        //var symbol = await nep5Service.GetSymbol();
        //var balance = await nep5Service.GetBalance("0x0ff9070d64d19076d08947ba4a82b72709f30baf", decimals);
    }
}
