using Nethereum.Geth;
using Nethereum.Hex.HexTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace etwatch
{
    class Program
    {
        private static List<string> userAddrList = new List<string>();
        static void Main(string[] args)
        {
            userAddrList.Add("adsada");
            userAddrList.Add("adqqwq");
            SyncMain(args);
            while (true)
            {
                System.Threading.Thread.Sleep(1);
            }
            Console.ReadKey();
        }
        async static void SyncMain(string[] args)
        {
            ETWatcher watcher = new ETWatcher();

            Console.WriteLine("etwatcher");
            while (true)
            {
                try
                {
                    Console.Write(">");
                    var line = Console.ReadLine();
                    var cmds = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    if (cmds.Length == 0) continue;
                    var cmd = cmds[0].ToLower();
                    if (cmd == "info")
                    {
                        await watcher.Info();
                    }
                    if (cmd == "getblock")
                    {
                        var n = UInt64.Parse(cmds[1]);
                        await watcher.GetBlock(n);
                    }
                }
                catch (Exception err)
                {
                    Console.WriteLine("err:" + err.Message);

                }
            }
        }
    }

    class ETWatcher
    {
        private const string url = "http://127.0.0.1:8545/";
        private Web3Geth Web3 = new Web3Geth(url);
        public async Task Info()
        {
            var sync = await Web3.Eth.Syncing.SendRequestAsync();
            Console.WriteLine("eth.Syncing.CurrentBlock=" + sync.CurrentBlock.Value);
            Console.WriteLine("eth.Syncing.HighestBlock=" + sync.HighestBlock.Value);
            Console.WriteLine("eth.Syncing.IsSyncing=" + sync.IsSyncing);
            Console.WriteLine("eth.Syncing.StartingBlock=" + sync.StartingBlock.Value);
            var bn = await Web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            Console.WriteLine("eth.BlockNumber=" + bn.Value);
        }
        public async Task GetBlock(UInt64 n)
        {
            var block = await Web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(n));
            Console.WriteLine("blockhash=" + block.BlockHash);
            Console.WriteLine("txcount" + block.Transactions.Length);
            for (var i = 0; i < block.Transactions.Length; i++)
            {
                var tran = block.Transactions[i];
                Console.WriteLine("==TX" + i + "== " + tran.TransactionHash);
                Console.WriteLine("tx.input=" + tran.Input);
                Console.WriteLine("tx.From=" + tran.From);
                Console.WriteLine("tx.to=" + tran.To);
                decimal v = (decimal)tran.Value.Value;
                decimal v2 = 1000000000000000000;
                Console.WriteLine("tran.value(ETH)=" + (v / v2));

            }
        }

    }
}
