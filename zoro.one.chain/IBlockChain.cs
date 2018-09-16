namespace zoro.one.chain
{
    /// <summary>
    /// 
    /// </summary>
    public interface IBlockChain
    {
        /// <summary>
        /// 
        /// </summary>
        void InitBlock();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        ulong GetBlockCount();
    }
}
