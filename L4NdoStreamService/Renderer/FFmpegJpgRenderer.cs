using L4NdoStreamService.FrameSources;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace L4NdoStreamService.Renderer
{
    public class FFmpegJpgRenderer: IDisposable
    {
        public ILogger Logger { get; set; }
        public FrameSource FrameSource { get; private set; }

        private Process _ffmpegProcess = null;
        private BinaryWriter _streamWriter = null;
        private object _frameReadyLock = new object();

        public FFmpegJpgRenderer(FrameSource frameSource)
        {
            this.FrameSource = frameSource;
        }

        public async Task Start()
        {
            if (this._ffmpegProcess != null && !this._ffmpegProcess.HasExited)
                return;

            await this.FrameSource.PlaySource();

            ProcessStartInfo ffmpegProcessInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg.exe",
                Arguments = "-y -re -pix_fmt rgb24 -s 4096x4096 -f rawvideo -i - -c:v libx264 -preset fast -pix_fmt yuv420p -threads 6 -vf scale=800x800 -f hls \"C:\\xampp\\htdocs\\test\\Videoframes\\output\\out.m3u8\"",
                //Arguments = "-y -f rawvideo -re -pix_fmt rgb24 -s 4096x4096 -i - -c:v libx264 -preset fast -pix_fmt yuv420p -s 1280x800 -threads 0 -vf scale=800x800 -f flv out.flv",
                RedirectStandardInput = true
            };

            this.FrameSource.FrameReady += OnFrameReady;
            this._ffmpegProcess = Process.Start(ffmpegProcessInfo);
            this._ffmpegProcess.Start();
            this._streamWriter = new BinaryWriter(this._ffmpegProcess.StandardInput.BaseStream);

            this._ffmpegProcess.ErrorDataReceived += this.OnFFmpegError;
            this._ffmpegProcess.OutputDataReceived += this.OnFFmpegData;
        }

        private void OnFFmpegData(object sender, DataReceivedEventArgs e)
        {
            Logger?.LogWarning("FFmpeg: " + e.Data);
        }

        public async Task Stop()
        {
            if (this._ffmpegProcess == null)
                return;

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

            await this.FrameSource.PauseSource();
            this._ffmpegProcess.ErrorDataReceived -= this.OnFFmpegError;
            this._ffmpegProcess.OutputDataReceived -= this.OnFFmpegData;
            this.FrameSource.FrameReady -= OnFrameReady;
            this._ffmpegProcess.Dispose();
            this._ffmpegProcess = null;
        }

        private void OnFFmpegError(object sender, DataReceivedEventArgs e)
        {
            Logger?.LogError("FFmpeg: " + e.Data);
        }

        private void OnFrameReady(byte[] frame)
        {
            try
            {
                if ((bool)this._ffmpegProcess?.StandardInput.BaseStream.CanWrite)
                {
                    this._streamWriter.Write(frame);
                    this._streamWriter.Flush();
                }
            }
            catch (Exception ex)
            {
                // TODO: Log exception
            }
        }

        public void Dispose()
        {
            this.Stop().Wait();
            this.FrameSource.Dispose();
        }
    }
}
