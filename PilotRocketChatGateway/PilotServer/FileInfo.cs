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
        string Format { get; }

        byte[] Data { get; }
    }


    class FileInfo : IFileInfo
    {
        private byte[] _data;
        private string _format;
        private INFile _file;
        private string _fileType;

        public FileInfo(byte[] data, INFile file)
        {
            _file = file;
            _data = data;
            _fileType = GetFileType(file.Name);
            _format = GetFileType(file.Name);
            _format = GetFileFormat(file.Name);
        }

        public INFile File => _file;
        public string FileType => _fileType;

        public byte[] Data => _data;
        public string Format => _format;

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
        private string GetFileFormat(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            switch (fileExtension)
            {
                case ".jpg":
                    return "jpeg";
                case ".png":
                    return "png";
                case ".bmp":
                    return "bmp";
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
