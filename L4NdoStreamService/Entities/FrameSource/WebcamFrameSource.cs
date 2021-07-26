using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.Drawing;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities.FrameSource
{
    public class WebcamFrameSource : IFrameSource
    {

        public float Scale { get; set; } = 1;

        private VideoCapture _camera;

        public WebcamFrameSource()
        {
            this._camera = new VideoCapture(0);
            this._camera.Open(0);
        }

        public async Task<Bitmap> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);

        public Bitmap GrabArgb()
        {
            using Mat image = new ();
            _camera.Read(image);

            using Mat scaled = image.Resize(new OpenCvSharp.Size(image.Width * this.Scale, image.Height * this.Scale));
            Bitmap bitmap = scaled.ToBitmap();

            image.Release();
            scaled.Release();

            return bitmap;
        }

        public void Dispose()
        {
            this._camera.Dispose();
        }
    }
}