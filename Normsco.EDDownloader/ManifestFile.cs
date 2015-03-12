using System.Net;

namespace Normsco.EDDownloader
{
    public class ManifestFile
    {
        public string Path { get; set; }
        public string Hash { get; set; }
        public long Size { get; set; }
        public string Download { get; set; }
        public decimal Percentage { get; set; }
        public long BytesDownloaded { get; set; }
        public WebClient Client { get; set; }
    }
}
