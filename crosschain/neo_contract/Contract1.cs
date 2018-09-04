using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using System.ComponentModel;
using System.Numerics;

namespace Nep5_Contract
{
    public class ContractNep5 : SmartContract
    {
        //nep5 notify
        public delegate void deleOutCall(byte[] callscript, string callmethod, byte[] witnessreturn, object[] _params);
        [DisplayName("outcall")]
        public static event deleOutCall notifyOutCall;

        public delegate void deleOutCallReturn(byte[] txid, byte[] returnvalue);
        [DisplayName("outcallreturn")]
        public static event deleOutCallReturn notifyOutcallReturn;

        public static string Protocol()
        {
            return "Zoro.NeoRPC";
        }

        public class CallState
        {
            public int state;//1 incall 2 havereturn
            public byte[] witnesscall;
            public byte[] witnessreturn;

            public byte[] returnvalue;
        }

        /// <summary>
        /// 发起一个外部调用
        /// </summary>
        /// <param name="callscript">外部调用的合约ID</param>
        /// <param name="callmethod">外部调用的方法名</param>
        /// <param name="witness">外部调用的见证人</param>
        /// <param name="_params">外部调用的参数</param>
        /// <returns></returns>
        public static bool OutCall(byte[] witnesscall, byte[] witnessreturn, byte[] callscript, string callmethod, object[] _params)
        {
            //必须由发起鉴证人签名
            if (!Runtime.CheckWitness(witnesscall))
                return false;
            var txid = (ExecutionEngine.ScriptContainer as Transaction).Hash;

            var key = new byte[] { 0x11 }.Concat(txid);
            var v = new CallState();
            v.state = 1;
            v.witnesscall = witnesscall;
            v.witnessreturn = witnessreturn;
            v.returnvalue = new byte[] { };
            var data = Neo.SmartContract.Framework.Helper.Serialize(v);
            Storage.Put(Storage.CurrentContext, key, data);

            notifyOutCall(callscript, callmethod, witnessreturn, _params);
            return true;
        }
        /// <summary>
        /// 取消一个外部调用，由用户发起
        /// </summary>
        /// <param name="txid"></param>
        /// <returns></returns>
        public static bool CancelOutCall(byte[] txid)
        {
            var key = new byte[] { 0x11 }.Concat(txid);
            var data = Storage.Get(Storage.CurrentContext, key);
            if (data.Length == 0)
                return false;
            CallState s = Neo.SmartContract.Framework.Helper.Deserialize(data) as CallState;
            if (s.state == 1)
            {
                //必须由发起鉴证人签名
                if (!Runtime.CheckWitness(s.witnesscall))
                    return false;

                Storage.Delete(Storage.CurrentContext, key);
                return true;
            }
            return false;
        }
        public static bool ReturnValue(byte[] txid, byte[] returnvalue)
        {
            var key = new byte[] { 0x11 }.Concat(txid);
            var data = Storage.Get(Storage.CurrentContext, key);
            if (data.Length == 0)
                return false;
            CallState s = Neo.SmartContract.Framework.Helper.Deserialize(data) as CallState;

            if (s.state == 1)
            {
                //必须由返回值鉴证人签名
                if (!Runtime.CheckWitness(s.witnessreturn))
                    return false;

                s.state = 2;
                s.returnvalue = returnvalue;
                data = Neo.SmartContract.Framework.Helper.Serialize(s);
                Storage.Put(Storage.CurrentContext, key, data);
                return true;
            }
            return false;
        }

        public static CallState GetCallState(byte[] txid)
        {
            var key = new byte[] { 0x11 }.Concat(txid);
            var data = Storage.Get(Storage.CurrentContext, key);
            if (data.Length == 0)
                return null;
            CallState s = Neo.SmartContract.Framework.Helper.Deserialize(data) as CallState;
            return s;

        }

        public static object Main(string method, object[] args)
        {
            var magicstr = "2018-09-04";

            if (Runtime.Trigger == TriggerType.Verification)//取钱才会涉及这里
            {
                return true;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                //this is in nep5
                if (method == "protocol") return Protocol();

                if (method == "outcall")
                {
                    byte[] witnesscall = (byte[])args[0];
                    byte[] witnessreturn = (byte[])args[1];

                    byte[] callscript = (byte[])args[2];
                    string callmethod = (string)args[3];
                    object[] _params = (object[])args[4];
                    return OutCall(witnesscall, witnessreturn, callscript, callmethod, _params);
                }
                if (method == "canceloutcall")
                {
                    byte[] txid = (byte[])args[0];
                    return CancelOutCall(txid);
                }
                if (method == "returnvalue")
                {
                    byte[] txid = (byte[])args[0];
                    byte[] v = (byte[])args[1];
                    return ReturnValue(txid, v);
                }
                if (method == "getcallstate")
                {
                    byte[] txid = (byte[])args[0];
                    return GetCallState(txid);
                }
            }
            return false;
        }

    }
}
