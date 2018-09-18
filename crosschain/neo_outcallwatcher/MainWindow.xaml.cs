using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using lib_neo_outcall_s;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ThinNeo;

namespace neo_outcallwatcher
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        static string url = "http://27.115.95.118:20332";

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            lib_neo_outcall_s.watcher.StartParse(int.Parse(txtBlockHeight.Text));
            //list1.Items.Add("height=" + n);
        }

        System.Windows.Threading.DispatcherTimer timer;

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            lib_neo_outcall_s.watcher.AddWatchContract("0x24192c2a72e0ce8d069232f345aea4db032faf72");
            lib_neo_outcall_s.watcher.StartWatcherThread();

            timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += (s, ee) =>
            {
                var n = lib_neo_outcall_s.watcher.GetHeight();
                var np = lib_neo_outcall_s.watcher.GetParseHeight();
                this.label01.Content = "height=" + n + "   parse height=" + np;
                if (lib_neo_outcall_s.watcher.GetCallItemCount() > 0)
                {
                    var item = lib_neo_outcall_s.watcher.PickCall();
                    this.list1.Items.Add(item);
                }
            };
            timer.Start();
        }

        private int Method(string method, int a, int b)
        {
            if (method == "add")
                return a + b;
            if (method == "multiply")
                return a * b;
            else
            {
                return 0;
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (this.btnStop.Content.ToString() == "Stop Parse")
            {
                timer.Stop();
                this.btnStop.Content = "Continue Parse";
            }

            else
            {
                timer.Start();
                this.btnStop.Content = "Stop Parse";

            }
        }

        private void btnSendReturn_Click(object sender, RoutedEventArgs e)
        {
            if (this.list1.SelectedItems.Count < 1)
            {
                MessageBox.Show("please choose an item");
                return;
            }

            var item = this.list1.SelectedItems[0] as CallItem;
            var a = (int)item.value["value"][5]["value"][0]["value"];
            var b = Convert.ToInt32(item.value["value"][5]["value"][1]["value"].ToString(), 16);
            var bytemethod = ThinNeo.Helper.HexString2Bytes(item.value["value"][3]["value"].ToString());
            var strmethod = Encoding.UTF8.GetString(bytemethod);
            var returnvalue = Method(strmethod, a, b);
            var ret = new ReturnInfo();
            ret.a = a;
            ret.b = b;
            ret.returnvalue = returnvalue;
            SendReturn(item.txid, ret);
        }

        private void SendReturn(string txid, ReturnInfo ret)
        {
            var prikey = ThinNeo.Helper.GetPrivateKeyFromWIF("");
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
            array.AddArrayValue("(hex256)" + txid); //txid
            array.AddArrayValue("(int)" + ret.returnvalue); //returnvalue
            var _params = new MyJson.JsonNode_Array();
            array.Add(_params);//params
            Random r = new Random();
            _params.AddArrayValue("(int)" + r.Next());

            sb.EmitParamJson(array);
            sb.EmitPushString("returnvalue");
            ThinNeo.Hash160 contractaddr = new ThinNeo.Hash160("0x24192c2a72e0ce8d069232f345aea4db032faf72");
            sb.EmitAppCall(contractaddr);
            invokedata.script = sb.ToArray();

            //签名 （谁来签名）
            var msg = tx.GetMessage();
            var data = ThinNeo.Helper.Sign(msg, prikey);
            tx.AddWitness(data, pubkey, addres);

            System.Net.WebClient wc = new System.Net.WebClient();

            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            Newtonsoft.Json.Linq.JObject upparam = new Newtonsoft.Json.Linq.JObject();
            upparam["jsonrpc"] = "2.0";
            upparam["id"] = 1;
            upparam["method"] = "sendrawtransaction";
            var _vparams = new Newtonsoft.Json.Linq.JArray();

            var vdata = tx.GetRawData();
            _vparams.Add(ThinNeo.Helper.Bytes2HexString(vdata));
            upparam["params"] = _vparams;

            var strdata = upparam.ToString(Formatting.None);
            var info = wc.UploadString(url, strdata);
            //Console.WriteLine(info);
            var this_txid = tx.GetHash();
            ret.txid = this_txid.ToString();
            ret.rsp = info;
            this.list2.Items.Add(ret);
            
        }

        private void btnGetReturnNotify_Click(object sender, RoutedEventArgs e)
        {
            if (this.list2.SelectedItems.Count < 1)
            {
                MessageBox.Show("please choose a send return item");
                return;
            }

            var ret = this.list2.SelectedItems[0] as ReturnInfo;

            GetReturnNotify(ret.txid);
        }

        private void GetReturnNotify(string txid)
        {
            var jobj = new JObject();
            jobj["jsonrpc"] = "2.0";
            jobj["id"] = 1;
            jobj["method"] = "getapplicationlog";
            jobj["params"] = new JArray();
            (jobj["params"] as JArray).Add(txid);

            var getcounturl = url + "?jsonrpc=2.0&id=1&method=getapplicationlog&params=[\"" + txid + "\"]";
            System.Net.WebClient wc = new System.Net.WebClient();
            var info = wc.DownloadString(getcounturl);
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            if (json.ContainsKey("result") == false)
                info = "unconfirmed";
            //var ss = json["result"]["notifications"] as JArray;
            //var notifications = json["result"]["notifications"] as JArray;
            this.list3.Items.Add(info);
        }
    }

    public class ReturnInfo
    {
        public int a;
        public int b;
        public int returnvalue;
        public string txid;
        public string rsp;

        public override string ToString()
        {
            return "a:" + a + " b:"+b+ " returnvalue:" + returnvalue + "\r\n txid:" + txid + "\r\n response: " + rsp;
        }
    }
}
