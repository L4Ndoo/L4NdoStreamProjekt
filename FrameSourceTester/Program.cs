using L4NdoStreamService.FrameSources;
using System;
using System.Threading.Tasks;

namespace FrameSourceTester
{
    class Program
    {
        static async Task Main(string[] args)
        {
            FrameSource frameSource = new JpegFrameSource();
            await frameSource.StartAutoCapture();
            Console.ReadLine();
        }
    }
}
