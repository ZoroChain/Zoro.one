1.bitcoin 常用的客户端

https://bitcoin.org/en/download

bitcoin core 是目前最常用的客户端

我只部署测试了windows 版本

bitcoin直接提供 正式网 和 testnet 两个网络

C:\Program Files\Bitcoin>bitcoin-qt 启动
C:\Program Files\Bitcoin>bitcoin-qt -testnet 启动测试网

bitcoin 客户端提供了rpc机制

使用如下命令开启
C:\Program Files\Bitcoin>bitcoin-qt -server -rest -testnet -rpcuser=1 -rpcpassword=1
-server 是打开服务器，必选
-rest 是允许公共连接 但公共连接能取到的信息太少，不足以监视网络，可以不开
-testnet 表示打开测试网络，此时默认rpc端口为18332 ，不开测试网络，则默认rpc端口为8332
-rpcuser 配置rpc连接用户名 必选
-rpcpassword 配置rpc连接密码 必选


2.测试程序说明

nuget 安装NBitcoin 支持netcore

有RestClient，功能不足以监视，忽略

使用RPCClinet
			
			var key = new System.Net.NetworkCredential("1", "1");
            
			var uri = new Uri("http://127.0.0.1:18332");
            
			NBitcoin.RPC.RPCClient rpcC = new NBitcoin.RPC.RPCClient(key, uri);

得到区块信息与区块高度，见示例

可使用区块索引得到一个区块中所有的交易，见示例。

比特币网络上交易比较单纯，已dump，见示例。

utxo模型说明略

如何监测一个转入交易，看交易的output 里面有没有监视的地址即可