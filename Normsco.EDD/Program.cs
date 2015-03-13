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
        static string[] downloadAnim = new[] { "|", "/", "-", "\\" };
        static void Main(string[] args)
        {
            Console.WriteLine("Initialising");
            d = new EDDEngine();


            d.DownloadEd(@"d:\edinstall\");

            timer.Interval = 100;
            timer.Elapsed += timer_Elapsed;

            while (d.Model.FilesToDownload.Count() > 0)
            {
                
            }

            Console.WriteLine("Done...");
            Console.ReadKey();
        }

        static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();

            Console.Clear();
            Console.WriteLine(d.Model.TotalBytesDownloaded);

            timer.Start();
        }

       
    }
}
