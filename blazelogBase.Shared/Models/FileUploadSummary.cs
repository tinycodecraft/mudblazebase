using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace blazelogBase.Shared.Models
{
    public class FileUploadSummary
    {
        public int TotalFilesUploaded { get; set; }
        public string TotalSizeUploaded { get; set; }
        public string[] FilePaths { get; set; } = new List<string>().ToArray();
        public string[] FileDescs { get; set; } = new List<string>().ToArray();
    }
}
