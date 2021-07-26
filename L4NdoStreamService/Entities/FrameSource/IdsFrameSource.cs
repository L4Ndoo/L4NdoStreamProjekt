using System;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using uEye;
using uEye.Types;

namespace L4NdoStreamService.Entities.FrameSource
{
    public class IdsFrameSource : IFrameSource
    {
        public float Scale
        {
            get => this._scale;
            set
            {
                int nearest = 0;
                float nearestValue = 1;
                for(int i = 2; i < 9; i+=2)
                {
                    float val = Math.Abs((i * value) - 1);
                    if(val < nearestValue)
                    {
                        nearestValue = val;
                        nearest = i;
                    }
                }
                for(int i = 1; i < 17; i+=15)
                {
                    float val = Math.Abs((i * value) - 1);
                    if (val < nearestValue)
                    {
                        nearestValue = val;
                        nearest = i;
                    }
                }

                this._camera.Acquisition.Stop();
                this._camera.Memory.GetList(out int[] idList);
                this._camera.Memory.Free(idList);
                switch (nearest)
                {
                    case 1:
                        this._camera.Size.Subsampling.Set(uEye.Defines.SubsamplingMode.Disable);
                        this._camera.Size.AOI.Set(0, 0, 2448, 2048);
                        break;
                    case 2:
                        this._camera.Size.Subsampling.Set(uEye.Defines.SubsamplingMode.Horizontal2X | uEye.Defines.SubsamplingMode.Vertical2X);
                        this._camera.Size.AOI.Set(0, 0, 1224, 1024);
                        break;
                    case 4:
                        this._camera.Size.Subsampling.Set(uEye.Defines.SubsamplingMode.Horizontal4X | uEye.Defines.SubsamplingMode.Vertical4X);
                        this._camera.Size.AOI.Set(0, 0, 612, 512);
                        break;
                    case 6:
                        this._camera.Size.Subsampling.Set(uEye.Defines.SubsamplingMode.Horizontal6X | uEye.Defines.SubsamplingMode.Vertical6X);
                        this._camera.Size.AOI.Set(0, 0, 408, 340);
                        break;
                    case 8:
                        this._camera.Size.Subsampling.Set(uEye.Defines.SubsamplingMode.Horizontal8X | uEye.Defines.SubsamplingMode.Vertical8X);
                        this._camera.Size.AOI.Set(0, 0, 304, 254);
                        break;
                    case 16:
                        this._camera.Size.Subsampling.Set(uEye.Defines.SubsamplingMode.Horizontal16X | uEye.Defines.SubsamplingMode.Vertical16X);
                        this._camera.Size.AOI.Set(0, 0, 152, 126);
                        break;
                }
                this._camera.Memory.Allocate();
                this._camera.Memory.GetList(out idList);
                this._camera.Memory.Sequence.Add(idList);
                this._camera.Acquisition.Capture();
                this._scale = 1f / nearest;
            }
        }

        private readonly Camera _camera;
        private float _scale;

        public IdsFrameSource()
        {
            uEye.Info.Camera.GetCameraList(out CameraInformation[] camInfos);
           this._camera = new Camera(camInfos.First().CameraID);
            this._camera.Memory.Allocate();
            this._camera.Memory.GetList(out int[] idList);
            this._camera.Memory.Sequence.Add(idList);
            this._camera.Acquisition.Capture();
        }

        public async Task<Bitmap> GrabArgbAsync() =>
            await Task.Run(this.GrabArgb);

        public Bitmap GrabArgb()
        {
            this._camera.Memory.GetLast(out int memoryId);
            this._camera.Memory.CopyToBitmap(memoryId, out Bitmap bitmap);

            if(bitmap == null) { return null; }

            Bitmap converted = new (bitmap.Width, bitmap.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using Graphics gr = Graphics.FromImage(converted);
            gr.DrawImage(bitmap, new Rectangle(0, 0, converted.Width, converted.Height));

            return converted;
        }

        public void Dispose()
        {
            this._camera?.Acquisition.Stop();
            this._camera?.Exit();
        }
    }
}
