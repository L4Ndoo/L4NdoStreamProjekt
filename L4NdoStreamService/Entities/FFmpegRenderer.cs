using System;
using System.Diagnostics;
using System.IO;

namespace L4NdoStreamService.Entities
{
    public class FFmpegRenderer: BackgroundUpdater
    {
        public string OutputPath { get; set; }

        public string CodecOptions { get; set; }
        public string OutputOptions { get; set; }

        public int OutputWidth { get; set; }
        public int OutputHeight { get; set; }

        public int FrameRate
        {
            get => this.UpdatesPerSecond;
            set => this.UpdatesPerSecond = value;
        }

        private FrameSource _frameSource;
        private Process _ffmpegProcess = null;
        private BinaryWriter _streamWriter = null;
        public string _options = "";

        public FFmpegRenderer(FrameSource frameSource, int frameRate = 10,
            string outputPath = "\"C:\\xampp\\htdocs\\test\\Videoframes\\output\\out.m3u8\"",
            string codecOptions = "-c:v libx264 -crf 30 -maxrate 1M -bufsize 2M -tune fastdecode -movflags +faststart",
            string outputOptions = "-f hls -flags +cgop -g 90 -hls_time 1",
            int outputWidth = 2048, int outputHeight = 2048)
        {
            this._frameSource = frameSource;
            this.FrameRate = frameRate;
            this.OutputPath = outputPath;
            this.OutputOptions = outputOptions;
            this.CodecOptions = codecOptions;
            this.OutputWidth = outputWidth;
            this.OutputHeight = outputHeight;
            this._options = $"-vf format=yuv420p -vf scale={this.OutputWidth}x{this.OutputHeight} -vsync:v 2 -threads 6";
        }

        public void Start()
        {
            if (this._ffmpegProcess != null && !this._ffmpegProcess.HasExited)
                return;

            this._frameSource.PlaySource();

            ProcessStartInfo ffmpegProcessInfo = new ProcessStartInfo()
            {
                FileName = "Libs\\ffmpeg_gpl.exe",
                Arguments = $"-y -f image2pipe -framerate 10 -i - " +
                    $"{CodecOptions} {_options} {OutputOptions} -r {FrameRate} {OutputPath}",
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

        protected override void Update()
        {
            byte[] frame = this._frameSource.GrabJpegFrame();
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
