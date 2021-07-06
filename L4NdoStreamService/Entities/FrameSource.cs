using System;
using System.Collections.Concurrent;
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
        public string Name { get; set; }

        public int Height { get; set; }
        public int Width { get; set; }

        public int FrameCount { get; set; } = 300;
        public int FramesPerSecond 
        { 
            get => this.UpdatesPerSecond;
            set => this.UpdatesPerSecond = value;
        }

        private int _frameIndex = 0;
        private object _frameIndexLock = new object();

        public FrameSource(string path, string name, int frameCount = 1,
            int framesPerSecond = 30, int width = 4096, int height = 4096)
        {
            this.Path = path;
            this.Name = name;

            this.Height = height;
            this.Width = width;

            this.FrameCount = frameCount;
            this.FramesPerSecond = framesPerSecond;
        }

        protected override Task Update()
        {
            return Task.Run(() =>
            {
                lock (this._frameIndexLock)
                {
                    this._frameIndex = (this._frameIndex + 1) % this.FrameCount;
                }
            });
        }

        public void PlaySource() =>
            this.StartUpdates();

        public void PauseSource() =>
            this.StopUpdates();


        public byte[] GrabFile() =>
            this.GrabFile(this._frameIndex);
        public async Task<byte[]> GrabFileAsync() =>
            await Task.Run(this.GrabFile);

        public Bitmap GrabBitmap() =>
            this.GrabBitmap(this._frameIndex);
        public async Task<Bitmap> GrabBitmapAsync() =>
            await Task.Run(this.GrabBitmap);

        public byte[] GrabRgb() =>
            this.GrabRgb(this._frameIndex);
        public async Task<byte[]> GrabRgbAsync() =>
            await Task.Run(this.GrabRgb);


        private byte[] GrabFile(int frameIndex)
        {
            var fileName = $"{this.Path}{this.Name}{frameIndex.ToString().PadLeft(4, '0')}.jpg";
            byte[] jpeg = File.ReadAllBytes(fileName);
            return jpeg;
        }

        private Bitmap GrabBitmap(int frameIndex)
        {
            byte[] file = this.GrabFile(frameIndex);
            using MemoryStream stream = new MemoryStream(file);
            using Image image = Image.FromStream(stream);
            return new Bitmap(image);
        }

        private byte[] GrabRgb(int frameIndex)
        {
            using Bitmap bitmap = this.GrabBitmap(frameIndex);
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