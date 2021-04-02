using FFMpegCore.Pipes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.FrameSources
{
    public class JpegFrameSource : FrameSource
    {
        public string Path { get; set; }
        public string Name { get; set; }
        public int FrameCount { get; set; } = 300;

        public int _FrameIndex
        {
            get { lock (this._frameIndexLock) { return this._frameIndex; } }
            set { lock (this._frameIndexLock) { this._frameIndex = value > -1 && value < FrameCount ? value : this._frameIndex; } }
        }

        private Task _updateTask;
        private CancellationTokenSource _cancellationTokenSource;

        private int _frameIndex = 0;

        private object _frameIndexLock = new object();

        public JpegFrameSource(string path, string name, int frameCount, int framesPerSecond)
        {
            this.Path = path;
            this.Name = name;
            this.FrameCount = frameCount;
            this.FramesPerSecond = framesPerSecond;

            this.Height = this.Width = 4096;
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
            var fileName = $"{this.Path}{this.Name}{this._FrameIndex.ToString().PadLeft(4, '0')}.jpg";
            return File.ReadAllBytesAsync(fileName);
        }

        public override void Dispose() =>
            _ = this.PauseSource();
    }
}
