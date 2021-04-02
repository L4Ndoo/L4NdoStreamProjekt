using L4NdoStreamService.FrameSources;
using L4NdoStreamService.Renderer;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L4NdoStreamService.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        private FrameSource _frameSource;
        public StreamController(FrameSource frameSource) =>
            this._frameSource = frameSource;

        [HttpPost("play")]
        public Task PlaySource() =>
            this._frameSource.PlaySource();

        [HttpPost("pause")]
        public Task PauseSource() =>
            this._frameSource.PauseSource();

        [HttpPost("fps/{fps}")]
        public void SetFps(int fps) =>
            this._frameSource.FramesPerSecond = fps;

        [HttpGet("image")]
        public async Task<IActionResult> GetImage()
        {
            var frame = await this._frameSource.GrabFrame();
            return File(frame, "image/jpeg");
        }

        [HttpGet("video")]
        public IActionResult GetVideo()
        {
            Task<bool> encoderTask;
            var stream = StreamRenderer.EncodeFrameSource(this._frameSource, out encoderTask);
            return this.File(stream, "video/webm");
        }
    }
}
