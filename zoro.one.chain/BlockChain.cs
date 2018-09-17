using System;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using System.Text;

namespace zoro.one.chain
{
    /// <summary>
    /// 
    /// </summary>
    public class BlockChain : IBlockChain
    {
        private LevelDB.DB db = null;
        private byte[] tablename = null;
        private LevelDB.Ex.Table table = null;

        public BlockChain(string dbPath, byte[] magic) {
            try
            {
                if (!File.Exists(dbPath))
                {
                    Console.WriteLine("Database not found. {0}", dbPath);
                    throw new Exception(String.Format("Database not found. {0}", dbPath));
                }

                if (magic == null || magic.Length == 0)
                {
                    throw new Exception(String.Format("Illegal table name. {0}", magic));
                }

                this.db = LevelDB.Ex.Helper.OpenDB(dbPath);
                this.tablename = magic;

                this.table = new LevelDB.Ex.Table(this.db, this.tablename);

                this.InitBlock();

                ThreadStart threadDelegate = new ThreadStart(TimerThread.Run);
                Thread thread = new Thread(threadDelegate);
                thread.IsBackground = true; // 设置为后台线程，主程序退出这个线程就会玩完儿了，不用特别管他
                thread.Start();

                Console.WriteLine("Zoro.One Version: {0}", this.GetType().Assembly.GetName().Version);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw ex;
            }
            finally
            {
                if (this.db != null)
                {
                    this.db.Dispose();
                }
            }
        }

        public void InitBlock()
        {
            var snapshot = LevelDB.Ex.Helper.CreateSnapshot(db);
            byte[] key =Encoding.ASCII.GetBytes("allblocks");
            var blocks = table.GetItem(snapshot, key) as LevelDB.Ex.Map;
            if (blocks == null)
            {
                blocks = new LevelDB.Ex.Map();
                table.PutItem(key, blocks);
            }
            snapshot = LevelDB.Ex.Helper.CreateSnapshot(db);
            if (blocks.Count(snapshot) == 0)
            {
                var batch = new LevelDB.Ex.WriteBatch(db);
                //写入创世块
                var blockzero = new LevelDB.Ex.Map();
                byte[] blockkey = BitConverter.GetBytes((UInt64)0);
                blocks.Batch_SetItem(batch, blockkey, blockzero);

                byte[] keydata = Encoding.ASCII.GetBytes("data");
                blockzero.Batch_SetItem(batch, keydata, new LevelDB.Ex.Bytes(new byte[0]));

                byte[] keyhash =Encoding.ASCII.GetBytes("hash");

                byte[] hash = SHA256.Create().ComputeHash(new byte[0]);
                blockzero.Batch_SetItem(batch, keyhash, new LevelDB.Ex.Bytes(hash));

                batch.Apply();
            }
        }
        public ulong GetBlockCount()
        {
            var snapshot = LevelDB.Ex.Helper.CreateSnapshot(db);
            byte[] key =Encoding.ASCII.GetBytes("allblocks");
            var blocks = table.GetItem(snapshot, key) as LevelDB.Ex.Map;
            if (blocks == null)
            {
                return 0;
            }
            return blocks.Count(snapshot);
        }
    }
}
