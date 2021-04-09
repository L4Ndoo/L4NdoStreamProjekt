using L4NdoStreamService.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace L4NdoStreamService.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StreamController : ControllerBase
    {
        private ILogger<StreamController> _logger;

        private FrameSource _frameSource;
        private FFmpegRenderer _renderer;

        public StreamController(ILogger<StreamController> logger, 
            FrameSource frameSource, FFmpegRenderer renderer)
        {
            this._logger = logger;
            this._frameSource = frameSource;
            this._renderer = renderer;
        }


        [HttpPost("source/play")]
        public void SourcePlay() =>
            this._frameSource.PlaySource();

        [HttpPost("source/pause")]
        public void SourcePause() =>
            this._frameSource.PauseSource();

        [HttpPost("source/{fps}")]
        public void SourceFps(int fps) =>
            this._frameSource.FramesPerSecond = fps;


        [HttpPost("renderer/start")]
        public void RendererStart() =>
            this._renderer.Start();

        [HttpPost("renderer/stop")]
        public void RendererStop() =>
            this._renderer.Stop();


        [HttpGet("source/image")]
        public IActionResult GetImage() =>
         File(this._frameSource.GrabJpegFrame(), "image/jpeg");

        [HttpGet("renderer/video")]
        public IActionResult GetVideo()
        {
            return null;
        }
    }
}
