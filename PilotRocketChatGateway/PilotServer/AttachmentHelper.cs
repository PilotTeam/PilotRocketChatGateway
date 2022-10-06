using Ascon.Pilot.DataClasses;
using Ascon.Pilot.DataModifier;
using Ascon.Pilot.Server.Api.Contracts;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

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
            using (var fileStream = new MemoryStream(data))
            {
                var image = Image.FromStream(fileStream);
                var type = _serverApi.GetMetadata(0).Types.First(x => x.Name == EXTERNAL_FILE_TYPE_NAME);
                var dObj = CreateAttachmentObject(type, image);

                var change = new DChange { New = dObj };
                var timestamp = DateTime.Now;

                var info = new DocumentInfo(fileName, () => new MemoryStream(data), timestamp, timestamp, timestamp);
                _fileManager.AddFileToChange(info, _currentPerson.Id, change);

                MakeThumbnail(image, fileName, timestamp, change);

                return change;
            }
        }

        private DObject CreateAttachmentObject(MType type, Image image)
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

            dObj.Attributes[SystemAttributes.WIDTH] = image.Width;
            dObj.Attributes[SystemAttributes.HEIGHT] = image.Height;
            return dObj;
        }

        private void MakeThumbnail(Image image, string fileName, DateTime timestamp, DChange chage)
        {
            
            var thumbnailStream = GetThumbnailStream(image);
            if (thumbnailStream != null)
            {
                _fileManager.AddFileToChange(new DocumentInfo($"{fileName}_{Constants.THUMBNAIL_FILE_NAME_POSTFIX}", () => thumbnailStream, timestamp, timestamp, timestamp), _currentPerson.Id, chage);
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

                var resized = ResizeImage(image, (int)(isLandscape ? maxSide : minSide),
                    (int)(isLandscape ? minSide : maxSide));

                var stream = new MemoryStream();
                resized.Save(stream, ImageFormat.Png);

                return stream;
            }

            return null;
        }

        private static Bitmap ResizeImage(System.Drawing.Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
