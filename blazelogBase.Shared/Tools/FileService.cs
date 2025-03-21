using DocumentFormat.OpenXml.Bibliography;
using blazelogBase.Shared.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using OpenXmlPowerTools;
using System.Xml.Linq;
using static blazelogBase.Shared.Interfaces;

namespace blazelogBase.Shared.Tools
{
    public class FileService : IFileService
    {
        //private const string UploadsSubDirectory = "FilesUploaded";

        private readonly IEnumerable<string> allowedExtensions = new List<string> { ".zip", ".bin", ".png", ".jpg", ".mp4" };

        private readonly ILogger _logger;
        private readonly PathSetting pathOps;

        public FileService(IOptions<PathSetting> pathsetting, ILogger<FileService> logger)
        {
            pathOps = pathsetting.Value;
            _logger = logger;
        }

        public string CreatePathFor(string type,string filename,bool inupload = false)
        {
            var file = string.Empty;
            try
            {
                file = SubStringExtensions.GetPath(pathOps, inupload ? PathType.Upload : PathType.Share, type, filename);
                if(File.Exists(file))
                {
                    return file;
                }

            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }
            return null;
        }

        public async Task<string> DownloadFilesAsync(Stream fileStream, string type, string filename,bool inupload=false)
        {
            var file = string.Empty;
            try
            {
                file = SubStringExtensions.GetPath(pathOps, inupload ? PathType.Upload: PathType.Share, type, filename);
                if (File.Exists(file))
                {
                    var bytes = await File.ReadAllBytesAsync(file);
                    await new MemoryStream(bytes).CopyToAsync(fileStream);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return "";
            }

            return file;

        }

        public string GenerateWordWithData(string xmldata,string templatename,string type=null)
        {
            string file = string.Empty;
            try
            {
                var templatepath = SubStringExtensions.GetPath(pathOps, PathType.Template, type ?? "All", templatename);
                var sharepath = SubStringExtensions.GetPath(pathOps, PathType.Share, type ?? "All",SubStringExtensions.GetRandomFileInType(type));
                WmlDocument wmlDoc = new WmlDocument(templatepath);
                using (StringReader textreader = new StringReader(xmldata))
                {
                    var data = XElement.Load(textreader);
                    bool templateError; 
                    var wmlAssembledDoc= DocumentAssembler.AssembleDocument(wmlDoc,data,out templateError);
                    if(templateError)
                    {
                        _logger.LogError($"{templatepath} get template error!");
                        _logger.LogError("Errors in template.");
                        _logger.LogError("See AssembledDoc.docx to determine the errors in the template.");
                    }

                    FileInfo assembledDoc = new FileInfo(sharepath);
                    if (!Directory.Exists(Path.GetDirectoryName(sharepath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(sharepath));
                    }
                    wmlAssembledDoc.SaveAs(assembledDoc.FullName);
                    return assembledDoc.FullName;

                }


            }
            catch(Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return "";
            }

            
        }

        public async Task<FileUploadSummary> UploadFileAsync(Stream fileStream, string contentType, string type)
        {
            var fileCount = 0;
            long totalSizeInBytes = 0;

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(contentType));
            var multipartReader = new MultipartReader(boundary, fileStream);
            var section = await multipartReader.ReadNextSectionAsync();

            var filePaths = new List<string>();
            var fileDescs = new List<string>();
            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out ContentDispositionHeaderValue contentDisposition);

                var fileSection = section.AsFileSection();
                if (fileSection != null && hasContentDispositionHeader && HasFileContentDisposition(contentDisposition) && fileSection != null)
                {
                    totalSizeInBytes += await SaveFileAsync(fileSection, filePaths, fileDescs, type);
                    fileCount++;
                }

                section = await multipartReader.ReadNextSectionAsync();
            }

            return new FileUploadSummary
            {
                TotalFilesUploaded = fileCount,
                TotalSizeUploaded = ConvertSizeToString(totalSizeInBytes),
                FilePaths = filePaths.ToArray(),
                FileDescs = fileDescs.ToArray(),

            };
        }

        private async Task<long> SaveFileAsync(FileMultipartSection fileSection, IList<string> filePaths, IList<string> fileDescs, string type)
        {



            var uploadpath = SubStringExtensions.GetPath(pathOps, PathType.Upload, type);
            Directory.CreateDirectory(uploadpath);


            var filePath = Path.Combine(uploadpath, fileSection.FileName);

            fileDescs.Add(fileSection.FileName);

            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 1024);

            await fileSection.FileStream.CopyToAsync(stream);

            var streampath = SubStringExtensions.GetPath(pathOps, PathType.Stream, type, fileSection.FileName);

            filePaths.Add(streampath);

            return fileSection.FileStream.Length;
        }

        private bool HasFileContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            // Content-Disposition: form-data; name="myfile1"; filename="Misc 002.jpg"
            return contentDisposition != null
                && contentDisposition.DispositionType.Equals("form-data")
                && (!string.IsNullOrEmpty(contentDisposition.FileName.Value)
                    || !string.IsNullOrEmpty(contentDisposition.FileNameStar.Value));
        }



        private string ConvertSizeToString(long bytes)
        {
            var fileSize = new decimal(bytes);
            var kilobyte = new decimal(1024);
            var megabyte = new decimal(1024 * 1024);
            var gigabyte = new decimal(1024 * 1024 * 1024);

            return fileSize switch
            {
                _ when fileSize < kilobyte => "Less then 1KB",
                _ when fileSize < megabyte =>
                    $"{Math.Round(fileSize / kilobyte, fileSize < 10 * kilobyte ? 2 : 1, MidpointRounding.AwayFromZero):##,###.##}KB",
                _ when fileSize < gigabyte =>
                    $"{Math.Round(fileSize / megabyte, fileSize < 10 * megabyte ? 2 : 1, MidpointRounding.AwayFromZero):##,###.##}MB",
                _ when fileSize >= gigabyte =>
                    $"{Math.Round(fileSize / gigabyte, fileSize < 10 * gigabyte ? 2 : 1, MidpointRounding.AwayFromZero):##,###.##}GB",
                _ => "n/a"
            };
        }

        private string GetBoundary(MediaTypeHeaderValue contentType)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            return boundary;
        }
    }
}
