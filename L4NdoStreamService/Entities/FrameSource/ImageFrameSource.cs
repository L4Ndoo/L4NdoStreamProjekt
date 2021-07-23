using L4NdoStreamService.Utilities;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities.FrameSource
{
    public class ImageFrameSource : BackgroundUpdater, IFrameSource
    {
        public float Scale { get; set; } = 1;

        public string Path { get; set; }
        public string FileName { get; set; }

        public int FrameCount { get; set; } = 300;
        public int FramesPerSecond 
        { 
            get => this.UpdatesPerSecond;
            set => this.UpdatesPerSecond = value;
        }

        private int _frameIndex = 0;
        private Bitmap _lastFrame = null;

        public ImageFrameSource(string path, string fileName, int frameCount = 1, int framesPerSecond = 30)
        {
            this.Path = path;
            this.FileName = fileName;
            this.FrameCount = frameCount;
            this.FramesPerSecond = framesPerSecond;
            this.PlaySource();
        }

        protected override Task Update()
            => Task.Run(() => this._frameIndex = (this._frameIndex + 1) % this.FrameCount);

        public void PlaySource() =>
            this.StartUpdates();

        public void PauseSource() =>
            this.StopUpdates();

        public async Task<BitmapData> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);

        public BitmapData GrabArgb()
        {
            this._lastFrame?.Dispose();
            using Image image = Image.FromFile($"{this.Path}{this.FileName}{this._frameIndex.ToString().PadLeft(4, '0')}.jpg");
            this._lastFrame = new (image, (int)(image.Width * this.Scale), (int)(image.Height * this.Scale));
            

            BitmapData data = this._lastFrame.LockBits(
                new (0, 0, this._lastFrame.Width, this._lastFrame.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            return data;
        }
    }
}