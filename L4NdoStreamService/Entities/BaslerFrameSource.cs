using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Basler.Pylon;

namespace L4NdoStreamService.Entities
{
    public class BaslerFrameSource : IFrameSource
    {
        private Camera _camera;
        private byte[] _currentImage;

        public BaslerFrameSource(string type)
        {
            ICameraInfo info = CameraFinder.Enumerate().FirstOrDefault(info => info.GetValueOrDefault(CameraInfoKey.DeviceType, string.Empty) == type);
            info = info == null ? CameraFinder.Enumerate().First() : info;
            this._camera = new Camera(info);
            this._camera.CameraOpened += Configuration.AcquireContinuous;
            this._camera.ConnectionLost += (sender, args) => this.Dispose();
            this._camera.Open();

            this._camera.StreamGrabber.ImageGrabbed += this.OnImageGrabbed;
            this._camera.StreamGrabber.Start(GrabStrategy.LatestImages, GrabLoop.ProvidedByStreamGrabber);
        }

        private void OnImageGrabbed(object sender, ImageGrabbedEventArgs e)
        {
            if(e.GrabResult.GrabSucceeded)
            {
                byte[] buffer;

                // Make this work ?
                using (PixelDataConverter converter = new PixelDataConverter { OutputPixelFormat = PixelType.RGBA8packed })
                {
                    long bufferSize = converter.GetBufferSizeForConversion(e.GrabResult);
                    buffer = new byte[bufferSize];
                    converter.Convert(buffer, e.GrabResult);
                }

                this._currentImage = buffer;
            }
        }

        public void Dispose()
        {
            this._camera.Close();
            this._camera.Dispose();
        }

        public async Task<BitmapData> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);

        public BitmapData GrabArgb()
        {
            if(this._currentImage != null)
            {
                using Bitmap bitmap = new Bitmap(Image.FromStream(new MemoryStream(_currentImage)));

                var data = bitmap.LockBits(
                    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                return data;
            }
            else { return null; }
        }
    }
}
