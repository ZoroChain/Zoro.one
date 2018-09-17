using Newtonsoft.Json.Linq;
using System;

namespace lib_neo_outcall_s
{
    public class ThreadSafe<T>
    {
        T value;
        object lockobj = new object();
        public T GetValue()
        {
            lock (lockobj)
            {
                return value;
            }
        }
        public void SetValue(T t)
        {
            lock (lockobj)
            {
                value = t;
            }
        }

    }
    public class CallItem
    {
        public int block;
        public string txid;
        public string callcontract;
        public JObject value;
        public override string ToString()
        {
            return "block:" + block + " call-txid:" + txid + "\r\n contract:" + callcontract +
                   "\r\n  notify state:" + value.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
    public class watcher
    {
        static string url = "http://27.115.95.118:20332";
        static System.Net.WebClient wc = new System.Net.WebClient();
        static int _getCount()
        {
            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            
            var count = (int)json["result"];
            return count;
        }
        static JObject _getBlock(int block)
        {
            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getblock&params=[" + block + ",1]";
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            if (info.Contains("result") == false)
                return null;
            return (JObject)json["result"];
        }
        static JArray _getNotify(string txid)
        {
            var jobj = new JObject();
            jobj["jsonrpc"] = "2.0";
            jobj["id"] = 1;
            jobj["method"] = "getapplicationlog";
            jobj["params"] = new JArray();
            (jobj["params"] as JArray).Add(txid);

            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getapplicationlog&params=[\"" + txid + "\"]";
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            if (json.ContainsKey("result") == false)
                return null;
            var ss = json["result"]["notifications"] as JArray;
            //var result = (JObject)(((JArray)json["result"])[0]);
            return json["result"]["notifications"] as JArray;

        }
        static ThreadSafe<int> nowheight = new ThreadSafe<int>();
        public static int GetHeight()
        {
            return nowheight.GetValue();
        }

        static ThreadSafe<int> parseheight = new ThreadSafe<int>();
        public static int GetParseHeight()
        {
            return parseheight.GetValue();
        }

        public static int GetCallItemCount()
        {
            return callitem.Count;
        }
        static System.Collections.Concurrent.ConcurrentQueue<CallItem> callitem = new System.Collections.Concurrent.ConcurrentQueue<CallItem>();

        public static CallItem PickCall()
        {
            CallItem outi = null;
            callitem.TryDequeue(out outi);
            return outi;
        }
        static bool bParse = false;
        public static void StartParse(int startblock)
        {
            bParse = true;
            parseheight.SetValue(startblock - 1);
        }
        static System.Collections.Generic.List<string> watchContract = new System.Collections.Generic.List<string>();
        //ThinNeo.Hash160 contractaddr = new ThinNeo.Hash160("0x24192c2a72e0ce8d069232f345aea4db032faf72");
        public static void AddWatchContract(string contractHash)
        {
            watchContract.Add(contractHash);
        }
        public static void StartWatcherThread()
        {
            System.Threading.Thread t = new System.Threading.Thread(_thread);
            t.IsBackground = true;
            t.Start();
        }

        static void _thread()
        {
            nowheight.SetValue(_getCount());


            DateTime timer = DateTime.Now;
            while (true)
            {

                var overtime = (DateTime.Now - timer).TotalSeconds;
                if (overtime > 10)
                {
                    nowheight.SetValue(_getCount());
                }
                int height = nowheight.GetValue();
                if (bParse)
                {
                    var next = parseheight.GetValue() + 1;
                    if (next < (height-1))//缓一个块
                    {
                        if (_parseHeight(next))
                        {
                            parseheight.SetValue(next);
                        }
                    }
                }

                System.Threading.Thread.Sleep(1);
            }
        }
        static bool _parseHeight(int height)
        {
            var block = _getBlock(height);
            if (block == null)
                return false;
            var txs = (JArray)block["tx"];
            foreach (JObject tx in txs)
            {
                var txid = (string)tx["txid"];
                
                var type = (string)tx["type"];
                if (type == "InvocationTransaction")
                {
                    var notify = _getNotify(txid);
                    if (notify == null)
                    {

                    }
                    else
                    {
                        var script = (string)tx["script"];
                        _parseCall(height, txid, script, notify);
                    }
                }
            }
            return true;
        }
        static string HexStr2String(string src)
        {
            byte[] data = new byte[src.Length / 2];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = byte.Parse(src.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
            }
            return System.Text.Encoding.UTF8.GetString(data);
        }
        static void _parseCall(int height, string txid, string script, JArray notify)
        {
            foreach (JObject n in notify)
            {
                var contract = (string)n["contract"];

                //过滤 事件太多，只监视关注的合约
                if (watchContract.Contains(contract) == false)
                    continue;

                var value = n["state"] as JObject;
                CallItem item = new CallItem();
                item.block = height;
                item.txid = txid;
                item.callcontract = contract;
                item.value = value;
                var firstv = ((value["value"] as JArray)[0] as JObject);
                var ftype = (string)firstv["type"];
                var fvalue = (string)firstv["value"];
                //if (ftype == "String")
                //    item.name = fvalue;
                //else if (ftype == "ByteArray")
                //    item.name = HexStr2String(fvalue);
                //else
                //    throw new Exception("error type");
                //no name here.
                callitem.Enqueue(item);

            }

        }
    }
}
