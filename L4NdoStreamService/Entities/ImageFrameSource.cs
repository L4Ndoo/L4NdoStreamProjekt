using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities
{
    public class ImageFrameSource : BackgroundUpdater, IFrameSource
    {
        public string Path { get; set; }
        public string FileName { get; set; }

        public int FrameCount { get; set; } = 300;
        public int FramesPerSecond 
        { 
            get => this.UpdatesPerSecond;
            set => this.UpdatesPerSecond = value;
        }

        private int _frameIndex = 0;

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


        public byte[] GrabFile()
        {
            var fileName = $"{this.Path}{this.FileName}{this._frameIndex.ToString().PadLeft(4, '0')}.jpg";
            byte[] jpeg = File.ReadAllBytes(fileName);
            return jpeg;
        }

        public async Task<byte[]> GrabFileAsync() =>
            await Task.Run(this.GrabFile);

        public async Task<Bitmap> GrabBitmapAsync() =>
            await Task.Run(this.GrabBitmap);

        public async Task<BitmapData> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);


        public Bitmap GrabBitmap()
        {
            byte[] file = this.GrabFile();
            using MemoryStream stream = new MemoryStream(file);
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        public BitmapData GrabArgb()
        {
            using Bitmap bitmap = this.GrabBitmap();

            var data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            return data;
        }
    }
}