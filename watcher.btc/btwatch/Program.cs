using System;
using System.Threading.Tasks;

namespace btwatch
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            AsyncMain();
            while (true)
            {

                var line = Console.ReadLine();
            }
        }
        /// bitcoin-qt -server -rest -testnet //启动testnet   server rest 参数是为了rpc接口用 testnet端口18332
        /// bitcoin-qt -server -rest //启动  server rest 参数是为了rpc接口用 端口8332
        /// <summary>
        /// 
        /// </summary>
        static async void TestRest()
        {
            //restClient 无需验证 但是功能太少，都不能用block index 获取block
            NBitcoin.RPC.RestClient restC = new NBitcoin.RPC.RestClient(new Uri("http://127.0.0.1:18332"));
            var info = await restC.GetChainInfoAsync();
            Console.WriteLine("chain=" + info.Chain);
            Console.WriteLine("blockcount=" + info.Blocks);
        }
        static async void AsyncMain()
        {
            //使用rpcClient 需要配置验证用户名和密码，可用如下参数配置
            //bitcoin-qt -server -rest -testnet -rpcuser=1 -rpcpassword=1
            var key = new System.Net.NetworkCredential("1", "1");
            var uri = new Uri("http://127.0.0.1:18332");
            NBitcoin.RPC.RPCClient rpcC = new NBitcoin.RPC.RPCClient(key, uri);
            var binfo = await rpcC.GetBlockchainInfoAsync();

            Console.WriteLine("chain=" + binfo.Chain.Name);
            Console.WriteLine("blockcount=" + binfo.Blocks);

            var count = await rpcC.GetBlockCountAsync();
            Console.WriteLine("blockcount quick=" + count);

            for(var i=0;i<10000;i++)
            {
                await GetOneBlock(rpcC, i);
            }
        }
        static async Task GetOneBlock(NBitcoin.RPC.RPCClient rpcC, int index)
        {
            var block = await rpcC.GetBlockAsync(index);
            Console.WriteLine("block 123 hashquick=" + block.Header.HashMerkleRoot.ToString());
            Console.WriteLine("tran count=" + block.Transactions.Count);
            for (var i = 0; i < block.Transactions.Count; i++)
            {
                var tran = block.Transactions[i];
                Console.WriteLine("==tran " + i + "==:" + tran.GetHash());
                Console.WriteLine("--Input--");
                for (var vi = 0; vi < tran.Inputs.Count; vi++)
                {
                    Console.WriteLine("Input" + vi + ":  ref=" + tran.Inputs[vi].PrevOut.Hash.ToString() + " n=" + tran.Inputs[vi].PrevOut.N.ToString("X08"));
                }
                Console.WriteLine("--Output--");
                for (var vo = 0; vo < tran.Outputs.Count; vo++)
                {
                    var vout = tran.Outputs[vo];
                    var address = vout.ScriptPubKey.GetDestinationAddress(rpcC.Network);//注意比特币地址和网络有关，testnet 和mainnet地址不通用

                    Console.WriteLine("Output" + vo + ":  recvier=" + address + " money=" + vout.Value.ToString());
                }
            }
        }
    }
}
