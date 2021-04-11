using System;
using System.Collections.Concurrent;
using System.Drawing;
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


        private ConcurrentDictionary<int, byte[]> _frameCache = 
            new ConcurrentDictionary<int, byte[]>();

        private ConcurrentDictionary<int, byte[]> _jpegCache =
            new ConcurrentDictionary<int, byte[]>();

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

            var tasks = new Task[FrameCount];
            for (int i = 0; i < FrameCount; i++)
            {
                var index = i;
                tasks[i] = Task.Run(() => this.GrabJpeg(index));
            }
            Task.WaitAll(tasks);
        }

        protected override void Update()
        {
            lock (this._frameIndexLock)
            {
                this._frameIndex = (this._frameIndex + 1) % this.FrameCount;
            }
        }

        public void PlaySource() =>
            this.StartUpdates();

        public void PauseSource() =>
            this.StopUpdates();

        public byte[] GrabFrame() =>
            this.GrabFrame(this._frameIndex);
        
        public async Task<byte[]> GrabFrameAsync() =>
            await Task.Run(this.GrabFrame);

        public byte[] GrabJpegFrame() =>
            this.GrabJpeg(this._frameIndex);

        private byte[] GrabFrame(int frameIndex)
        {
            if (this._frameCache.ContainsKey(frameIndex))
            {
                return this._frameCache[frameIndex];
            }
            else
            {
                var fileName = $"{this.Path}{this.Name}{frameIndex.ToString().PadLeft(4, '0')}.jpg";
                byte[] frame = null;

                using (Image image = Image.FromFile(fileName))
                using (Bitmap bitmap = new Bitmap(image))
                {
                    var data = bitmap.LockBits(
                        new Rectangle(0, 0, bitmap.Width, bitmap.Height), 
                        System.Drawing.Imaging.ImageLockMode.ReadOnly, 
                        System.Drawing.Imaging.PixelFormat.Format24bppRgb
                    );
                    try
                    {
                        IntPtr ptr = data.Scan0;
                        int bytes = Math.Abs(data.Stride) * bitmap.Height;
                        frame = new byte[bytes];
                        Marshal.Copy(ptr, frame, 0, bytes);
                    }
                    finally
                    {
                        bitmap.UnlockBits(data);
                    }
                }

                this._frameCache[frameIndex] = frame;
                return frame;
            }
        }
        private byte[] GrabJpeg(int frameIndex)
        {
            if (this._jpegCache.ContainsKey(frameIndex))
            {
                return this._jpegCache[frameIndex];
            }
            else
            {
                var fileName = $"{this.Path}{this.Name}{frameIndex.ToString().PadLeft(4, '0')}.jpg";
                byte[] jpeg = File.ReadAllBytes(fileName);
                this._jpegCache[frameIndex] = jpeg;
                return jpeg;
            }
        }
    }
}