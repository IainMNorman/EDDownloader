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

namespace Normsco.EDDownloader
{
    public class EDDEngine
    {
        public EDDEngine()
        {
            Model = new DownloaderViewModel();
        }

        internal void Stop()
        {
            foreach (var download in Model.CurrentDownloads)
            {
                download.Client.CancelAsync();
                download.Client.Dispose();
            }
        }

        public DownloaderViewModel Model { get; set; }


        private string installLocation = "d:\\eddtest\\";
        private SHA1 sha;
        private int maxConcurrentDownloads = 5;


        public void Init()
        {
            Model.IsDownloading = false;
            Model.CurrentDownloads = new List<ManifestFile>();
            Model.IgnoredFiles = new List<ManifestFile>();
            Model.FinishedFiles = new List<ManifestFile>();
            sha = SHA1.Create();

            LoadFiles(GetManifestXmlDoc());
            Model.TotalFiles = Model.AllFiles.Count();
            Model.TotalBytesToDownload = Model.AllFiles.Sum(f => (decimal)f.Size);
            Model.TotalBytesDownloaded = 0;

            CreateFolders();
            RemoveUnchangedFilesFromList();
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
                var client = new WebClient();
                client.DownloadProgressChanged += Client_DownloadProgressChanged;
                client.DownloadFileCompleted += Client_DownloadFileCompleted;
                StartDownload(client);
            }
        }

        private void StartDownload(WebClient client)
        {

            ManifestFile manifestFile;

            if (Model.DownloadQueue.TryTake(out manifestFile))
            {

                manifestFile.Client = client;
                Model.CurrentDownloads.Add(manifestFile);
                client.DownloadFileAsync(new Uri(manifestFile.Download), installLocation + manifestFile.Path, manifestFile);
            }
            else
            {
                client.Dispose();
                if (Model.CurrentDownloads.Count == 0)
                {
                    Model.IsDownloading = false;
                }
            }
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var manifestFile = (ManifestFile)e.UserState;
            Model.CurrentDownloads.Remove(manifestFile);
            Model.FinishedFiles.Add(manifestFile);
            Model.TotalBytesDownloaded += manifestFile.Size;

            StartDownload(manifestFile.Client);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            var manifestFile = (ManifestFile)e.UserState;
            manifestFile.BytesDownloaded = e.BytesReceived;
            manifestFile.Percentage = (decimal)e.BytesReceived / e.TotalBytesToReceive;
        }

        private void RemoveUnchangedFilesFromList()
        {
            foreach (var file in Model.AllFiles.ToList())
            {
                if (File.Exists(installLocation + file.Path) &&
                    CheckHash(installLocation + file.Path, file.Hash))
                {
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

    
    }
}


