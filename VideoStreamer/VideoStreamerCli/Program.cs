using FFmpeg.AutoGen;
using SkiaSharp;
using VideoStreamer.Cli.Util;
using Size = System.Drawing.Size;

namespace VideoStreamer.Cli;

internal unsafe class Program
{
    private static string _outputFile = "output.mp4";
    private static string _inputDirectory = "frames";
    private static Size _imageSize = new Size(800, 800);

    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            string executable = Environment.GetCommandLineArgs()[0];
            Console.WriteLine($"Usage: {executable} <frames directory> <output file>");
            Console.WriteLine($"Using default frames directory <{_inputDirectory}> and output file <{_outputFile}>");
        }
        else
        {
            _inputDirectory = args[0];
            _outputFile = args[1];
        }

        if (File.Exists(_outputFile)) { File.Delete(_outputFile); }
        using Stream stream = File.Create(_outputFile);

        using VideoStreamEncoder encoder = new VideoStreamEncoder(stream, 10, _imageSize);
        using VideoFrameConverter frameConverter = new VideoFrameConverter(
            new Size(4096, 4096),
            AVPixelFormat.AV_PIX_FMT_RGB0,
            _imageSize,
            AVPixelFormat.AV_PIX_FMT_YUV420P);

        IEnumerable<string> frameFiles = Directory.GetFiles(_inputDirectory);
        foreach (string file in frameFiles)
        {
            using SKBitmap image = SKBitmap.Decode(file);
            if(image == null) { continue; }

            IntPtr pixelsAddr = image.GetPixels();
            byte* ptr = (byte*)pixelsAddr.ToPointer();
            byte_ptrArray8 data = new();
            data[0] = ptr;
            int_array8 linesize = new();
            linesize[0] = image.Width * 4;

            AVFrame rgbFrame = new AVFrame
            {
                data = data,
                format = (int)AVPixelFormat.AV_PIX_FMT_RGB0,
                linesize = linesize,
                width = image.Width,
                height = image.Height
            };

            AVFrame frame = frameConverter.Convert(rgbFrame);
            encoder.Encode(frame);

            Console.WriteLine($"Encoded '{file}'");
        }

        stream.Flush();
        stream.Close();
        return 0;
    }
}