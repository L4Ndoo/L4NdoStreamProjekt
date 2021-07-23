using System;
using System.Diagnostics;
using System.Drawing.Imaging;
using L4NdoStreamService.Entities.FrameSource;
using Microsoft.MixedReality.WebRTC;

namespace L4NdoStreamService.Entities
{
    public delegate void FrameTimeHandler(TimeSpan time);
    public class WebRtcRenderer : IDisposable
    {
        public ExternalVideoTrackSource VideoTrackSource { get; private set; }

        public readonly IFrameSource FrameSource;
        public event FrameTimeHandler NewFrametime;

        private Stopwatch _timer = null;

        public WebRtcRenderer(IFrameSource frameSource)
        {
            this.FrameSource = frameSource;
            this.VideoTrackSource = ExternalVideoTrackSource.CreateFromArgb32Callback(FrameCallback);
            this.VideoTrackSource.Argb32VideoFrameReady += frame =>
            {
                this._timer?.Stop();
                this.NewFrametime.Invoke(this._timer?.Elapsed ?? TimeSpan.Zero);
                this._timer = null;
            };
        }

        private void FrameCallback(in FrameRequest request)
        {
            if(_timer == null) { this._timer = Stopwatch.StartNew(); }

            BitmapData data = this.FrameSource.GrabArgb();
            if(data != null)
            {
                Argb32VideoFrame frame = new ()
                {
                    data = data.Scan0,
                    height = (uint)data.Height,
                    width = (uint)data.Width,
                    stride = data.Stride
                };
                request.CompleteRequest(frame);
            }
        }

        public void Dispose()
        {
            this.FrameSource.Dispose();
        }
    }
}
