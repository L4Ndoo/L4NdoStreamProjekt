using System;
using System.Diagnostics;
using System.IO;

namespace L4NdoStreamService.Entities
{
    public enum OutputTypes { mp4, hls, rtmp }
    public class FFmpegRenderer: BackgroundUpdater
    {
        public string OutputPath { get; set; }
        public OutputTypes OutputType { get; set; }

        public string CodecOptions { get; set; }

        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }

        public int FrameRate
        {
            get => this.UpdatesPerSecond;
            protected set => this.UpdatesPerSecond = value;
        }

        private FrameSource _frameSource;
        private Process _ffmpegProcess = null;
        private BinaryWriter _streamWriter = null;

        public FFmpegRenderer(FrameSource frameSource, int frameRate = 15,
            string outputPath = "\"C:\\xampp\\htdocs\\test\\Videoframes\\output\\out.m3u8\"",
            OutputTypes outputType = OutputTypes.hls,
            string codecOptions = "libx264 -preset fast -pix_fmt yuv420p -threads 6",
            int outputWidth = 800, int outputHeight = 800)
        {
            this._frameSource = frameSource;
            this.FrameRate = frameRate;
            this.OutputPath = outputPath;
            this.OutputType = outputType;
            this.CodecOptions = codecOptions;
            this.OutputWidth = outputWidth;
            this.OutputHeight = outputHeight;
        }

        public void Start()
        {
            if (this._ffmpegProcess != null && !this._ffmpegProcess.HasExited)
                return;

            this._frameSource.PlaySource();

            ProcessStartInfo ffmpegProcessInfo = new ProcessStartInfo()
            {
                FileName = "Libs\\ffmpeg_gpl.exe",
                Arguments = $"-y -r {this._frameSource.FramesPerSecond} " +
                    $"-re -pix_fmt rgb24 -s " +
                    $"{this._frameSource.Width}x{this._frameSource.Height} " +
                    $"-f rawvideo -i - -c:v {CodecOptions} -vsync 1 " +
                    $"-vf scale={this.OutputWidth}x{this.OutputHeight} " +
                    $"-r {this.FrameRate} -vsync:v 2 " +
                    (this.OutputType == OutputTypes.hls ?
                        $"-f hls {OutputPath}":
                        $"{OutputPath}"
                    ),
                RedirectStandardInput = true
            };

            this._ffmpegProcess = Process.Start(ffmpegProcessInfo);
            this._streamWriter = new BinaryWriter(this._ffmpegProcess.StandardInput.BaseStream);

            this.StartUpdates();
        }

        public void Stop()
        {
            if (this._ffmpegProcess == null)
                return;

            this.StopUpdates();
            if (!this._ffmpegProcess.HasExited)
            {
                try
                {
                    this._ffmpegProcess.Kill();
                }
                catch (Exception ex)
                {
                    // TODO: Log Exception;
                }
            }

            this._frameSource.PauseSource();
            this._ffmpegProcess.Dispose();
            this._ffmpegProcess = null;
        }

        protected override void Update(TimeSpan timeSinceLastUpdate)
        {
            byte[] frame = this._frameSource.GrabFrame();
            this._streamWriter.Write(frame);
        }

        public override void Dispose()
        {
            this.Stop();
            this._frameSource.Dispose();
            base.Dispose();
        }
    }
}
