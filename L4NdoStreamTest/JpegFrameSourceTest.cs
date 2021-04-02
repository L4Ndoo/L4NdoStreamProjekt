using System;
using System.Threading;
using System.Threading.Tasks;
using L4NdoStreamService.FrameSources;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace L4NdoStreamTest
{
    [TestClass]
    public class JpegFrameSourceTest
    {
        [TestMethod]
        public async Task Test30Fps()
        {
            var frameSource = new JpegFrameSource(".\\Videoframes\\", "colorshift_", 300, 30);

            var frame = await frameSource.GrabFrame();
            Assert.IsNotNull(frame, "Frame was null.");

            var lastGrabbed = DateTime.Now;
            var lastIndex = frameSource._FrameIndex;

            Thread.Sleep(1000 / frameSource.FramesPerSecond);
            bool first = true;

            frameSource.FrameReady += frame =>
            {
                if (first)
                {
                    first = false;
                    return;
                }

                var now = DateTime.Now;
                Assert.AreEqual(
                    (int)Math.Round(1000.0 / frameSource.FramesPerSecond),
                    (int)Math.Round((now - lastGrabbed).TotalMilliseconds),
                    "Frame took too long."
                );

                Assert.AreEqual(
                    (lastIndex + 1) % frameSource.FrameCount,
                    frameSource._FrameIndex,
                    1,
                    "Wrong FrameIndex."
                );

                Assert.IsNotNull(frame, "Frame was null.");

                Assert.AreNotEqual(
                    lastIndex,
                    frameSource._FrameIndex,
                    "Frames are the same."
                );

                lastGrabbed = now;
                lastIndex = frameSource._FrameIndex;
            };

            frameSource.PlaySource();
            Thread.Sleep(30000);

            frameSource.Dispose();
        }

        [TestMethod]
        public async Task Test60Fps()
        {
            var frameSource = new JpegFrameSource(".\\Videoframes\\", "colorshift_", 300, 60);

            var frame = await frameSource.GrabFrame();
            Assert.IsNotNull(frame, "Frame was null.");

            var lastGrabbed = DateTime.Now;
            var lastIndex = frameSource._FrameIndex;

            Thread.Sleep(1000 / frameSource.FramesPerSecond);
            bool first = true;

            frameSource.FrameReady += frame =>
            {
                if (first)
                {
                    first = false;
                    return;
                }

                var now = DateTime.Now;
                Assert.AreEqual(
                    (int)Math.Round(1000.0 / frameSource.FramesPerSecond),
                    (int)Math.Round((now - lastGrabbed).TotalMilliseconds),
                    "Frame took too long."
                );

                Assert.AreEqual(
                    (lastIndex + 1) % frameSource.FrameCount,
                    frameSource._FrameIndex,
                    1,
                    "Wrong FrameIndex."
                );

                Assert.IsNotNull(frame, "Frame was null.");

                Assert.AreNotEqual(
                    lastIndex,
                    frameSource._FrameIndex,
                    "Frames are the same."
                );

                lastGrabbed = now;
                lastIndex = frameSource._FrameIndex;
            };

            frameSource.PlaySource();
            Thread.Sleep(30000);

            frameSource.Dispose();
        }
    }
}
