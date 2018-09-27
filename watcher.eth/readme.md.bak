1.以太坊节点，最常用的是 geth，go语言实现的版本

启动需要带参数

我测试使用

geth --rpc --syncmode "fast" --cache=512 console

--rpc 是打开rpc接口，默认 http://localhost:8545 可以用参数改

--syncmode "fast" 是同步模式，有 full fast light 三种，据说full要几天， fast 一天，light马上就用，但是安全性最低

--cache=512 512M缓存，据说越大越快，我没有测试

console 打开控制台，如果不加这个，这个节点没有任何交互。

控制台常用命令

eth.syncing 同步状态检测
eth.getBlockNumber 显示当前块（即高度）

发交易要考虑 eth.getBlockNumber

而我们只是监测，不用管，主要盯住 eth.syncing 里面的 CurrentBlock 即可。eth.syncing中的HighestBlock为网络最高块

2.watch程序的使用

依赖nuget Nethereum.Geth

需要的指令
a.Web3.Eth.Syncing 得到网络最高块和连接的节点的可用块，即CurrentBlock HighestBlock
b.Web3.Eth.Blocks.GetBlockWithTransactionsByNumber 根据高度得到一个块和他里面的交易
c.交易的from to value 就是 eth的转移量。其它的代币都是erc20 没研究怎么观察，eth 直接观察 from to value 即可

watch 演示程序用法
a.先打开本地geth节点,演示程序连接         private const string url = "http://127.0.0.1:8545/";
b.输入 info 执行Web3.Eth.Syncing
c.输入 getblock 123123 显示制定块的信息

3.爬虫的开发，略
此处已经提供了所有的功能，爬虫接着堆出逻辑即可
