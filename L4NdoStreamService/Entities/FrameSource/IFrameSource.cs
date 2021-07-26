using System;
using System.Drawing;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities.FrameSource
{
    public interface IFrameSource : IDisposable
    {
        public float Scale { get; set; }
        public Bitmap GrabArgb();
        public Task<Bitmap> GrabArgbAsync();
    }
}
