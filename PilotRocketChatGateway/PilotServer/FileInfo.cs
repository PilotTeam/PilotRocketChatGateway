using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.StaticFiles;

namespace PilotRocketChatGateway.PilotServer
{
    public enum SupportedMedia
    {
        jpg,
        png,
        bmp
    }
    public interface IFileInfo
    {
        INFile File { get; }
        string FileType { get; }
        string Format { get; }
        byte[] Data { get; }
    }

    class FileInfo : IFileInfo
    {
        private byte[] _data;
        private string _format;
        private INFile _file;
        private string _fileType;
        IContentTypeProvider _contentTypeProvider;

        public FileInfo(byte[] data, INFile file, IContentTypeProvider contentTypeProvider)
        {
            _contentTypeProvider = contentTypeProvider;
            _file = file;
            _data = data;
            _fileType = GetFileType(file.Name, contentTypeProvider);
            _format = GetFileType(file.Name, contentTypeProvider);
            _format = GetFileFormat(file.Name);
    }

        public INFile File => _file;
        public string FileType => _fileType;

        public byte[] Data => _data;
        public string Format => _format;

        public static bool IsSupportedMediaFile(string filename)
        {
            var ext = Path.GetExtension(filename).ToLower();
            if (string.IsNullOrEmpty(ext))
                return false;

            return Enum.IsDefined(typeof(SupportedMedia), ext.Substring(1));
        }
        public static string GetFileType(string filename, IContentTypeProvider contentTypeProvider)
        {
            contentTypeProvider.TryGetContentType(filename, out var contentType);
            return contentType;
        }
        public static string GetFileFormat(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            if (string.IsNullOrEmpty(fileExtension))
                return string.Empty;
            return fileExtension.Substring(1);
        }
    }
}
