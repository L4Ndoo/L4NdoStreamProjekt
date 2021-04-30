using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

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
        public string _options;

        public FFmpegRenderer(FrameSource frameSource, int frameRate = 5,
            string outputPath = "\"C:\\xampp\\htdocs\\test\\Videoframes\\output\\out.m3u8\"",
            string codecOptions =
            //"-c:v libx264 -preset veryfast -b:v 3000k -maxrate 3000k -bufsize 3000",
            "-c:v libx264 -preset ultrafast -threads 1 -profile:v baseline -level:v 3.1 " +
            "-b:v 3000k -crf 25 -minrate 500K -maxrate 3000k -bufsize 3000k " +
            "-tune zerolatency -async 1 -movflags +faststart " +
            "-vsync:v 2 -bf 1 -keyint_min 120 -g 30 -sc_threshold 0 -b_strategy 0",
            string outputOptions = "-f hls -flags +cgop -hls_time 1 -hls_wrap 10",
            int outputWidth = 200, int outputHeight = 200)
        {
            this._frameSource = frameSource;
            this.FrameRate = frameRate;
            this.OutputPath = outputPath;
            this.OutputOptions = outputOptions;
            this.CodecOptions = codecOptions;
            this.OutputWidth = outputWidth;
            this.OutputHeight = outputHeight;
            this._options = $"-vf \"realtime,scale={this.OutputWidth}x{this.OutputHeight},format=yuv420p\"";
        }

        public void Start()
        {
            if (this._ffmpegProcess != null && !this._ffmpegProcess.HasExited)
                return;

            //this._frameSource.PlaySource();

            ProcessStartInfo ffmpegProcessInfo = new ProcessStartInfo()
            {
                FileName = "Libs\\ffmpeg_gpl.exe",
                Arguments = $"-y -f image2pipe -framerate {FrameRate} -i - " +
                    $"{CodecOptions} {_options} {OutputOptions} {OutputPath}",
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

        protected override Task Update()
        {
            return Task.Run(() =>
            {
                byte[] frame = this._frameSource.GrabJpegFrame();
                this._streamWriter.Write(frame);
            });
        }

        public override void Dispose()
        {
            this.Stop();
            this._frameSource.Dispose();
            base.Dispose();
        }
    }
}
