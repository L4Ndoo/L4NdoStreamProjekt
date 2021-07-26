using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Basler.Pylon;

namespace L4NdoStreamService.Entities.FrameSource
{
    public class BaslerFrameSource : IFrameSource
    {
        public float Scale { get; set; } = 1;

        private readonly Camera _camera;
        private Bitmap _currentImage;
        private object _imagelock = new ();
        public BaslerFrameSource(bool emulator = false)
        {
            ICameraInfo info = CameraFinder.Enumerate().FirstOrDefault(info => info.GetValueOrDefault(CameraInfoKey.DeviceType, string.Empty) == DeviceType.GigE);
            info = emulator || info == null ? CameraFinder.Enumerate().First() : info;
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
                using PixelDataConverter converter = new PixelDataConverter { OutputPixelFormat = PixelType.BGR8packed };
                long bufferSize = converter.GetBufferSizeForConversion(e.GrabResult);
                byte[] buffer = new byte[bufferSize];
                converter.Convert(buffer, e.GrabResult);

                Bitmap bitmap = new(e.GrabResult.Width, e.GrabResult.Height, PixelFormat.Format24bppRgb);
                BitmapData data = bitmap.LockBits(new(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
                Marshal.Copy(buffer, 0, data.Scan0, buffer.Length);
                bitmap.UnlockBits(data);

                lock (this._imagelock)
                {
                    this._currentImage?.Dispose();
                    this._currentImage = bitmap;
                }
            }
        }

        public async Task<Bitmap> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);

        public Bitmap GrabArgb()
        {
            if(this._currentImage != null)
            {
                Bitmap currentImage;
                lock(this._imagelock)
                {
                    currentImage = this._currentImage;
                    this._currentImage = null;
                }
                using Bitmap converted = new Bitmap(currentImage.Width, currentImage.Height, PixelFormat.Format32bppArgb);
                using Graphics gr = Graphics.FromImage(converted);
                gr.DrawImage(currentImage, new Rectangle(0, 0, converted.Width, converted.Height));
                currentImage.Dispose();

                return new Bitmap(converted, (int)(converted.Width * this.Scale), (int)(converted.Height * this.Scale));
            }
            else { return null; }
        }

        public void Dispose()
        {
            this._currentImage?.Dispose();
            this._camera.Close();
            this._camera.Dispose();
        }
    }
}
