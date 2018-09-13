## 实现

* 实现原理：<br>
 NEO的nep5转账交易可以产生notify，ZoroChain可以通过观察notify得到NEO链的信息；
ZoroChain也可以通过构造NEO链上的交易将信息带到NEO链上。<br>
* 实现机制：<br>
     1、在NEO上发支持输出notify的布智能合约，为了安全、需要支持验证签名；<br>

     2、ZoroChain根据调用合约的交易id进行监控获取notify，从而得到NEO链的信息；<br>

     3、ZoroChain 向Neo链发送具备专用私钥签名的交易，用于向Neo链发送数据。<br>
* 实现过程：<br>
    1、neo端交易输出notify携带信息，该交易称为A。<br>

    2、ZoroChain捕获到这个信息，在rootchain上发起一笔对应的交易，用A作为该交易的ID。
足够的记账节点捕获到该信息，该信息才能上链（这需要额外的机制，不在此处赘述）。<br>

    3、该交易执行完毕之后，ZoroChain向NEO对应的智能合约发起一笔交易，通知这次调用的返回结果，该交易一样留下独特的Notify记号。交易在记账节点执行，每个记账节点都会尝试向NEO发起这笔交易（是同一笔交易）。

ZoroChain 捕获到交易A 对应的 返回结果 notify 后，在rootchain上发起一笔对应的调用完成交易
这个过程可以记为 Rpccall_zoro(txid,state,“xxx”,[]);

对于NEO端来说，他会看到两笔invoke交易，一笔用于向zorochain传递参数（输出notify的交易），一笔得到zorochain的执行结果（通知调用的返回结果）。

对于ZoroChain端来说，他也会看到两笔交易，一笔传递参数（在rootchain上发起交易的交易），一笔显示结果（通知调用的返回结果）<br>