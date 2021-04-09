using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.FrameSources
{
    public class PixelFrameSource : FrameSource
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public int FrameCount { get; set; } = 300;

        private Dictionary<int, byte[]> _frameCache = new Dictionary<int, byte[]>();

        public int _FrameIndex
        {
            get { lock (this._frameIndexLock) { return this._frameIndex; } }
            set { lock (this._frameIndexLock) { this._frameIndex = value > -1 && value < FrameCount ? value : this._frameIndex; } }
        }

        private Task _updateTask;
        private CancellationTokenSource _cancellationTokenSource;

        private int _frameIndex = 0;

        private object _frameIndexLock = new object();

        public PixelFrameSource(string path, string name, int frameCount, int framesPerSecond)
        {
            this.Path = path;
            this.Name = name;
            this.FrameCount = frameCount;
            this.FramesPerSecond = framesPerSecond;
            this.Height = this.Width = 4096;
            this.InitFrameCache();
        }

        private void InitFrameCache()
        {
            for(int i = 0; i < FrameCount; i++)
            {
                var fileName = $"{this.Path}{this.Name}{i.ToString().PadLeft(4, '0')}.jpg";
                byte[] frame;
                using (Bitmap bitmap = new Bitmap(Image.FromFile(fileName)))
                {
                    var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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
                this._frameCache[this._FrameIndex] = frame;
            }
        }

        public override async Task PlaySource()
        {
            if(_updateTask == null || _updateTask.IsCompleted || _updateTask.IsFaulted)
            {
                await PauseSource();

                _cancellationTokenSource = new CancellationTokenSource();

                _updateTask = Task.Run(async () =>
                {
                    var before = DateTime.Now;
                    var time = DateTime.Now - before;
                    while(!_cancellationTokenSource.IsCancellationRequested)
                    {
                        time = DateTime.Now - before;
                        before = DateTime.Now;

                        this._FrameIndex = ((int)Math.Round(time.TotalMilliseconds / (1000.0 / this.FramesPerSecond)) + this._FrameIndex) % this.FrameCount;

                        this.InvokeFrameReady(await this.GrabFrame());
                        Thread.Sleep(1000 / this.FramesPerSecond);
                    }
                }, this._cancellationTokenSource.Token);
            }
        }

        public override async Task PauseSource()
        {
            this._cancellationTokenSource?.Cancel();

            if (this._updateTask != null)
                await this._updateTask;

            _updateTask?.Dispose();
            _updateTask = null;
        }

        public override Task<byte[]> GrabFrame()
        {
            return Task.Run(() =>
            {
                if (this._frameCache.ContainsKey(this._FrameIndex))
                {
                    return this._frameCache[this._FrameIndex];
                }
                else
                {
                    var fileName = $"{this.Path}{this.Name}{this._FrameIndex.ToString().PadLeft(4, '0')}.jpg";
                    byte[] frame;
                    using (Bitmap bitmap = new Bitmap(Image.FromFile(fileName)))
                    {
                        var data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
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
                    this._frameCache[this._FrameIndex] = frame;
                    return frame;
                }
            });
        }

        public override void Dispose() =>
            _ = this.PauseSource();
    }
}
