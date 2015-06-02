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
        static long counter = 0;
        static int maxDownloaders = 16;
        static string downloadLocation = AppDomain.CurrentDomain.BaseDirectory + "\\download\\";
        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                int.TryParse(args[0], out maxDownloaders);               
                if (args.Count()>1)
                {
                    downloadLocation = args[1];
                }
            }
            Console.WriteLine("Usage");
            Console.WriteLine("=====");
            Console.WriteLine("edd [Concurrant Downloads] [Download Location]");
            Console.WriteLine();
            Console.WriteLine("Downloading to {0} with {1} downloaders", downloadLocation, maxDownloaders);
            Console.WriteLine("Press a key to start downloading...");
            Console.ReadLine();
            Console.WriteLine("Downloading manifest and creating folders.");
            d = new EDDEngine();


            d.DownloadEd(downloadLocation, maxDownloaders);

            timer.Interval = 50;
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            Console.Clear();
            while (d.Model.FilesToDownload.Count() > 0 || d.Model.DownloadingFiles.Where(f => f.IsDownloading).Count() > 0)
            {

            }
            timer.Stop();
            Console.Clear();
            Console.WriteLine("Done...");
            Console.ReadKey();
        }

        static void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            timer.Stop();
            counter++;
            Console.SetCursorPosition(0, 0);
            ConsoleWriteColor(ConsoleColor.White, "Downloading " + downloadAnim[counter % 4]); 
            Console.WriteLine();
            //Console.WriteLine("{0:P}",(decimal)d.Model.TotalBytesDownloaded / d.Model.TotalBytesToDownload);
            ConsoleWriteColor(ConsoleColor.Green, "{0}", ProgressBar((decimal)d.Model.TotalBytesDownloaded / d.Model.TotalBytesToDownload));

            var downloads = d.Model.DownloadingFiles.Where(f => f.IsDownloading).ToList();
            

            foreach (var file in downloads)
            {
                ConsoleWriteColor(ConsoleColor.Cyan, "{0}", ProgressBar((decimal)file.BytesDownloaded / file.Size));
            }

            var blanks = maxDownloaders - downloads.Count();

            Console.Write(new String(' ', blanks * 80));

            Console.Write("{0} files left to download.",
                d.Model.FilesToDownload.Count());

            timer.Start();
        }

        static string ProgressBar(decimal percent)
        {
            var number = (int)(78 * percent);
            var bar = new String('█', number);
            var space = new String('░', 78 - number);
            return String.Format("[{0}{1}]", bar, space);
        }

        static void ConsoleWriteColor(ConsoleColor color, string format, params object[] args)
        {
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(format, args);
            Console.ForegroundColor = originalColor;
        }
    }
}