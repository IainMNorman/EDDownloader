using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Normsco.EDDownloader;
using System.IO;

namespace Normsco.EDD
{
    class Program
    {
        static System.Timers.Timer timer = new System.Timers.Timer();
        static EDDEngine d;

        static void Main(string[] args)
        {
            d = new EDDEngine();            

            timer.Interval = 100;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            d.Init();
            d.Start();

            while (d.Model.IsDownloading)
            {
                // loop
            }

            Console.WriteLine("Done and exiting!");
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            var currentDownloads = d.Model.CurrentDownloads.ToList();

            timer.Stop();
            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Current Downloads: {0}", currentDownloads.Count());
            foreach (var item in currentDownloads)
            {
                Console.WriteLine("{0} {1:P}", Path.GetFileName(item.Path), item.Percentage);
            }
            timer.Start();
        }
    }
}
