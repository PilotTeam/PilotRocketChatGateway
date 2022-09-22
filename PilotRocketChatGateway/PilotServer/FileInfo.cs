using Ascon.Pilot.DataClasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IFileInfo
    {
        INFile File { get; }
        string FileType { get; }

        MemoryStream Stream { get; }
    }


    class FileInfo : IFileInfo
    {
        private MemoryStream _stream;
        private INFile _file;
        private string _fileType;

        public FileInfo(MemoryStream stream, INFile file)
        {
            _file = file;
            _stream = stream;
            _fileType = GetFileType(file.Name);
        }

        public INFile File => _file;
        public string FileType => _fileType;

        public MemoryStream Stream => _stream;
        private string GetFileType(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            switch (fileExtension)
            {
                case ".jpg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".bmp":
                    return "image/bmp";
                case ".gif":
                case ".3gp":
                case ".asf":
                case ".avi":
                case ".f4v":
                case ".flv":
                case ".m2ts":
                case ".m4v":
                case ".mkv":
                case ".mov":
                case ".mp4":
                case ".mpeg":
                case ".mpg":
                case ".mts":
                case ".mxf":
                case ".ogv":
                case ".ts":
                case ".vob":
                case ".webm":
                case ".wmv":
                case ".wav":
                    return string.Empty;
                default:
                    return string.Empty;
            }
        }
    }
}
