using Ascon.Pilot.DataClasses;
using Ascon.Pilot.DataModifier;
using Ascon.Pilot.Server.Api.Contracts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace PilotRocketChatGateway.PilotServer
{
    public interface IAttachmentHelper
    {
        DChange CreateChangeWithAttachmentObject(string fileName, byte[] data);
    }
    public class AttachmentHelper : IAttachmentHelper
    {
        private const double MAX_IMAGE_SIZE_WITHOUT_THUMB = 1280;
        private const string EXTERNAL_FILE_TYPE_NAME = "externalFile_";

        private IServerApi _serverApi;
        private IFileFileManager _fileManager;
        private readonly DPerson _currentPerson;
        
        public AttachmentHelper(IServerApi serverApi, IFileFileManager fileManager, DPerson currentPerson)
        {
            _serverApi = serverApi;
            _fileManager = fileManager;
            _currentPerson = currentPerson;
        }


        public DChange CreateChangeWithAttachmentObject(string fileName, byte[] data)
        {
            var type = _serverApi.GetMetadata(0).Types.First(x => x.Name == EXTERNAL_FILE_TYPE_NAME);
            var dObj = CreateAttachmentObject(type);

            var change = new DChange { New = dObj };
            var timestamp = DateTime.Now;

            var info = new DocumentInfo(fileName, () => new MemoryStream(data), timestamp, timestamp, timestamp);
            var file = _fileManager.CreateFile(info, _currentPerson.Id);
            change.New.ActualFileSnapshot.AddFile(file, _currentPerson.Id);

            if (FileInfo.IsSupportedMediaFile(fileName))
            {
                var image = Image.Load(data);
                dObj.Attributes[SystemAttributes.WIDTH] = image.Width;
                dObj.Attributes[SystemAttributes.HEIGHT] = image.Height;
                MakeThumbnail(image, fileName, timestamp, change);
            }

            return change;
        }

        private DObject CreateAttachmentObject(MType type)
        {
            var dObj = new DObject
            {
                Id = Guid.NewGuid(),
                ParentId = Guid.Empty,
                TypeId = type.Id,
                CreatorId = _currentPerson.Id,
                Created = DateTime.UtcNow
            };
            dObj.Access.AddDistinct(new AccessRecord(_currentPerson.MainPosition(), _currentPerson.MainPosition(), dObj.Id, AccessCalculator.GetCreatorAccess()));
            dObj.Access.AddDistinct(new AccessRecord(0, _currentPerson.MainPosition(), dObj.Id, new Access(AccessLevel.View, DateTime.MaxValue, AccessInheritance.None, AccessType.Allow)));  

            return dObj;
        }

        private void MakeThumbnail(Image image, string fileName, DateTime timestamp, DChange change)
        {
            
            var thumbnailStream = GetThumbnailStream(image);
            if (thumbnailStream != null)
            {
                var file = _fileManager.CreateFile(new DocumentInfo($"{fileName}_{Constants.THUMBNAIL_FILE_NAME_POSTFIX}", () => thumbnailStream, timestamp, timestamp, timestamp), _currentPerson.Id);
                change.New.ActualFileSnapshot.AddFile(file, _currentPerson.Id);
            }
        }

        private MemoryStream GetThumbnailStream(Image image)
        {
            var maxSide = (double)Math.Max(image.Width, image.Height);

            if (maxSide > MAX_IMAGE_SIZE_WITHOUT_THUMB)
            {
                var minSide = (double)Math.Min(image.Width, image.Height);

                if (maxSide > MAX_IMAGE_SIZE_WITHOUT_THUMB)
                {
                    var ratio = maxSide / minSide;
                    maxSide = MAX_IMAGE_SIZE_WITHOUT_THUMB;
                    minSide = maxSide / ratio;
                }

                var isLandscape = image.Width > image.Height;
                var width = (int)(isLandscape ? maxSide : minSide);
                var height = (int)(isLandscape ? minSide : maxSide);

                var stream = new MemoryStream();

                image.Mutate(x => x.Resize(width, height));
                image.SaveAsPng(stream);

                return stream;
            }

            return null;
        }
    }
}
