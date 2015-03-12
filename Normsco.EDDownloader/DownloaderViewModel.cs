using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Normsco.EDDownloader
{
    public class DownloaderViewModel
    {
        public BlockingCollection<EDDWebClient> Clients { get; set; }
        public List<ManifestFile> AllFiles { get; set; }
        public List<ManifestFile> IgnoredFiles { get; set; }
        public BlockingCollection<ManifestFile> DownloadQueue { get; set; }
        public int TotalFiles { get; set; }
        public decimal TotalBytesToDownload { get; set; }
        public decimal TotalBytesDownloaded { get; set; }
        public int FilesStarted { get; set; }
        public int FilesDownloaded { get; set; }
        public bool IsDownloading { get; set; }
    }
}
