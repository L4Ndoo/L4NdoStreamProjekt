using System;
using System.Drawing;
using System.Drawing.Imaging;
using Microsoft.Extensions.Logging;
using Microsoft.MixedReality.WebRTC;

namespace L4NdoStreamService.Entities
{
    public class WebRtcRenderer : IDisposable
    {
        public ExternalVideoTrackSource VideoTrackSource { get; private set; }

        private FrameSource _frameSource;

        public WebRtcRenderer(ILogger<WebRtcRenderer> logger)
        {
            this._frameSource = new FrameSource(".\\Videoframes\\", "colorshift_", 300, 30);
            this._frameSource.PlaySource();

            this.VideoTrackSource = ExternalVideoTrackSource.CreateFromArgb32Callback(FrameCallback);
        }

        public void PlaySource() => this._frameSource.PlaySource();
        public void PauseSource() => this._frameSource.PauseSource();

        private unsafe void FrameCallback(in FrameRequest request)
        {
            using Bitmap bitmap = this._frameSource.GrabBitmap();

            var data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);

            Argb32VideoFrame frame = new Argb32VideoFrame
            {
                data = data.Scan0,
                height = (uint)data.Height,
                width = (uint)data.Width,
                stride = data.Stride
            };
            request.CompleteRequest(frame);

            bitmap.UnlockBits(data);
        }

        public void Dispose()
        {
            this._frameSource.Dispose();
        }
    }
}
