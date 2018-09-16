﻿using System;
using zoro.one.chain;

namespace zoro.one
{
    class Program
    {
        static IBlockChain blockChain = new BlockChain();
        static zoro.one.httpserver.httpserver http = new one.httpserver.httpserver();
        static bool bExit = false;
        static void Main(string[] args)
        {

            byte[] magic = BitConverter.GetBytes((UInt64)12345);
            blockChain.Start("c:\\zorotest.1", magic);
            InitHttp();

            InitMenu();
            while (!bExit)
            {
                Console.Write(">");
                var line = Console.ReadLine();
                DoMenu(line);
            }
        }
        static void InitHttp()
        {

            var pfxpath = "http" + System.IO.Path.DirectorySeparatorChar + "214541951070440.pfx";
            var password = "214541951070440";

            http.Start(80, 443, pfxpath, password);
            Console.WriteLine("start http on 80,https on 443");

        }
        static void InitMenu()
        {
            ShowMenu();
        }
        static void ShowMenu()
        {
            Console.WriteLine("====menu====");
            Console.WriteLine("? or help:show this menu.");
            Console.WriteLine("exit: quit this app");
            Console.WriteLine("count: show block count");
            Console.WriteLine("test.add: add empty block");
        }
        static void DoMenu(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;
            var words = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            if (words.Length == 0)
                return;

            switch (words[0].ToLower())
            {
                case "?":
                case "help":
                    ShowMenu();
                    break;
                case "exit":
                    bExit = true;
                    break;
                case "count":
                    {
                        Console.WriteLine("count=" + blockChain.GetBlockCount());
                    }
                    break;
                case "test.add":
                    {
                        blockChain._Test_Add();
                    }
                    break;
            }

        }
    }
}
