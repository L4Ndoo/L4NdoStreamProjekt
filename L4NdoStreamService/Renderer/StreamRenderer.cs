using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FFMpegCore;
using FFMpegCore.Pipes;
using L4NdoStreamService.FrameSources;

namespace L4NdoStreamService.Renderer
{
    public static class StreamRenderer
    {
        public static Stream EncodeFrameSource(FrameSource frameSource, out Task<bool> encoderTask)
        {
            var frameStream = new MemoryStream();
            var outputStream = new MemoryStream();

            frameSource.FrameReady += frame => frameStream.Write(frame);
            encoderTask = FFMpegArguments
                .FromPipeInput(new StreamPipeSource(frameStream))
                .OutputToPipe(new StreamPipeSink(outputStream), options => options
                    .WithVideoCodec("vp9")
                    .ForceFormat("webm")
                )
                .ProcessAsynchronously();

            return outputStream;
        }
    }
}
