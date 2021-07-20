using System;
using System.Drawing.Imaging;
using Basler.Pylon;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;

namespace L4NdoStreamService.Entities
{
    public class WebRtcRenderer : IDisposable
    {
        public ExternalVideoTrackSource VideoTrackSource { get; private set; }

        private IFrameSource _frameSource;

        public WebRtcRenderer(ILogger<WebRtcRenderer> logger)
        {
            this._frameSource = new ImageFrameSource(".\\Videoframes\\", "colorshift_", 300, 30);
            this.VideoTrackSource = ExternalVideoTrackSource.CreateFromArgb32Callback(FrameCallback);
        }

        private void FrameCallback(in FrameRequest request)
        {
            BitmapData data = this._frameSource.GrabArgb();
            if(data != null)
            {
                Argb32VideoFrame frame = new Argb32VideoFrame
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
            this._frameSource.Dispose();
        }
    }
}
