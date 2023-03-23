using System;
using Ascon.Pilot.DataClasses;
using Ascon.Pilot.Server.Api.Contracts;
using System.IO;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IFileLoader
    {
        IFileInfo Download(INFile inFile);
    }

    class FileLoader : IFileLoader
    {
        private static int CHUNK_SIZE = 1024 * 1024; // 1 Mb

        private readonly IFileArchiveApi _fileArchiveApi;
        private readonly IContentTypeProvider _contentTypeProvider;

        public FileLoader(IFileArchiveApi fileArchiveApi, IContentTypeProvider contentTypeProvider)
        {
            _fileArchiveApi = fileArchiveApi;
            _contentTypeProvider = contentTypeProvider;
        }

        public IFileInfo Download(INFile inFile)
        {
            if (inFile == null)
                return null;

            using (var stream = new MemoryStream())
            { 
                var filePos = _fileArchiveApi.GetFilePosition(inFile.Id);
                long fileSize = inFile.Size;
                while (fileSize > 0)
                {
                    int chunkSize = fileSize > CHUNK_SIZE ? CHUNK_SIZE : (int)fileSize;
                    var data = _fileArchiveApi.GetFileChunk(inFile.Id, filePos + inFile.Size - fileSize, chunkSize);
                    stream.Write(data);
                    fileSize -= chunkSize;
                }
                return new FileInfo(stream.ToArray(), inFile, _contentTypeProvider);
            }
        }
    }
}
