using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace L4NdoStreamService.Utilities
{
    public static class Extensions
    {
        public static Bitmap Scale(this Image source, float scale, 
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb, 
            InterpolationMode quality = InterpolationMode.Low)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var width = (int)(source.Width * scale);
            var height = (int)(source.Height * scale);
            var bmp = new Bitmap(width, height, pixelFormat);

            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = quality;
                g.DrawImage(source, new Rectangle(0, 0, width, height));
                g.Save();
            }

            return bmp;
        }

        public static async Task<Bitmap> ScaleAsync(this Image source, float scale, 
            PixelFormat pixelFormat = PixelFormat.Format32bppArgb, 
            InterpolationMode quality = InterpolationMode.Low)
        {
            return await Task.Run(() => source.Scale(scale, pixelFormat, quality));
        }
    }
}
