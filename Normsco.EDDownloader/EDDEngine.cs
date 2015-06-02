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
using System.ComponentModel;

namespace Normsco.EDDownloader
{
    public class EDDEngine
    {
        public EDDEngine()
        {
            Model = new DownloaderViewModel();
        }

        public DownloaderViewModel Model { get; set; }


        //private string installLocation = @"D:\Program Files (x86)\Frontier\EDLaunch\Products\FORC-FDEV-D-1000\";
        private string installLocation = @"D:\EDDtester\";
        private object modelLock = new object();
        private int maxConcurrentDownloads = 8;

        public async void DownloadEd(string installPath, int maxDownloaders)
        {
            Init(installPath, maxDownloaders);
            await Start();
        }


        public void Init(string installPath, int maxDownloaders)
        {
            installLocation = installPath;
            maxConcurrentDownloads = maxDownloaders;
            LoadFiles(GetManifestXmlDoc());
            Model.FilesToDownload = new BlockingCollection<ManifestFile>();
            Model.DownloadingFiles = new BlockingCollection<ManifestFile>();

            CreateFolders();
            Model.TotalBytesToDownload = Model.AllFiles.Sum(f => (decimal)f.Size);
            Model.TotalBytesDownloaded = 0;
        }

        public async Task Start()
        {

            foreach (var item in Model.AllFiles)
            {
                Model.FilesToDownload.Add(item);
            }
            Model.FilesToDownload.CompleteAdding();

            for (int i = 0; i < maxConcurrentDownloads; ++i)
            {
                var client = new WebClient();
                client.DownloadProgressChanged += DownloadProgressChanged;
                client.DownloadFileCompleted += DownloadFileCompleted;
                await StartDownload(client);
            }
        }

        async Task StartDownload(WebClient client)
        {
            ManifestFile file;
            if (Model.FilesToDownload.TryTake(out file))
            {
                if (await CheckUpToDateAsync(file))
                {
                    file.Percentage = 100;
                    file.IsDownloading = false;
                    Model.DownloadingFiles.Add(file);
                    Model.TotalBytesDownloaded += file.Size;
                    await StartDownload(client);
                }
                else
                {
                    // start the asynchronous download.
                    file.Client = client;
                    file.IsDownloading = true;
                    Model.DownloadingFiles.Add(file);
                    client.DownloadFileAsync(new Uri(file.Download), file.Path, file);
                }
            }
            else
            {
                // Couldn't get a url. The queue is empty.
                // Dispose the WebClient instance.
                client.Dispose();
            }
        }

        private Task<bool> CheckUpToDateAsync(ManifestFile file)
        {
            return Task.Run(() =>
            {
                if (File.Exists(file.Path))
                {
                    return CheckHash(file.Path, file.Hash);
                }
                else
                {
                    return false;
                }
            });
        }


        void DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var file = e.UserState as ManifestFile;
            file.Percentage = e.ProgressPercentage;
            file.BytesDownloaded = e.BytesReceived;
        }

        async void DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            // The file has been downloaded.
            // DownloadState is in the UserState property
            var file = (ManifestFile)e.UserState;
            file.IsDownloading = false;
            Model.TotalBytesDownloaded += file.Size;
            // If an error occurred, it will be identified in the e.Error property.
            // Do whatever processing is necessary to complete this download.
            // And then start a new download.

            await StartDownload(file.Client);
        }

        public bool CheckHash(string path, string hash)
        {
            var sha = SHA1.Create();
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
                var folder = Path.GetDirectoryName(file.Path);
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
                        Path = installLocation + t.Element("Path").Value,
                        Download = t.Element("Download").Value,
                        Size = int.Parse(t.Element("Size").Value),
                        Hash = t.Element("Hash").Value,
                        IsDownloading = false
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

        protected virtual void OnProgressChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = ProgressChanged;
            if (handler != null) handler(this, e);
        }
        public event EventHandler<EventArgs> ProgressChanged;

    }
}


