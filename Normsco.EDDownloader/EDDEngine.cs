using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Normsco.EDDownloader
{
    public class EDDEngine
    {
        public EDDEngine()
        {
            Model = new DownloaderViewModel();
        }

        public DownloaderViewModel Model { get; set; }


        private string installLocation = @"D:\EDDTester\";
        private SHA1 sha;
        private int maxConcurrentDownloads = 3;
        private object modelLock = new object();


        public void Init()
        {
            Model.IsDownloading = false;
            Model.IgnoredFiles = new List<ManifestFile>();
            Model.Clients = new BlockingCollection<EDDWebClient>();
            sha = SHA1.Create();

            Console.WriteLine("Loading files from manifest.");
            LoadFiles(GetManifestXmlDoc());
            Model.TotalFiles = Model.AllFiles.Count();

            Console.WriteLine("Creating folders.");
            CreateFolders();
            Console.WriteLine("Determining files to skip.");
            RemoveUnchangedFilesFromList();

            Model.TotalBytesToDownload = Model.AllFiles.Sum(f => (decimal)f.Size);
            Model.TotalBytesDownloaded = 0;
            Console.WriteLine("Init finished.");
        }

        public void Start()
        {
            Model.IsDownloading = true;
            Model.DownloadQueue = new BlockingCollection<ManifestFile>();

            foreach (var download in Model.AllFiles)
            {
                Model.DownloadQueue.Add(download);
            }
            Model.DownloadQueue.CompleteAdding();


            for (int i = 0; i < maxConcurrentDownloads; i++)
            {
                var client = new EDDWebClient();
                Model.Clients.Add(client);
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadFileCompleted += Client_DownloadFileCompleted;
                StartDownload(client);
            }
        }

        private void StartDownload(EDDWebClient client)
        {
            
            ManifestFile manifestFile;

            if (Model.DownloadQueue.TryTake(out manifestFile))
            {
                client.ManifestFile = manifestFile;
                Debug.Write("Downloading " + client.ManifestFile.Path);
                client.DownloadFileAsync(new Uri(manifestFile.Download), installLocation + manifestFile.Path, manifestFile);
            }
            else
            {
                client.Dispose();
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            Debug.Write("File Complete");
            StartDownload(sender as EDDWebClient);

        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var m = e.UserState as ManifestFile;
            m.Percentage = e.ProgressPercentage;
            Debug.Write("Dowload Progress " + e.ProgressPercentage);
            
        }

        private void RemoveUnchangedFilesFromList()
        {
            foreach (var file in Model.AllFiles.ToList())
            {
                if (File.Exists(installLocation + file.Path) &&
                    CheckHash(installLocation + file.Path, file.Hash))
                {
                    Console.WriteLine("Ignoring " + file.Path);
                    Model.AllFiles.Remove(file);
                    Model.IgnoredFiles.Add(file);
                }
            }
        }



        private bool CheckHash(string path, string hash)
        {
            var fileHash = sha.ComputeHash(File.OpenRead(path));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileHash.Length; i++)
            {
                sb.Append(fileHash[i].ToString("x2"));
            }
            if (hash == sb.ToString())
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void CreateFolders()
        {
            foreach (var file in Model.AllFiles)
            {
                var folder = Path.GetDirectoryName(installLocation + file.Path);
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }
            }
        }

        private void LoadFiles(XDocument doc)
        {
            Model.AllFiles = doc.Descendants("File")
                .Select(t =>
                    new ManifestFile
                    {
                        Path = t.Element("Path").Value,
                        Download = t.Element("Download").Value,
                        Size = int.Parse(t.Element("Size").Value),
                        Hash = t.Element("Hash").Value
                    }
                )
                .ToList();
        }

        private XDocument GetManifestXmlDoc()
        {
            var manifestInfoUrl = "http://teknohippy.net/manifest.php";

            using (var client = new WebClient())
            {
                string json = client.DownloadString(manifestInfoUrl);

                var manifestInfo = JsonConvert.DeserializeObject<ManifestInfo>(json);

                using (var decomp = new GZipStream(
                    client.OpenRead(HexString2Ascii(manifestInfo.RemotePath)),
                    CompressionMode.Decompress))
                {
                    var doc = XDocument.Load(decomp);
                    return doc;
                }
            }
        }

        private string HexString2Ascii(string hexString)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= hexString.Length - 2; i += 2)
            {
                sb.Append(Convert.ToString(Convert.ToChar(Int32.Parse(hexString.Substring(i, 2), System.Globalization.NumberStyles.HexNumber))));
            }
            return sb.ToString();
        }

        private static async Task<Tuple<string, string, Exception>> DownloadFileTaskAsync(string remotePath,
    string localPath = null, int timeOut = 3000)
        {
            try
            {
                if (remotePath == null)
                {
                    Debug.WriteLine("DownloadFileTaskAsync (null remote path): skipping");
                    throw new ArgumentNullException("remotePath");
                }

                if (localPath == null)
                {
                    Debug.WriteLine(
                        string.Format(
                            "DownloadFileTaskAsync (null local path): generating a temporary file name for {0}",
                            remotePath));
                    localPath = Path.GetTempFileName();
                }

                using (var client = new WebClient())
                {
                    TimerCallback timerCallback = c =>
                    {
                        var webClient = (WebClient)c;
                        if (!webClient.IsBusy) return;
                        webClient.CancelAsync();
                        Debug.WriteLine(string.Format("DownloadFileTaskAsync (time out due): {0}", remotePath));
                    };
                    using (var timer = new Timer(timerCallback, client, timeOut, Timeout.Infinite))
                    {
                        await client.DownloadFileTaskAsync(remotePath, localPath);
                    }
                    Debug.WriteLine(string.Format("DownloadFileTaskAsync (downloaded): {0}", remotePath));
                    return new Tuple<string, string, Exception>(remotePath, localPath, null);
                }
            }
            catch (Exception ex)
            {
                return new Tuple<string, string, Exception>(remotePath, null, ex);
            }
        }
    }

    public static class Extensions
    {
        public static Task ForEachAsync<TSource, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor)
        {
            var oneAtATime = new SemaphoreSlim(5, 10);
            return Task.WhenAll(
                from item in source
                select ProcessAsync(item, taskSelector, resultProcessor, oneAtATime));
        }

        private static async Task ProcessAsync<TSource, TResult>(
            TSource item,
            Func<TSource, Task<TResult>> taskSelector, Action<TSource, TResult> resultProcessor,
            SemaphoreSlim oneAtATime)
        {
            TResult result = await taskSelector(item);
            await oneAtATime.WaitAsync();
            try
            {
                resultProcessor(item, result);
            }
            finally
            {
                oneAtATime.Release();
            }
        }
    }
}


