using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities
{
    public class FrameSource : BackgroundUpdater
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

        public FrameSource(string path, string fileName, int frameCount = 1, int framesPerSecond = 30)
        {
            this.Path = path;
            this.FileName = fileName;
            this.FrameCount = frameCount;
            this.FramesPerSecond = framesPerSecond;
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

        public async Task<byte[]> GrabRgbAsync() =>
            await Task.Run(this.GrabRgb);


        public Bitmap GrabBitmap()
        {
            byte[] file = this.GrabFile();
            using MemoryStream stream = new MemoryStream(file);
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        public byte[] GrabRgb()
        {
            using Bitmap bitmap = this.GrabBitmap();
            var data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            byte[] rgb;
            try
            {
                IntPtr ptr = data.Scan0;
                int bytes = Math.Abs(data.Stride) * bitmap.Height;
                rgb = new byte[bytes];
                Marshal.Copy(ptr, rgb, 0, bytes);
            }
            finally
            {
                bitmap.UnlockBits(data);
            }
            return rgb;
        }
    }
}