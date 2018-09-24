# NEO 跨链通信
## 通信
通信是什么？无非就是信息的传递。一般是一方主动发送信息，另一方接收信息；一方暴露出信息，一方主动获取（监听）信息。
## NEO 智能合约
基于通信的本质，NEO 的跨链通讯可以使用智能合约来实现，大体思路是用智能合约实现暴露信息的接口和接收信息的接口，由于 NEO 不会主动向外发送信息，所以需要一种方式使得 NEO 能产生出信息，外部来主动获取，产生信息可以使用 NEO 的 log 机制、也就是 notifications（Notify）来实现。

### notifications（Notify）
区块链上记录的只有交易、智能合约的部署和调用也是通过发送交易来实现，所以要通过 NEO 区块链得到交易以外的信息，就需要用到一些附加操作。比如智能合约的运行可以产生 log，即 notifications，也叫作 notify，nep5 类型资产转账交易都会默认产生 notifications，因为 nep5 中的 token 资产转账方法 transfer 默认实现了产生 notifications 的方法。除了 nep5 资产转账以外，其他调用合约的时候也可以产生 notifications，只需要在智能合约中增加产生 notifications 的方法即可。
### 获取 notifications
知道了可以产生 notifications（Notify），那么 notifications 保存在哪里呢？如何获取到它呢？
首先，notifications 不保存在区块链上，它只有在智能合约执行到产生 notifications 的语句时会输出，在使用 cli 节点同步区块数据时会将所有智能合约相关的操作在智能合约虚拟机 neo-vm 中运行一次，合约的状态等信息就是在运行过程中推出来的。在启动 cli 节点时，如果打开了 --log 命令，节点在运行每个区块中的智能合约相关操作时，会将输出的 notifications 保存到本地 nel-cli 目录下的 ApplicationLogs_ 文件夹中，记录为 txid.json 的文件(2.9 以后改为 区块高度.ldb 的文件了，如 003898.ldb)。然后我们使用 cli 提供的 rpc 接口 getapplicationlog 就可以查到对应的 notifications，结构如下：
```
{
    GET：http://27.115.95.118:20332?jsonrpc=2.0&id=1&method=getapplicationlog&params=["0xe19ecff18894315e088402fe0d53f7a23ff9f2a54dc1042dc9f31212f4b6764a"]


    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "txid": "0x350f91ffc04cca4c8c6ff26f0699e8dce0b397d2abd674ed686c041e2e88504e",
        "vmstate": "HALT, BREAK",
        "gas_consumed": "1.539",
        "stack": [
            {
                "type": "Integer",
                "value": "1"
            }
        ],
        "notifications": [
            {
                "contract": "0x24192c2a72e0ce8d069232f345aea4db032faf72",
                "state": {
                    "type": "Array",
                    "value": [
                        {
                            "type": "ByteArray",
                            "value": "6f757463616c6c"
                        },
                        {
                            "type": "ByteArray",
                            "value": "63616c6c"
                        },
                        {
                            "type": "ByteArray",
                            "value": "f25d29b0059a3feecd3862fea44fdb4351a67755"
                        },
                        {
                            "type": "ByteArray",
                            "value": "6d756c7469706c79"
                        },
                        {
                            "type": "ByteArray",
                            "value": "f25d29b0059a3feecd3862fea44fdb4351a67755"
                        },
                        {
                            "type": "Array",
                            "value": [
                                {
                                    "type": "Integer",
                                    "value": "12"
                                },
                                {
                                    "type": "Integer",
                                    "value": "8"
                                },
                                {
                                    "type": "ByteArray",
                                    "value": "8ced4f74"
                                }
                            ]
                        }
                    ]
                }
            }
        ]
    }
}
```
notifications 是智能合约运行的 log，contract 是智能合约 hash；
state 是输出的数据，value 中是合约中设定的要输出的内容。
### 使用 notifications
最典型的是可以使用 notifications 实现 nep5 资产的充值：
1. 通过 getblock api 获取每个区块的详情，其中便包括该区块中所有交易的详情；
2. 分析每笔交易的交易类型，过滤出所有类型为"InvocationTransaction"的交易，任何非"InvocationTransaction"类型的交易都不可能成为 NEP-5 类型资产的转账交易；
3. 调用 getapplicationlog api 获取每笔"InvocationTransaction"交易的详情，分析交易内容完成用户充值。
之后我们要使用 notifications 实现跨链通信。

