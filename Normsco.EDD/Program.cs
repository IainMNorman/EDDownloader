﻿using System;
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
        static int maxDownloaders = 10;
        static void Main(string[] args)
        {
            if (args.Count() > 0)
            {
                int.TryParse(args[0], out maxDownloaders);
            }
            Console.Write("Press ENTER key to start downloading...");
            Console.ReadLine();
            Console.WriteLine("Initialising");
            d = new EDDEngine();


            d.DownloadEd(AppDomain.CurrentDomain.BaseDirectory + "\\download\\", maxDownloaders);

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