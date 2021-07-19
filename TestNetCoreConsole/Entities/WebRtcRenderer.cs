using System;
using Microsoft.MixedReality.WebRTC;

namespace L4NdoStreamService.Entities
{
    public class WebRtcRenderer : IDisposable
    {
        public ExternalVideoTrackSource VideoTrackSource { get; private set; }

        private FrameSource _frameSource;

        public WebRtcRenderer()
        {
            this._frameSource = new FrameSource(".\\Videoframes\\", "colorshift_", 300, 30);
            this._frameSource.PlaySource();

            this.VideoTrackSource = ExternalVideoTrackSource.CreateFromI420ACallback(FrameCallback);
        }

        public void PlaySource() => this._frameSource.PlaySource();
        public void PauseSource() => this._frameSource.PauseSource();

        private unsafe void FrameCallback(in FrameRequest request)
        {
            //using Bitmap bitmap = _frameSource.GrabBitmap();

            //var data = bitmap.LockBits(
            //    new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            //    ImageLockMode.ReadOnly,
            //    PixelFormat.Format32bppArgb);

            //Argb32VideoFrame frame = new Argb32VideoFrame
            //{
            //    data = data.Scan0,
            //    height = (uint)data.Height,
            //    width = (uint)data.Width,
            //    stride = data.Stride
            //};
            //request.CompleteRequest(in frame);

            //bitmap.UnlockBits(data);

            var data = stackalloc byte[32 * 16 + 16 * 8 * 2];
            int k = 0;
            // Y plane (full resolution)
            for (int j = 0; j < 16; ++j)
            {
                for (int i = 0; i < 32; ++i)
                {
                    data[k++] = 0x7F;
                }
            }
            // U plane (halved chroma in both directions)
            for (int j = 0; j < 8; ++j)
            {
                for (int i = 0; i < 16; ++i)
                {
                    data[k++] = 0x30;
                }
            }
            // V plane (halved chroma in both directions)
            for (int j = 0; j < 8; ++j)
            {
                for (int i = 0; i < 16; ++i)
                {
                    data[k++] = 0xB2;
                }
            }
            var dataY = new IntPtr(data);
            var frame = new I420AVideoFrame
            {
                dataY = dataY,
                dataU = dataY + (32 * 16),
                dataV = dataY + (32 * 16) + (16 * 8),
                dataA = IntPtr.Zero,
                strideY = 32,
                strideU = 16,
                strideV = 16,
                strideA = 0,
                width = 32,
                height = 16
            };
            request.CompleteRequest(frame);
        }

        public void Dispose()
        {
            this._frameSource.Dispose();
        }
    }
}
