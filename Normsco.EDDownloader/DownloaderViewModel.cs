using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

namespace Normsco.EDDownloader
{
    public class DownloaderViewModel
    {
        public List<ManifestFile> AllFiles { get; set; }
        public decimal TotalBytesToDownload { get; set; }
        public decimal TotalBytesDownloaded { get; set; }
        public BlockingCollection<ManifestFile> FilesToDownload { get; set; }
        public BlockingCollection<ManifestFile> DownloadingFiles { get; set; }
    }
}
