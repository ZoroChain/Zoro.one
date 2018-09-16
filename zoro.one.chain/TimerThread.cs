using System;

namespace zoro.one.chain
{
    public class TimerThread
    {
        public static void Run()
        {
            while (true)
            {
                System.Threading.Thread.Sleep(500);
                Tick();
            }
        }

        public static void Tick()
        {
            Console.WriteLine("Do something here.");
        }
    }
}
