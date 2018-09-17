using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace neo_outcaller
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            List<string> methodList = new List<string>();
            methodList.Add("add");
            methodList.Add("multiply");
            this.methodCombox.ItemsSource = methodList;
        }
        static string url = "http://27.115.95.118:20332";
        [ThreadStatic]
        static System.Net.WebClient wc = new System.Net.WebClient();
        static int _getCount()
        {
            if(wc==null)
                wc = new System.Net.WebClient();
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            //var getcounturl = url + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";
            Newtonsoft.Json.Linq.JObject upparam = new Newtonsoft.Json.Linq.JObject();
            upparam["jsonrpc"] = "2.0";
            upparam["id"] = 1;
            upparam["method"] = "getblockcount";
            upparam["params"] = new Newtonsoft.Json.Linq.JArray();

            var info = wc.UploadString(url, upparam.ToString());
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);         
            var count = (int)json["result"];
            return count;
        }
        static JArray CallScript(byte[] script)
        {
            if (wc == null)
                wc = new System.Net.WebClient();
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            Newtonsoft.Json.Linq.JObject upparam = new Newtonsoft.Json.Linq.JObject();
            upparam["jsonrpc"] = "2.0";
            upparam["id"] = 1;
            upparam["method"] = "invokescript";
            var _params = new Newtonsoft.Json.Linq.JArray();
            _params.Add(ThinNeo.Helper.Bytes2HexString(script));
            upparam["params"] = _params;

            var info = wc.UploadString(url, upparam.ToString());
            var result = JObject.Parse(info)["result"]["stack"] as JArray;
            return result;
            //Console.WriteLine(info);

        }
        class State
        {
            public int state;//0 wait //1?
        }

        System.Collections.Concurrent.ConcurrentDictionary<string, State> mapTxState = new System.Collections.Concurrent.ConcurrentDictionary<string, State>();
        void DoCallTran(ThinNeo.Transaction tx)
        {
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            Newtonsoft.Json.Linq.JObject upparam = new Newtonsoft.Json.Linq.JObject();
            upparam["jsonrpc"] = "2.0";
            upparam["id"] = 1;
            upparam["method"] = "sendrawtransaction";
            var _params = new Newtonsoft.Json.Linq.JArray();

            var data = tx.GetRawData();
            _params.Add(ThinNeo.Helper.Bytes2HexString(data));
            upparam["params"] = _params;

            var info = wc.UploadString(url, upparam.ToString());
            Console.WriteLine(info);

            var txid = tx.GetHash();
            State s = new State();
            s.state = 0;
            mapTxState[txid.ToString()] = s;

        }
        Random r = new Random();
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var prikey = ThinNeo.Helper.GetPrivateKeyFromWIF("L2CmHCqgeNHL1i9XFhTLzUXsdr5LGjag4d56YY98FqEi4j5d83Mv");

            var pubkey = ThinNeo.Helper.GetPublicKeyFromPrivateKey(prikey);
            var scripthash = ThinNeo.Helper.GetScriptHashFromPublicKey(pubkey);
            var addres = ThinNeo.Helper.GetAddressFromScriptHash(scripthash);


            ThinNeo.Transaction tx = new ThinNeo.Transaction();
            tx.inputs = new ThinNeo.TransactionInput[0];
            tx.outputs = new ThinNeo.TransactionOutput[0];
            tx.type = ThinNeo.TransactionType.InvocationTransaction;
            tx.version = 1;
            //附加一个见证人  ///要他签名
            tx.attributes = new ThinNeo.Attribute[1];
            tx.attributes[0] = new ThinNeo.Attribute();
            tx.attributes[0].usage = ThinNeo.TransactionAttributeUsage.Script;
            tx.attributes[0].data = scripthash;

            //拼接调用脚本
            var invokedata = new ThinNeo.InvokeTransData();
            tx.extdata = invokedata;
            invokedata.gas = 0;
            var sb = new ThinNeo.ScriptBuilder();

            MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();
            array.AddArrayValue("(hex160)" + scripthash.ToString());//witnesscall
            array.AddArrayValue("(hex160)" + scripthash.ToString());//witnessreturn
            array.AddArrayValue("(hex160)" + scripthash.ToString());//callscript
            array.AddArrayValue("(str)" + methodCombox.Text);//callmethod
            var _params = new MyJson.JsonNode_Array();
            _params.AddArrayValue(int.Parse(textbox1.Text));
            _params.AddArrayValue(int.Parse(textbox2.Text));
            array.Add(_params);//params
            _params.AddArrayValue("(int)" + r.Next());

            sb.EmitParamJson(array);
            sb.EmitPushString("outcall");
            ThinNeo.Hash160 contractaddr = new ThinNeo.Hash160("0x24192c2a72e0ce8d069232f345aea4db032faf72");
            sb.EmitAppCall(contractaddr);
            invokedata.script = sb.ToArray();

            //签名（谁签名）
            var msg = tx.GetMessage();
            var data = ThinNeo.Helper.Sign(msg, prikey);
            tx.AddWitness(data, pubkey, addres);

            DoCallTran(tx);
            //CallScript(invokedata.script);

        }


        private System.Windows.Threading.DispatcherTimer timer;
        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //开一个UI 定时器 刷UI
            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (s, ee) =>
            {
                updateCallUI();

            };
            timer.Start();

            //开一个线程检查交易
            Thread t = new Thread(threadCheckTx);
            t.IsBackground = true;
            t.Start();
        }
        void updateCallUI()
        {
            this.list1.Items.Clear();

            foreach (var item in mapTxState)
            {
                this.list1.Items.Add(item + item.Value.state.ToString());
            }

        }
        void threadCheckTx()
        {
            while (true)
            {
                foreach (var item in mapTxState)
                {
                    var txid = item.Key;
                    var sb = new ThinNeo.ScriptBuilder();

                    MyJson.JsonNode_Array array = new MyJson.JsonNode_Array();

                    array.AddArrayValue("(hex256)" + txid);
                    //array.AddArrayValue(new MyJson.JsonNode_Array());
                    sb.EmitParamJson(array);
                    sb.EmitPushString("getcallstate");
                    ThinNeo.Hash160 contractaddr = new ThinNeo.Hash160("0x24192c2a72e0ce8d069232f345aea4db032faf72");
                    sb.EmitAppCall(contractaddr);
                    var script = sb.ToArray();
                    var result = CallScript(script);
                    if (!string.IsNullOrEmpty(result[0]["value"].ToString()))
                        item.Value.state = (int) result[0]["value"][0]["value"];
                }
                System.Threading.Thread.Sleep(1000);
            }
        } 
        
    }
}
