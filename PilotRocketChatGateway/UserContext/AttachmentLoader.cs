using Ascon.Pilot.DataClasses;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.WebSockets;
using SixLabors.ImageSharp;

namespace PilotRocketChatGateway.UserContext
{
    public interface IMediaAttachmentLoader
    {
        (IList<FileAttachment>, int) LoadFiles(string roomId, int offset);
        Attachment LoadAttachment(Guid objId);
        Dictionary<Guid, Guid> GetAttachmentsIds(IList<DChatRelation> chatRelations);
    }
    public class MediaAttachmentLoader : IMediaAttachmentLoader
    {
        private const string DOWNLOAD_URL = "/download";
        private readonly ICommonDataConverter _commonDataConverter;
        private readonly IContext _context;
        public MediaAttachmentLoader(ICommonDataConverter commonDataConverter, IContext context)
        {
            _commonDataConverter = commonDataConverter;
            _context = context;
        }
        public (IList<FileAttachment>, int) LoadFiles(string roomId, int offset)
        {
            var id = _commonDataConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            var attachs = GetAttachmentsIds(chat.Relations).Skip(offset);
            return (attachs.Select(x => LoadFileAttachment(x.Value, roomId)).ToList(), attachs.Count());
        }
        public Attachment LoadAttachment(Guid objId)
        {
            if (objId == Guid.Empty)
                return null;

            var attach = LoadFileInfo(objId);
            if (attach == null)
                return null;

            if (string.IsNullOrEmpty(attach.FileType) || string.IsNullOrEmpty(attach.Format))
                return null;

            var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", objId.ToString()) });
            var image = Image.Load(attach.Data);
            return new Attachment()
            {
                title = attach.File.Name,
                title_link = downloadUrl,
                image_dimensions = new Dimension { width = image.Width, height = image.Height },
                image_type = attach.FileType,
                image_size = attach.File.Size,
                image_url = downloadUrl,
                type = "file",
            };
        }
        public Dictionary<Guid, Guid> GetAttachmentsIds(IList<DChatRelation> chatRelations)
        {
            var attachs = new Dictionary<Guid, Guid>();
            foreach (var rel in chatRelations.Where(x => x.Type == ChatRelationType.Attach && x.MessageId.HasValue))
                attachs[rel.MessageId.Value] = rel.ObjectId;
            return attachs;
        }
        private string MakeDownloadLink(IList<(string, string)> @params)
        {
            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            foreach (var p in @params)
                queryString.Add(p.Item1, p.Item2);

            return $"{DOWNLOAD_URL}/{@params.First().Item2}";
        }
        public IFileInfo LoadFileInfo(Guid objId)
        {
            var obj = _context.RemoteService.ServerApi.GetObject(objId);
            var fileLoader = _context.RemoteService.FileManager.FileLoader;

            var file = obj.ActualFileSnapshot.Files.FirstOrDefault();
            return fileLoader.Download(file);
        }
        private FileAttachment LoadFileAttachment(Guid objId, string roomId)
        {
            if (objId == Guid.Empty)
                return null;

            var attach = LoadFileInfo(objId);
            var creator = _commonDataConverter.ConvertToUser(_context.RemoteService.ServerApi.GetPerson(attach.File.CreatorId));
            using (var ms = new MemoryStream(attach.Data))
            {
                var image = Image.Load(attach.Data);
                var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", objId.ToString()) });

                return new FileAttachment
                {
                    name = attach.File.Name,
                    type = attach.FileType,
                    id = attach.File.Id.ToString(),
                    size = attach.File.Size,
                    roomId = roomId,
                    userId = creator.id,
                    identify = new FileIdentity
                    {
                        format = attach.Format,
                        size = new Dimension { width = image.Width, height = image.Height },
                    },
                    url = downloadUrl,
                    typeGroup = "image",
                    user = creator,
                    uploadedAt = _commonDataConverter.ConvertToJSDate(attach.File.Created)
                };
            }
        }
    }
}
