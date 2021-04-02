using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.FrameSources
{
    public delegate void FrameReadyEventHandler(byte[] frame);

    public abstract class FrameSource : IDisposable
    {
        public event FrameReadyEventHandler FrameReady;

        public int Height { get; protected set; }
        public int Width { get; protected set; }

        public int FramesPerSecond
        {
            get { lock (this._fpsLock) { return this._framesPerSecond; } }
            set { lock (this._fpsLock) { this._framesPerSecond = value > -1 ? value : this._framesPerSecond; } }
        }
        private int _framesPerSecond = 30;
        private object _fpsLock = new object();

        public abstract Task PlaySource();
        public abstract Task PauseSource();

        public abstract Task<byte[]> GrabFrame();
        public abstract void Dispose();

        protected void InvokeFrameReady(byte[] frame) =>
            this.FrameReady?.Invoke(frame);
    }
}
