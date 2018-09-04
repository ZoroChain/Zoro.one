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
            return block + " call:" + callcontract + "\r\n    v=" + value.ToString(Newtonsoft.Json.Formatting.None);
        }
    }
    public class watcher
    {
        static string url = "https://api.nel.group/api/testnet";
        static System.Net.WebClient wc = new System.Net.WebClient();
        static int _getCount()
        {
            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            JObject result = (JObject)(((JArray)(json["result"]))[0]);
            var count = (int)result["blockcount"];
            return count;
        }
        static JObject _getBlock(int block)
        {
            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getblock&params=[" + block + ",1]";
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            return (JObject)(((JArray)json["result"])[0]);
        }
        static JArray _getNotify(string txid)
        {
            var jobj = new JObject();
            jobj["jsonrpc"] = "2.0";
            jobj["id"] = 1;
            jobj["method"] = "getnotify";
            jobj["params"] = new JArray();
            (jobj["params"] as JArray).Add(txid);

            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getnotify&params=[\"" + txid + "\"]";
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            var result = (JObject)(((JArray)json["result"])[0]);
            return result["notifications"] as JArray;

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
                    if (next <= height)
                    {
                        _parseHeight(next);
                        parseheight.SetValue(next);
                    }
                }

                System.Threading.Thread.Sleep(1);
            }
        }
        static void _parseHeight(int height)
        {
            var block = _getBlock(height);
            var txs = (JArray)block["tx"];
            foreach (JObject tx in txs)
            {
                var txid = (string)tx["txid"];
                var type = (string)tx["type"];
                if (type == "InvocationTransaction")
                {
                    var notify = _getNotify(txid);
                    var script = (string)tx["script"];
                    _parseCall(height, txid, script, notify);
                }
            }
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
