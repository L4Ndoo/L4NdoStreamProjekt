using System;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace L4NdoStreamService.Entities
{
    interface IFrameSource : IDisposable
    {
        public BitmapData GrabArgb();
        public Task<BitmapData> GrabArgbAsync();
    }
}
