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
        static int tickCount = 0;
        static void Main(string[] args)
        {
            Console.WriteLine("Initialising");
            d = new EDDEngine();

            timer.Interval = 100;
            timer.Elapsed += Timer_Elapsed;


            d.Init();
            Console.WriteLine("Total Files Available: {0}", d.Model.TotalFiles);
            Console.WriteLine("Skipping {0} files that are up to date.", d.Model.IgnoredFiles.Count());
            Console.WriteLine("Queued {0} files for download, total size is {1} bytes", d.Model.AllFiles.Count(), d.Model.TotalBytesToDownload);
            Console.WriteLine("Press ENTER key to start...");
            Console.ReadLine();
            d.Start();
            timer.Start();

            while (d.Model.DownloadQueue.Count() > 0 || d.Model.Clients.Count() > 0)
            {
                // wait
            }

            Console.WriteLine("Done and exiting!");
            Console.WriteLine("Press ENTER key to exit...");
            Console.ReadLine();
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            timer.Stop();


            Console.Clear();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine("Downloading " + downloadAnim[tickCount % 4]);
            Console.WriteLine("Files Left: {0}", d.Model.DownloadQueue.Count());

            foreach (var client in d.Model.Clients)
            {
                Console.WriteLine("{0} {1}%", Path.GetFileName(client.ManifestFile.Path), client.ManifestFile.Percentage);
            }
            timer.Start();
            tickCount++;
        }
    }
}
