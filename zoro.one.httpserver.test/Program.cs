using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace zoro.one.httpserver.test
{
    class Program
    {
        static bool bExit = false;
        static void Main(string[] args)
        {
            Console.WriteLine("http server test!");
            InitHttp();

            InitMenu();
            while (!bExit)
            {
                Console.Write(">");
                var line = Console.ReadLine();
                DoMenu(line);
            }
        }
        static void InitMenu()
        {

        }
        static void DoMenu(string line)
        {

        }
        static zoro.one.httpserver.httpserver http = new one.httpserver.httpserver();
        static void InitHttp()
        {

            var pfxpath = "http" + System.IO.Path.DirectorySeparatorChar + "214541951070440.pfx";
            var password = "214541951070440";

            http.Start(80, 443, pfxpath, password);

            Console.WriteLine("start http on 80,https on 443");

            //设置一个path,当访问此httpPath时，调用此函数进行处理
            http.SetHttpAction("/", httpTest1);
            http.SetFailAction(httpFail);//设置http访问出现问题的处理函数

            //设置一个jsonrpc路径，以及某个method 对应的处理函数
            http.AddJsonRPC("/rpc", "hello", rpcTest1);
            http.SetJsonRPCFail("/rpc", rpcFail);//设置jsonrpc出错的处理函数
        }
        async static Task<JObject> rpcTest1(JObject json)
        {
            JObject fuck = new JObject();
            fuck["abc"] = "aa";
            return fuck;
        }
        async static Task<JSONRPCController.ErrorObject> rpcFail(JObject json, string errmsg)
        {
            var errobj = new JSONRPCController.ErrorObject();
            errobj.message = errmsg;
            errobj.code = -32000;
            errobj.data = new JObject();
            errobj.data["hahah"] = 12;
            return errobj;
        }
        async static Task httpTest1(HttpContext context)
        {
            var p = context.Request.Path;
            byte[] writedata = System.Text.Encoding.UTF8.GetBytes("hello world." + p);

            await context.Response.Body.WriteAsync(writedata, 0, writedata.Length);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json;charset=UTF-8";
            //"text/html;charset=UTF-8"; ;
            context.Response.ContentLength = writedata.Length;

            return;
        }
        async static Task httpFail(HttpContext context)
        {
            var p = context.Request.Path;
            byte[] writedata = System.Text.Encoding.UTF8.GetBytes("you 404:" + p);

            await context.Response.Body.WriteAsync(writedata, 0, writedata.Length);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json;charset=UTF-8";
            //"text/html;charset=UTF-8"; ;
            context.Response.ContentLength = writedata.Length;

            return;
        }
    }
}
