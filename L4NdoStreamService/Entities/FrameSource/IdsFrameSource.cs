using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading.Tasks;
using uEye;
using uEye.Types;

namespace L4NdoStreamService.Entities.FrameSource
{
    public class IdsFrameSource : IFrameSource
    {
        public float Scale { get; set; } = 1f;

        private readonly Camera _camera;
        private Bitmap _lastGrabbed;

        public IdsFrameSource()
        {
            uEye.Info.Camera.GetCameraList(out CameraInformation[] camInfos);
            this._camera = new Camera(camInfos.First().CameraID);
            for (int i = 0; i < 3; i++) { this._camera.Memory.Allocate(); }
            this._camera.Memory.GetList(out int[] idList);
            this._camera.Memory.Sequence.Add(idList);
            this._camera.Acquisition.Capture();
        }

        public async Task<BitmapData> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);

        public BitmapData GrabArgb()
        {
            this._camera.Memory.GetLast(out int memoryId);
            this._camera.Memory.CopyToBitmap(memoryId, out Bitmap bitmap);

            using Bitmap converted = new (bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using Graphics gr = Graphics.FromImage(converted);
            gr.DrawImage(bitmap, new Rectangle(0, 0, converted.Width, converted.Height));

            this._lastGrabbed?.Dispose();
            this._lastGrabbed = new Bitmap(converted, (int)(converted.Width * this.Scale), (int)(converted.Height * this.Scale));
            BitmapData data = this._lastGrabbed.LockBits(
                    new Rectangle(0, 0, this._lastGrabbed.Width, this._lastGrabbed.Height),
                    ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            return data;
        }

        public void Dispose()
        {
            this._lastGrabbed?.Dispose();
            this._camera?.Acquisition.Stop();
            this._camera?.Exit();
        }
    }
}
