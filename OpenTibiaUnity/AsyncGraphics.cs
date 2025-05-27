using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using SkiaSharp;

namespace OpenTibiaUnity
{
    public class AsyncGraphics
    {
        SKBitmap _bitmap;
        SKCanvas _canvas;
        List<Task> m_DrawTasks = new List<Task>();

        // mutex
        object m_DrawLock = new object();

        public AsyncGraphics(SKBitmap bitmap)
        {
            _bitmap = bitmap ?? throw new System.ArgumentNullException("Invalid bitmap");
            _canvas = new SKCanvas(_bitmap);
        }

        public void DrawImage(SKBitmap image, int x, int y) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, x, y)));
        public void DrawImage(SKBitmap image, Point point) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, point.X, point.Y)));
        public void DrawImage(SKBitmap image, int x, int y, int w, int h) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, x, y, w, h)));
        public void DrawImage(SKBitmap image, Rectangle rect) => m_DrawTasks.Add(Task.Run(() => InternalDraw(image, rect.X, rect.Y, rect.Width, rect.Height)));

        public void DrawImages(List<SKBitmap> images, List<Point> points)
        {
            if (images.Count != points.Count)
                throw new System.ArgumentException("Invalid draw call: points.Count != image.Count");

            for (int i = 0; i < images.Count; i++)
            {
                var image = images[i];
                var point = points[i];

                DrawImage(image, point);
            }
        }

        void InternalDraw(SKBitmap image, int x, int y)
        {
            lock (m_DrawLock)
            {
                _canvas.DrawBitmap(image, x, y);
                image.Dispose();
            }
        }

        void InternalDraw(SKBitmap image, int x, int y, int w, int h)
        {
            lock (m_DrawLock)
            {
                // _canvas.DrawImage(image, x, y, w, h);
                _canvas.DrawBitmap(image, x, y);

                image.Dispose();
            }
        }

        public async Task DisposeOnDone(IEnumerable<SKBitmap> bitmaps)
        {
            await Task.WhenAll(m_DrawTasks);
            foreach (var bitmap in bitmaps)
            {
                bitmap?.Dispose();
            }
        }

        public async Task SaveAndDispose(string filename)
        {
            await Task.WhenAll(m_DrawTasks);

            using var memStream = new MemoryStream();
            using var skStream = new SKManagedWStream(memStream);
            _bitmap.Encode(skStream, SKEncodedImageFormat.Png, 100);

            _bitmap.Dispose();
            _canvas.Dispose();

            File.WriteAllBytes(filename, memStream.ToArray());
        }
    }
}
