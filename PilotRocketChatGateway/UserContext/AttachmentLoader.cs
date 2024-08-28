using Ascon.Pilot.DataClasses;
using Microsoft.AspNetCore.StaticFiles;
using PilotRocketChatGateway.PilotServer;
using PilotRocketChatGateway.Utils;
using SixLabors.ImageSharp;

namespace PilotRocketChatGateway.UserContext
{
    public interface IMediaAttachmentLoader
    {
        (IList<FileAttachment>, int) LoadFiles(string roomId, int offset);
        Attachment LoadAttachment(Guid? objId);
        Attachment GetSimpleAttachment(Guid? objId);
        FileAttachment LoadFileAttachment(DObject obj, string roomId);
        Dictionary<Guid, Guid> GetAttachmentsIds(IList<DChatRelation> chatRelations);
    }
    public class MediaAttachmentLoader : IMediaAttachmentLoader
    {
        private const string DOWNLOAD_URL = "/download";
        private readonly ICommonDataConverter _commonDataConverter;
        private readonly IContentTypeProvider _contentTypeProvider;
        private readonly IContext _context;

        public MediaAttachmentLoader(ICommonDataConverter commonDataConverter, IContext context, IContentTypeProvider contentTypeProvider)
        {
            _commonDataConverter = commonDataConverter;
            _context = context;
            _contentTypeProvider = contentTypeProvider;
        }
        public (IList<FileAttachment>, int) LoadFiles(string roomId, int offset)
        {
            var id = _commonDataConverter.ConvertToChatId(roomId);
            var chat = _context.RemoteService.ServerApi.GetChat(id);
            var allAttachments = GetAttachmentsIds(chat.Relations);
            var attachs = allAttachments.Reverse().Skip(offset).Take(10);
            return (attachs.Select(x => LoadFileAttachment(x.Value, roomId)).Where(x => x != null).ToList(), allAttachments.Count());
        }
        public Attachment LoadAttachment(Guid? objId) 
        {
            if (objId == null || objId == Guid.Empty)
                return null;

            var obj = _context.RemoteService.ServerApi.GetObject(objId.Value);
            var file = obj.ActualFileSnapshot.Files.First();
            var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", objId.ToString()) });

            if (PilotServer.FileInfo.IsSupportedMediaFile(file.Name))
            {
                return new Attachment()
                {
                    title = file.Name,
                    title_link = downloadUrl,
                    image_dimensions = GetDimension(obj),
                    image_type = PilotServer.FileInfo.GetFileType(file.Name, _contentTypeProvider),
                    image_size = file.Body.Size,
                    image_url = downloadUrl,
                    type = "file",
                };
            }

            return new Attachment()
            {
                title = obj.ActualFileSnapshot.Files[0].Name,
                title_link = downloadUrl,
                type = "file",
            };
        }

        public Attachment GetSimpleAttachment(Guid? objId)
        {
            if (objId == null || objId == Guid.Empty)
                return null;


            var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", objId.ToString()) });
            return new Attachment()
            {
                title_link = downloadUrl,
                image_url = downloadUrl,
                type = "file",
            };
        }
        public FileAttachment LoadFileAttachment(DObject obj, string roomId)
        {
            if (obj == null || obj.StateInfo.State != ObjectState.Alive)
                return null;

            var creator = _commonDataConverter.ConvertToUser(GetAttachCreator(obj));
            var downloadUrl = MakeDownloadLink(new List<(string, string)> { ("objId", obj.Id.ToString()) });
            var file = obj.ActualFileSnapshot.Files.First();
            if (PilotServer.FileInfo.IsSupportedMediaFile(file.Name))
            {
                return new FileAttachment
                {
                    name = file.Name,
                    type = PilotServer.FileInfo.GetFileType(file.Name, _contentTypeProvider),
                    id = obj.Id.ToString(),
                    size = file.Body.Size,
                    roomId = roomId,
                    userId = creator.id,
                    identify = new FileIdentity
                    {
                        format = PilotServer.FileInfo.GetFileFormat(file.Name),
                        size = GetDimension(obj),
                    },
                    url = downloadUrl,
                    typeGroup = "image",
                    user = creator,
                    uploadedAt = _commonDataConverter.ConvertToJSDate(file.Body.Created)
                };
            }

            return new FileAttachment
            {
                name = file.Name,
                roomId = roomId,
                userId = creator.id,
                url = downloadUrl,
                user = creator,
                uploadedAt = _commonDataConverter.ConvertToJSDate(obj.Created)
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

        private FileAttachment LoadFileAttachment(Guid objId, string roomId)
        {
            if (objId == Guid.Empty)
                return null;

            var obj = _context.RemoteService.ServerApi.GetObject(objId);
            return LoadFileAttachment(obj, roomId);
        }


        private INPerson GetAttachCreator(DObject obj)
        {
            return _context.RemoteService.ServerApi.GetPerson(obj.CreatorId);
        }

        private Dimension GetDimension(DObject obj)
        {
            return new Dimension { width = (int)obj.Attributes[SystemAttributes.WIDTH].IntValue, height = (int)obj.Attributes[SystemAttributes.HEIGHT] };
        }
    }
}
