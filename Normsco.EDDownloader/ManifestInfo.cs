namespace Normsco.EDDownloader
{
    class ManifestInfo
    {
        public string RemotePath { get; set; }
        public string LocalFile { get; set; }
        public string MD5 { get; set; }
        public string Version { get; set; }
        public int Size { get; set; }
    }
}
