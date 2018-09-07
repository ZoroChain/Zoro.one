using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        }
        static string url = "https://api.nel.group/api/testnet";
        static System.Net.WebClient wc = new System.Net.WebClient();
        static int _getCount()
        {
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            //var getcounturl = url + "?jsonrpc=2.0&id=1&method=getblockcount&params=[]";
            Newtonsoft.Json.Linq.JObject upparam = new Newtonsoft.Json.Linq.JObject();
            upparam["jsonrpc"] = "2.0";
            upparam["id"] = 1;
            upparam["method"] = "getblockcount";
            upparam["params"] = new Newtonsoft.Json.Linq.JArray();

            var info = wc.UploadString(url, upparam.ToString());
            var json = Newtonsoft.Json.Linq.JObject.Parse(info);
            JObject result = (JObject)(((JArray)(json["result"]))[0]);
            var count = (int)result["blockcount"];
            return count;
        }
        public static void CallScript(byte[] script)
        {
            wc.Headers["content-type"] = "text/plain;charset=UTF-8";
            Newtonsoft.Json.Linq.JObject upparam = new Newtonsoft.Json.Linq.JObject();
            upparam["jsonrpc"] = "2.0";
            upparam["id"] = 1;
            upparam["method"] = "invokescript";
            var _params = new Newtonsoft.Json.Linq.JArray();
            _params.Add(ThinNeo.Helper.Bytes2HexString(script));
            upparam["params"] = _params;

            var info = wc.UploadString(url, upparam.ToString());
            Console.WriteLine(info);

        }
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
            //附加一个见证人
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
            //array.AddArrayValue("(str)protocol");
            //array.AddArrayValue(new MyJson.JsonNode_Array());
            sb.EmitParamJson(array);
            sb.EmitPushString("protocol");
            ThinNeo.Hash160 contractaddr = new ThinNeo.Hash160("0x24192c2a72e0ce8d069232f345aea4db032faf72");
            sb.EmitAppCall(contractaddr);
            invokedata.script = sb.ToArray();

            //签名
            var msg = tx.GetMessage();
            var data = ThinNeo.Helper.Sign(msg, prikey);
            tx.AddWitness(data, pubkey, addres);

            CallScript(invokedata.script);

        }
    }
}
