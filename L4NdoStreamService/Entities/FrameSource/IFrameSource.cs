using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities.FrameSource
{
    public interface IFrameSource : IDisposable
    {
        public float Scale { get; set; }
        public BitmapData GrabArgb();
        public Task<BitmapData> GrabArgbAsync();
    }
}
