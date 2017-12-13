using System;

namespace AudioIndexer.Models
{
    public class AssetInfo
    {
        public Uri InputBlobContainer { get; set; }
        public string InputFilename { get; set; }
        public Uri OutputBlobContainer { get; set; }
        public String OutputCloseCaptionTTML { get; set; }
        public string OutputCloseCaptionVTT { get; set; }
    }
}
