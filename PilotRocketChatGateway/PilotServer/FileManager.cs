using Ascon.Pilot.Common;
using Ascon.Pilot.DataClasses;
using Ascon.Pilot.DataModifier;
using Ascon.Pilot.Server.Api.Contracts;
using System.Security.Cryptography;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IFileFileManager
    {
        IFileLoader FileLoader { get; }
        DFile CreateFile(IDocumentInfo document, int creatorId);
        IFileInfo LoadFileInfo(Guid objId);
    }
    public class FileManager : IFileFileManager
    {
        private const long MIN_RESUME_UPLOAD_FILE_SIZE = 50 * 1024 * 1024;
        private static int CHUNK_SIZE = 1024 * 1024; // 1 Mb

        private readonly IFileArchiveApi _fileArchiveApi;
        private readonly IServerApi _serverApi;
        private readonly IFileLoader _fileLoader;


        public FileManager(IFileArchiveApi fileArchiveApi, IServerApi serverApi, IFileLoader fileLoader)
        {
            _fileArchiveApi = fileArchiveApi;
            _serverApi = serverApi;
            _fileLoader = fileLoader;
        }
        public IFileLoader FileLoader => _fileLoader;
        public IFileInfo LoadFileInfo(Guid objId)
        {
            var obj = _serverApi.GetObjects(new Guid[] { objId }).First();
            var file = obj.ActualFileSnapshot.Files.First();
            return _fileLoader.Download(file);
        }
        public DFile CreateFile(IDocumentInfo document, int creatorId)
        {
            using (var stream = document.GetStream())
            {
                var fileId = Guid.NewGuid();
                stream.Position = 0;

                Md5 md5;
                using (var localFileStorageStream = new MemoryStream())
                using (var md5Hasher = new MD5CryptoServiceProvider())
                    md5 = CopyToAndHashNew(stream, localFileStorageStream, md5Hasher);

                var fileBody = new DFileBody
                {
                    Id = fileId,
                    Size = stream.Length,
                    Md5 = md5,
                    Modified = document.LastWriteTimeUtc,
                    Created = document.CreationTimeUtc,
                    Accessed = document.LastAccessTimeUtc
                };

                var file = new DFile
                {
                    Name = document.Name,
                    Body = fileBody,
                    CreatorId = creatorId
                };

                CreateFile(file, stream);
                return file;
            }
        }
        private void CreateFile(INFile file, Stream stream)
        {
            long pos = 0;
            if (file.Size > MIN_RESUME_UPLOAD_FILE_SIZE)
            {
                pos = _fileArchiveApi.GetFilePosition(file.Id);
                if (pos > file.Size)
                    throw new Exception(string.Format("File with id {0} is corrupted", file.Id));
            }

            using (var fs = stream)
            {
                if (file.Size != fs.Length)
                    throw new Exception(string.Format("Local file size is incorrect: {0}", file.Id));

                const int MAX_ATTEMPT_COUNT = 5;
                int attemptCount = 0;
                bool succeed = false;
                do
                {
                    UploadData(fs, file.Id, pos);
                    try
                    {
                        _fileArchiveApi.PutFileInArchive(file.Dto.Body);
                        succeed = true;
                    }
                    catch (Exception e)
                    {
                        pos = 0;
                    }
                    attemptCount++;
                } while (!succeed && attemptCount < MAX_ATTEMPT_COUNT);

                if (!succeed)
                    throw new PilotException(string.Format("Unable to upload file {0}", file.Id));
            }
        }
        private static Md5 CopyToAndHashNew(Stream stream, Stream destination, HashAlgorithm hashAlgorithm, int bufferSize = 32768, Func<long, bool> token = null)
        {
            var buffer = new byte[bufferSize];
            var copied = 0;
            int count;

            if (token != null)
                token(copied);

            while ((count = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, count);
                hashAlgorithm.TransformBlock(buffer, 0, count, null, 0);
                copied += count;
                if (token == null)
                    continue;

                if (token(copied))
                    return new Md5();
            }
            hashAlgorithm.TransformFinalBlock(buffer, 0, 0);
            return new Md5() { Part1 = BitConverter.ToInt64(hashAlgorithm.Hash, 0), Part2 = BitConverter.ToInt64(hashAlgorithm.Hash, 8) };
        }
        private void UploadData(Stream fs, Guid id, long pos)
        {
            if (fs.Length == 0)
            {
                _fileArchiveApi.PutFileChunk(id, new byte[0], 0);
                return;
            }

            var chunkSize = CHUNK_SIZE;
            var buffer = new byte[chunkSize];

            fs.Seek(pos, SeekOrigin.Begin);
            while (pos < fs.Length)
            {
                var readBytes = fs.Read(buffer, 0, chunkSize);
                _fileArchiveApi.PutFileChunk(id, TrimBuffer(buffer, readBytes), pos);

                pos += readBytes;
            }
        }
        private byte[] TrimBuffer(byte[] buffer, int size)
        {
            if (size < buffer.Length)
            {
                var trimmed = new byte[size];
                Array.Copy(buffer, trimmed, size);
                return trimmed;
            }
            return buffer;
        }
    }
}
