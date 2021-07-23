using System;
using System.Drawing.Imaging;
using L4NdoStreamService.Entities.FrameSource;
using Microsoft.MixedReality.WebRTC;

namespace L4NdoStreamService.Entities
{
    public class WebRtcRenderer : IDisposable
    {
        public ExternalVideoTrackSource VideoTrackSource { get; private set; }

        public readonly IFrameSource FrameSource;

        public WebRtcRenderer(IFrameSource frameSource)
        {
            this.FrameSource = frameSource;
            this.VideoTrackSource = ExternalVideoTrackSource.CreateFromArgb32Callback(FrameCallback);
        }

        private void FrameCallback(in FrameRequest request)
        {
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
